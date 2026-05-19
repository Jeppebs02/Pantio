import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  StoreConnectionDto,
  StoreConnectionSyncResultDto,
  PendingReceiptDto,
  SyncLogDto,
} from '../services/types'
import * as service from '../services/storeConnection'
import { useAuthStore } from './auth'

function normalizeChain(chain: number | string): string {
  if (typeof chain === 'string') return chain
  return ['Netto', 'Fotex', 'Bilka'][chain] ?? String(chain)
}

function normalizeStatus(status: number | string): string {
  if (typeof status === 'string') return status
  return ['Active', 'Disconnected', 'PendingSync'][status] ?? String(status)
}

export const useStoreConnectionStore = defineStore('storeConnection', () => {
  const connections = ref<StoreConnectionDto[]>([])
  const pendingReceipts = ref<PendingReceiptDto[]>([])
  const syncHistory = ref<SyncLogDto[]>([])
  const isSyncing = ref(false)
  const isLoadingPending = ref(false)
  const lastSyncResult = ref<StoreConnectionSyncResultDto | null>(null)

  const nettoConnection = computed(() =>
    connections.value.find((c) => normalizeChain(c.chain) === 'Netto') ?? null,
  )

  const nettoStatus = computed(() => {
    const conn = nettoConnection.value
    if (!conn) return 'disconnected'
    const s = normalizeStatus(conn.status)
    if (s === 'Disconnected') return 'disconnected'
    if (s === 'PendingSync') return 'pending'
    return 'active'
  })

  function userId() {
    return useAuthStore().localUser?.id ?? ''
  }

  async function fetchConnections() {
    connections.value = await service.getStoreConnections(userId())
  }

  async function linkNetto(authorizationCode: string, codeVerifier: string, redirectUri: string) {
    const conn = await service.linkNetto(userId(), authorizationCode, codeVerifier, redirectUri)
    const idx = connections.value.findIndex((c) => normalizeChain(c.chain) === 'Netto')
    if (idx !== -1) connections.value[idx] = conn
    else connections.value.push(conn)
    return conn
  }

  async function fetchPendingReceipts() {
    const conn = nettoConnection.value
    if (!conn) return
    isLoadingPending.value = true
    try {
      pendingReceipts.value = await service.getPendingReceipts(userId(), conn.id)
    } finally {
      isLoadingPending.value = false
    }
  }

  async function importSelected(selectedIds: string[], horizonDate: string | null) {
    const conn = nettoConnection.value
    if (!conn) throw new Error('No Netto connection found')
    isSyncing.value = true
    try {
      lastSyncResult.value = await service.importSelected(userId(), conn.id, {
        selectedDsgReceiptIds: selectedIds,
        importHorizonDate: horizonDate,
      })
      await fetchConnections()
      return lastSyncResult.value
    } finally {
      isSyncing.value = false
    }
  }

  async function sync() {
    const conn = nettoConnection.value
    if (!conn) throw new Error('No Netto connection found')
    isSyncing.value = true
    try {
      lastSyncResult.value = await service.syncConnection(userId(), conn.id)
      await fetchConnections()
      return lastSyncResult.value
    } finally {
      isSyncing.value = false
    }
  }

  async function fetchSyncHistory() {
    const conn = nettoConnection.value
    if (!conn) return
    syncHistory.value = await service.getSyncHistory(userId(), conn.id)
  }

  async function disconnect() {
    const conn = nettoConnection.value
    if (!conn) return
    await service.disconnectStore(userId(), conn.id)
    await fetchConnections()
    pendingReceipts.value = []
    syncHistory.value = []
  }

  return {
    connections,
    pendingReceipts,
    syncHistory,
    isSyncing,
    isLoadingPending,
    lastSyncResult,
    nettoConnection,
    nettoStatus,
    fetchConnections,
    linkNetto,
    fetchPendingReceipts,
    importSelected,
    sync,
    fetchSyncHistory,
    disconnect,
    normalizeChain,
    normalizeStatus,
  }
})
