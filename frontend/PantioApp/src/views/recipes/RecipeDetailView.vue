<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Check, X, ShoppingCart, ChefHat } from 'lucide-vue-next'
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

const recipe = computed(
  () => recipeStore.currentRecipe ?? recipeStore.suggestions.find((r) => r.id === recipeId),
)

const steps = computed(() => {
  if (!recipe.value) return []
  return recipe.value.instructions
    .split('\n')
    .map((s) => s.replace(/^\d+\.\s*/, '').trim())
    .filter(Boolean)
})

const haveIngredients = computed(() => recipe.value?.ingredients.filter((i) => i.inInventory) ?? [])
const needIngredients = computed(() => recipe.value?.ingredients.filter((i) => !i.inInventory) ?? [])

const primaryInventoryId = computed(() => invStore.inventories[0]?.id)

onMounted(() => {
  if (!recipe.value) {
    router.replace('/recipes')
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
  router.push('/shopping')
}

async function completeRecipe() {
  if (!recipe.value) return
  await recipeStore.completeRecipe(recipe.value.id)
  router.replace('/')
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar
        :title="recipe?.name ?? 'Recipe'"
        :back-route="{ name: 'recipes' }"
      />
    </template>

    <div v-if="recipe" class="page">
      <!-- Description -->
      <div class="card">
        <div class="recipe-meta">
          <div class="recipe-icon">
            <ChefHat :size="24" />
          </div>
          <div>
            <p class="eyebrow">Recipe</p>
            <h2>{{ recipe.name }}</h2>
          </div>
        </div>
        <p class="recipe-desc">{{ recipe.description }}</p>
        <p class="portions-info eyebrow">{{ recipe.portions }} portion{{ recipe.portions !== 1 ? 's' : '' }}</p>
      </div>

      <!-- Ingredients -->
      <div class="card">
        <h3>Ingredients</h3>

        <div v-if="haveIngredients.length > 0">
          <p class="ingredient-group-label">
            <Check :size="14" />
            You have
          </p>
          <ul class="ingredient-list">
            <li v-for="ing in haveIngredients" :key="ing.productName" class="ingredient-row have">
              <PBadge tone="fresh" dot>{{ ing.productName }}</PBadge>
              <span class="mono ingredient-qty">
                {{ ing.quantity }}{{ ing.measuringUnit ? ' ' + ing.measuringUnit : '' }}
              </span>
            </li>
          </ul>
        </div>

        <div v-if="needIngredients.length > 0">
          <p class="ingredient-group-label">
            <X :size="14" />
            You need
          </p>
          <ul class="ingredient-list">
            <li v-for="ing in needIngredients" :key="ing.productName" class="ingredient-row need">
              <PBadge tone="neutral">{{ ing.productName }}</PBadge>
              <span class="mono ingredient-qty">
                {{ ing.quantity }}{{ ing.measuringUnit ? ' ' + ing.measuringUnit : '' }}
              </span>
            </li>
          </ul>
        </div>
      </div>

      <!-- Steps -->
      <div class="card">
        <h3>Method</h3>
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
          Add missing to shopping list
        </PButton>
        <PButton full-width @click="completeRecipe">
          <Check :size="16" />
          I cooked this — update inventory
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

.portions-info {
  margin-top: calc(-1 * var(--space-2));
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
</style>
