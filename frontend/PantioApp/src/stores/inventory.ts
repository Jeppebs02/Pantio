import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { InventoryDto, InventoryItemDto, CreateInventoryItemRequest, UpdateInventoryItemRequest } from '../services/types'
import * as inventoryService from '../services/inventory'
import { useAuthStore } from './auth'

export const useInventoryStore = defineStore('inventory', () => {
  const inventories = ref<InventoryDto[]>([])
  const currentInventory = ref<InventoryDto | null>(null)
  const items = ref<InventoryItemDto[]>([])
  const isLoadingInventories = ref(false)
  const isLoadingItems = ref(false)

  function userId() {
    return useAuthStore().localUser?.id ?? ''
  }

  async function fetchInventories() {
    isLoadingInventories.value = true
    try {
      inventories.value = await inventoryService.getInventories(userId())
    } finally {
      isLoadingInventories.value = false
    }
  }

  async function createInventory(name: string): Promise<InventoryDto> {
    const inv = await inventoryService.createInventory(userId(), name)
    inventories.value.push(inv)
    return inv
  }

  async function deleteInventory(id: string) {
    await inventoryService.deleteInventory(userId(), id)
    inventories.value = inventories.value.filter((i) => i.id !== id)
  }

  async function fetchItems(inventoryId: string) {
    isLoadingItems.value = true
    currentInventory.value = inventories.value.find((i) => i.id === inventoryId) ?? null
    try {
      items.value = await inventoryService.getInventoryItems(inventoryId)
    } finally {
      isLoadingItems.value = false
    }
  }

  async function createItem(inventoryId: string, req: CreateInventoryItemRequest): Promise<InventoryItemDto> {
    const item = await inventoryService.createInventoryItem(inventoryId, req)
    items.value.push(item)
    return item
  }

  async function updateItem(inventoryId: string, itemId: string, req: UpdateInventoryItemRequest): Promise<InventoryItemDto> {
    const updated = await inventoryService.updateInventoryItem(inventoryId, itemId, req)
    const idx = items.value.findIndex((i) => i.id === itemId)
    if (idx !== -1) items.value[idx] = updated
    return updated
  }

  async function deleteItem(inventoryId: string, itemId: string) {
    await inventoryService.deleteInventoryItem(inventoryId, itemId)
    items.value = items.value.filter((i) => i.id !== itemId)
  }

  async function patchExpiry(inventoryId: string, itemId: string, overrideDate: string) {
    const expiry = await inventoryService.patchExpiryDate(inventoryId, itemId, overrideDate)
    const idx = items.value.findIndex((i) => i.id === itemId)
    if (idx !== -1) items.value[idx] = { ...items.value[idx], expiryDate: expiry }
  }

  return {
    inventories,
    currentInventory,
    items,
    isLoadingInventories,
    isLoadingItems,
    fetchInventories,
    createInventory,
    deleteInventory,
    fetchItems,
    createItem,
    updateItem,
    deleteItem,
    patchExpiry,
  }
})
