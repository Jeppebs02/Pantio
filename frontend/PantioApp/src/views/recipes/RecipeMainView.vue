<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ChefHat, Search, Bookmark, Sparkles, X, Plus, Heart } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PInput from '../../components/ui/PInput.vue'
import PButton from '../../components/ui/PButton.vue'
import PBadge from '../../components/ui/PBadge.vue'
import PAlert from '../../components/ui/PAlert.vue'
import { useRecipesStore } from '../../stores/recipes'
import { useInventoryStore } from '../../stores/inventory'
import type { InventoryItemDto } from '../../services/types'

const route = useRoute()
const router = useRouter()
const store = useRecipesStore()
const invStore = useInventoryStore()

type Tab = 'saved' | 'browse' | 'generate'
const activeTab = ref<Tab>((route.query.tab as Tab) ?? 'browse')

watch(activeTab, (tab) => {
  router.replace({ query: { tab } })
  if ((tab === 'browse' || tab === 'saved') && store.recipeList.length === 0) loadRecipes()
})

// ── Browse state ──
const searchQuery = ref('')
const ingredientSearch = ref('')
const selectedIngredients = ref<Set<string>>(new Set())
let debounceTimer: ReturnType<typeof setTimeout>

const allIngredientNames = computed(() => {
  const names = new Set<string>()
  store.recipeList.forEach((r) => r.ingredientNames.forEach((n) => names.add(n)))
  return [...names].sort()
})

// Suggestions: ingredient names matching what the user typed, excluding already-selected ones
const ingredientSuggestions = computed(() => {
  const q = ingredientSearch.value.trim().toLowerCase()
  if (!q) return []
  return allIngredientNames.value
    .filter((n) => n.toLowerCase().includes(q) && !selectedIngredients.value.has(n))
    .slice(0, 8)
})

// Client-side filtering — no extra API call when adding/removing ingredient tokens
const filteredRecipes = computed(() => {
  if (selectedIngredients.value.size === 0) return store.recipeList
  return store.recipeList.filter((r) =>
    r.ingredientNames.some((n) => selectedIngredients.value.has(n)),
  )
})

function addIngredient(name: string) {
  selectedIngredients.value.add(name)
  ingredientSearch.value = ''
}

function removeIngredient(name: string) {
  selectedIngredients.value.delete(name)
}

function onSearchInput() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => loadRecipes(), 300)
}

function loadRecipes() {
  // Ingredient filtering is client-side; only pass title search to the backend
  store.fetchRecipeList(searchQuery.value || undefined)
}

// ── Generate state ──
const selectedItemIds = ref<Set<string>>(new Set())
const generateError = ref('')

const inventoryItems = computed<InventoryItemDto[]>(() => {
  const all = invStore.inventories.flatMap((inv) =>
    invStore.items.filter((i) => i.inventoryId === inv.id),
  )
  return [...all].sort((a, b) => {
    const dA = a.expiryDate ? new Date(a.expiryDate.estimatedExpiry).getTime() : Infinity
    const dB = b.expiryDate ? new Date(b.expiryDate.estimatedExpiry).getTime() : Infinity
    return dA - dB
  })
})

const hasInventoryItems = computed(() => invStore.items.length > 0)

function toggleItem(id: string) {
  if (selectedItemIds.value.has(id)) {
    selectedItemIds.value.delete(id)
  } else {
    selectedItemIds.value.add(id)
  }
}

