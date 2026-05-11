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
      meta: { requiresAuth: true },
    },
    {
      path: '/',
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
      component: () => import('../views/recipes/RecipeSuggestionsView.vue'),
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
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

router.beforeEach(async (to) => {
  if (!to.meta.requiresAuth) return true

  const auth = useAuthStore()

  // Wait for auth to finish initializing before guarding
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

  if (!auth.isAuthenticated) {
    return { name: 'login' }
  }

  return true
})

export default router
