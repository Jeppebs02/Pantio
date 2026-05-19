<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ChevronRight, Receipt } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PBadge from '../../components/ui/PBadge.vue'
import { useStoreConnectionStore } from '../../stores/storeConnection'
import { useAuthStore } from '../../stores/auth'
import { useToast } from '../../composables/useToast'
import { useConfirm } from '../../composables/useConfirm'

const storeConn = useStoreConnectionStore()
const auth = useAuthStore()
const { visible: toastVisible, message: toastMessage, variant: toastVariant, show: showToast } = useToast()
const { ask } = useConfirm()

const nettoEmail = ref('')
const isProcessing = ref(false)

// Netto PKCE env vars
const nettoAuthorizeUrl = (import.meta.env['VITE_NETTO_AUTHORIZE_URL'] as string | undefined) ?? 'https://p-idp.dsgapps.dk/apps'
const nettoClientId = (import.meta.env['VITE_NETTO_CLIENT_ID'] as string | undefined) ?? 'customer-program'
const nettoRedirectUri = (import.meta.env['VITE_NETTO_REDIRECT_URI'] as string | undefined) ?? window.location.origin
const nettoTenantId = (import.meta.env['VITE_NETTO_TENANT_ID'] as string | undefined) ?? '4'
const nettoChannel = (import.meta.env['VITE_NETTO_CHANNEL'] as string | undefined) ?? 'CustomerProgram'
const nettoClientFlow = (import.meta.env['VITE_NETTO_CLIENT_FLOW'] as string | undefined) ?? 'gigya'

const lastSync = computed(() => {
  const conn = storeConn.nettoConnection
  if (!conn?.lastPolledAt) return 'Aldrig synkroniseret'
  return new Date(conn.lastPolledAt).toLocaleString('da-DK', {
    day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
  })
})
const router = useRouter()

onMounted(async () => {
  if (auth.localUser) {
    await storeConn.fetchConnections()
  }
})

