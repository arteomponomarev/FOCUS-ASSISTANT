import { CircleAlert, Inbox, LoaderCircle } from 'lucide-react'
import type { ReactNode } from 'react'
import { errorMessage, isApiFailure } from '../api/errors'

export function Busy({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="busy" role="status">
      <LoaderCircle size={20} className="spin" aria-hidden="true" /> {label}
    </div>
  )
}

export function Skeleton({ label = 'Loading content…' }: { label?: string }) {
  return (
    <div className="skeleton-wrap" role="status">
      <span className="sr-only">{label}</span>
      <span className="skeleton" aria-hidden="true" />
      <span className="skeleton short" aria-hidden="true" />
      <span className="skeleton long" aria-hidden="true" />
    </div>
  )
}

export function Empty({
  title,
  children,
  action,
}: {
  title: string
  children?: ReactNode
  action?: ReactNode
}) {
  return (
    <div className="empty">
      <span className="icon-tile">
        <Inbox aria-hidden="true" size={22} />
      </span>
      <h3>{title}</h3>
      {children && <p>{children}</p>}
      {action && <div>{action}</div>}
    </div>
  )
}

export function Failure({ error, retry }: { error: unknown; retry?: () => void }) {
  return (
    <div className="failure" role="alert">
      <CircleAlert size={20} aria-hidden="true" />
      <div>
        <p>{errorMessage(error)}</p>
        {isApiFailure(error) && error.traceId && <small>Request ID: {error.traceId}</small>}
        {retry && (
          <button className="button secondary" type="button" onClick={retry}>
            Try again
          </button>
        )}
      </div>
    </div>
  )
}
