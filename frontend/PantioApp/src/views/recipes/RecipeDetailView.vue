<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Check, X, ShoppingCart, ChefHat, Heart, Minus, Plus } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PBadge from '../../components/ui/PBadge.vue'
import { useRecipesStore } from '../../stores/recipes'
import { useInventoryStore } from '../../stores/inventory'
import { useShoppingListStore } from '../../stores/shoppingList'
import { createShoppingListFromRecipe } from '../../services/shoppingList'
import { useAuthStore } from '../../stores/auth'

const route = useRoute()
const router = useRouter()
const recipeStore = useRecipesStore()
const invStore = useInventoryStore()
const shoppingStore = useShoppingListStore()
const auth = useAuthStore()

const recipeId = route.params.id as string

const isLoadingRecipe = ref(false)
const loadError = ref(false)

const recipe = computed(() => {
  const current = recipeStore.currentRecipe
  if (current?.id === recipeId) return current
  return recipeStore.suggestions.find((r) => r.id === recipeId) ?? null
})

const steps = computed(() => {
  if (!recipe.value) return []
  const normalized = recipe.value.instructions.replace(/\s+(\d+)\.\s+/g, '\n$1. ')
  return normalized
    .split('\n')
    .map((s) => s.replace(/^\d+\.\s*/, '').trim())
    .filter(Boolean)
})

const selectedPortions = ref(0)

watch(recipe, (r) => {
  if (r) selectedPortions.value = r.portions
}, { immediate: true })

function scaleQty(qty: number): string {
  const scaled = qty * (selectedPortions.value / (recipe.value?.portions ?? 1))
  return parseFloat(scaled.toFixed(2)).toString()
}

function normalizeName(s: string): string {
  return s.toLowerCase().trim().replace(/[-_]/g, ' ')
}

function isInInventory(productName: string): boolean {
  const needle = normalizeName(productName)
  return invStore.items.some((item) => {
    const hay = normalizeName(item.productName)
    return hay === needle || hay.includes(needle) || needle.includes(hay)
  })
}

const haveIngredients = computed(
  () => recipe.value?.ingredients.filter((i) => isInInventory(i.productName)) ?? [],
)
const needIngredients = computed(
  () => recipe.value?.ingredients.filter((i) => !isInInventory(i.productName)) ?? [],
)

const primaryInventoryId = computed(() => invStore.inventories[0]?.id)

onMounted(async () => {
  if (!recipe.value) {
    isLoadingRecipe.value = true
    try {
      await recipeStore.fetchRecipeById(recipeId)
    } catch {
      loadError.value = true
    } finally {
      isLoadingRecipe.value = false
    }
  }
  if (invStore.inventories.length === 0) await invStore.fetchInventories()
  for (const inv of invStore.inventories) {
    if (invStore.items.filter((i) => i.inventoryId === inv.id).length === 0) {
      await invStore.fetchItems(inv.id)
    }
  }
})

async function addMissingToList() {
  if (!recipe.value || !primaryInventoryId.value) return
  const userId = auth.localUser?.id ?? ''
  const list = await createShoppingListFromRecipe(
    userId,
    recipe.value.id,
    primaryInventoryId.value,
    `${recipe.value.name} - shopping`,
  )
  shoppingStore.lists.push(list)
  router.push({ name: 'shopping-detail', params: { id: list.id } })
}

async function completeRecipe() {
  if (!recipe.value) return
  await recipeStore.completeRecipe(recipe.value.id)
  router.replace('/')
}

