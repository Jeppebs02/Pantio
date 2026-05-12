<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { RefreshCw, Unlink, Receipt } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PAlert from '../../components/ui/PAlert.vue'
import PBadge from '../../components/ui/PBadge.vue'
import PInput from '../../components/ui/PInput.vue'
import { useStoreConnectionStore } from '../../stores/storeConnection'
import { useAuthStore } from '../../stores/auth'

const storeConn = useStoreConnectionStore()
const auth = useAuthStore()

const nettoEmail = ref('')
const statusMsg = ref('')
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

onMounted(async () => {
  if (auth.localUser) {
    await storeConn.fetchConnections()
    await handleNettoRedirectIfPresent()
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
    statusMsg.value = 'Netto-omdirigering returnerede, men ingen PKCE-verifikator fundet.'
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }
  if (storedState && returnedState && storedState !== returnedState) {
    statusMsg.value = 'State mismatch — prøv at tilslutte igen.'
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }

  isProcessing.value = true
  statusMsg.value = 'Fuldfører Netto-forbindelsen...'
  try {
    await storeConn.linkNetto(code, storedVerifier, storedRedirectUri)
    sessionStorage.removeItem('pantio.netto.codeVerifier')
    sessionStorage.removeItem('pantio.netto.state')
    sessionStorage.removeItem('pantio.netto.redirectUri')
    window.history.replaceState({}, document.title, window.location.pathname)
    statusMsg.value = 'Tilsluttet. Starter synkronisering...'
    await storeConn.sync()
    statusMsg.value = ''
  } catch {
    statusMsg.value = 'Netto-forbindelsen fejlede. Prøv igen.'
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
    statusMsg.value = 'Indtast din Netto-konto e-mail først.'
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
  statusMsg.value = 'Synkroniserer kvitteringer...'
  try {
    const result = await storeConn.sync()
    statusMsg.value = `Synkronisering færdig. Importerede ${result.importedReceiptCount} kvitteringer, ${result.processedInventoryItemCount} varer.`
  } catch {
    statusMsg.value = 'Synkronisering fejlede. Prøv igen.'
  } finally {
    isProcessing.value = false
  }
}

async function disconnect() {
  if (!confirm('Afbryd Netto? Dine eksisterende lagervarer beholdes.')) return
  await storeConn.disconnect()
  statusMsg.value = ''
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Butiksintegrationer" />
    </template>

    <div class="page">
      <PAlert v-if="statusMsg" :variant="statusMsg.includes('failed') || statusMsg.includes('mismatch') ? 'error' : 'info'">
        {{ statusMsg }}
      </PAlert>

      <!-- Netto card -->
      <div class="store-card">
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
              {{ storeConn.nettoStatus === 'active' ? 'Tilsluttet' : storeConn.nettoStatus === 'pending' ? 'Afventer synkronisering' : 'Ikke tilsluttet' }}
            </PBadge>
          </div>
        </div>

        <template v-if="storeConn.nettoStatus !== 'disconnected'">
          <dl class="store-meta">
            <div>
              <dt>Seneste synk.</dt>
              <dd>{{ lastSync }}</dd>
            </div>
          </dl>
          <div class="store-actions">
            <PButton variant="secondary" :disabled="isProcessing || storeConn.isSyncing" @click="syncNow">
              <RefreshCw :size="16" />
              {{ storeConn.isSyncing ? 'Synkroniserer...' : 'Synkroniser nu' }}
            </PButton>
            <PButton variant="ghost" @click="disconnect">
              <Unlink :size="16" />
              Afbryd
            </PButton>
          </div>
        </template>

        <template v-else>
          <p class="store-desc">Tilslut din Netto-konto for at importere kvitteringer automatisk.</p>
          <form class="connect-form" @submit.prevent="connectNetto">
            <PInput
              v-model="nettoEmail"
              label="Netto-konto e-mail"
              type="email"
              placeholder="name@example.com"
              autocomplete="username"
            />
            <PButton type="submit" full-width :disabled="isProcessing || !nettoEmail.trim()">
              Tilslut Netto
            </PButton>
          </form>
        </template>
      </div>

      <!-- Future stores placeholder -->
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
  flex-direction: column;
  gap: var(--space-4);
}

.store-card--future {
  opacity: 0.5;
}

.store-card-header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
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

.store-meta {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.store-meta > div {
  display: flex;
  justify-content: space-between;
  font-size: 14px;
}

.store-meta dt {
  color: var(--fg-muted);
}

.store-actions {
  display: flex;
  gap: var(--space-2);
}

.store-desc {
  font-size: 14px;
  color: var(--fg-muted);
}

.connect-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.future-name {
  color: var(--fg-muted);
}
</style>
