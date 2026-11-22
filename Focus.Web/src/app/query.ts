import { QueryClient } from '@tanstack/react-query'
import { auth } from '../api/client'
import { ApiError } from '../api/errors'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      networkMode: 'always',
      retry: (count, error) =>
        count < 1 &&
        navigator.onLine &&
        error instanceof ApiError &&
        (error.status === 0 || error.status >= 500),
    },
    mutations: { networkMode: 'always', retry: false },
  },
})

auth.subscribe(() => queryClient.clear())
