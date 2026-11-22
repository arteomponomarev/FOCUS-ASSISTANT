import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, auth, done, read } from '../../api/client'
import { ApiError } from '../../api/errors'
import { Failure } from '../../ui/feedback'
import { timestamp } from '../../ui/format'
import { Button, Dialog, Field, PasswordField, Section } from '../../ui/primitives'
import { downloadAccount } from './export'
import { fieldValue, MutationFeedback, PendingData } from './shared'

export function Privacy() {
  const key = ['settings', 'privacy']
  const cache = useQueryClient()
  const navigate = useNavigate()
  const [showDelete, setShowDelete] = useState(false)
  const [confirmation, setConfirmation] = useState('')
  const transfer = useRef<AbortController>(null)
  const query = useQuery({
    queryKey: key,
    queryFn: ({ signal }) => read(api.GET('/api/v1/settings/privacy', { signal })),
  })
  const consent = useMutation({
    mutationFn: (telemetryEnabled: boolean) =>
      read(api.PATCH('/api/v1/settings/privacy', { body: { telemetryEnabled } })),
    onSuccess: (value) => cache.setQueryData(key, value),
  })
  const exportJob = useMutation({
    mutationFn: async () => {
      const controller = new AbortController()
      transfer.current = controller
      try {
        await downloadAccount(controller.signal)
      } finally {
        transfer.current = null
      }
    },
  })
  useEffect(
    () => () => {
      transfer.current?.abort()
    },
    [],
  )
  const deletion = useMutation({
    mutationFn: (password: string) =>
      done(api.POST('/api/v1/settings/privacy/delete-account', { body: { password } })),
    onSuccess: () => {
      auth.setSession(null)
      navigate('/login', { replace: true, state: { notice: 'Your account was deleted.' } })
    },
  })

  return (
    <div className="stack">
      <Section
        title="Telemetry consent"
        subtitle="Control whether your account accepts aggregate activity from a desktop integration."
      >
        <PendingData query={query}>
          {(data) => (
            <div className="form-stack">
              <label className="toggle-field">
                <input
                  type="checkbox"
                  checked={consent.isPending ? consent.variables : data.telemetryEnabled}
                  disabled={consent.isPending}
                  onChange={(event) => consent.mutate(event.target.checked)}
                />
                <span>Allow aggregate telemetry</span>
              </label>
              {data.consentedAt && (
                <p className="muted">Consent recorded {timestamp(data.consentedAt)}.</p>
              )}
              <MutationFeedback error={consent.error} saved={consent.isSuccess} />
            </div>
          )}
        </PendingData>
      </Section>
      <Section
        title="Data export"
        subtitle="Download a JSON copy of your profile, settings, sessions, and research aggregates."
      >
        <div className="form-stack">
          <div className="actions">
            <Button kind="secondary" busy={exportJob.isPending} onClick={() => exportJob.mutate()}>
              Download account data
            </Button>
            {exportJob.isPending && (
              <Button kind="ghost" onClick={() => transfer.current?.abort()}>
                Cancel export
              </Button>
            )}
          </div>
          {exportJob.isError &&
            !(exportJob.error instanceof DOMException && exportJob.error.name === 'AbortError') && (
              <Failure error={exportJob.error} />
            )}
          {exportJob.isSuccess && (
            <p role="status" className="notice">
              Your export was downloaded.
            </p>
          )}
        </div>
      </Section>
      <Section
        title="Delete account"
        subtitle="Permanently remove your account and the data it owns."
      >
        <Button
          kind="danger"
          onClick={() => {
            deletion.reset()
            setConfirmation('')
            setShowDelete(true)
          }}
        >
          Delete my account
        </Button>
      </Section>
      {showDelete && (
        <Dialog
          title="Delete account permanently"
          close={() => setShowDelete(false)}
          busy={deletion.isPending}
        >
          <form
            className="form-stack"
            onSubmit={(event) => {
              event.preventDefault()
              if (confirmation === 'DELETE')
                deletion.mutate(fieldValue(new FormData(event.currentTarget), 'password'))
            }}
          >
            <p>
              This permanently removes your sessions, settings, and research records from the
              server.
            </p>
            <Field
              label="Type DELETE to confirm"
              value={confirmation}
              onChange={(event) => setConfirmation(event.target.value)}
              required
              autoComplete="off"
            />
            <PasswordField
              label="Current password"
              name="password"
              autoComplete="current-password"
              required
              maxLength={128}
            />
            <MutationFeedback
              error={
                deletion.error instanceof ApiError && deletion.error.status === 401
                  ? new ApiError('Check your current password.', 401)
                  : deletion.error
              }
              saved={false}
            />
            <Button
              kind="danger"
              type="submit"
              disabled={confirmation !== 'DELETE'}
              busy={deletion.isPending}
            >
              Permanently delete account
            </Button>
          </form>
        </Dialog>
      )}
    </div>
  )
}
