import { runSaga, stdChannel } from 'redux-saga'
import { describe, expect, it, vi } from 'vitest'
import { focusApi } from '../api/state'
import { createTransport } from '../api/transport'
import { sessionChanged } from '../app/auth'
import { bindAuth, createAppStore } from '../app/store'
import { signalsClosed, signalsSaga, signalsViewed } from '../features/sessions/signalsSaga'

describe('Redux account isolation', () => {
  it('clears cached data and ignores a late response from the previous account', async () => {
    let finishOldRequest: (result: { data: string }) => void = () => undefined
    let oldSignal: AbortSignal | undefined
    const testApi = focusApi.injectEndpoints({
      endpoints: (build) => ({
        privateRecord: build.query<string, string>({
          queryFn: (record, { signal }) => {
            if (record === 'cached') return { data: 'old account record' }
            oldSignal = signal
            return new Promise((resolve) => {
              finishOldRequest = resolve
            })
          },
        }),
      }),
    })
    const store = createAppStore()
    const transport = createTransport('https://focus.example')
    const unbind = bindAuth(store, transport)
    const oldTokens = { accessToken: 'old access', refreshToken: 'old refresh', expiresIn: 60 }
    try {
      transport.setSession(oldTokens)
      expect(store.getState().auth.signedIn).toBe(true)
      const cached = store.dispatch(testApi.endpoints.privateRecord.initiate('cached'))
      await expect(cached.unwrap()).resolves.toBe('old account record')
      const pending = store.dispatch(testApi.endpoints.privateRecord.initiate('pending'))
      expect(oldSignal?.aborted).toBe(false)

      transport.setSession(null)
      expect(store.getState().auth.signedIn).toBe(false)
      expect(oldSignal?.aborted).toBe(true)
      transport.setSession({ ...oldTokens, accessToken: 'new access' })
      finishOldRequest({ data: 'late old account record' })
      await pending

      expect(store.getState().auth.signedIn).toBe(true)
      expect(store.getState().focusApi.queries).toEqual({})
      const state = JSON.stringify(store.getState())
      expect(state).not.toContain('new access')
      expect(state).not.toContain('old refresh')
    } finally {
      unbind()
    }
  })
})

describe('session signals saga', () => {
  function tracker() {
    const channel = stdChannel()
    const dispatch = vi.fn()
    const task = runSaga({ channel, dispatch }, signalsSaga)
    return { channel, dispatch, task }
  }

  it('polls until the final telemetry window expires', async () => {
    vi.useFakeTimers()
    const { channel, dispatch, task } = tracker()
    try {
      const endedAt = new Date(Date.now() - 115_000).toISOString()
      channel.put(signalsViewed({ id: 'short', status: 'Completed', endedAt }))
      await vi.advanceTimersByTimeAsync(12_000)
      expect(dispatch).toHaveBeenCalledTimes(2)
      expect(dispatch).toHaveBeenCalledWith(
        focusApi.util.invalidateTags([
          { type: 'Signals', id: 'short' },
          { type: 'Settings', id: 'privacy' },
        ]),
      )
    } finally {
      task.cancel()
      vi.useRealTimers()
    }
  })

  it('replaces an existing poll and stops on navigation or logout', async () => {
    vi.useFakeTimers()
    const { channel, dispatch, task } = tracker()
    try {
      channel.put(signalsViewed({ id: 'old', status: 'Active', endedAt: null }))
      await vi.advanceTimersByTimeAsync(1_000)
      channel.put(signalsViewed({ id: 'new', status: 'Paused', endedAt: null }))
      await vi.advanceTimersByTimeAsync(2_000)
      expect(dispatch).toHaveBeenCalledTimes(1)
      expect(dispatch).toHaveBeenLastCalledWith(
        focusApi.util.invalidateTags([
          { type: 'Signals', id: 'new' },
          { type: 'Settings', id: 'privacy' },
        ]),
      )
      channel.put(signalsClosed())
      await vi.advanceTimersByTimeAsync(4_000)
      expect(dispatch).toHaveBeenCalledTimes(1)
      channel.put(signalsViewed({ id: 'new', status: 'Active', endedAt: null }))
      channel.put(sessionChanged(false))
      await vi.advanceTimersByTimeAsync(4_000)
      expect(dispatch).toHaveBeenCalledTimes(1)
    } finally {
      task.cancel()
      vi.useRealTimers()
    }
  })
})
