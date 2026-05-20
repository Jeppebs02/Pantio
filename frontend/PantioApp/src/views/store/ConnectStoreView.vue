<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ChevronRight, Receipt } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PBadge from '../../components/ui/PBadge.vue'
import { useStoreConnectionStore } from '../../stores/storeConnection'
import { useAuthStore } from '../../stores/auth'

const storeConn = useStoreConnectionStore()
const auth = useAuthStore()
const router = useRouter()

onMounted(async () => {
  if (auth.localUser) {
    await storeConn.fetchConnections()
  }
})
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Butiksintegrationer" :back-route="{ name: 'settings' }" />
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
