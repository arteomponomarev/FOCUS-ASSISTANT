import createClient from 'openapi-fetch'
import type { components, paths } from './schema'
import { ApiError, normalizeResponse } from './errors'
import { createTransport, isTokens } from './transport'

const base = new URL(import.meta.env.VITE_API_BASE_URL || '/', window.location.origin)
if (
  !['https:', 'http:'].includes(base.protocol) ||
  base.username ||
  base.password ||
  base.search ||
  base.hash
) {
  throw new Error('Invalid API base URL')
}
const baseUrl = base.href.replace(/\/$/, '')
export const auth = createTransport(baseUrl)
export const api = createClient<paths>({ baseUrl, fetch: auth.fetch })

type Result<T> = { data?: T; error?: unknown; response: Response }
export async function read<T>(result: Promise<Result<T>>): Promise<T> {
  const { data, error, response } = await result
  if (!response.ok) throw normalizeResponse(response, error)
  if (data === undefined)
    throw new ApiError('The server returned an empty response.', response.status)
  return data
}

export async function done(result: Promise<Result<unknown>>) {
  const { error, response } = await result
  if (!response.ok) throw normalizeResponse(response, error)
}

export type Model = components['schemas']

export async function login(body: Model['LoginRequest']) {
  const data = await read(api.POST('/api/v1/auth/login', { body }))
  if (!isTokens(data)) throw new ApiError('The sign-in response was invalid.', 500)
  auth.setSession(data)
}
