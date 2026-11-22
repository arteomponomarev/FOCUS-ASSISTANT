import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react'
import { ApiError, serializeApiError } from './errors'
import type { ApiFailure } from './errors'

export const focusApi = createApi({
  reducerPath: 'focusApi',
  baseQuery: fakeBaseQuery<ApiFailure>(),
  tagTypes: ['Profile', 'Sessions', 'Session', 'Signals', 'Analytics', 'WorkProfiles', 'Settings'],
  refetchOnMountOrArgChange: 30,
  refetchOnFocus: true,
  endpoints: () => ({}),
})

async function request<T>(
  signal: AbortSignal,
  operation: () => Promise<T>,
  retryRead: boolean,
): Promise<{ data: T } | { error: ApiFailure }> {
  try {
    return { data: await operation() }
  } catch (error) {
    if (
      retryRead &&
      !signal.aborted &&
      navigator.onLine &&
      error instanceof ApiError &&
      (error.status === 0 || error.status >= 500)
    ) {
      return request(signal, operation, false)
    }
    return { error: serializeApiError(error) }
  }
}

export function queryRequest<A, T>(operation: (args: A, signal: AbortSignal) => Promise<T>) {
  return (args: A, { signal }: { signal: AbortSignal }) =>
    request(signal, () => operation(args, signal), true)
}

export function mutationRequest<A, T>(operation: (args: A, signal: AbortSignal) => Promise<T>) {
  return (args: A, { signal }: { signal: AbortSignal }) =>
    request(signal, () => operation(args, signal), false)
}
