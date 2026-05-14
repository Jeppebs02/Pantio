<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, Search, Archive, Trash2 } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import InventoryRow from '../../components/ui/InventoryRow.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import { useInventoryStore } from '../../stores/inventory'
import type { InventoryItemDto } from '../../services/types'

const route = useRoute()
const router = useRouter()
const store = useInventoryStore()

const inventoryId = route.params.id as string
const isDeleting = ref(false)
const searchQuery = ref('')
type StatusFilter = 'all' | 'expired' | 'soon' | 'good'
const activeFilter = ref<StatusFilter>('all')
const showDropdown = ref(false)
const showNewInventoryForm = ref(false)
const newInventoryName = ref('')

async function deleteInventory() {
  if (!confirm(`Slet "${inventoryName.value}"? Alle varer indeni vil blive fjernet permanent.`)) return
  isDeleting.value = true
  try {
    await store.deleteInventory(inventoryId)
    router.replace({ name: 'inventory-list' })
  } finally {
    isDeleting.value = false
  }
}

function daysUntilExpiry(item: InventoryItemDto): number {
  if (!item.expiryDate) return Infinity
  const effective = item.expiryDate.overrideDate ?? item.expiryDate.estimatedExpiry
  return Math.ceil(
    (new Date(effective).getTime() - Date.now()) / (1000 * 60 * 60 * 24),
  )
}

function matchesQuery(item: InventoryItemDto): boolean {
  if (!searchQuery.value.trim()) return true
  return item.productName.toLowerCase().includes(searchQuery.value.trim().toLowerCase())
}

const expired = computed(() =>
  store.items.filter((i) => daysUntilExpiry(i) < 0 && matchesQuery(i)),
)
const expiringSoon = computed(() =>
  store.items.filter((i) => daysUntilExpiry(i) >= 0 && daysUntilExpiry(i) <= 3 && matchesQuery(i)),
)
const allGood = computed(() =>
  store.items.filter((i) => daysUntilExpiry(i) > 3 && matchesQuery(i)),
)

const showExpired = computed(() => activeFilter.value === 'all' || activeFilter.value === 'expired')
const showSoon = computed(() => activeFilter.value === 'all' || activeFilter.value === 'soon')
const showGood = computed(() => activeFilter.value === 'all' || activeFilter.value === 'good')

const hasResults = computed(() =>
  (showExpired.value && expired.value.length > 0) ||
  (showSoon.value && expiringSoon.value.length > 0) ||
  (showGood.value && allGood.value.length > 0),
)

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

async function createNewInventory() {
  if (!newInventoryName.value.trim()) return
  const inv = await store.createInventory(newInventoryName.value.trim())
  newInventoryName.value = ''
  showNewInventoryForm.value = false
  router.push({ name: 'inventory', params: { id: inv.id } })
}

