import { apiFetch } from './api'
import type { ShoppingListDto, ShoppingListItemDto } from './types'

export function getShoppingLists(userId: string): Promise<ShoppingListDto[]> {
  return apiFetch(`/api/users/${userId}/shopping-lists`)
}

export function createShoppingList(userId: string, name: string): Promise<ShoppingListDto> {
  return apiFetch(`/api/users/${userId}/shopping-lists`, {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
}

export function createShoppingListFromRecipe(
  userId: string,
  recipeId: string,
  inventoryId: string,
  name?: string,
): Promise<ShoppingListDto> {
  return apiFetch(`/api/users/${userId}/shopping-lists/from-recipe`, {
    method: 'POST',
    body: JSON.stringify({ recipeId, inventoryId, name }),
  })
}

export function deleteShoppingList(userId: string, listId: string): Promise<void> {
  return apiFetch(`/api/users/${userId}/shopping-lists/${listId}`, { method: 'DELETE' })
}

export function addShoppingItem(
  userId: string,
  listId: string,
  name: string,
  quantity?: number | null,
  measuringUnit?: string | null,
): Promise<ShoppingListItemDto> {
  return apiFetch(`/api/users/${userId}/shopping-lists/${listId}/items`, {
    method: 'POST',
    body: JSON.stringify({ name, quantity, measuringUnit }),
  })
}

export function deleteShoppingItem(
  userId: string,
  listId: string,
  itemId: string,
): Promise<void> {
  return apiFetch(`/api/users/${userId}/shopping-lists/${listId}/items/${itemId}`, {
    method: 'DELETE',
  })
}

export function toggleShoppingItem(
  userId: string,
  listId: string,
  itemId: string,
): Promise<ShoppingListItemDto> {
  return apiFetch(`/api/users/${userId}/shopping-lists/${listId}/items/${itemId}/toggle`, {
    method: 'PATCH',
  })
}
