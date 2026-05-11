import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { RecipeDto } from '../services/types'
import * as recipesService from '../services/recipes'
import { useAuthStore } from './auth'

export const useRecipesStore = defineStore('recipes', () => {
  const suggestions = ref<RecipeDto[]>([])
  const currentRecipe = ref<RecipeDto | null>(null)
  const isLoading = ref(false)

  async function getSuggestions(inventoryItemIds: string[]) {
    const userId = useAuthStore().localUser?.id ?? ''
    isLoading.value = true
    suggestions.value = []
    try {
      const result = await recipesService.getRecipeSuggestions(userId, inventoryItemIds)
      suggestions.value = result.suggestions
    } finally {
      isLoading.value = false
    }
  }

  async function linkRecipe(recipeId: string, inventoryId: string) {
    const recipe = await recipesService.linkRecipe(recipeId, inventoryId)
    const idx = suggestions.value.findIndex((r) => r.id === recipeId)
    if (idx !== -1) suggestions.value[idx] = recipe
    if (currentRecipe.value?.id === recipeId) currentRecipe.value = recipe
    return recipe
  }

  async function completeRecipe(recipeId: string) {
    await recipesService.completeRecipe(recipeId)
    suggestions.value = suggestions.value.filter((r) => r.id !== recipeId)
    if (currentRecipe.value?.id === recipeId) currentRecipe.value = null
  }

  function setCurrentRecipe(recipe: RecipeDto) {
    currentRecipe.value = recipe
  }

  return { suggestions, currentRecipe, isLoading, getSuggestions, linkRecipe, completeRecipe, setCurrentRecipe }
})
