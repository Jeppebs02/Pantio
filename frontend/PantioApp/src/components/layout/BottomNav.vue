<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { House, Archive, ShoppingCart, ChefHat, User } from 'lucide-vue-next'
import { useInventoryStore } from '../../stores/inventory'

const route = useRoute()
const router = useRouter()
const inventoryStore = useInventoryStore()

const tabs = [
  { label: 'Hjem', icon: House, to: '/' },
  { label: 'Indkøbsliste', icon: ShoppingCart, to: '/shopping' },
  { label: 'Opskrifter', icon: ChefHat, to: '/recipes' },
  { label: 'Dig', icon: User, to: '/settings' },
]

function isActive(tabTo: string) {
  if (tabTo === '/') return route.path === '/'
  return route.path.startsWith(tabTo)
}

async function goToInventory() {
  if (inventoryStore.inventories.length === 0) {
    await inventoryStore.fetchInventories()
  }
  if (inventoryStore.inventories.length === 1) {
    router.push({ name: 'inventory', params: { id: inventoryStore.inventories[0].id } })
  } else {
    router.push({ name: 'inventory-list' })
  }
}
</script>

<template>
  <nav class="bottom-nav" aria-label="Main navigation">
    <router-link
      to="/"
      class="nav-tab"
      :class="{ active: isActive('/') }"
      :aria-current="isActive('/') ? 'page' : undefined"
    >
      <House :size="22" class="nav-icon" />
      <span class="nav-label">Hjem</span>
    </router-link>

    <button
      class="nav-tab"
      :class="{ active: route.path.startsWith('/inventory') }"
      :aria-current="route.path.startsWith('/inventory') ? 'page' : undefined"
      @click="goToInventory"
    >
      <Archive :size="22" class="nav-icon" />
      <span class="nav-label">Beholdning</span>
    </button>

    <router-link
      v-for="tab in tabs.slice(1)"
      :key="tab.to"
      :to="tab.to"
      class="nav-tab"
      :class="{ active: isActive(tab.to) }"
      :aria-current="isActive(tab.to) ? 'page' : undefined"
    >
      <component :is="tab.icon" :size="22" class="nav-icon" />
      <span class="nav-label">{{ tab.label }}</span>
    </router-link>
  </nav>
</template>

<style scoped>
.bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 100;
  height: var(--bottomnav-height);
  display: flex;
  align-items: center;
  justify-content: space-around;
  padding: 0 var(--space-2);
  background: rgba(247, 242, 234, 0.85);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-top: 1px solid var(--border);
}

.nav-tab {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 3px;
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-lg);
  flex: 1;
  min-width: 0;
  color: var(--fg-faint);
  text-decoration: none;
  transition: color var(--motion-default), background var(--motion-default);
}

@media (max-width: 360px) {
  .nav-tab {
    padding-left: var(--space-1);
    padding-right: var(--space-1);
  }

  .nav-label {
    font-size: 9px;
  }
}

.nav-tab.active {
  background: var(--surface);
  color: var(--sage-600);
  box-shadow: var(--shadow-sm);
}

.nav-icon {
  flex-shrink: 0;
}

.nav-label {
  font-size: 11px;
  font-weight: 500;
  line-height: 1;
}
</style>
