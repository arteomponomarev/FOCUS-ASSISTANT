import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App, { ErrorBoundary } from './app/App'
import { queryClient } from './app/query'
import './styles/base.css'
import './styles/workspace.css'
import './styles/features.css'

const element = document.getElementById('root')
if (!element) throw new Error('Missing application root')
createRoot(element).render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
)
