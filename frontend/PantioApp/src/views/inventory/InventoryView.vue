<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, Search, Archive } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import InventoryRow from '../../components/ui/InventoryRow.vue'
import PButton from '../../components/ui/PButton.vue'
import { useInventoryStore } from '../../stores/inventory'
import type { InventoryItemDto } from '../../services/types'

const route = useRoute()
const router = useRouter()
const store = useInventoryStore()

const inventoryId = route.params.id as string

function daysUntilExpiry(item: InventoryItemDto): number {
  if (!item.expiryDate) return Infinity
  return Math.ceil(
    (new Date(item.expiryDate.estimatedExpiry).getTime() - Date.now()) / (1000 * 60 * 60 * 24),
  )
}

const expired = computed(() => store.items.filter((i) => daysUntilExpiry(i) < 0))
const expiringSoon = computed(() => store.items.filter((i) => daysUntilExpiry(i) >= 0 && daysUntilExpiry(i) <= 3))
const allGood = computed(() => store.items.filter((i) => daysUntilExpiry(i) > 3))

const inventoryName = computed(
  () => store.currentInventory?.name ?? store.inventories.find((i) => i.id === inventoryId)?.name ?? 'Inventory',
)

onMounted(async () => {
  if (store.inventories.length === 0) await store.fetchInventories()
  await store.fetchItems(inventoryId)
})

function openItem(itemId: string) {
  router.push({ name: 'item-detail', params: { id: inventoryId, itemId } })
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar :title="inventoryName" back-route="/">
        <button
          class="icon-btn"
          aria-label="Search"
          @click="router.push('/search')"
        >
          <Search :size="20" />
        </button>
        <button
          class="icon-btn"
          aria-label="Add item"
          @click="router.push({ name: 'manual-entry', params: { id: inventoryId } })"
        >
          <Plus :size="20" />
        </button>
      </TopBar>
    </template>

    <div class="page">
      <!-- Loading -->
      <div v-if="store.isLoadingItems" class="skeleton-list">
        <div v-for="i in 5" :key="i" class="skeleton-row" />
      </div>

      <!-- Empty state -->
      <div v-else-if="store.items.length === 0" class="empty-state">
        <Archive :size="48" class="empty-icon" />
        <h2>Nothing here yet</h2>
        <p>Connect Netto to import receipts, or add items manually.</p>
        <div class="empty-actions">
          <PButton @click="router.push({ name: 'manual-entry', params: { id: inventoryId } })">
            <Plus :size="16" />
            Add item
          </PButton>
          <PButton variant="secondary" @click="router.push('/store')">Connect Netto</PButton>
        </div>
      </div>

      <!-- Sections -->
      <template v-else>
        <section v-if="expired.length > 0" class="section">
          <h2 class="section-title section-title--past">Expired</h2>
          <div class="item-list">
            <InventoryRow v-for="item in expired" :key="item.id" :item="item" @click="openItem(item.id)" />
          </div>
        </section>

        <section v-if="expiringSoon.length > 0" class="section">
          <h2 class="section-title section-title--soon">Expiring soon</h2>
          <div class="item-list">
            <InventoryRow v-for="item in expiringSoon" :key="item.id" :item="item" @click="openItem(item.id)" />
          </div>
        </section>

        <section v-if="allGood.length > 0" class="section">
          <h2 class="section-title">All good</h2>
          <div class="item-list">
            <InventoryRow v-for="item in allGood" :key="item.id" :item="item" @click="openItem(item.id)" />
          </div>
        </section>
      </template>
    </div>
  </AppShell>
</template>

<style scoped>
.page {
  padding: var(--space-4);
  max-width: var(--max-width);
  margin: 0 auto;
}

.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.skeleton-row {
  height: 72px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-20) var(--space-4);
  text-align: center;
  color: var(--fg-muted);
}

.empty-icon {
  color: var(--border-strong);
}

.empty-state h2 {
  color: var(--fg);
}

.empty-actions {
  display: flex;
  gap: var(--space-2);
  flex-wrap: wrap;
  justify-content: center;
}

.section {
  margin-bottom: var(--space-6);
}

.section-title {
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--fg-muted);
  margin-bottom: var(--space-3);
}

.section-title--past {
  color: var(--past);
}

.section-title--soon {
  color: var(--soon);
}

.item-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: var(--radius-md);
  color: var(--fg-muted);
  transition: background var(--motion-default);
  cursor: pointer;
  background: none;
  border: none;
}

.icon-btn:hover {
  background: var(--surface-raised);
}
</style>
