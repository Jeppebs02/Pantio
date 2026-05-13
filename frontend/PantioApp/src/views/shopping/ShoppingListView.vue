<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, ShoppingCart, ChevronRight } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import { useShoppingListStore } from '../../stores/shoppingList'

const router = useRouter()
const store = useShoppingListStore()

const showNewList = ref(false)
const newListName = ref('')

onMounted(async () => {
  await store.fetchLists()
})

async function createList() {
  if (!newListName.value.trim()) return
  const newList = await store.createList(newListName.value.trim())
  newListName.value = ''
  showNewList.value = false
  router.push({ name: 'shopping-detail', params: { id: newList.id } })
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Indkøbsliste">
        <button class="icon-btn" aria-label="Ny liste" @click="showNewList = !showNewList">
          <Plus :size="22" />
        </button>
      </TopBar>
    </template>

    <div class="page">
      <!-- New list form -->
      <form v-if="showNewList" class="create-form" @submit.prevent="createList">
        <PInput v-model="newListName" label="Listenavn" placeholder="f.eks. Ugentlig indkøb" />
        <div class="create-actions">
          <PButton type="submit" size="sm" :disabled="!newListName.trim()">Opret</PButton>
          <PButton variant="ghost" size="sm" @click="showNewList = false; newListName = ''">Annuller</PButton>
        </div>
      </form>

      <!-- Loading -->
      <div v-if="store.isLoading" class="skeleton-list">
        <div v-for="i in 3" :key="i" class="skeleton-row" />
      </div>

      <!-- Empty state -->
      <div v-else-if="store.lists.length === 0" class="empty-state">
        <ShoppingCart :size="48" class="empty-icon" />
        <h2>Ingen lister endnu</h2>
        <p>Opret en indkøbsliste for at begynde at tilføje varer.</p>
        <PButton @click="showNewList = true">
          <Plus :size="16" />
          Ny liste
        </PButton>
      </div>

      <!-- List cards -->
      <div v-else class="lists">
        <button
          v-for="list in store.lists"
          :key="list.id"
          class="list-card"
          @click="router.push({ name: 'shopping-detail', params: { id: list.id } })"
        >
          <div class="list-card-info">
            <span class="list-card-name">{{ list.name }}</span>
            <span class="list-card-meta">
              {{ list.items.length }} varer
              <template v-if="list.items.some(i => !i.isChecked)">
                · {{ list.items.filter(i => !i.isChecked).length }} tilbage
              </template>
            </span>
          </div>
          <ChevronRight :size="18" class="list-card-chevron" />
        </button>
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

.lists {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.list-card {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  cursor: pointer;
  text-align: left;
  width: 100%;
  transition: background var(--motion-default);
}

.list-card:hover {
  background: var(--surface-raised);
}

.list-card-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.list-card-name {
  font-size: 15px;
  font-weight: 600;
  color: var(--fg);
}

.list-card-meta {
  font-size: 13px;
  color: var(--fg-muted);
}

.list-card-chevron {
  color: var(--fg-faint);
  flex-shrink: 0;
}

.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.skeleton-row {
  height: 64px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
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