async function suggest() {
  if (selectedItemIds.value.size === 0) return
  generateError.value = ''
  try {
    await store.getSuggestions([...selectedItemIds.value])
  } catch {
    generateError.value = "Vi kunne ikke generere opskrifter lige nu. Prøv igen."
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

// ── Lifecycle ──
onMounted(async () => {
  if (activeTab.value === 'browse' || activeTab.value === 'saved') loadRecipes()
  if (invStore.inventories.length === 0) await invStore.fetchInventories()
  for (const inv of invStore.inventories) {
    await invStore.fetchItems(inv.id)
  }
})

function openRecipe(id: string) {
  router.push({ name: 'recipe-detail', params: { id }, query: { from: activeTab.value } })
}

const savedRecipes = computed(() => store.recipeList.filter((r) => r.isSaved))

async function toggleSave(event: Event, recipeId: string) {
  event.stopPropagation()
  await store.toggleSave(recipeId)
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Opskrifter" />
    </template>

    <div class="page">
      <!-- Tab bar -->
      <div class="tab-bar">
        <button class="tab" :class="{ active: activeTab === 'saved' }" @click="activeTab = 'saved'">
          <Bookmark :size="15" />
          Gemt
        </button>
        <button
          class="tab"
          :class="{ active: activeTab === 'browse' }"
          @click="activeTab = 'browse'"
        >
          <Search :size="15" />
          Gennemse
        </button>
        <button
          class="tab"
          :class="{ active: activeTab === 'generate' }"
          @click="activeTab = 'generate'"
        >
          <Sparkles :size="15" />
          Generer
        </button>
      </div>

      <!-- ── Saved tab ── -->
      <template v-if="activeTab === 'saved'">
        <div v-if="store.isLoadingList" class="skeleton-list">
          <div v-for="i in 3" :key="i" class="skeleton-card" />
        </div>
        <div v-else-if="savedRecipes.length === 0" class="empty-state">
          <Heart :size="48" class="empty-icon" />
          <h2>Ingen gemte opskrifter</h2>
          <p>Tryk på hjertet på en opskrift for at gemme den her.</p>
        </div>
        <div v-else class="recipe-list">
          <button
            v-for="recipe in savedRecipes"
            :key="recipe.id"
            class="recipe-card"
            @click="openRecipe(recipe.id)"
          >
            <div class="recipe-card-icon">
              <ChefHat :size="24" />
            </div>
            <div class="recipe-card-body">
              <h3 class="recipe-name">{{ recipe.name }}</h3>
              <p class="recipe-desc">{{ recipe.description }}</p>
              <p class="recipe-meta">
                {{ recipe.ingredientCount }} ingredienser ·
                {{ recipe.portions }} portion{{ recipe.portions !== 1 ? 'er' : '' }}
              </p>
            </div>
            <button class="card-heart saved" aria-label="Fjern fra gemte" @click="toggleSave($event, recipe.id)">
              <Heart :size="18" fill="currentColor" />
            </button>
          </button>
        </div>
      </template>

      <!-- ── Browse tab ── -->
      <template v-else-if="activeTab === 'browse'">
        <div class="browse-controls">
          <!-- Recipe title search -->
          <PInput
            v-model="searchQuery"
            placeholder="Søg efter opskrift..."
            @input="onSearchInput"
          >
            <template #icon><Search :size="16" /></template>
          </PInput>

          <!-- Ingredient token input -->
          <div class="ingredient-filter">
            <PInput
              v-model="ingredientSearch"
              placeholder="Filtrer på ingredienser..."
            >
              <template #icon><Plus :size="16" /></template>
            </PInput>

            <!-- Suggestions -->
            <div v-if="ingredientSuggestions.length > 0" class="suggestions-list">
              <button
                v-for="name in ingredientSuggestions"
                :key="name"
                class="suggestion-item"
                @click="addIngredient(name)"
              >
                <Plus :size="13" />
                {{ name }}
              </button>
            </div>

            <!-- Active ingredient tokens -->
            <div v-if="selectedIngredients.size > 0" class="active-tokens">
              <button
                v-for="name in selectedIngredients"
                :key="name"
                class="active-token"
                @click="removeIngredient(name)"
              >
                {{ name }}
                <X :size="12" />
              </button>
            </div>
          </div>
        </div>

        <div v-if="store.isLoadingList" class="skeleton-list">
          <div v-for="i in 3" :key="i" class="skeleton-card" />
        </div>

        <div v-else-if="filteredRecipes.length === 0" class="empty-state">
          <ChefHat :size="48" class="empty-icon" />
          <h2>Ingen opskrifter fundet</h2>
          <p>Prøv at generere opskrifter med dine varer.</p>
        </div>

        <div v-else class="recipe-list">
          <button
            v-for="recipe in filteredRecipes"
            :key="recipe.id"
            class="recipe-card"
            @click="openRecipe(recipe.id)"
          >
            <div class="recipe-card-icon">
              <ChefHat :size="24" />
            </div>
            <div class="recipe-card-body">
              <h3 class="recipe-name">{{ recipe.name }}</h3>
              <p class="recipe-desc">{{ recipe.description }}</p>
              <p class="recipe-meta">
                {{ recipe.ingredientCount }} ingredienser ·
                {{ recipe.portions }} portion{{ recipe.portions !== 1 ? 'er' : '' }}
              </p>
            </div>
            <button
              class="card-heart"
              :class="{ saved: recipe.isSaved }"
              :aria-label="recipe.isSaved ? 'Fjern fra gemte' : 'Gem opskrift'"
              @click="toggleSave($event, recipe.id)"
            >
              <Heart :size="18" :fill="recipe.isSaved ? 'currentColor' : 'none'" />
            </button>
          </button>
        </div>
      </template>

      <!-- ── Generate tab ── -->
      <template v-else-if="activeTab === 'generate'">
        <PAlert v-if="generateError" variant="error">{{ generateError }}</PAlert>

        <div v-if="!hasInventoryItems" class="empty-state">
          <ChefHat :size="48" class="empty-icon" />
          <h2>Ingen varer på lager</h2>
          <p>Tilføj varer til dit lager, så finder vi opskrifter der bruger dem.</p>
          <PButton @click="router.push('/')">Gå til lager</PButton>
        </div>

        <template v-else>
          <div class="section-header">
            <h2 class="section-title">Vælg hvad du vil bruge</h2>
            <p class="section-sub">
              Vælg varer fra dit lager — vi foreslår opskrifter, der bruger dem op.
            </p>
          </div>

          <div class="chip-grid">
            <button
              v-for="item in inventoryItems"
              :key="item.id"
              class="item-chip"
              :class="{ selected: selectedItemIds.has(item.id) }"
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
              :disabled="selectedItemIds.size === 0 || store.isLoading"
              @click="suggest"
            >
              <Sparkles :size="16" />
              {{ store.isLoading ? 'Genererer opskrifter...' : 'Lav mad med det du har' }}
            </PButton>
          </div>

          <div v-if="store.suggestions.length > 0" class="suggestions">
            <h2 class="section-title">Forslag</h2>
            <div class="recipe-list">
              <button
                v-for="recipe in store.suggestions"
                :key="recipe.id"
                class="recipe-card"
                @click="
                  store.setCurrentRecipe(recipe);
                  router.push({
                    name: 'recipe-detail',
                    params: { id: recipe.id },
                    query: { from: 'generate' },
                  })
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

          <div v-if="store.isLoading" class="skeleton-list">
            <div v-for="i in 3" :key="i" class="skeleton-card" />
          </div>
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

/* ── Tabs ── */
.tab-bar {
  display: flex;
  gap: var(--space-1);
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-1);
}

.tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-1);
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-md);
  border: none;
  background: none;
  font-size: 13px;
  font-weight: 500;
  color: var(--fg-muted);
  cursor: pointer;
  transition: background var(--motion-default), color var(--motion-default);
  white-space: nowrap;
}

