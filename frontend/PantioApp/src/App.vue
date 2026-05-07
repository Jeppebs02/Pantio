<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { createAuth0Client, type Auth0Client, type User } from '@auth0/auth0-spa-js'

const auth0Domain = import.meta.env['VITE_AUTH0_DOMAIN'] as string | undefined
const auth0ClientId = import.meta.env['VITE_AUTH0_CLIENT_ID'] as string | undefined
const auth0Audience = import.meta.env['VITE_AUTH0_AUDIENCE'] as string | undefined
const apiBaseUrl =
  (import.meta.env['VITE_API_BASE_URL'] as string | undefined) ??
  'https://api.thisisalegitwebsite.qzz.io'
const nettoAuthorizeUrl =
  (import.meta.env['VITE_NETTO_AUTHORIZE_URL'] as string | undefined) ??
  'https://p-idp.dsgapps.dk/apps'
const nettoClientId =
  (import.meta.env['VITE_NETTO_CLIENT_ID'] as string | undefined) ??
  'customer-program'
const nettoRedirectUri =
  (import.meta.env['VITE_NETTO_REDIRECT_URI'] as string | undefined) ??
  'http://localhost'
const nettoTenantId =
  (import.meta.env['VITE_NETTO_TENANT_ID'] as string | undefined) ??
  '4'
const nettoChannel =
  (import.meta.env['VITE_NETTO_CHANNEL'] as string | undefined) ??
  'CustomerProgram'
const nettoClientFlow =
  (import.meta.env['VITE_NETTO_CLIENT_FLOW'] as string | undefined) ??
  'gigya'

type Auth0User = User | undefined
type LocalUser = {
  id: string
  email: string
  onboardingDone: boolean
}

type StoreConnectionDto = {
  id: string
  userId: string
  chain: number | string
  status: number | string
  connectedAt: string
  disconnectedAt: string | null
  lastPolledAt: string | null
  tokenExpiresAt: string | null
}

type StoreConnectionSyncResultDto = {
  connectionId: string
  chain: number | string
  status: number | string
  syncedAt: string
  importedReceiptCount: number
  processedInventoryItemCount: number
}

const auth0Client = ref<Auth0Client | null>(null)
const currentUser = ref<Auth0User>(undefined)
const accessToken = ref<string | null>(null)
const localUser = ref<LocalUser | null>(null)
const storeConnections = ref<StoreConnectionDto[]>([])
const status = ref('Checking Auth0 session...')
const isBusy = ref(true)
const ensureStatus = ref('Local user has not been checked yet.')
const integrationStatus = ref('Netto integration has not been checked yet.')
const isSyncing = ref(false)
const nettoEmail = ref('')

const missingConfig = computed(() => {
  const missing: string[] = []

  if (!auth0Domain) missing.push('VITE_AUTH0_DOMAIN')
  if (!auth0ClientId) missing.push('VITE_AUTH0_CLIENT_ID')
  if (!auth0Audience) missing.push('VITE_AUTH0_AUDIENCE')

  return missing
})

const redirectUri = `${window.location.origin}/`
const nettoConnection = computed(() =>
  storeConnections.value.find((connection) => normalizeChain(connection.chain) === 'Netto') ?? null,
)
const nettoButtonLabel = computed(() => {
  if (!currentUser.value) return 'Login required'
  if (!nettoConnection.value || normalizeStatus(nettoConnection.value.status) === 'Disconnected') {
    return 'Connect Netto and Sync'
  }

  return 'Sync Netto'
})