async function toggleSave() {
  if (!recipe.value) return
  await recipeStore.toggleSave(recipe.value.id)
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar
        :title="recipe?.name ?? 'Recipe'"
        :back-route="{ name: 'recipes', query: { tab: route.query.from ?? 'browse' } }"
      >
        <button
          v-if="recipe"
          class="icon-btn"
          :class="{ saved: recipe.isSaved }"
          aria-label="Gem opskrift"
          @click="toggleSave"
        >
          <Heart :size="20" :fill="recipe.isSaved ? 'currentColor' : 'none'" />
        </button>
      </TopBar>
    </template>

    <div v-if="loadError" class="error-state">
      <p>Opskriften kunne ikke hentes.</p>
      <button class="back-btn" @click="router.replace({ name: 'recipes', query: { tab: route.query.from ?? 'browse' } })">
        Gå tilbage
      </button>
    </div>

    <div v-else-if="isLoadingRecipe" class="loading-state">
      <div class="skeleton-card" />
      <div class="skeleton-card" />
      <div class="skeleton-card" />
    </div>

    <div v-else-if="recipe" class="page">
      <!-- Description -->
      <div class="card">
        <div class="recipe-meta">
          <div class="recipe-icon">
            <ChefHat :size="24" />
          </div>
          <div>
            <p class="eyebrow">Opskrift</p>
            <h2>{{ recipe.name }}</h2>
          </div>
        </div>
        <p class="recipe-desc">{{ recipe.description }}</p>
        <div class="portions-row">
          <span class="eyebrow">Portioner</span>
          <div class="portions-stepper">
            <button
              class="stepper-btn"
              :disabled="selectedPortions <= 1"
              aria-label="Færre portioner"
              @click="selectedPortions--"
            >
              <Minus :size="14" />
            </button>
            <span class="stepper-value">{{ selectedPortions }}</span>
            <button
              class="stepper-btn"
              aria-label="Flere portioner"
              @click="selectedPortions++"
            >
              <Plus :size="14" />
            </button>
          </div>
        </div>
      </div>

      <!-- Ingredients -->
      <div class="card">
        <h3>Ingredienser</h3>

        <div v-if="haveIngredients.length > 0">
          <p class="ingredient-group-label">
            <Check :size="14" />
            Du har
          </p>
          <ul class="ingredient-list">
            <li v-for="ing in haveIngredients" :key="ing.productName" class="ingredient-row have">
              <PBadge tone="fresh" dot>{{ ing.productName }}</PBadge>
              <span class="mono ingredient-qty">
                {{ scaleQty(ing.quantity) }}{{ ing.measuringUnit ? ' ' + ing.measuringUnit : '' }}
              </span>
            </li>
          </ul>
        </div>

        <div v-if="needIngredients.length > 0">
          <p class="ingredient-group-label">
            <X :size="14" />
            Du mangler
          </p>
          <ul class="ingredient-list">
            <li v-for="ing in needIngredients" :key="ing.productName" class="ingredient-row need">
              <PBadge tone="neutral">{{ ing.productName }}</PBadge>
              <span class="mono ingredient-qty">
                {{ scaleQty(ing.quantity) }}{{ ing.measuringUnit ? ' ' + ing.measuringUnit : '' }}
              </span>
            </li>
          </ul>
        </div>
      </div>

      <!-- Steps -->
      <div class="card">
        <h3>Fremgangsmåde</h3>
        <ol class="steps-list">
          <li v-for="(step, i) in steps" :key="i" class="step">
            <span class="step-num">{{ i + 1 }}</span>
            <span class="step-text">{{ step }}</span>
          </li>
        </ol>
      </div>

      <!-- CTAs -->
      <div class="cta-stack">
        <PButton v-if="needIngredients.length > 0" variant="secondary" full-width @click="addMissingToList">
          <ShoppingCart :size="16" />
          Tilføj manglende til indkøbsliste
        </PButton>
        <PButton full-width @click="completeRecipe">
          <Check :size="16" />
          Jeg lavede dette — opdater lager
        </PButton>
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

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.recipe-meta {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.recipe-icon {
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

.recipe-desc {
  color: var(--fg-muted);
  font-size: 15px;
}

.portions-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: calc(-1 * var(--space-2));
}

.portions-stepper {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.stepper-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: var(--radius-md);
  border: 1.5px solid var(--border-strong);
  background: var(--surface);
  color: var(--fg);
  cursor: pointer;
  transition: background var(--motion-default), border-color var(--motion-default);
}

.stepper-btn:hover:not(:disabled) {
  border-color: var(--sage-600);
  background: var(--sage-100);
}

.stepper-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.stepper-value {
  font-size: 18px;
  font-weight: 700;
  color: var(--fg);
  min-width: 24px;
  text-align: center;
}

.ingredient-group-label {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--fg-muted);
  margin-bottom: var(--space-2);
}

.ingredient-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-bottom: var(--space-4);
}

.ingredient-list:last-child {
  margin-bottom: 0;
}

.ingredient-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.ingredient-qty {
  color: var(--fg-faint);
  font-size: 12px;
}

.steps-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.step {
  display: flex;
  gap: var(--space-3);
  align-items: flex-start;
}

.step-num {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: var(--sage-100);
  color: var(--sage-700);
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 2px;
}

.step-text {
  font-size: 15px;
  line-height: 22px;
  color: var(--fg);
}

.cta-stack {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding-bottom: var(--space-4);
}

.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-16) var(--space-4);
  text-align: center;
  color: var(--fg-muted);
}

.back-btn {
  padding: var(--space-2) var(--space-4);
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--fg);
  cursor: pointer;
  font-size: 14px;
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: var(--radius-md);
  border: none;
  background: none;
  color: var(--fg-muted);
  cursor: pointer;
  transition: background var(--motion-default), color var(--motion-default);
}

.icon-btn:hover {
  background: var(--surface-raised);
}

.icon-btn.saved {
  color: var(--clay-600);
}

.loading-state {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  padding: var(--space-4);
}

.skeleton-card {
  height: 120px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
}
</style>
