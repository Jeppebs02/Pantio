import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { RecipeDto, RecipeListItemDto } from '../services/types'
import * as recipesService from '../services/recipes'
import { useAuthStore } from './auth'

export const useRecipesStore = defineStore('recipes', () => {
  const suggestions = ref<RecipeDto[]>([])
  const currentRecipe = ref<RecipeDto | null>(null)
  const isLoading = ref(false)
  const recipeList = ref<RecipeListItemDto[]>([])
  const isLoadingList = ref(false)

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

  async function linkRecipe(recipeId: string, inventoryIds: string[]) {
    const recipe = await recipesService.linkRecipe(recipeId, inventoryIds)
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

  async function fetchRecipeById(recipeId: string): Promise<RecipeDto> {
    const userId = useAuthStore().localUser?.id ?? ''
    const recipe = await recipesService.getRecipeById(userId, recipeId)
    currentRecipe.value = recipe
    return recipe
  }

  async function fetchRecipeList(search?: string, ingredients?: string[]) {
    const userId = useAuthStore().localUser?.id ?? ''
    isLoadingList.value = true
    try {
      recipeList.value = await recipesService.listRecipes(userId, search, ingredients)
    } finally {
      isLoadingList.value = false
    }
  }

  async function toggleSave(recipeId: string) {
    const userId = useAuthStore().localUser?.id ?? ''
    const { isSaved } = await recipesService.toggleSave(userId, recipeId)
    const listItem = recipeList.value.find((r) => r.id === recipeId)
    if (listItem) listItem.isSaved = isSaved
    const current = currentRecipe.value
    if (current?.id === recipeId) current.isSaved = isSaved
    const suggestion = suggestions.value.find((r) => r.id === recipeId)
    if (suggestion) suggestion.isSaved = isSaved
    return isSaved
  }

  return { suggestions, currentRecipe, isLoading, recipeList, isLoadingList, getSuggestions, linkRecipe, completeRecipe, setCurrentRecipe, fetchRecipeList, fetchRecipeById, toggleSave }
})
