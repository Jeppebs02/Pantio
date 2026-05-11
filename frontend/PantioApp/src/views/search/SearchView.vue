<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Search } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PInput from '../../components/ui/PInput.vue'
import InventoryRow from '../../components/ui/InventoryRow.vue'
import { useInventoryStore } from '../../stores/inventory'

const router = useRouter()
const invStore = useInventoryStore()

const query = ref('')

const results = computed(() => {
  const q = query.value.toLowerCase().trim()
  if (!q) return []
  return invStore.items.filter((item) =>
    item.productName.toLowerCase().includes(q) ||
    item.ean?.toLowerCase().includes(q),
  )
})

onMounted(async () => {
  if (invStore.inventories.length === 0) await invStore.fetchInventories()
  for (const inv of invStore.inventories) {
    await invStore.fetchItems(inv.id)
  }
})
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Search" />
    </template>

    <div class="page">
      <PInput v-model="query" placeholder="Search inventory...">
        <template #icon><Search :size="16" /></template>
      </PInput>

      <div v-if="query && results.length === 0" class="no-results">
        <p>No items match "{{ query }}".</p>
      </div>

      <div v-if="results.length > 0" class="results">
        <p class="results-count eyebrow">{{ results.length }} result{{ results.length !== 1 ? 's' : '' }}</p>
        <div class="results-list">
          <InventoryRow
            v-for="item in results"
            :key="item.id"
            :item="item"
            @click="router.push({ name: 'item-detail', params: { id: item.inventoryId, itemId: item.id } })"
          />
        </div>
      </div>

      <div v-if="!query" class="hint">
        <Search :size="40" class="hint-icon" />
        <p>Search across all your inventories by product name or barcode.</p>
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

.results-count {
  margin-bottom: var(--space-2);
}

.results-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.no-results {
  text-align: center;
  color: var(--fg-muted);
  padding: var(--space-10) 0;
  font-size: 15px;
}

.hint {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-14) var(--space-4);
  text-align: center;
  color: var(--fg-muted);
}

.hint-icon {
  color: var(--border-strong);
}
</style>
