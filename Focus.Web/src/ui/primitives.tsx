import { Eye, EyeOff, LoaderCircle, X } from 'lucide-react'
import { useEffect, useId, useRef, useState } from 'react'
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode } from 'react'

export function Button({
  busy,
  kind = 'primary',
  className = '',
  children,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  busy?: boolean
  kind?: 'primary' | 'secondary' | 'ghost' | 'danger'
}) {
  return (
    <button
      {...props}
      className={'button ' + kind + ' ' + className}
      disabled={disabled || busy}
      aria-busy={busy || undefined}
    >
      {busy && <LoaderCircle size={18} className="spin" aria-hidden="true" />}
      {children}
    </button>
  )
}

export function Field({
  label,
  id,
  error,
  hint,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string
  error?: string
  hint?: string
}) {
  const generated = useId()
  const inputId = id ?? generated
  return (
    <div className="field">
      <label htmlFor={inputId}>{label}</label>
      <input
        id={inputId}
        {...props}
        aria-invalid={error ? true : undefined}
        aria-describedby={error || hint ? inputId + '-note' : undefined}
      />
      {(error || hint) && (
        <small id={inputId + '-note'} className={error ? 'field-error' : 'muted'}>
          {error ?? hint}
        </small>
      )}
    </div>
  )
}

export function PasswordField({
  label,
  error,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string }) {
  const id = useId()
  const [visible, setVisible] = useState(false)
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <div className="password-wrap">
        <input
          {...props}
          id={id}
          type={visible ? 'text' : 'password'}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? id + '-error' : undefined}
        />
        <button
          className="icon-button"
          type="button"
          onClick={() => setVisible(!visible)}
          aria-label={visible ? 'Hide password' : 'Show password'}
        >
          {visible ? <EyeOff size={19} aria-hidden="true" /> : <Eye size={19} aria-hidden="true" />}
        </button>
      </div>
      {error && (
        <small className="field-error" id={id + '-error'}>
          {error}
        </small>
      )}
    </div>
  )
}

export function Section({
  title,
  children,
  subtitle,
  action,
}: {
  title: string
  children: ReactNode
  subtitle?: string
  action?: ReactNode
}) {
  return (
    <section className="card">
      <div className="section-head">
        <div>
          <h2>{title}</h2>
          {subtitle && <p>{subtitle}</p>}
        </div>
        {action}
      </div>
      {children}
    </section>
  )
}

export function PageTitle({
  title,
  children,
  action,
}: {
  title: string
  children?: ReactNode
  action?: ReactNode
}) {
  return (
    <header className="page-title">
      <div>
        <h1>{title}</h1>
        {children && <p>{children}</p>}
      </div>
      {action}
    </header>
  )
}

export function Dialog({
  title,
  children,
  close,
  busy = false,
}: {
  title: string
  children: ReactNode
  close: () => void
  busy?: boolean
}) {
  const titleId = useId()
  const ref = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const dialog = ref.current
    const previous = document.activeElement
    dialog?.showModal()
    return () => {
      dialog?.close()
      if (previous instanceof HTMLElement && previous.isConnected) previous.focus()
    }
  }, [])
  return (
    <dialog
      ref={ref}
      className="dialog"
      aria-labelledby={titleId}
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) close()
      }}
    >
      <div className="dialog-head">
        <h2 id={titleId}>{title}</h2>
        <Button
          type="button"
          kind="ghost"
          className="icon-button"
          aria-label="Close dialog"
          disabled={busy}
          onClick={close}
        >
          <X size={22} aria-hidden="true" />
        </Button>
      </div>
      {children}
    </dialog>
  )
}

export function Pagination({
  page,
  total,
  size,
  setPage,
}: {
  page: number
  total: number
  size: number
  setPage: (n: number) => void
}) {
  const last = Math.max(1, Math.ceil(total / size))
  if (last <= 1 && page === 1) return null
  return (
    <nav className="pagination" aria-label="Pagination">
      <Button type="button" kind="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>
        Previous
      </Button>
      <span>
        Page {page} of {last}
      </span>
      <Button
        type="button"
        kind="secondary"
        disabled={page >= last}
        onClick={() => setPage(page + 1)}
      >
        Next
      </Button>
    </nav>
  )
}
