import { apiFetch } from './api'
import type { RecipeSuggestionsDto, RecipeDto } from './types'

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

export function completeRecipe(recipeId: string): Promise<void> {
  return apiFetch(`/api/recipes/${recipeId}/complete`, { method: 'POST' })
}
