<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Archive, House, ShoppingCart, ChefHat, Receipt, User } from 'lucide-vue-next'

const route = useRoute()

const sections = [
  {
    label: 'Beholdning',
    icon: Archive,
    to: '/inventory',
    matches: (p: string) => p.startsWith('/inventory'),
  },
  {
    label: 'Liste',
    icon: ShoppingCart,
    to: '/shopping',
    matches: (p: string) => p.startsWith('/shopping'),
  },
  {
    label: 'Opskrifter',
    icon: ChefHat,
    to: '/recipes',
    matches: (p: string) => p.startsWith('/recipes'),
  },
  {
    label: 'Butik',
    icon: Receipt,
    to: '/store',
    matches: (p: string) => p.startsWith('/store'),
  },
  {
    label: 'Dig',
    icon: User,
    to: '/settings',
    matches: (p: string) => p.startsWith('/settings'),
  },
]

const displayedTabs = computed(() =>
  sections.map((s) =>
    s.matches(route.path)
      ? { label: 'Hjem', icon: House, to: '/', isHome: true }
      : { ...s, isHome: false },
  ),
)
</script>

<template>
  <nav class="bottom-nav" aria-label="Main navigation">
    <router-link
      v-for="(tab, i) in displayedTabs"
      :key="i"
      :to="tab.to"
      class="nav-tab"
      :class="{ home: tab.isHome }"
      :aria-label="tab.label"
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
  min-width: 56px;
  color: var(--fg-faint);
  text-decoration: none;
  transition: color var(--motion-default), background var(--motion-default);
}

.nav-tab.home {
  color: var(--clay-600);
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