.tab.active {
  background: var(--bg);
  color: var(--fg);
  box-shadow: var(--shadow-sm);
}

/* ── Browse controls ── */
.browse-controls {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.ingredient-filter {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.suggestions-list {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.suggestion-item {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
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

.suggestion-item:hover {
  border-color: var(--sage-600);
  color: var(--sage-700);
}

.active-tokens {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.active-token {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding: var(--space-1) var(--space-3);
  border-radius: var(--radius-full);
  border: 1.5px solid var(--sage-600);
  background: var(--sage-100);
  font-size: 13px;
  font-weight: 500;
  color: var(--sage-700);
  cursor: pointer;
  transition: all var(--motion-default);
}

.active-token:hover {
  background: var(--sage-200, var(--sage-100));
  border-color: var(--sage-700, var(--sage-600));
}

/* ── Generate: item chips ── */
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

/* ── Shared: recipe cards ── */
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

.card-heart {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: var(--radius-md);
  border: none;
  background: none;
  color: var(--fg-faint);
  cursor: pointer;
  flex-shrink: 0;
  transition: color var(--motion-default), background var(--motion-default);
}

.card-heart:hover {
  background: var(--surface-raised);
  color: var(--fg-muted);
}

.card-heart.saved {
  color: var(--clay-600);
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

/* ── Empty / Skeleton ── */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-16) var(--space-4);
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
  height: 88px;
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
