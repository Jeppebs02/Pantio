<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ChefHat, Sparkles } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PBadge from '../../components/ui/PBadge.vue'
import PAlert from '../../components/ui/PAlert.vue'
import { useInventoryStore } from '../../stores/inventory'
import { useRecipesStore } from '../../stores/recipes'
import type { InventoryItemDto } from '../../services/types'
import RecipeGeneratingLoader from '../../components/recipes/RecipeGeneratingLoader.vue'

const router = useRouter()
const invStore = useInventoryStore()
const recipeStore = useRecipesStore()

const selectedIds = ref<Set<string>>(new Set())
const error = ref('')

const inventoryItems = computed<InventoryItemDto[]>(() => {
  const all = Object.values(invStore.itemsByInventory).flat()
  return [...all].sort((a, b) => {
    const dA = a.expiryDate ? new Date(a.expiryDate.estimatedExpiry).getTime() : Infinity
    const dB = b.expiryDate ? new Date(b.expiryDate.estimatedExpiry).getTime() : Infinity
    return dA - dB
  })
})

const hasInventoryItems = computed(() =>
  Object.values(invStore.itemsByInventory).some((arr) => arr.length > 0),
)

onMounted(async () => {
  if (invStore.inventories.length === 0) await invStore.fetchInventories()
  await invStore.fetchAllItemSummaries()
})

function toggleItem(id: string) {
  if (selectedIds.value.has(id)) {
    selectedIds.value.delete(id)
  } else {
    selectedIds.value.add(id)
  }
}

async function suggest() {
  if (selectedIds.value.size === 0) return
  error.value = ''
  try {
    await recipeStore.getSuggestions([...selectedIds.value])
  } catch {
    error.value = "Vi kunne ikke generere opskrifter lige nu. Prøv igen."
  }
}

function goToInventory() {
  if (invStore.inventories.length >= 2) {
    router.push({ name: 'inventory-list' })
  } else if (invStore.inventories.length === 1) {
    router.push({ name: 'inventory', params: { id: invStore.inventories[0].id } })
  } else {
    router.push({ name: 'inventory-list' })
  }
}

function expiryLabel(item: InventoryItemDto) {
  if (!item.expiryDate) return null
  const diff = Math.ceil(
    (new Date(item.expiryDate.estimatedExpiry).getTime() - Date.now()) / (1000 * 60 * 60 * 24),
  )
  if (diff < 0) return { text: 'Udløbet', tone: 'past' as const }
  if (diff <= 3) return { text: `${diff}d tilbage`, tone: 'soon' as const }
  return null
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Opskrifter" />
    </template>

    <div class="page">
      <PAlert v-if="error" variant="error">{{ error }}</PAlert>

      <div v-if="!hasInventoryItems" class="empty-state">
        <ChefHat :size="48" class="empty-icon" />
        <h2>Ingen varer på lager</h2>
        <p>Tilføj varer først, så foreslår vi opskrifter der bruger dem.</p>
        <PButton @click="goToInventory">Gå til lager</PButton>
      </div>

      <template v-else>
        <div class="section-header">
          <h2 class="section-title">Vælg hvad du vil bruge</h2>
          <p class="section-sub">
            Vælg varer fra dit lager — vi foreslår opskrifter der bruger dem op.
          </p>
        </div>

        <div class="chip-grid">
          <button
            v-for="item in inventoryItems"
            :key="item.id"
            class="item-chip"
            :class="{ selected: selectedIds.has(item.id) }"
            @click="toggleItem(item.id)"
          >
            <span class="chip-name">{{ item.productName }}</span>
            <PBadge v-if="expiryLabel(item)" :tone="expiryLabel(item)!.tone">
              {{ expiryLabel(item)!.text }}
            </PBadge>
          </button>
        </div>

        <div class="suggest-bar">
          <PButton
            full-width
            :disabled="selectedIds.size === 0 || recipeStore.isLoading"
            @click="suggest"
          >
            <Sparkles :size="16" />
            {{ recipeStore.isLoading ? 'Genererer opskrifter...' : 'Lav mad med det du har' }}
          </PButton>
        </div>

        <!-- Suggestions -->
        <div v-if="recipeStore.suggestions.length > 0" class="suggestions">
          <h2 class="section-title">Forslag</h2>
          <div class="recipe-list">
            <button
              v-for="recipe in recipeStore.suggestions"
              :key="recipe.id"
              class="recipe-card"
              @click="
                recipeStore.setCurrentRecipe(recipe);
                router.push({ name: 'recipe-detail', params: { id: recipe.id } })
              "
            >
              <div class="recipe-card-icon">
                <ChefHat :size="24" />
              </div>
              <div class="recipe-card-body">
                <h3 class="recipe-name">{{ recipe.name }}</h3>
                <p class="recipe-desc">{{ recipe.description }}</p>
                <p class="recipe-meta">
                  {{ recipe.ingredients.length }} ingredienser · {{ recipe.portions }} portioner
                </p>
              </div>
            </button>
          </div>
        </div>

        <RecipeGeneratingLoader v-if="recipeStore.isLoading" />
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
  gap: var(--space-5);
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

.section-header {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.section-title {
  font-size: 18px;
  font-weight: 700;
  color: var(--fg);
}

.section-sub {
  font-size: 14px;
  color: var(--fg-muted);
}

.chip-grid {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.item-chip {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-full);
  border: 1.5px solid var(--border-strong);
  background: var(--surface);
  cursor: pointer;
  transition: border-color var(--motion-default), background var(--motion-default),
    color var(--motion-default);
  font-size: 14px;
  color: var(--fg);
}

.item-chip:hover {
  border-color: var(--sage-600);
}

.item-chip.selected {
  border-color: var(--sage-600);
  background: var(--sage-100);
  color: var(--sage-700);
}

.chip-name {
  font-weight: 500;
}

.suggest-bar {
  position: sticky;
  bottom: calc(var(--bottomnav-height) + var(--space-4));
}

.recipe-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.recipe-card {
  display: flex;
  align-items: flex-start;
  gap: var(--space-4);
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  cursor: pointer;
  text-align: left;
  transition: box-shadow var(--motion-default);
  width: 100%;
}

.recipe-card:hover {
  box-shadow: var(--shadow-sm);
}

.recipe-card-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  background: var(--clay-100);
  color: var(--clay-600);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.recipe-card-body {
  flex: 1;
  min-width: 0;
}

.recipe-name {
  font-size: 16px;
  font-weight: 700;
  color: var(--fg);
}

.recipe-desc {
  font-size: 14px;
  color: var(--fg-muted);
  margin-top: 2px;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.recipe-meta {
  font-size: 12px;
  color: var(--fg-faint);
  margin-top: var(--space-2);
}

.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.skeleton-card {
  height: 100px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%,
  100% {
    opacity: 0.4;
  }
  50% {
    opacity: 0.7;
  }
}
</style>
