<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Plus, ShoppingCart } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import ShoppingItem from '../../components/ui/ShoppingItem.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import { useShoppingListStore } from '../../stores/shoppingList'

const store = useShoppingListStore()

const newItemName = ref('')
const isAdding = ref(false)
const showNewList = ref(false)
const newListName = ref('')

onMounted(async () => {
  await store.fetchLists()
})

async function addItem() {
  if (!newItemName.value.trim() || !store.currentList) return
  isAdding.value = true
  try {
    await store.addItem(store.currentList.id, newItemName.value.trim())
    newItemName.value = ''
  } finally {
    isAdding.value = false
  }
}

async function createList() {
  if (!newListName.value.trim()) return
  await store.createList(newListName.value.trim())
  newListName.value = ''
  showNewList.value = false
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Shopping list">
        <button class="icon-btn" aria-label="New list" @click="showNewList = !showNewList">
          <Plus :size="22" />
        </button>
      </TopBar>
    </template>

    <div class="page">
      <!-- New list form -->
      <form v-if="showNewList" class="create-form" @submit.prevent="createList">
        <PInput v-model="newListName" label="List name" placeholder="e.g. Weekly shop" />
        <div class="create-actions">
          <PButton type="submit" size="sm" :disabled="!newListName.trim()">Create</PButton>
          <PButton variant="ghost" size="sm" @click="showNewList = false; newListName = ''">Cancel</PButton>
        </div>
      </form>

      <!-- List selector -->
      <div v-if="store.lists.length > 1" class="list-tabs">
        <button
          v-for="list in store.lists"
          :key="list.id"
          class="list-tab"
          :class="{ active: store.currentList?.id === list.id }"
          @click="store.selectList(list.id)"
        >
          {{ list.name }}
        </button>
      </div>

      <!-- Empty state -->
      <div v-if="store.lists.length === 0 && !store.isLoading" class="empty-state">
        <ShoppingCart :size="48" class="empty-icon" />
        <h2>No lists yet</h2>
        <p>Create a shopping list to start adding items.</p>
        <PButton @click="showNewList = true">
          <Plus :size="16" />
          New list
        </PButton>
      </div>

      <!-- Loading -->
      <div v-if="store.isLoading" class="skeleton-list">
        <div v-for="i in 4" :key="i" class="skeleton-row" />
      </div>

      <!-- List items -->
      <template v-if="store.currentList && !store.isLoading">
        <div v-if="store.currentList.items.length === 0" class="list-empty">
          <p>Nothing on this list yet. Add something below.</p>
        </div>

        <div class="items-list">
          <ShoppingItem
            v-for="item in store.currentList.items"
            :key="item.id"
            :item="item"
            @toggle="store.toggleItem(store.currentList!.id, item.id)"
            @delete="store.deleteItem(store.currentList!.id, item.id)"
          />
        </div>

        <!-- Add item form -->
        <form class="add-form" @submit.prevent="addItem">
          <PInput v-model="newItemName" placeholder="Add an item..." />
          <PButton type="submit" size="sm" :disabled="isAdding || !newItemName.trim()">
            <Plus :size="16" />
            Add
          </PButton>
        </form>
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

.list-tabs {
  display: flex;
  gap: var(--space-2);
  overflow-x: auto;
  padding-bottom: var(--space-1);
}

.list-tab {
  padding: var(--space-2) var(--space-4);
  border-radius: var(--radius-full);
  border: 1.5px solid var(--border-strong);
  background: var(--surface);
  font-size: 14px;
  font-weight: 500;
  color: var(--fg-muted);
  cursor: pointer;
  white-space: nowrap;
  transition: all var(--motion-default);
}

.list-tab.active {
  border-color: var(--sage-600);
  background: var(--sage-100);
  color: var(--sage-700);
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

.add-form {
  display: flex;
  gap: var(--space-2);
  align-items: flex-end;
  position: sticky;
  bottom: calc(var(--bottomnav-height) + var(--space-4));
  background: var(--bg);
  padding: var(--space-3) 0;
}

.add-form .pinput-wrap {
  flex: 1;
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
</style>
