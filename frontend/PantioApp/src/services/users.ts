import { apiFetch } from './api'
import type { LocalUser } from './types'

export function ensureUser(email: string, auth0Sub: string): Promise<LocalUser> {
  return apiFetch('/api/users/ensure', {
    method: 'POST',
    body: JSON.stringify({ email, auth0Sub }),
  })
}

export function deleteUser(userId: string): Promise<void> {
  return apiFetch(`/api/users/${userId}`, { method: 'DELETE' })
}
