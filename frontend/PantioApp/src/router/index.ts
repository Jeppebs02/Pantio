import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/auth/LoginView.vue'),
    },
    {
      path: '/onboarding',
      name: 'onboarding',
      component: () => import('../views/auth/OnboardingView.vue'),
    },
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/inventory',
      name: 'inventory-list',
      component: () => import('../views/inventory/InventoryListView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/inventory/:id',
      name: 'inventory',
      component: () => import('../views/inventory/InventoryView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/inventory/:id/item/:itemId',
      name: 'item-detail',
      component: () => import('../views/inventory/ItemDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/inventory/:id/add',
      name: 'manual-entry',
      component: () => import('../views/inventory/ManualEntryView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/recipes',
      name: 'recipes',
      component: () => import('../views/recipes/RecipeMainView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/recipes/:id',
      name: 'recipe-detail',
      component: () => import('../views/recipes/RecipeDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/shopping',
      name: 'shopping',
      component: () => import('../views/shopping/ShoppingListView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/shopping/:id',
      name: 'shopping-detail',
      component: () => import('../views/shopping/ShoppingDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/shopping/:id/add',
      name: 'shopping-add-item',
      component: () => import('../views/shopping/ShoppingAddItemView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/store',
      name: 'store',
      component: () => import('../views/store/ConnectStoreView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/search',
      name: 'search',
      component: () => import('../views/search/SearchView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('../views/settings/SettingsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/settings/privacy',
      name: 'privacy',
      component: () => import('../views/settings/PrivacyView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // DSG ignores redirect_uri and always redirects to the origin root.
  // Forward any Netto callback (identified by the PKCE state in sessionStorage)
  // to /store so ConnectStoreView can handle it, preserving the hash fragment.
  const hasNettoCallback =
    !!sessionStorage.getItem('pantio.netto.state') &&
    (window.location.hash.includes('code=') || window.location.search.includes('code='))

  if (hasNettoCallback && to.path !== '/store') {
    return { path: '/store', hash: window.location.hash }
  }

  // Always wait for auth to initialise so guards have accurate state
  if (auth.isLoading) {
    await new Promise<void>((resolve) => {
      const unwatch = setInterval(() => {
        if (!auth.isLoading) {
          clearInterval(unwatch)
          resolve()
        }
      }, 50)
    })
  }

  // Authenticated users skip onboarding and login — go straight to the app
  if (auth.isAuthenticated && (to.name === 'onboarding' || to.name === 'login')) {
    return { name: 'home' }
  }

  // Unauthenticated users can only access onboarding and login
  if (!auth.isAuthenticated && to.meta.requiresAuth) {
    return { name: 'onboarding' }
  }

  return true
})

export default router
