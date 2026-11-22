const messages: Record<number, string> = {
  400: 'Check your entries and try again.',
  401: 'Please sign in or check your credentials.',
  403: 'This action is not available to your account.',
  404: 'This item is no longer available.',
  409: 'The data has changed or the action is not allowed in the current state.',
  413: 'The request is too large. Reduce its size and try again.',
  415: 'The server could not read this request format.',
  422: 'The requested changes are not valid.',
  429: 'Too many requests. Wait a moment before trying again.',
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly traceId?: string,
    readonly retryAfter?: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export type ApiFailure = {
  status: number
  message: string
  traceId?: string
  retryAfter?: string
}

export function isApiFailure(error: unknown): error is ApiFailure {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    typeof error.status === 'number' &&
    'message' in error &&
    typeof error.message === 'string'
  )
}

export function serializeApiError(error: unknown): ApiFailure {
  if (!isApiFailure(error)) return { status: 500, message: errorMessage(error) }
  const { status, message, traceId, retryAfter } = error
  return { status, message, traceId, retryAfter }
}

export function normalizeResponse(response: Response, problem?: unknown): ApiError {
  const body = typeof problem === 'object' && problem !== null ? problem : {}
  const detail = 'detail' in body ? body.detail : undefined
  const traceId = 'traceId' in body && typeof body.traceId === 'string' ? body.traceId : undefined
  const useDetail = [400, 403, 409, 422].includes(response.status)
  const message =
    useDetail && typeof detail === 'string' && detail.length <= 240
      ? detail
      : (messages[response.status] ??
        (response.status >= 500
          ? 'The server is having trouble. Try again shortly.'
          : 'The request could not be completed.'))

  return new ApiError(
    message,
    response.status,
    response.headers.get('X-Request-Id') ?? traceId,
    response.headers.get('Retry-After') ?? undefined,
  )
}

export function errorMessage(error: unknown) {
  return isApiFailure(error) ? error.message : 'Something went wrong. Please try again.'
}
