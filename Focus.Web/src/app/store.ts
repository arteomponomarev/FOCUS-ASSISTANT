import { configureStore } from '@reduxjs/toolkit'
import createSagaMiddleware from 'redux-saga'
import { auth } from '../api/client'
import { focusApi } from '../api/state'
import { signalsSaga } from '../features/sessions/signalsSaga'
import { accountExportSlice } from '../features/settings/accountExport'
import { authSlice, sessionChanged } from './auth'
import { uiSlice } from './ui'

export function createAppStore() {
  const sagaMiddleware = createSagaMiddleware()
  const store = configureStore({
    reducer: {
      auth: authSlice.reducer,
      ui: uiSlice.reducer,
      accountExport: accountExportSlice.reducer,
      [focusApi.reducerPath]: focusApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(focusApi.middleware, sagaMiddleware),
    devTools: import.meta.env.DEV,
  })
  sagaMiddleware.run(signalsSaga)
  return store
}

type AppStore = ReturnType<typeof createAppStore>
export type RootState = ReturnType<AppStore['getState']>
export type AppDispatch = AppStore['dispatch']

export function bindAuth(store: AppStore, transport: Pick<typeof auth, 'snapshot' | 'subscribe'>) {
  const update = () => {
    const { dispatch } = store
    dispatch(focusApi.util.getRunningQueriesThunk()).forEach((request) => request.abort())
    dispatch(focusApi.util.getRunningMutationsThunk()).forEach((request) => request.abort())
    dispatch(focusApi.util.resetApiState())
    dispatch(sessionChanged(transport.snapshot() > 0))
  }
  const unsubscribe = transport.subscribe(update)
  update()
  return unsubscribe
}
