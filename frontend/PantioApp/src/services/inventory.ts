import { apiFetch } from './api'
import type {
  InventoryDto,
  InventoryItemDto,
  CreateInventoryItemRequest,
  UpdateInventoryItemRequest,
  ExpiryDateDto,
  ProductDto,
} from './types'

export function getInventories(userId: string): Promise<InventoryDto[]> {
  return apiFetch(`/api/users/${userId}/inventories`)
}

export function createInventory(userId: string, name: string): Promise<InventoryDto> {
  return apiFetch(`/api/users/${userId}/inventories`, {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
}

export function updateInventory(
  userId: string,
  id: string,
  name: string,
  rowVersion: number,
): Promise<InventoryDto> {
  return apiFetch(`/api/users/${userId}/inventories/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ name, rowVersion }),
  })
}

export function deleteInventory(userId: string, id: string): Promise<void> {
  return apiFetch(`/api/users/${userId}/inventories/${id}`, { method: 'DELETE' })
}

export function getInventoryItems(inventoryId: string): Promise<InventoryItemDto[]> {
  return apiFetch(`/api/inventories/${inventoryId}/items`)
}

export function createInventoryItem(
  inventoryId: string,
  req: CreateInventoryItemRequest,
): Promise<InventoryItemDto> {
  return apiFetch(`/api/inventories/${inventoryId}/items`, {
    method: 'POST',
    body: JSON.stringify(req),
  })
}

export function updateInventoryItem(
  inventoryId: string,
  itemId: string,
  req: UpdateInventoryItemRequest,
): Promise<InventoryItemDto> {
  return apiFetch(`/api/inventories/${inventoryId}/items/${itemId}`, {
    method: 'PUT',
    body: JSON.stringify(req),
  })
}

export function deleteInventoryItem(inventoryId: string, itemId: string): Promise<void> {
  return apiFetch(`/api/inventories/${inventoryId}/items/${itemId}`, { method: 'DELETE' })
}

export function patchExpiryDate(
  inventoryId: string,
  itemId: string,
  overrideDate: string,
): Promise<ExpiryDateDto> {
  return apiFetch(`/api/inventories/${inventoryId}/items/${itemId}/expiry`, {
    method: 'PATCH',
    body: JSON.stringify({ overrideDate }),
  })
}

export function getProductByEan(ean: string): Promise<ProductDto> {
  return apiFetch(`/api/products/${ean}`)
}

export function contributeQuantity(
  ean: string,
  quantity: number,
  quantityUnit: string | null,
): Promise<void> {
  return apiFetch(`/api/products/${ean}/contribute/quantity`, {
    method: 'POST',
    body: JSON.stringify({ quantity, quantityUnit }),
  })
}

export function contributeNewProduct(
  ean: string,
  productName: string,
  quantity: number | null,
  quantityUnit: string | null,
): Promise<void> {
  return apiFetch(`/api/products/${ean}/contribute/new-product`, {
    method: 'POST',
    body: JSON.stringify({ productName, quantity, quantityUnit }),
  })
}

export function contributeNutritionImage(ean: string, image: File): Promise<void> {
  const body = new FormData()
  body.append('image', image)
  return apiFetch(`/api/products/${ean}/contribute/nutrition-image`, {
    method: 'POST',
    body,
  })
}
