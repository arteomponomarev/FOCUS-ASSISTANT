import { Component, lazy } from 'react'
import type { ReactNode } from 'react'
import {
  createBrowserRouter,
  Link,
  Navigate,
  RouterProvider,
  useRouteError,
} from 'react-router-dom'
import { Empty } from '../ui/feedback'
import { AppLayout, PublicLayout, RequireSession, useSignedIn } from './layout'

const Login = lazy(() => import('../features/auth/Login'))
const Register = lazy(() => import('../features/auth/Register'))
const Dashboard = lazy(() => import('../features/dashboard/Dashboard'))
const Sessions = lazy(() => import('../features/sessions/History'))
const Create = lazy(() => import('../features/sessions/Create'))
const Detail = lazy(() => import('../features/sessions/Detail'))
const Analytics = lazy(() => import('../features/analytics/Analytics'))
const Settings = lazy(() => import('../features/settings/Settings'))

function Home() {
  return <Navigate to={useSignedIn() ? '/dashboard' : '/login'} replace />
}

function NotFound() {
  return (
    <div className="standalone">
      <h1>Page not found</h1>
      <Empty
        title="This page is unavailable"
        action={
          <Link className="button primary" to="/">
            Back to Focus Assistant
          </Link>
        }
      />
    </div>
  )
}

function RouteError() {
  const error = useRouteError()
  console.error('Focus Assistant route error', error instanceof Error ? error.name : 'Unknown')
  return (
    <div className="standalone" role="alert">
      <h1>This page could not load</h1>
      <Link className="button primary" to="/">
        Back to Focus Assistant
      </Link>
    </div>
  )
}

const router = createBrowserRouter([
  { path: '/', element: <Home />, errorElement: <RouteError /> },
  {
    element: <PublicLayout />,
    errorElement: <RouteError />,
    children: [
      { path: '/login', element: <Login /> },
      { path: '/register', element: <Register /> },
    ],
  },
  {
    element: <RequireSession />,
    errorElement: <RouteError />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: '/dashboard', element: <Dashboard /> },
          { path: '/sessions', element: <Sessions /> },
          { path: '/sessions/new', element: <Create /> },
          { path: '/sessions/:id', element: <Detail /> },
          { path: '/analytics', element: <Analytics /> },
          { path: '/settings/:section?', element: <Settings /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFound />, errorElement: <RouteError /> },
])

export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }
  static getDerivedStateFromError() {
    return { failed: true }
  }
  componentDidCatch(error: Error) {
    console.error('Focus Assistant application error', error.name)
  }
  render() {
    return this.state.failed ? (
      <main className="standalone" role="alert">
        <h1>Focus Assistant could not load</h1>
        <a className="button primary" href="/">
          Reload application
        </a>
      </main>
    ) : (
      this.props.children
    )
  }
}

export default function App() {
  return <RouterProvider router={router} />
}