async function ensureLocalUser() {
  if (!auth0Client.value || !currentUser.value || !accessToken.value) {
    ensureStatus.value = 'Cannot ensure local user without a logged-in Auth0 session.'
    return
  }

  const auth0Sub = currentUser.value.sub
  const email = currentUser.value.email

  if (!auth0Sub || !email) {
    ensureStatus.value = 'Auth0 user is missing email or sub.'
    return
  }

  const response = await fetch(`${apiBaseUrl}/api/users/ensure`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken.value}`,
    },
    body: JSON.stringify({
      email,
      auth0Sub,
    }),
  })

  if (!response.ok) {
    const bodyText = await response.text()
    throw new Error(`Ensure user failed (${response.status}): ${bodyText}`)
  }

  localUser.value = (await response.json()) as LocalUser
  ensureStatus.value = 'Local user exists in this environment.'
  await loadStoreConnections()
}

async function loadStoreConnections() {
  if (!localUser.value || !accessToken.value) {
    storeConnections.value = []
    integrationStatus.value = 'Log in to check Netto integration state.'
    return
  }

  const response = await fetch(`${apiBaseUrl}/api/users/${localUser.value.id}/store-connections`, {
    headers: {
      Authorization: `Bearer ${accessToken.value}`,
    },
  })

  if (!response.ok) {
    const bodyText = await response.text()
    throw new Error(`Loading store connections failed (${response.status}): ${bodyText}`)
  }

  storeConnections.value = (await response.json()) as StoreConnectionDto[]

  if (!nettoConnection.value) {
    integrationStatus.value = 'No Netto credentials stored on the backend.'
    return
  }

  const lastSync = nettoConnection.value.lastPolledAt
    ? new Date(nettoConnection.value.lastPolledAt).toLocaleString()
    : 'never'
  integrationStatus.value = `Netto connection found. Last sync: ${lastSync}.`
}

async function handleNettoAction() {
  if (!currentUser.value || !accessToken.value || !localUser.value) {
    integrationStatus.value = 'Log in with Pantio/Auth0 before using Netto sync.'
    return
  }

  try {
    const connection = nettoConnection.value
    const isDisconnected = connection && normalizeStatus(connection.status) === 'Disconnected'

    if (!connection || isDisconnected) {
      await startNettoLoginFlow()
      return
    }

    await syncNetto(connection.id)
  } catch (error) {
    integrationStatus.value = error instanceof Error ? error.message : 'Netto flow failed.'
  }
}

async function syncNetto(connectionId: string) {
  if (!localUser.value || !accessToken.value) {
    integrationStatus.value = 'Pantio login is required before syncing Netto.'
    return
  }

  isSyncing.value = true
  integrationStatus.value = 'Syncing Netto receipts...'

  try {
    const response = await fetch(
      `${apiBaseUrl}/api/users/${localUser.value.id}/store-connections/${connectionId}/sync`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${accessToken.value}`,
        },
      },
    )

    if (response.status === 404) {
      integrationStatus.value = 'Backend has no active Netto credentials. Opening Netto login...'
      await startNettoLoginFlow()
      return
    }

    if (!response.ok) {
      const bodyText = await response.text()
      throw new Error(`Sync failed (${response.status}): ${bodyText}`)
    }

    const result = (await response.json()) as StoreConnectionSyncResultDto
    integrationStatus.value =
      `Sync completed. Imported ${result.importedReceiptCount} receipts and processed ` +
      `${result.processedInventoryItemCount} inventory items.`
    await loadStoreConnections()
  } finally {
    isSyncing.value = false
  }
}

async function startNettoLoginFlow() {
  if (!currentUser.value) {
    integrationStatus.value = 'Pantio login is required before Netto login.'
    return
  }

  if (!nettoEmail.value) {
    integrationStatus.value = 'Enter the Netto account email before opening Netto login.'
    return
  }

  integrationStatus.value = 'Opening Netto login...'

  const codeVerifier = createRandomString(64)
  const codeChallenge = await createCodeChallenge(codeVerifier)
  const nonce = createRandomString(12)
  const state = createRandomString(24)
  const clientTraceId = createRandomString(24)

  sessionStorage.setItem('pantio.netto.codeVerifier', codeVerifier)
  sessionStorage.setItem('pantio.netto.state', state)
  sessionStorage.setItem('pantio.netto.redirectUri', nettoRedirectUri)

  const url = new URL(nettoAuthorizeUrl)
  url.searchParams.set('clientId', nettoClientId)
  url.searchParams.set('tenantId', nettoTenantId)
  url.searchParams.set('channel', nettoChannel)
  url.searchParams.set('clientFlow', nettoClientFlow)
  url.searchParams.set('code_challenge_method', 'S256')
  url.searchParams.set('code_challenge', codeChallenge)
  url.searchParams.set('nonce', nonce)
  url.searchParams.set('clientTraceId', clientTraceId)
  url.searchParams.set('redirect_uri', nettoRedirectUri)
  url.searchParams.set('emailOrPhone', nettoEmail.value)
  url.searchParams.set('state', state)

  integrationStatus.value = 'Opening Netto login flow...'
  window.location.assign(url.toString())
}

