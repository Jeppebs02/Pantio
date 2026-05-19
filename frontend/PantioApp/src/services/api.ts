export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

let getToken: (() => string | null) | null = null

export function registerTokenProvider(provider: () => string | null) {
  getToken = provider
}

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const base = (import.meta.env['VITE_API_BASE_URL'] as string | undefined) ?? ''
  const token = getToken?.()

  const res = await fetch(`${base}${path}`, {
    ...options,
    cache: 'no-store',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  })

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new ApiError(res.status, text)
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}
