<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, Trash2 } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import ShoppingItem from '../../components/ui/ShoppingItem.vue'
import { useShoppingListStore } from '../../stores/shoppingList'

const route = useRoute()
const router = useRouter()
const store = useShoppingListStore()

const id = route.params.id as string
const isDeleting = ref(false)

onMounted(async () => {
  await store.fetchLists()
  store.selectList(id)
})

async function deleteCurrentList() {
  if (!store.currentList) return
  if (!confirm(`Slet "${store.currentList.name}"?`)) return
  isDeleting.value = true
  try {
    await store.deleteList(store.currentList.id)
    router.push({ name: 'shopping' })
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar :title="store.currentList?.name ?? ''" :back-route="{ name: 'shopping' }">
        <button
          class="icon-btn danger"
          aria-label="Slet liste"
          :disabled="isDeleting"
          @click="deleteCurrentList"
        >
          <Trash2 :size="20" />
        </button>
        <button
          class="icon-btn"
          aria-label="Tilføj vare"
          @click="router.push({ name: 'shopping-add-item', params: { id } })"
        >
          <Plus :size="22" />
        </button>
      </TopBar>
    </template>

    <div class="page">
      <!-- Loading -->
      <div v-if="store.isLoading" class="skeleton-list">
        <div v-for="i in 4" :key="i" class="skeleton-row" />
      </div>

      <template v-if="store.currentList && !store.isLoading">
        <!-- Empty state -->
        <div v-if="store.currentList.items.length === 0" class="list-empty">
          <p>Intet på denne liste endnu. Tryk + for at tilføje varer.</p>
        </div>

        <!-- Items -->
        <div class="items-list">
          <ShoppingItem
            v-for="item in store.currentList.items"
            :key="item.id"
            :item="item"
            @toggle="store.toggleItem(store.currentList!.id, item.id)"
            @delete="store.deleteItem(store.currentList!.id, item.id)"
          />
        </div>
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

.list-empty {
  text-align: center;
  color: var(--fg-faint);
  font-size: 14px;
  padding: var(--space-8) 0;
}

.items-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.skeleton-row {
  height: 52px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
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
</style>
