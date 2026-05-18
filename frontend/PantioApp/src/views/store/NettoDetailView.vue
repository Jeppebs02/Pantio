<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { RefreshCw, Unlink, CheckCircle, XCircle } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PAlert from '../../components/ui/PAlert.vue'
import PBadge from '../../components/ui/PBadge.vue'
import PInput from '../../components/ui/PInput.vue'
import { useStoreConnectionStore } from '../../stores/storeConnection'
import { useAuthStore } from '../../stores/auth'
import { ApiError } from '../../services/api'

const storeConn = useStoreConnectionStore()
const auth = useAuthStore()
const router = useRouter()

const nettoEmail = ref('')
const statusMsg = ref('')
const isProcessing = ref(false)
const selectedIds = ref<Set<string>>(new Set())

// Netto PKCE env vars
const nettoAuthorizeUrl = (import.meta.env['VITE_NETTO_AUTHORIZE_URL'] as string | undefined) ?? 'https://p-idp.dsgapps.dk/apps'
const nettoClientId = (import.meta.env['VITE_NETTO_CLIENT_ID'] as string | undefined) ?? 'customer-program'
const nettoRedirectUri = (import.meta.env['VITE_NETTO_REDIRECT_URI'] as string | undefined) ?? `${window.location.origin}/store/netto`
const nettoTenantId = (import.meta.env['VITE_NETTO_TENANT_ID'] as string | undefined) ?? '4'
const nettoChannel = (import.meta.env['VITE_NETTO_CHANNEL'] as string | undefined) ?? 'CustomerProgram'
const nettoClientFlow = (import.meta.env['VITE_NETTO_CLIENT_FLOW'] as string | undefined) ?? 'gigya'

const allSelected = computed(() =>
  storeConn.pendingReceipts.length > 0 &&
  storeConn.pendingReceipts.every(r => selectedIds.value.has(r.dsgReceiptId))
)

const lastSync = computed(() => {
  const conn = storeConn.nettoConnection
  if (!conn?.lastPolledAt) return 'Aldrig synkroniseret'
  return new Date(conn.lastPolledAt).toLocaleString('da-DK', {
    day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
  })
})

