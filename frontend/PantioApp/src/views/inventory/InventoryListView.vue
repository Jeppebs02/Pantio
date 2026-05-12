<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Archive, AlertTriangle } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import { useInventoryStore } from '../../stores/inventory'

const router = useRouter()
const store = useInventoryStore()

const showCreate = ref(false)
const newName = ref('')
const isCreating = ref(false)

onMounted(async () => {
  await store.fetchInventories()
  if (store.inventories.length === 1) {
    router.replace({ name: 'inventory', params: { id: store.inventories[0].id } })
    return
  }
  if (store.inventories.length === 0) {
    showCreate.value = true
  }
})

async function createInventory() {
  if (!newName.value.trim()) return
  isCreating.value = true
  try {
    const inv = await store.createInventory(newName.value.trim())
    newName.value = ''
    showCreate.value = false
    router.push({ name: 'inventory', params: { id: inv.id } })
  } finally {
    isCreating.value = false
  }
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Lager">
        <button
          class="icon-btn"
          aria-label="Ny beholdning"
          @click="showCreate = !showCreate"
        >
          <Plus :size="22" />
        </button>
      </TopBar>
    </template>

    <div class="page">
      <!-- Create form -->
      <form v-if="showCreate" class="create-form" @submit.prevent="createInventory">
        <PInput
          v-model="newName"
          label="Navn"
          placeholder="f.eks. Køkken, Køleskab, Spisekammer"
          autofocus
        />
        <div class="create-actions">
          <PButton type="submit" :disabled="isCreating || !newName.trim()">
            {{ isCreating ? 'Opretter...' : 'Opret beholdning' }}
          </PButton>
          <PButton variant="ghost" @click="showCreate = false; newName = ''">Annuller</PButton>
        </div>
      </form>

      <!-- Empty state -->
      <div v-if="!store.isLoadingInventories && store.inventories.length === 0 && !showCreate" class="empty-state">
        <Archive :size="48" class="empty-icon" />
        <h2>Ingen beholdninger endnu</h2>
        <p>Opret din første beholdning for at starte med at holde styr på hvad du har.</p>
        <PButton @click="showCreate = true">
          <Plus :size="16" />
          Ny beholdning
        </PButton>
      </div>

      <!-- Loading -->
      <div v-if="store.isLoadingInventories" class="skeleton-list">
        <div v-for="i in 3" :key="i" class="skeleton-card" />
      </div>

      <!-- Inventory cards -->
      <div v-if="!store.isLoadingInventories" class="inv-grid">
        <button
          v-for="inv in store.inventories"
          :key="inv.id"
          class="inv-card"
          @click="router.push({ name: 'inventory', params: { id: inv.id } })"
        >
          <span class="inv-card-icon">
            <Archive :size="28" />
          </span>
          <span class="inv-card-body">
            <span class="inv-card-name">{{ inv.name }}</span>
          </span>
          <AlertTriangle :size="16" class="inv-card-arrow" />
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
}

.create-form {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  margin-bottom: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
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

.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.skeleton-card {
  height: 80px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
}

.inv-grid {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.inv-card {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-4) var(--space-5);
  cursor: pointer;
  text-align: left;
  transition: box-shadow var(--motion-default), background var(--motion-default);
}

.inv-card:hover {
  box-shadow: var(--shadow-sm);
  background: var(--surface-raised);
}

.inv-card-icon {
  width: 52px;
  height: 52px;
  border-radius: var(--radius-md);
  background: var(--sage-100);
  color: var(--sage-600);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.inv-card-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.inv-card-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--fg);
}

.inv-card-arrow {
  color: var(--fg-faint);
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
