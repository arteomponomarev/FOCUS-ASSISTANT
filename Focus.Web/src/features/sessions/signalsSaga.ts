import { createAction } from '@reduxjs/toolkit'
import { delay, put, takeLatest } from 'redux-saga/effects'
import type { Model } from '../../api/client'
import { focusApi } from '../../api/state'
import { sessionChanged } from '../../app/auth'

export const signalsViewed = createAction<{
  id: string
  status: Model['SessionStatus']
  endedAt: string | null
}>('signals/viewed')
export const signalsClosed = createAction('signals/closed')

const pollIntervalMs = 2_000
const finalWindowMs = 120_000

export function* watchSignals(
  action: ReturnType<typeof signalsViewed | typeof signalsClosed | typeof sessionChanged>,
) {
  if (!signalsViewed.match(action)) return
  const { id, status, endedAt } = action.payload
  let deadline = 0
  if (status === 'Active' || status === 'Paused') deadline = Infinity
  if (status === 'Completed' && endedAt) deadline = Date.parse(endedAt) + finalWindowMs

  while (Date.now() < deadline) {
    yield delay(Math.min(pollIntervalMs, deadline - Date.now()))
    if (Date.now() >= deadline) return
    yield put(
      focusApi.util.invalidateTags([
        { type: 'Signals', id },
        { type: 'Settings', id: 'privacy' },
      ]),
    )
  }
}

export function* signalsSaga() {
  yield takeLatest([signalsViewed.type, signalsClosed.type, sessionChanged.type], watchSignals)
}