async function handleNettoRedirectIfPresent() {
  const hashParams = new URLSearchParams(window.location.hash.replace(/^#/, ''))
  const queryParams = new URLSearchParams(window.location.search)
  const code = hashParams.get('code') ?? queryParams.get('code')
  if (!code) return

  const storedVerifier = sessionStorage.getItem('pantio.netto.codeVerifier')
  const storedState = sessionStorage.getItem('pantio.netto.state')
  const storedRedirectUri = sessionStorage.getItem('pantio.netto.redirectUri') ?? nettoRedirectUri
  const returnedState = hashParams.get('state') ?? queryParams.get('state')

  if (!storedVerifier) {
    showToast('Netto-omdirigering returnerede, men ingen PKCE-verifikator fundet.', 'error')
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }
  if (storedState && returnedState && storedState !== returnedState) {
    showToast('State mismatch — prøv at tilslutte igen.', 'error')
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }

  isProcessing.value = true
  try {
    await storeConn.linkNetto(code, storedVerifier, storedRedirectUri)
    sessionStorage.removeItem('pantio.netto.codeVerifier')
    sessionStorage.removeItem('pantio.netto.state')
    sessionStorage.removeItem('pantio.netto.redirectUri')
    window.history.replaceState({}, document.title, window.location.pathname)
    const result = await storeConn.sync()
    const count = result.processedInventoryItemCount
    showToast(count === 1 ? '1 vare importeret' : `${count} varer importeret`, 'success')
  } catch {
    showToast('Netto-forbindelsen fejlede. Prøv igen.', 'error')
  } finally {
    isProcessing.value = false
  }
}

function createRandomString(len: number) {
  const bytes = crypto.getRandomValues(new Uint8Array(len))
  return btoa(String.fromCharCode(...bytes)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '').slice(0, len)
}

async function createCodeChallenge(verifier: string) {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier))
  return btoa(String.fromCharCode(...new Uint8Array(digest))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')
}

async function connectNetto() {
  if (!nettoEmail.value.trim()) {
    showToast('Indtast din Netto-konto e-mail først.', 'error')
    return
  }
  const verifier = createRandomString(64)
  const challenge = await createCodeChallenge(verifier)
  const state = createRandomString(24)
  const nonce = createRandomString(12)
  const clientTraceId = createRandomString(24)

  sessionStorage.setItem('pantio.netto.codeVerifier', verifier)
  sessionStorage.setItem('pantio.netto.state', state)
  sessionStorage.setItem('pantio.netto.redirectUri', nettoRedirectUri)

  const url = new URL(nettoAuthorizeUrl)
  url.searchParams.set('clientId', nettoClientId)
  url.searchParams.set('tenantId', nettoTenantId)
  url.searchParams.set('channel', nettoChannel)
  url.searchParams.set('clientFlow', nettoClientFlow)
  url.searchParams.set('code_challenge_method', 'S256')
  url.searchParams.set('code_challenge', challenge)
  url.searchParams.set('nonce', nonce)
  url.searchParams.set('clientTraceId', clientTraceId)
  url.searchParams.set('redirect_uri', nettoRedirectUri)
  url.searchParams.set('emailOrPhone', nettoEmail.value.trim())
  url.searchParams.set('state', state)

  window.location.assign(url.toString())
}

async function syncNow() {
  isProcessing.value = true
  try {
    const result = await storeConn.sync()
    const count = result.processedInventoryItemCount
    showToast(count === 1 ? '1 vare importeret' : `${count} varer importeret`, 'success')
  } catch {
    showToast('Synkronisering fejlede. Prøv igen.', 'error')
  } finally {
    isProcessing.value = false
  }
}

async function disconnect() {
  if (!await ask('Dine eksisterende lagervarer beholdes.', { title: 'Afbryd Netto', confirmLabel: 'Afbryd', danger: true })) return
  await storeConn.disconnect()
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Butiksintegrationer" />
    </template>

    <div class="page">
      <button class="store-card" @click="router.push('/store/netto')">
        <div class="store-card-header">
          <div class="store-logo">
            <Receipt :size="28" />
          </div>
          <div class="store-info">
            <h3>Netto</h3>
            <PBadge
              :tone="storeConn.nettoStatus === 'active' ? 'fresh' : storeConn.nettoStatus === 'pending' ? 'soon' : 'neutral'"
              :dot="true"
            >
              {{ storeConn.nettoStatus === 'active' ? 'Tilsluttet' : storeConn.nettoStatus === 'pending' ? 'Afventer' : 'Ikke tilsluttet' }}
            </PBadge>
          </div>
        </div>
        <ChevronRight :size="20" class="chevron" />
      </button>

      <div class="store-card store-card--future">
        <div class="store-card-header">
          <div class="store-logo store-logo--muted">
            <Receipt :size="28" />
          </div>
          <div class="store-info">
            <h3 class="future-name">Føtex</h3>
            <PBadge tone="neutral">Kommer snart</PBadge>
          </div>
        </div>
      </div>
    </div>
  </AppShell>
</template>

<style scoped>
.page {
  padding: var(--space-4);
  max-width: var(--max-width);
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.store-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  text-align: left;
  cursor: pointer;
  transition: background var(--motion-default);
}

.store-card:not(.store-card--future):hover {
  background: var(--surface-raised);
}

.store-card--future {
  opacity: 0.5;
  cursor: default;
}

.store-card-header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  flex: 1;
}

.store-logo {
  width: 52px;
  height: 52px;
  border-radius: var(--radius-md);
  background: var(--clay-100);
  color: var(--clay-600);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.store-logo--muted {
  background: var(--surface-raised);
  color: var(--fg-faint);
}

.store-info {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.store-info h3 {
  font-size: 16px;
  font-weight: 600;
  margin: 0;
}

.future-name {
  color: var(--fg-muted);
}

.chevron {
  color: var(--fg-faint);
  flex-shrink: 0;
}
</style>
