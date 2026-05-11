import { apiFetch } from './api'
import type { StoreConnectionDto, StoreConnectionSyncResultDto } from './types'

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

export function syncConnection(
  userId: string,
  connectionId: string,
): Promise<StoreConnectionSyncResultDto> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}/sync`, {
    method: 'POST',
  })
}

export function disconnectStore(userId: string, connectionId: string): Promise<void> {
  return apiFetch(`/api/users/${userId}/store-connections/${connectionId}`, { method: 'DELETE' })
}