async function handleNettoRedirectIfPresent() {
  const hashParams = new URLSearchParams(window.location.hash.replace(/^#/, ''))
  const queryParams = new URLSearchParams(window.location.search)
  const code = hashParams.get('code') ?? queryParams.get('code')

  if (!code) {
    return
  }

  const storedCodeVerifier = sessionStorage.getItem('pantio.netto.codeVerifier')
  const storedState = sessionStorage.getItem('pantio.netto.state')
  const storedRedirectUri = sessionStorage.getItem('pantio.netto.redirectUri') ?? nettoRedirectUri
  const returnedState = hashParams.get('state') ?? queryParams.get('state')

  if (!storedCodeVerifier) {
    integrationStatus.value = 'Netto redirect returned a code, but no PKCE verifier was stored.'
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }

  if (storedState && returnedState && storedState !== returnedState) {
    integrationStatus.value = 'Netto redirect state did not match the stored state.'
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }

  if (!accessToken.value || !localUser.value) {
    integrationStatus.value = 'Netto redirect completed, but Pantio login is missing.'
    window.history.replaceState({}, document.title, window.location.pathname)
    return
  }

  integrationStatus.value = 'Completing Netto link with backend...'

  try {
    const response = await fetch(`${apiBaseUrl}/api/users/${localUser.value.id}/store-connections/Netto`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken.value}`,
      },
      body: JSON.stringify({
        authorizationCode: code,
        codeVerifier: storedCodeVerifier,
        redirectUri: storedRedirectUri,
      }),
    })

    if (!response.ok) {
      const bodyText = await response.text()
      throw new Error(`Netto link failed (${response.status}): ${bodyText}`)
    }

    integrationStatus.value = 'Netto credentials stored on the backend. Starting sync...'
    sessionStorage.removeItem('pantio.netto.codeVerifier')
    sessionStorage.removeItem('pantio.netto.state')
    sessionStorage.removeItem('pantio.netto.redirectUri')
    window.history.replaceState({}, document.title, window.location.pathname)

    await loadStoreConnections()

    if (nettoConnection.value) {
      await syncNetto(nettoConnection.value.id)
    }
  } catch (error) {
    integrationStatus.value = error instanceof Error ? error.message : 'Netto link failed.'
    throw error
  }
}

function normalizeChain(chain: number | string) {
  if (typeof chain === 'string') return chain
  return ['Netto', 'Fotex', 'Bilka'][chain] ?? String(chain)
}

function normalizeStatus(statusValue: number | string) {
  if (typeof statusValue === 'string') return statusValue
  return ['Active', 'Disconnected', 'PendingSync'][statusValue] ?? String(statusValue)
}

function createRandomString(length: number) {
  const bytes = crypto.getRandomValues(new Uint8Array(length))
  return base64UrlEncode(bytes).slice(0, length)
}

async function createCodeChallenge(codeVerifier: string) {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(codeVerifier))
  return base64UrlEncode(new Uint8Array(digest))
}

function base64UrlEncode(bytes: Uint8Array) {
  let binary = ''
  for (const byte of bytes) {
    binary += String.fromCharCode(byte)
  }

  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')
}

async function syncSessionState() {
  if (!auth0Client.value) {
    return
  }

  const isAuthenticated = await auth0Client.value.isAuthenticated()

  if (!isAuthenticated) {
    currentUser.value = undefined
    accessToken.value = null
    localUser.value = null
    storeConnections.value = []
    status.value = 'Not logged in.'
    ensureStatus.value = 'Local user has not been checked yet.'
    integrationStatus.value = 'Log in with Pantio/Auth0 before using Netto sync.'
    return
  }

  currentUser.value = await auth0Client.value.getUser()
  accessToken.value = await auth0Client.value.getTokenSilently({
    authorizationParams: {
      audience: auth0Audience,
    },
  })
  status.value = `Logged in as ${currentUser.value?.email ?? currentUser.value?.name ?? 'unknown user'}.`
  await ensureLocalUser()
}

async function login() {
  if (!auth0Client.value) {
    return
  }

  await auth0Client.value.loginWithRedirect({
    authorizationParams: {
      audience: auth0Audience,
      redirect_uri: redirectUri,
      screen_hint: 'login',
    },
  })
}

async function signup() {
  if (!auth0Client.value) {
    return
  }

  await auth0Client.value.loginWithRedirect({
    authorizationParams: {
      audience: auth0Audience,
      redirect_uri: redirectUri,
      screen_hint: 'signup',
    },
  })
}

async function logout() {
  if (!auth0Client.value) {
    return
  }

  await auth0Client.value.logout({
    logoutParams: {
      returnTo: redirectUri,
    },
  })
}

onMounted(async () => {
  if (missingConfig.value.length > 0) {
    status.value = `Missing frontend config: ${missingConfig.value.join(', ')}`
    isBusy.value = false
    return
  }

  const client = await createAuth0Client({
    domain: auth0Domain!,
    clientId: auth0ClientId!,
    authorizationParams: {
      audience: auth0Audience,
      redirect_uri: redirectUri,
    },
    cacheLocation: 'localstorage',
  })
  auth0Client.value = client

  try {
    const hasNettoRedirectInProgress =
      !!sessionStorage.getItem('pantio.netto.state') &&
      (window.location.search.includes('code=') || window.location.hash.includes('code='))

    if (!hasNettoRedirectInProgress && window.location.search.includes('code=') && window.location.search.includes('state=')) {
      status.value = 'Completing Auth0 redirect...'
      await client.handleRedirectCallback()
      window.history.replaceState({}, document.title, window.location.pathname)
    }

    await syncSessionState()
    await handleNettoRedirectIfPresent()
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Frontend login flow failed.'
    status.value = message
    if (integrationStatus.value === 'Netto integration has not been checked yet.') {
      integrationStatus.value = message
    }
  } finally {
    isBusy.value = false
  }
})
</script>

<template>
  <main class="shell">
    <section class="panel">
      <p class="eyebrow">Pantio Auth Check</p>
      <h1>Login</h1>
      <p class="lede">
        This page signs into Pantio via Auth0, ensures the local user exists, and gives you a
        minimal Netto test flow that mirrors the proof of concept.
      </p>

      <div class="actions">
        <button class="primary" type="button" :disabled="isBusy || missingConfig.length > 0" @click="login">
          Login
        </button>
        <button class="secondary" type="button" :disabled="isBusy || missingConfig.length > 0" @click="signup">
          Sign Up
        </button>
        <button class="ghost" type="button" :disabled="isBusy || !currentUser" @click="logout">
          Logout
        </button>
      </div>

      <div class="status-grid">
        <article>
          <h2>Session</h2>
          <p>{{ status }}</p>
        </article>
        <article>
          <h2>Provisioning</h2>
          <p>{{ ensureStatus }}</p>
        </article>
        <article class="status-span">
          <h2>Netto Test</h2>
          <p>{{ integrationStatus }}</p>
        </article>
      </div>

      <article v-if="currentUser" class="user-card">
        <h2>Auth0 User</h2>
        <dl>
          <div>
            <dt>Email</dt>
            <dd>{{ currentUser.email ?? 'missing' }}</dd>
          </div>
          <div>
            <dt>Sub</dt>
            <dd>{{ currentUser.sub ?? 'missing' }}</dd>
          </div>
          <div>
            <dt>Backend</dt>
            <dd>{{ apiBaseUrl }}</dd>
          </div>
        </dl>
      </article>

      <article class="integration-card">
        <div class="integration-header">
          <div>
            <h2>Netto Sync</h2>
            <p class="integration-copy">
              Test flow: log into Pantio first, enter the Netto account email, open the DSG login,
              then let the frontend hand the returned code to the backend for storage and sync.
            </p>
          </div>
          <button
            class="primary"
            type="button"
            :disabled="isBusy || isSyncing || !currentUser || missingConfig.length > 0"
            @click="handleNettoAction"
          >
            {{ isSyncing ? 'Working...' : nettoButtonLabel }}
          </button>
        </div>

        <div class="integration-form">
          <label>
            <span>Netto Email</span>
            <input
              v-model="nettoEmail"
              type="email"
              autocomplete="username"
              placeholder="name@example.com"
            />
          </label>
        </div>

        <dl class="integration-meta">
          <div>
            <dt>Connection</dt>
            <dd>{{ nettoConnection ? normalizeStatus(nettoConnection.status) : 'Not linked' }}</dd>
          </div>
          <div>
            <dt>Last Sync</dt>
            <dd>{{ nettoConnection?.lastPolledAt ? new Date(nettoConnection.lastPolledAt).toLocaleString() : 'Never' }}</dd>
          </div>
          <div>
            <dt>Redirect Uri</dt>
            <dd>{{ nettoRedirectUri }}</dd>
          </div>
        </dl>
      </article>

      <article class="setup-card">
        <h2>Frontend Env Vars</h2>
        <pre><code>VITE_AUTH0_DOMAIN={{ auth0Domain ?? 'missing' }}
VITE_AUTH0_CLIENT_ID={{ auth0ClientId ?? 'missing' }}
VITE_AUTH0_AUDIENCE={{ auth0Audience ?? 'missing' }}
VITE_API_BASE_URL={{ apiBaseUrl }}
VITE_NETTO_AUTHORIZE_URL={{ nettoAuthorizeUrl }}
VITE_NETTO_CLIENT_ID={{ nettoClientId }}
VITE_NETTO_REDIRECT_URI={{ nettoRedirectUri }}
VITE_NETTO_TENANT_ID={{ nettoTenantId }}
VITE_NETTO_CHANNEL={{ nettoChannel }}
VITE_NETTO_CLIENT_FLOW={{ nettoClientFlow }}</code></pre>
      </article>
    </section>
  </main>
</template>
