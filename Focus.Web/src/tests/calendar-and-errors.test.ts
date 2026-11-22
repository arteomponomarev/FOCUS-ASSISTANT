/* @vitest-environment node */
import { describe, expect, it, vi } from 'vitest'
import { normalizeResponse } from '../api/errors'
import { createTransport } from '../api/transport'
import { calendarRange, localDay } from '../ui/format'

describe('date range conversion', () => {
  it.each(['2026-02-29', '2026-02-30', '2026-4-01', '0000-01-01'])(
    'rejects invalid date %s',
    (date) => {
      expect(calendarRange(date, '')).toBeNull()
      expect(calendarRange('', date)).toBeNull()
    },
  )

  it('uses an exclusive end at the next local midnight', () => {
    const range = calendarRange('2028-02-29', '2028-02-29')
    expect(localDay(new Date(range?.From ?? ''))).toBe('2028-02-29')
    expect(localDay(new Date(range?.To ?? ''))).toBe('2028-03-01')
    expect(calendarRange('2028-03-01', '2028-02-29')).toBeNull()
  })
})

describe('client error classification', () => {
  it.each([
    [400, 'Check your entries'],
    [401, 'Please sign in'],
    [403, 'not available'],
    [404, 'no longer available'],
    [409, 'data has changed'],
    [415, 'request format'],
    [429, 'Too many requests'],
    [500, 'server is having trouble'],
  ] as const)('presents a safe message for HTTP %i', (status, message) => {
    const problem = normalizeResponse(new Response(null, { status }), { traceId: 'qa-trace' })
    expect(problem.message).toContain(message)
    expect(problem.traceId).toBe('qa-trace')
  })

  it('distinguishes timeout from a disconnected network', async () => {
    const request = () => new Request('https://localhost/api/v1/sessions')
    const disconnected = createTransport('https://localhost', async () => {
      throw new TypeError('network error')
    })
    await expect(disconnected.fetch(request())).rejects.toMatchObject({
      status: 0,
      message: expect.stringContaining('Could not reach'),
    })
    const timeout = AbortSignal.abort(new DOMException('Timeout', 'TimeoutError'))
    const clock = vi.spyOn(AbortSignal, 'timeout').mockReturnValue(timeout)
    try {
      const timed = createTransport('https://localhost', async ({ signal }) => {
        signal.throwIfAborted()
        throw new Error('Expected an aborted request')
      })
      await expect(timed.fetch(request())).rejects.toMatchObject({
        status: 0,
        message: expect.stringContaining('timed out'),
      })
    } finally {
      clock.mockRestore()
    }
  })

  it('preserves caller cancellation', async () => {
    const abort = new AbortController()
    const request = new Request('https://localhost/api/v1/sessions', { signal: abort.signal })
    abort.abort()
    const transport = createTransport('https://localhost', async ({ signal }) => {
      signal.throwIfAborted()
      throw new Error('Expected cancellation')
    })
    await expect(transport.fetch(request)).rejects.toMatchObject({ name: 'AbortError' })
  })
})