const filters: { key: StatusFilter; label: string }[] = [
  { key: 'all', label: 'Alle' },
  { key: 'expired', label: 'Udløbet' },
  { key: 'soon', label: 'Snart' },
  { key: 'good', label: 'Alt godt' },
]
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar :title="inventoryName" :back-route="{ name: 'inventory-list' }">
        <button
          class="icon-btn danger"
          aria-label="Slet beholdning"
          :disabled="isDeleting"
          @click="deleteInventory"
        >
          <Trash2 :size="20" />
        </button>
        <button
          class="icon-btn"
          aria-label="Search"
          @click="router.push('/search')"
        >
          <Search :size="20" />
        </button>
        <div v-if="showDropdown" class="dropdown-backdrop" @click="showDropdown = false" />
        <div class="dropdown-wrap">
          <button class="icon-btn" aria-label="Tilføj" @click="showDropdown = !showDropdown">
            <Plus :size="22" />
          </button>
          <div v-if="showDropdown" class="dropdown-menu">
            <button class="dropdown-item" @click="showDropdown = false; router.push({ name: 'manual-entry', params: { id: inventoryId } })">
              Tilføj ny vare
            </button>
            <button class="dropdown-item" @click="showDropdown = false; showNewInventoryForm = true">
              Tilføj nyt lager
            </button>
          </div>
        </div>
      </TopBar>
    </template>

    <div class="page">
      <!-- New inventory form -->
      <form v-if="showNewInventoryForm" class="create-form" @submit.prevent="createNewInventory">
        <PInput v-model="newInventoryName" label="Lagernavn" placeholder="f.eks. Køleskab" />
        <div class="create-actions">
          <PButton type="submit" size="sm" :disabled="!newInventoryName.trim()">Opret</PButton>
          <PButton variant="ghost" size="sm" @click="showNewInventoryForm = false; newInventoryName = ''">Annuller</PButton>
        </div>
      </form>

      <!-- Loading -->
      <div v-if="store.isLoadingItems" class="skeleton-list">
        <div v-for="i in 5" :key="i" class="skeleton-row" />
      </div>

      <!-- Empty state (no items at all) -->
      <div v-else-if="store.items.length === 0" class="empty-state">
        <Archive :size="48" class="empty-icon" />
        <h2>Intet her endnu</h2>
        <p>Tilslut Netto for at importere kvitteringer, eller tilføj varer manuelt.</p>
        <div class="empty-actions">
          <PButton @click="router.push({ name: 'manual-entry', params: { id: inventoryId } })">
            <Plus :size="16" />
            Tilføj vare
          </PButton>
          <PButton variant="secondary" @click="router.push('/store')">Tilslut Netto</PButton>
        </div>
      </div>

      <!-- Search + filters + list -->
      <template v-else>
        <div class="search-bar">
          <PInput v-model="searchQuery" placeholder="Søg i beholdning...">
            <template #icon><Search :size="16" /></template>
          </PInput>
        </div>

        <div class="filter-chips">
          <button
            v-for="f in filters"
            :key="f.key"
            class="filter-chip"
            :class="{ active: activeFilter === f.key }"
            @click="activeFilter = f.key"
          >
            {{ f.label }}
          </button>
        </div>

        <!-- No results from search/filter -->
        <div v-if="!hasResults" class="empty-state empty-state--inline">
          <Search :size="32" class="empty-icon" />
          <p>Ingen varer matcher din søgning.</p>
        </div>

        <template v-else>
          <section v-if="showExpired && expired.length > 0" class="section">
            <h2 class="section-title section-title--past">Udløbet</h2>
            <div class="item-list">
              <InventoryRow v-for="item in expired" :key="item.id" :item="item" @click="openItem(item.id)" />
            </div>
          </section>

          <section v-if="showSoon && expiringSoon.length > 0" class="section">
            <h2 class="section-title section-title--soon">Udløber snart</h2>
            <div class="item-list">
              <InventoryRow v-for="item in expiringSoon" :key="item.id" :item="item" @click="openItem(item.id)" />
            </div>
          </section>

          <section v-if="showGood && allGood.length > 0" class="section">
            <h2 class="section-title">Alt godt</h2>
            <div class="item-list">
              <InventoryRow v-for="item in allGood" :key="item.id" :item="item" @click="openItem(item.id)" />
            </div>
          </section>
        </template>
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

.search-bar {
  /* full width, no extra wrapper needed */
}

.filter-chips {
  display: flex;
  gap: var(--space-2);
  flex-wrap: wrap;
}

.filter-chip {
  display: inline-flex;
  align-items: center;
  padding: var(--space-1) var(--space-3);
  border-radius: var(--radius-full);
  border: 1.5px solid var(--border-strong);
  background: var(--surface);
  font-size: 13px;
  font-weight: 500;
  color: var(--fg-muted);
  cursor: pointer;
  transition: all var(--motion-default);
}

.filter-chip:hover {
  border-color: var(--sage-600);
  color: var(--sage-700);
}

.filter-chip.active {
  border-color: var(--sage-600);
  background: var(--sage-100);
  color: var(--sage-700);
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

.empty-state--inline {
  padding: var(--space-10) var(--space-4);
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
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.section-title {
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--fg-muted);
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

.icon-btn.danger:hover {
  color: var(--past);
  background: var(--past-bg);
}

.dropdown-backdrop {
  position: fixed;
  inset: 0;
  z-index: 10;
}

.dropdown-wrap {
  position: relative;
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 4px);
  right: 0;
  background: var(--surface-raised);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  box-shadow: 0 4px 12px rgba(0,0,0,0.12);
  min-width: 160px;
  z-index: 20;
  overflow: hidden;
}

.dropdown-item {
  display: block;
  width: 100%;
  padding: var(--space-3) var(--space-4);
  text-align: left;
  background: none;
  border: none;
  font-size: 14px;
  color: var(--fg);
  cursor: pointer;
}

.dropdown-item:hover {
  background: var(--surface);
}

.create-form {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.create-actions {
  display: flex;
  gap: var(--space-2);
}
</style>
