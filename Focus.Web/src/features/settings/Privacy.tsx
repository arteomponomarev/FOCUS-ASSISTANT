import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, auth, done } from '../../api/client'
import { ApiError } from '../../api/errors'
import { useAppDispatch, useAppSelector } from '../../app/hooks'
import { Failure } from '../../ui/feedback'
import { timestamp } from '../../ui/format'
import { Button, Dialog, Field, PasswordField, Section } from '../../ui/primitives'
import { usePrivateAction } from '../auth/usePrivateAction'
import { exportAccount, exportClosed } from './accountExport'
import { ComputerVision } from './ComputerVision'
import { usePrivacyQuery, useSaveConsentMutation } from './data'
import { fieldValue, MutationFeedback, PendingData } from './shared'

export function Privacy() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [showDelete, setShowDelete] = useState(false)
  const [confirmation, setConfirmation] = useState('')
  const query = usePrivacyQuery()
  const [saveConsent, consent] = useSaveConsentMutation()
  const exportJob = useAppSelector((state) => state.accountExport)
  const transfer = useRef<ReturnType<ReturnType<typeof exportAccount>>>(null)
  function startExport() {
    if (transfer.current) return
    const task = dispatch(exportAccount())
    transfer.current = task
    void task.then(() => {
      if (transfer.current === task) transfer.current = null
    })
  }
  useEffect(
    () => () => {
      transfer.current?.abort()
      transfer.current = null
      dispatch(exportClosed())
    },
    [dispatch],
  )
  const deletion = usePrivateAction(
    (password: string) =>
      done(api.POST('/api/v1/settings/privacy/delete-account', { body: { password } })),
    () => {
      auth.setSession(null)
      navigate('/login', { replace: true, state: { notice: 'Your account was deleted.' } })
    },
  )

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
                  checked={
                    consent.isLoading
                      ? (consent.originalArgs ?? data.telemetryEnabled)
                      : data.telemetryEnabled
                  }
                  disabled={consent.isLoading}
                  onChange={(event) => void saveConsent(event.target.checked)}
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
      <ComputerVision />
      <Section
        title="Data export"
        subtitle="Download a JSON copy of your profile, settings, sessions, and research aggregates."
      >
        <div className="form-stack">
          <div className="actions">
            <Button kind="secondary" busy={exportJob.status === 'pending'} onClick={startExport}>
              Download account data
            </Button>
            {exportJob.status === 'pending' && (
              <Button kind="ghost" onClick={() => transfer.current?.abort()}>
                Cancel export
              </Button>
            )}
          </div>
          {exportJob.status === 'failed' && <Failure error={exportJob.error} />}
          {exportJob.status === 'succeeded' && (
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
          busy={deletion.isLoading}
        >
          <form
            className="form-stack"
            onSubmit={(event) => {
              event.preventDefault()
              if (confirmation === 'DELETE')
                void deletion.submit(fieldValue(new FormData(event.currentTarget), 'password'))
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
              busy={deletion.isLoading}
            >
              Permanently delete account
            </Button>
          </form>
        </Dialog>
      )}
    </div>
  )
}