onMounted(async () => {
  if (!auth.localUser) return
  await storeConn.fetchConnections()
  await handleNettoRedirectIfPresent()
  if (storeConn.nettoStatus === 'pending') {
    await storeConn.fetchPendingReceipts()
  }
  if (storeConn.nettoStatus === 'active') {
    await storeConn.fetchSyncHistory()
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
    statusMsg.value = 'Tilsluttet! Henter dine kvitteringer...'
    await storeConn.fetchPendingReceipts()
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

function toggleSelectAll() {
  if (allSelected.value) {
    selectedIds.value = new Set()
  } else {
    selectedIds.value = new Set(storeConn.pendingReceipts.map(r => r.dsgReceiptId))
  }
}

function toggleReceipt(id: string) {
  if (selectedIds.value.has(id)) {
    selectedIds.value.delete(id)
  } else {
    selectedIds.value.add(id)
  }
  // trigger reactivity on the Set
  selectedIds.value = new Set(selectedIds.value)
}

function computeHorizonDate(): string | null {
  if (selectedIds.value.size === 0) return null
  const selected = storeConn.pendingReceipts.filter(r => selectedIds.value.has(r.dsgReceiptId))
  if (selected.length === 0) return null
  const oldest = selected.reduce((a, b) => (a.createdAt < b.createdAt ? a : b))
  return oldest.createdAt
}

function extractApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError) {
    try {
      const parsed = JSON.parse(err.message) as { message?: string }
      if (parsed.message) return parsed.message
    } catch {
      // raw text fallback
      if (err.message) return err.message
    }
  }
  return fallback
}

async function confirmImport() {
  isProcessing.value = true
  statusMsg.value = 'Importerer valgte kvitteringer...'
  try {
    const result = await storeConn.importSelected([...selectedIds.value], computeHorizonDate())
    statusMsg.value = `Importeret ${result.importedReceiptCount} kvitteringer, ${result.processedInventoryItemCount} varer.`
    await storeConn.fetchSyncHistory()
    selectedIds.value = new Set()
  } catch (err) {
    statusMsg.value = extractApiErrorMessage(err, 'Import fejlede. Prøv igen.')
  } finally {
    isProcessing.value = false
  }
}

async function startFresh() {
  isProcessing.value = true
  statusMsg.value = 'Starter forfra...'
  try {
    await storeConn.importSelected([], null)
    statusMsg.value = ''
    await storeConn.fetchSyncHistory()
  } catch (err) {
    statusMsg.value = extractApiErrorMessage(err, 'Noget gik galt. Prøv igen.')
  } finally {
    isProcessing.value = false
  }
}

async function syncNow() {
  isProcessing.value = true
  statusMsg.value = 'Synkroniserer kvitteringer...'
  try {
    const result = await storeConn.sync()
    statusMsg.value = `Synkronisering færdig. ${result.importedReceiptCount} kvitteringer, ${result.processedInventoryItemCount} varer.`
    await storeConn.fetchSyncHistory()
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
  router.push('/store')
}

function formatSyncDate(isoString: string) {
  return new Date(isoString).toLocaleString('da-DK', {
    day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
  })
}

function formatReceiptDate(isoString: string) {
  return new Date(isoString).toLocaleDateString('da-DK', {
    day: 'numeric', month: 'short', year: 'numeric',
  })
}

function formatDkk(amount: number) {
  return amount.toLocaleString('da-DK', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' kr'
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Netto" back-route="/store" />
    </template>

    <div class="page">
      <PAlert v-if="statusMsg" :variant="statusMsg.includes('fejl') || statusMsg.includes('mismatch') ? 'error' : 'info'">
        {{ statusMsg }}
      </PAlert>

      <!-- ── DISCONNECTED ── -->
      <template v-if="storeConn.nettoStatus === 'disconnected'">
        <div class="section-card">
          <p class="desc">Tilslut din Netto-konto for at importere kvitteringer og holde styr på dine varer.</p>
          <form class="connect-form" @submit.prevent="connectNetto">
            <PInput
              v-model="nettoEmail"
              label="Netto-konto e-mail"
              type="email"
              placeholder="navn@eksempel.dk"
              autocomplete="username"
            />
            <PButton type="submit" full-width :disabled="isProcessing || !nettoEmail.trim()">
              Tilslut Netto
            </PButton>
          </form>
        </div>
      </template>

      <!-- ── PENDING: receipt picker ── -->
      <template v-else-if="storeConn.nettoStatus === 'pending'">
        <div class="section-card">
          <div class="picker-header">
            <h2 class="section-title">Vælg kvitteringer at importere</h2>
            <p class="desc">Kun markerede kvitteringer importeres. Ældre kvitteringer du springer over, hentes aldrig igen.</p>
          </div>

          <div v-if="storeConn.isLoadingPending" class="skeleton-list">
            <div v-for="i in 5" :key="i" class="skeleton-row" />
          </div>

          <template v-else-if="storeConn.pendingReceipts.length > 0">
            <label class="select-all-row">
              <input type="checkbox" :checked="allSelected" @change="toggleSelectAll" />
              <span class="select-all-label">Vælg alle ({{ storeConn.pendingReceipts.length }})</span>
            </label>

            <div class="receipt-list">
              <label
                v-for="receipt in storeConn.pendingReceipts"
                :key="receipt.dsgReceiptId"
                class="receipt-row"
              >
                <input
                  type="checkbox"
                  :checked="selectedIds.has(receipt.dsgReceiptId)"
                  @change="toggleReceipt(receipt.dsgReceiptId)"
                />
                <div class="receipt-info">
                  <span class="receipt-store">{{ receipt.storeName ?? 'Netto' }}</span>
                  <span class="receipt-date">{{ formatReceiptDate(receipt.createdAt) }}</span>
                </div>
                <span class="receipt-total">{{ formatDkk(receipt.salesTotalDkk) }}</span>
              </label>
            </div>

            <div class="picker-actions">
              <PButton
                :disabled="isProcessing || selectedIds.size === 0"
                @click="confirmImport"
              >
                Importer valgte ({{ selectedIds.size }})
              </PButton>
              <PButton variant="ghost" :disabled="isProcessing" @click="startFresh">
                Start forfra
              </PButton>
            </div>
          </template>

          <p v-else class="desc">Ingen kvitteringer fundet på din Netto-konto.</p>
        </div>
      </template>

      <!-- ── ACTIVE ── -->
      <template v-else>
        <div class="section-card">
          <div class="status-row">
            <div class="status-info">
              <PBadge tone="fresh" :dot="true">Tilsluttet</PBadge>
              <span class="last-sync-label">Seneste synk. {{ lastSync }}</span>
            </div>
            <div class="action-row">
              <PButton variant="secondary" size="sm" :disabled="isProcessing || storeConn.isSyncing" @click="syncNow">
                <RefreshCw :size="15" />
                {{ storeConn.isSyncing ? 'Synkroniserer...' : 'Synkroniser nu' }}
              </PButton>
              <PButton variant="ghost" size="sm" @click="disconnect">
                <Unlink :size="15" />
                Afbryd
              </PButton>
            </div>
          </div>
        </div>

        <div class="section-card">
          <h2 class="section-title">Importhistorik</h2>

          <div v-if="storeConn.syncHistory.length === 0" class="empty-history">
            Ingen synkroniseringer endnu.
          </div>

          <div v-else class="history-list">
            <div
              v-for="log in storeConn.syncHistory"
              :key="log.id"
              class="history-row"
            >
              <component
                :is="log.status === 'Success' ? CheckCircle : XCircle"
                :size="18"
                :class="log.status === 'Success' ? 'icon-success' : 'icon-error'"
              />
              <div class="history-info">
                <span class="history-date">{{ formatSyncDate(log.syncedAt) }}</span>
                <span class="history-counts">
                  {{ log.importedReceiptCount }} kvitteringer · {{ log.processedInventoryCount }} varer
                </span>
              </div>
              <PBadge :tone="log.status === 'Success' ? 'fresh' : 'past'">
                {{ log.status === 'Success' ? 'OK' : 'Fejl' }}
              </PBadge>
            </div>
          </div>
        </div>
      </template>
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

.section-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.section-title {
  font-size: 15px;
  font-weight: 600;
  margin: 0;
}

.desc {
  font-size: 14px;
  color: var(--fg-muted);
  margin: 0;
}

.connect-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

/* ── Receipt picker ── */
.picker-header {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.select-all-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--border);
}

.select-all-label {
  color: var(--fg);
}

.receipt-list {
  display: flex;
  flex-direction: column;
  gap: 0;
  max-height: min(55vh, 380px);
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
}

.receipt-row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--border);
  cursor: pointer;
}

.receipt-row:last-child {
  border-bottom: none;
}

.receipt-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.receipt-store {
  font-size: 14px;
  font-weight: 500;
}

.receipt-date {
  font-size: 12px;
  color: var(--fg-muted);
}

.receipt-total {
  font-size: 14px;
  font-weight: 500;
  color: var(--fg);
  white-space: nowrap;
}

.picker-actions {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding-top: var(--space-2);
}

/* ── Skeleton ── */
.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.skeleton-row {
  height: 48px;
  border-radius: var(--radius-md);
  background: var(--surface-raised);
  animation: pulse 1.5s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

/* ── Active state ── */
.status-row {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.status-info {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.last-sync-label {
  font-size: 13px;
  color: var(--fg-muted);
}

.action-row {
  display: flex;
  gap: var(--space-2);
}

/* ── History ── */
.empty-history {
  font-size: 14px;
  color: var(--fg-muted);
  text-align: center;
  padding: var(--space-4) 0;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.history-row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--border);
}

.history-row:last-child {
  border-bottom: none;
}

.history-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.history-date {
  font-size: 14px;
  font-weight: 500;
}

.history-counts {
  font-size: 12px;
  color: var(--fg-muted);
}

.icon-success {
  color: var(--fresh);
  flex-shrink: 0;
}

.icon-error {
  color: var(--past);
  flex-shrink: 0;
}
</style>
