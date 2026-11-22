import { useQuery } from '@tanstack/react-query'
import { BarChart3, LayoutGrid, Settings, Timer } from 'lucide-react'
import { Suspense, useLayoutEffect, useSyncExternalStore } from 'react'
import { Link, Navigate, NavLink, Outlet, useLocation } from 'react-router-dom'
import { api, auth, profileKey, read } from '../api/client'
import { Busy } from '../ui/feedback'
import { ThemeToggle } from './theme'

const links = [
  { to: '/dashboard', label: 'Overview', Icon: LayoutGrid },
  { to: '/sessions', label: 'Sessions', Icon: Timer },
  { to: '/analytics', label: 'Analytics', Icon: BarChart3 },
  { to: '/settings', label: 'Settings', Icon: Settings },
]

export function Brand() {
  return (
    <Link className="brand" to="/" aria-label="Focus Assistant home">
      <img src="/focus.svg" width="38" height="38" alt="" />
      <span>
        Focus<span className="brand-tail"> Assistant</span>
      </span>
    </Link>
  )
}

export function useSignedIn() {
  return useSyncExternalStore(auth.subscribe, auth.snapshot) > 0
}

export function RequireSession() {
  const location = useLocation()
  const signedIn = useSignedIn()
  return signedIn ? (
    <Outlet />
  ) : (
    <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  )
}

export function PublicLayout() {
  const signedIn = useSignedIn()
  if (signedIn) return <Navigate to="/dashboard" replace />
  return (
    <div className="auth-shell">
      <div className="auth-top">
        <Brand />
        <ThemeToggle />
      </div>
      <main id="main-content" className="auth-main">
        <Suspense fallback={<Busy />}>
          <Outlet />
        </Suspense>
      </main>
      <aside className="auth-visual">
        <div className="rings" aria-hidden="true" />
        <div>
          <h2>Make time for focus.</h2>
          <p>Plan sessions, capture your own reflections, and review how your work unfolds.</p>
        </div>
      </aside>
    </div>
  )
}

export function useProfile() {
  return useQuery({
    queryKey: profileKey,
    queryFn: ({ signal }) => read(api.GET('/api/v1/auth/me', { signal })),
  })
}

export function AppLayout() {
  const location = useLocation()
  const profile = useProfile()
  useLayoutEffect(() => {
    window.scrollTo(0, 0)
    document.getElementById('main-content')?.focus({ preventScroll: true })
  }, [location.pathname])

  return (
    <div className="app-shell">
      <a className="skip" href="#main-content">
        Skip to content
      </a>
      <aside className="sidebar">
        <Brand />
        <nav aria-label="Main navigation" className="main-navigation">
          {links.map(({ to, label, Icon }) => (
            <NavLink key={to} to={to}>
              <Icon size={22} aria-hidden="true" />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-foot">
          <Link to="/settings" className="profile-link" aria-label="Account settings">
            <span className="avatar" aria-hidden="true">
              {profile.data?.displayName?.slice(0, 1).toUpperCase() || 'F'}
            </span>
            <span className="profile-name">{profile.data?.displayName || 'Account'}</span>
          </Link>
        </div>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div className="mobile-brand">
            <Brand />
          </div>
          <div className="top-section">
            {links.find(({ to }) => location.pathname.startsWith(to))?.label ?? 'Focus Assistant'}
          </div>
          <ThemeToggle />
        </header>
        <main id="main-content" tabIndex={-1} className="page">
          <Suspense fallback={<Busy />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  )
}
