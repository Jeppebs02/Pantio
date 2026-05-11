import { apiFetch } from './api'
import type { RecipeSuggestionsDto, RecipeDto, RecipeListItemDto } from './types'

export function getRecipeSuggestions(
  userId: string,
  inventoryItemIds: string[],
): Promise<RecipeSuggestionsDto> {
  return apiFetch(`/api/users/${userId}/recipe-suggestions`, {
    method: 'POST',
    body: JSON.stringify({ inventoryItemIds }),
  })
}

export function linkRecipe(recipeId: string, inventoryId: string): Promise<RecipeDto> {
  return apiFetch(`/api/recipes/${recipeId}/link`, {
    method: 'POST',
    body: JSON.stringify({ inventoryId }),
  })
}

export function getRecipeById(userId: string, recipeId: string): Promise<RecipeDto> {
  return apiFetch(`/api/users/${userId}/recipes/${recipeId}`)
}

export function completeRecipe(recipeId: string): Promise<void> {
  return apiFetch(`/api/recipes/${recipeId}/complete`, { method: 'POST' })
}

export function listRecipes(
  userId: string,
  search?: string,
  ingredients?: string[],
): Promise<RecipeListItemDto[]> {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  ingredients?.forEach((i) => params.append('ingredient', i))
  const qs = params.toString()
  return apiFetch(`/api/users/${userId}/recipes${qs ? `?${qs}` : ''}`)
}

export function toggleSave(userId: string, recipeId: string): Promise<{ isSaved: boolean }> {
  return apiFetch(`/api/users/${userId}/recipes/${recipeId}/save`, { method: 'POST' })
}
