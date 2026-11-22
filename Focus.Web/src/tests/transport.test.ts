/* @vitest-environment node */
import { describe, expect, it, vi } from 'vitest'
import { normalizeResponse } from '../api/errors'
import { createTransport } from '../api/transport'

const origin = 'https://localhost'
const tokens = { accessToken: 'first', expiresIn: 900, refreshToken: 'first-refresh' }

describe('HTTP integration service', () => {
  it('redacts server exceptions and preserves diagnostic headers', () => {
    const problem = normalizeResponse(
      new Response(null, { status: 500, headers: { 'X-Request-Id': 'trace-1' } }),
      { detail: 'Private database details' },
    )
    expect(problem.message).not.toContain('Private')
    expect(problem.traceId).toBe('trace-1')
    expect(normalizeResponse(new Response(null, { status: 429 })).message).toContain(
      'Too many requests',
    )
  })

  it('shares refresh across requests and retries with the rotated credential', async () => {
    const mock = vi.fn(async (request: Request) => {
      if (request.url.endsWith('/refresh')) {
        return Response.json({
          accessToken: 'second',
          expiresIn: 900,
          refreshToken: 'second-refresh',
        })
      }
      return new Response(null, {
        status: request.headers.get('Authorization') === 'Bearer second' ? 200 : 401,
      })
    })
    const transport = createTransport(origin, mock)
    transport.setSession(tokens)
    const replies = await Promise.all([
      transport.fetch(new Request(origin + '/api/v1/sessions')),
      transport.fetch(new Request(origin + '/api/v1/analytics/summary')),
    ])
    expect(replies.map((reply) => reply.status)).toEqual([200, 200])
    expect(mock.mock.calls.filter(([request]) => request.url.endsWith('/refresh'))).toHaveLength(1)
  })

  it('keeps an authenticated session when a password-change request rejects the current password', async () => {
    const transport = createTransport(origin, async () =>
      Response.json({ detail: 'Invalid credentials.' }, { status: 401 }),
    )
    transport.setSession(tokens)
    const response = await transport.fetch(
      new Request(origin + '/api/v1/auth/me/change-password', { method: 'POST' }),
    )
    expect(response.status).toBe(401)
    expect(transport.snapshot()).toBeGreaterThan(0)
  })

  it('clears browser credentials if the refresh credential has expired', async () => {
    const transport = createTransport(origin, async () => new Response(null, { status: 401 }))
    transport.setSession(tokens)
    await expect(transport.fetch(new Request(origin + '/api/v1/sessions'))).rejects.toMatchObject({
      status: 401,
    })
    expect(transport.snapshot()).toBe(0)
  })
})
