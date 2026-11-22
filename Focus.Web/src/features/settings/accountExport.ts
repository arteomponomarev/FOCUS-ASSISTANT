import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { auth } from '../../api/client'
import { serializeApiError } from '../../api/errors'
import type { ApiFailure } from '../../api/errors'
import { sessionChanged } from '../../app/auth'

type ExportState =
  | { status: 'idle' | 'succeeded' }
  | { status: 'pending'; requestId: string }
  | { status: 'failed'; error: ApiFailure }

const idle: ExportState = { status: 'idle' }

export const exportAccount = createAsyncThunk<
  void,
  void,
  { state: { accountExport: ExportState; auth: { signedIn: boolean } }; rejectValue: ApiFailure }
>(
  'accountExport/download',
  async (_, { signal, rejectWithValue }) => {
    const session = new AbortController()
    const unsubscribe = auth.subscribe(() => session.abort())
    const combined = AbortSignal.any([signal, session.signal])
    try {
      const { downloadAccount } = await import('./export')
      combined.throwIfAborted()
      await downloadAccount(combined)
    } catch (error) {
      combined.throwIfAborted()
      return rejectWithValue(serializeApiError(error))
    } finally {
      unsubscribe()
    }
  },
  {
    condition: (_, { getState }) =>
      getState().auth.signedIn && getState().accountExport.status !== 'pending',
  },
)

export const accountExportSlice = createSlice({
  name: 'accountExport',
  initialState: (): ExportState => idle,
  reducers: { exportClosed: (): ExportState => idle },
  extraReducers: (build) => {
    build
      .addCase(sessionChanged, () => idle)
      .addCase(exportAccount.pending, (_, { meta }) => ({
        status: 'pending',
        requestId: meta.requestId,
      }))
      .addCase(exportAccount.fulfilled, (state, { meta }) => {
        if (state.status === 'pending' && state.requestId === meta.requestId)
          return { status: 'succeeded' }
      })
      .addCase(exportAccount.rejected, (state, { meta, payload }) => {
        if (state.status === 'pending' && state.requestId === meta.requestId)
          return payload ? { status: 'failed', error: payload } : idle
      })
  },
})

export const { exportClosed } = accountExportSlice.actions
