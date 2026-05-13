import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { createAuth0Client, type Auth0Client, type User as Auth0User } from '@auth0/auth0-spa-js'
import { registerTokenProvider } from '../services/api'
import { ensureUser } from '../services/users'
import type { LocalUser } from '../services/types'

export const useAuthStore = defineStore('auth', () => {
  const auth0Domain = import.meta.env['VITE_AUTH0_DOMAIN'] as string
  const auth0ClientId = import.meta.env['VITE_AUTH0_CLIENT_ID'] as string
  const auth0Audience = import.meta.env['VITE_AUTH0_AUDIENCE'] as string

  const client = ref<Auth0Client | null>(null)
  const auth0User = ref<Auth0User | undefined>(undefined)
  const accessToken = ref<string | null>(null)
  const localUser = ref<LocalUser | null>(null)
  const isLoading = ref(true)

  const isAuthenticated = computed(() => !!auth0User.value && !!accessToken.value)
  const redirectUri = `${window.location.origin}/`

  registerTokenProvider(() => accessToken.value)

  async function initialize() {
    if (client.value) return
    if (!auth0Domain || !auth0ClientId) {
      isLoading.value = false
      return
    }

    const auth0 = await createAuth0Client({
      domain: auth0Domain,
      clientId: auth0ClientId,
      authorizationParams: { audience: auth0Audience, redirect_uri: redirectUri },
      cacheLocation: 'localstorage',
    })
    client.value = auth0

    try {
      const hasNettoRedirect =
        !!sessionStorage.getItem('pantio.netto.state') &&
        (window.location.search.includes('code=') || window.location.hash.includes('code='))

      if (
        !hasNettoRedirect &&
        window.location.search.includes('code=') &&
        window.location.search.includes('state=')
      ) {
        try {
          await auth0.handleRedirectCallback()
        } catch {
          // code already consumed or state mismatch — fall through to session sync
        }
        window.history.replaceState({}, document.title, window.location.pathname)
      }

      await _syncSession()
    } catch {
      // session sync failed; stay logged out
    } finally {
      isLoading.value = false
    }
  }

  async function _syncSession() {
    if (!client.value) return
    const authed = await client.value.isAuthenticated()
    if (!authed) {
      auth0User.value = undefined
      accessToken.value = null
      localUser.value = null
      return
    }

    auth0User.value = await client.value.getUser()
    try {
      accessToken.value = await client.value.getTokenSilently({
        authorizationParams: { audience: auth0Audience },
      })
    } catch {
      // Token fetch failed — reset to prevent partial isAuthenticated state
      auth0User.value = undefined
      accessToken.value = null
      localUser.value = null
      return
    }

    if (auth0User.value?.sub && auth0User.value?.email) {
      try {
        localUser.value = await ensureUser(auth0User.value.email, auth0User.value.sub)
      } catch {
        // Backend error — user stays authenticated, localUser stays null
      }
    }
  }

  async function login() {
    await client.value?.loginWithRedirect({
      authorizationParams: {
        audience: auth0Audience,
        redirect_uri: redirectUri,
        screen_hint: 'login',
      },
    })
  }

  async function signup() {
    await client.value?.loginWithRedirect({
      authorizationParams: {
        audience: auth0Audience,
        redirect_uri: redirectUri,
        screen_hint: 'signup',
      },
    })
  }

  async function logout() {
    await client.value?.logout({ logoutParams: { returnTo: redirectUri } })
    auth0User.value = undefined
    accessToken.value = null
    localUser.value = null
  }

  return {
    auth0User,
    accessToken,
    localUser,
    isLoading,
    isAuthenticated,
    initialize,
    login,
    signup,
    logout,
  }
})
