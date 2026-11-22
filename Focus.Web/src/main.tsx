import { setupListeners } from '@reduxjs/toolkit/query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import { auth } from './api/client'
import App, { ErrorBoundary } from './app/App'
import { bindAuth, createAppStore } from './app/store'
import './styles/base.css'
import './styles/workspace.css'
import './styles/features.css'

const element = document.getElementById('root')
if (!element) throw new Error('Missing application root')
const store = createAppStore()
bindAuth(store, auth)
setupListeners(store.dispatch)
createRoot(element).render(
  <StrictMode>
    <ErrorBoundary>
      <Provider store={store}>
        <App />
      </Provider>
    </ErrorBoundary>
  </StrictMode>,
)
