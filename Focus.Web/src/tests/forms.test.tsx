import { screen, render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/errors'
import Login from '../features/auth/Login'
import { registrationForm } from '../features/auth/forms'
import { createForm } from '../features/sessions/session-form'
import { SessionActions } from '../features/sessions/SessionActions'
import { Failure } from '../ui/feedback'

describe('session and authentication forms', () => {
  it('announces invalid login values and lets the user reveal the password', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>,
    )
    await user.type(screen.getByRole('textbox', { name: 'Email' }), 'invalid-email')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(await screen.findByText('Enter a valid email address.')).toBeVisible()
    expect(screen.getByLabelText('Password', { exact: true })).toHaveAttribute('type', 'password')
    await user.click(screen.getByRole('button', { name: 'Show password' }))
    expect(screen.getByLabelText('Password', { exact: true })).toHaveAttribute('type', 'text')
  })

  it('rejects password confirmation mismatches', () => {
    const result = registrationForm.safeParse({
      displayName: 'Researcher',
      email: 'sample@example.invalid',
      password: 'synthetic fixture password',
      confirmation: 'another value',
    })
    expect(result.success).toBe(false)
    if (!result.success) expect(result.error.issues[0]?.path).toEqual(['confirmation'])
  })

  it('checks planned minutes and trims session goal text', () => {
    expect(createForm.safeParse({ minutes: 0, profile: '', goals: [] }).success).toBe(false)
    expect(
      createForm.safeParse({ minutes: 30, profile: '', goals: [{ description: '  ' }] }).success,
    ).toBe(false)
    expect(
      createForm.parse({ minutes: 30, profile: '', goals: [{ description: '  Draft notes ' }] })
        .goals,
    ).toEqual([{ description: 'Draft notes' }])
  })

  it('shows only valid controls and emits the selected command', async () => {
    const action = vi.fn()
    const { rerender } = render(<SessionActions status="Paused" onAction={action} />)
    expect(screen.queryByRole('button', { name: 'Pause' })).not.toBeInTheDocument()
    await userEvent.setup().click(screen.getByRole('button', { name: 'Resume' }))
    expect(action).toHaveBeenCalledWith('resume')
    rerender(<SessionActions status="Completed" onAction={action} />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })
})

describe('API failure feedback', () => {
  it('announces a request failure and lets the user retry', async () => {
    const retry = vi.fn()
    render(
      <Failure
        error={new ApiError('The server is unavailable.', 503, 'test-request')}
        retry={retry}
      />,
    )

    const alert = screen.getByRole('alert')
    expect(alert).toHaveTextContent('The server is unavailable.')
    expect(alert).toHaveTextContent('Request ID: test-request')
    await userEvent.setup().click(screen.getByRole('button', { name: 'Try again' }))
    expect(retry).toHaveBeenCalledTimes(1)
  })
})
