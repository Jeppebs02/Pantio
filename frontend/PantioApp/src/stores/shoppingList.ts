import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ShoppingListDto } from '../services/types'
import * as service from '../services/shoppingList'
import { useAuthStore } from './auth'

export const useShoppingListStore = defineStore('shoppingList', () => {
  const lists = ref<ShoppingListDto[]>([])
  const currentList = ref<ShoppingListDto | null>(null)
  const isLoading = ref(false)

  function userId() {
    return useAuthStore().localUser?.id ?? ''
  }

  async function fetchLists() {
    isLoading.value = true
    try {
      lists.value = await service.getShoppingLists(userId())
      if (lists.value.length > 0 && !currentList.value) {
        currentList.value = lists.value[0]
      }
    } finally {
      isLoading.value = false
    }
  }

  async function createList(name: string) {
    const list = await service.createShoppingList(userId(), name)
    lists.value.push(list)
    currentList.value = list
    return list
  }

  async function deleteList(listId: string) {
    await service.deleteShoppingList(userId(), listId)
    lists.value = lists.value.filter((l) => l.id !== listId)
    if (currentList.value?.id === listId) {
      currentList.value = lists.value[0] ?? null
    }
  }

  async function addItem(listId: string, name: string, quantity?: number | null, measuringUnit?: string | null) {
    const item = await service.addShoppingItem(userId(), listId, name, quantity, measuringUnit)
    const list = lists.value.find((l) => l.id === listId)
    if (list) list.items.push(item)
    if (currentList.value?.id === listId) currentList.value = { ...list! }
    return item
  }

  async function deleteItem(listId: string, itemId: string) {
    await service.deleteShoppingItem(userId(), listId, itemId)
    const list = lists.value.find((l) => l.id === listId)
    if (list) list.items = list.items.filter((i) => i.id !== itemId)
    if (currentList.value?.id === listId) currentList.value = { ...list! }
  }

  async function toggleItem(listId: string, itemId: string) {
    const updated = await service.toggleShoppingItem(userId(), listId, itemId)
    const list = lists.value.find((l) => l.id === listId)
    if (list) {
      const idx = list.items.findIndex((i) => i.id === itemId)
      if (idx !== -1) list.items[idx] = updated
    }
    if (currentList.value?.id === listId) currentList.value = { ...list! }
  }

  function selectList(listId: string) {
    currentList.value = lists.value.find((l) => l.id === listId) ?? null
  }

  return { lists, currentList, isLoading, fetchLists, createList, deleteList, addItem, deleteItem, toggleItem, selectList }
})
