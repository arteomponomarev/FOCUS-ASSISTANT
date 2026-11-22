import { ApiError, normalizeResponse } from './errors'

export type Tokens = { accessToken: string; refreshToken: string; expiresIn: number }

export function createTransport(
  baseUrl: string,
  send: (request: Request) => Promise<Response> = globalThis.fetch.bind(globalThis),
) {
  let tokens: Tokens | null = null
  let deadline = 0
  let generation = 0
  let refresh: Promise<void> | undefined
  const listeners = new Set<() => void>()

  function assign(value: Tokens | null) {
    tokens = value
    deadline = value ? Date.now() + value.expiresIn * 1000 : 0
  }

  function setSession(value: Tokens | null) {
    if (!tokens && !value) return
    generation++
    refresh = undefined
    assign(value)
    listeners.forEach((notify) => notify())
  }

  async function dispatch(request: Request, accessToken?: string) {
    const headers = new Headers(request.headers)
    if (accessToken) headers.set('Authorization', 'Bearer ' + accessToken)
    const timeout = AbortSignal.timeout(15_000)
    try {
      return await send(
        new Request(request, {
          headers,
          credentials: 'omit',
          redirect: 'error',
          cache: 'no-store',
          signal: AbortSignal.any([request.signal, timeout]),
        }),
      )
    } catch (error) {
      if (request.signal.aborted) throw error
      throw new ApiError(
        timeout.aborted
          ? 'The request timed out. Try again.'
          : 'Could not reach the server. Check your connection.',
        0,
      )
    }
  }

  function renew() {
    if (refresh) return refresh
    if (!tokens) return Promise.reject(new ApiError('Please sign in to continue.', 401))
    const started = generation
    const request = new Request(baseUrl + '/api/v1/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: tokens.refreshToken }),
    })
    refresh = (async () => {
      const result = await dispatch(request)
      if (generation !== started) throw new ApiError('Your session has ended.', 401)
      if (!result.ok) {
        if (result.status === 401) setSession(null)
        throw normalizeResponse(result)
      }
      const body: unknown = await result.json()
      if (!isTokens(body)) {
        setSession(null)
        throw new ApiError('The server returned an invalid session. Please sign in again.', 401)
      }
      if (generation !== started) throw new ApiError('Your session has ended.', 401)
      assign(body)
    })().finally(() => {
      if (generation === started) refresh = undefined
    })
    return refresh
  }

  async function fetch(request: Request): Promise<Response> {
    const path = new URL(request.url).pathname
    if (['login', 'register', 'refresh'].some((name) => path.endsWith('/auth/' + name)))
      return dispatch(request)

    const started = generation
    if (tokens && deadline - Date.now() <= 15_000) await renew()
    const access = tokens?.accessToken
    const copy = request.clone()
    let result = await dispatch(request, access)
    if (generation !== started) throw new ApiError('Your session has ended.', 401)
    if (result.status !== 401 || !access || !tokens) return result

    // These workflows also respond with 401 when the current password is wrong.
    if (path.endsWith('/change-password') || path.endsWith('/delete-account')) {
      const problem: unknown = await result
        .clone()
        .json()
        .catch(() => null)
      if (typeof problem === 'object' && problem !== null && 'detail' in problem && problem.detail)
        return result
    }

    if (tokens.accessToken === access) await renew()
    if (generation !== started) throw new ApiError('Your session has ended.', 401)
    result = await dispatch(copy, tokens?.accessToken)
    if (generation !== started) throw new ApiError('Your session has ended.', 401)
    if (result.status === 401) setSession(null)
    return result
  }

  return {
    fetch,
    setSession,
    snapshot: () => (tokens ? generation : 0),
    subscribe(notify: () => void) {
      listeners.add(notify)
      return () => {
        listeners.delete(notify)
      }
    },
  }
}

export function isTokens(value: unknown): value is Tokens {
  return (
    typeof value === 'object' &&
    value !== null &&
    'accessToken' in value &&
    typeof value.accessToken === 'string' &&
    value.accessToken.length > 0 &&
    'refreshToken' in value &&
    typeof value.refreshToken === 'string' &&
    value.refreshToken.length > 0 &&
    'expiresIn' in value &&
    typeof value.expiresIn === 'number' &&
    Number.isFinite(value.expiresIn) &&
    value.expiresIn > 0
  )
}
