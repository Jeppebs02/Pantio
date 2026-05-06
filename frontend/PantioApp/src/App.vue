<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { createAuth0Client, type Auth0Client, type User } from '@auth0/auth0-spa-js'

const auth0Domain = import.meta.env['VITE_AUTH0_DOMAIN'] as string | undefined
const auth0ClientId = import.meta.env['VITE_AUTH0_CLIENT_ID'] as string | undefined
const auth0Audience = import.meta.env['VITE_AUTH0_AUDIENCE'] as string | undefined
const apiBaseUrl =
  (import.meta.env['VITE_API_BASE_URL'] as string | undefined) ??
  'https://api.thisisalegitwebsite.qzz.io'

type Auth0User = User | undefined

const auth0Client = ref<Auth0Client | null>(null)
const currentUser = ref<Auth0User>(undefined)
const accessToken = ref<string | null>(null)
const status = ref('Checking Auth0 session...')
const isBusy = ref(true)
const ensureStatus = ref('Local user has not been checked yet.')

const missingConfig = computed(() => {
  const missing: string[] = []

  if (!auth0Domain) missing.push('VITE_AUTH0_DOMAIN')
  if (!auth0ClientId) missing.push('VITE_AUTH0_CLIENT_ID')
  if (!auth0Audience) missing.push('VITE_AUTH0_AUDIENCE')

  return missing
})

const redirectUri = `${window.location.origin}/`

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

  ensureStatus.value = 'Local user exists in this environment.'
}

async function syncSessionState() {
  if (!auth0Client.value) {
    return
  }

  const isAuthenticated = await auth0Client.value.isAuthenticated()

  if (!isAuthenticated) {
    currentUser.value = undefined
    accessToken.value = null
    status.value = 'Not logged in.'
    ensureStatus.value = 'Local user has not been checked yet.'
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
    status.value = `Missing frontend Auth0 config: ${missingConfig.value.join(', ')}`
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
    if (window.location.search.includes('code=') && window.location.search.includes('state=')) {
      status.value = 'Completing Auth0 redirect...'
      await client.handleRedirectCallback()
      window.history.replaceState({}, document.title, window.location.pathname)
    }

    await syncSessionState()
  } catch (error) {
    status.value = error instanceof Error ? error.message : 'Auth0 login failed.'
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
        This page does one thing: send you through Auth0, fetch an API token, and ensure your
        local Pantio user exists in the current environment.
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

      <article class="setup-card">
        <h2>Frontend Env Vars</h2>
        <pre><code>VITE_AUTH0_DOMAIN={{ auth0Domain ?? 'missing' }}
VITE_AUTH0_CLIENT_ID={{ auth0ClientId ?? 'missing' }}
VITE_AUTH0_AUDIENCE={{ auth0Audience ?? 'missing' }}
VITE_API_BASE_URL={{ apiBaseUrl }}</code></pre>
      </article>
    </section>
  </main>
</template>
