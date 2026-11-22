import { useMutation, useQueryClient } from '@tanstack/react-query'
import { LogOut } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, auth, done, profileKey, read } from '../../api/client'
import { ApiError } from '../../api/errors'
import { useProfile } from '../../app/layout'
import { timestamp } from '../../ui/format'
import { Button, Field, PasswordField, Section } from '../../ui/primitives'
import { passwordPolicy } from '../auth/forms'
import { fieldValue, MutationFeedback, PendingData } from './shared'

export function Account() {
  const navigate = useNavigate()
  const profile = useProfile()
  const cache = useQueryClient()
  const [passwordError, setPasswordError] = useState('')
  const rename = useMutation({
    mutationFn: (displayName: string) =>
      read(api.PATCH('/api/v1/auth/me', { body: { displayName } })),
    onSuccess: (value) => cache.setQueryData(profileKey, value),
  })
  const change = useMutation({
    mutationFn: (body: { currentPassword: string; newPassword: string }) =>
      done(api.POST('/api/v1/auth/me/change-password', { body })),
    onSuccess: () => {
      auth.setSession(null)
      navigate('/login', { replace: true, state: { notice: 'Password changed. Sign in again.' } })
    },
  })
  const logout = useMutation({
    mutationFn: () => done(api.POST('/api/v1/auth/logout')),
    onSuccess: () => {
      auth.setSession(null)
      navigate('/login', { replace: true })
    },
  })

  return (
    <div className="stack">
      <Section title="Account profile">
        <PendingData query={profile}>
          {(data) => (
            <form
              className="form-stack"
              onSubmit={(event) => {
                event.preventDefault()
                const value = fieldValue(new FormData(event.currentTarget), 'displayName').trim()
                rename.mutate(value)
              }}
            >
              <dl className="kv">
                <div>
                  <dt>Email</dt>
                  <dd>{data.email}</dd>
                </div>
                <div>
                  <dt>Joined</dt>
                  <dd>{timestamp(data.createdAt)}</dd>
                </div>
              </dl>
              <Field
                label="Display name"
                name="displayName"
                autoComplete="name"
                defaultValue={data.displayName}
                required
                maxLength={100}
              />
              <MutationFeedback error={rename.error} saved={rename.isSuccess} />
              <Button type="submit" busy={rename.isPending}>
                Save display name
              </Button>
            </form>
          )}
        </PendingData>
      </Section>
      <Section
        title="Change password"
        subtitle="Changing your password revokes your active logins."
      >
        <form
          className="form-stack"
          onSubmit={(event) => {
            event.preventDefault()
            setPasswordError('')
            change.reset()
            const form = new FormData(event.currentTarget)
            const currentPassword = fieldValue(form, 'current')
            const newPassword = fieldValue(form, 'next')
            if (!passwordPolicy.safeParse(newPassword).success)
              return setPasswordError('Use a password between 12 and 128 characters.')
            if (newPassword !== fieldValue(form, 'confirmation'))
              return setPasswordError('Passwords do not match.')
            change.mutate({ currentPassword, newPassword })
          }}
        >
          <PasswordField
            label="Current password"
            name="current"
            autoComplete="current-password"
            required
            maxLength={128}
          />
          <div className="form-grid">
            <PasswordField
              label="New password"
              name="next"
              autoComplete="new-password"
              required
              minLength={12}
              maxLength={128}
            />
            <PasswordField
              label="Confirm new password"
              name="confirmation"
              autoComplete="new-password"
              required
              maxLength={128}
              error={passwordError || undefined}
            />
          </div>
          <MutationFeedback
            error={
              change.error instanceof ApiError && change.error.status === 401
                ? new ApiError('Check your current password and try again.', 401)
                : change.error
            }
            saved={false}
          />
          <Button type="submit" busy={change.isPending}>
            Update password
          </Button>
        </form>
      </Section>
      <Section title="Sign out" subtitle="End this login session on the server.">
        <div className="stack">
          <div>
            <Button kind="secondary" busy={logout.isPending} onClick={() => logout.mutate()}>
              <LogOut size={18} aria-hidden="true" /> Sign out
            </Button>
          </div>
          <MutationFeedback error={logout.error} saved={false} />
        </div>
      </Section>
    </div>
  )
}
