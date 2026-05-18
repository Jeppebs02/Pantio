import { apiFetch } from './api'
import type {
  StoreConnectionDto,
  StoreConnectionSyncResultDto,
  PendingReceiptDto,
  ImportSelectedReceiptsDto,
  SyncLogDto,
} from './types'

export function getStoreConnections(userId: string): Promise<StoreConnectionDto[]> {
  return apiFetch(`/api/users/${userId}/store-connections`)
}

export function linkNetto(
  userId: string,
  authorizationCode: string,
  codeVerifier: string,
  redirectUri: string,
): Promise<StoreConnectionDto> {
  return apiFetch(`/api/users/${userId}/store-connections/Netto`, {
    method: 'POST',
    body: JSON.stringify({ authorizationCode, codeVerifier, redirectUri }),
  })
}

export function getPendingReceipts(userId: string, connectionId: string): Promise<PendingReceiptDto[]> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}/pending-receipts`)
}

export function importSelected(
  userId: string,
  connectionId: string,
  dto: ImportSelectedReceiptsDto,
): Promise<StoreConnectionSyncResultDto> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}/import`, {
    method: 'POST',
    body: JSON.stringify(dto),
  })
}

export function syncConnection(userId: string, connectionId: string): Promise<StoreConnectionSyncResultDto> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}/sync`, {
    method: 'POST',
  })
}

export function getSyncHistory(userId: string, connectionId: string): Promise<SyncLogDto[]> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}/sync-history`)
}

export function disconnectStore(userId: string, connectionId: string): Promise<void> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}`, { method: 'DELETE' })
}
