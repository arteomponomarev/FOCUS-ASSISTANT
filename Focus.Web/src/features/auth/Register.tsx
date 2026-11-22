import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import type { z } from 'zod'
import { api, read } from '../../api/client'
import { Failure } from '../../ui/feedback'
import { Button, Field, PasswordField } from '../../ui/primitives'
import { registrationForm } from './forms'

export default function Register() {
  const navigate = useNavigate()
  const [failure, setFailure] = useState<unknown>()
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof registrationForm>>({
    resolver: zodResolver(registrationForm),
    defaultValues: { displayName: '', email: '', password: '', confirmation: '' },
  })
  return (
    <div className="auth-card">
      <h1>Create an account</h1>
      <p className="muted">Start planning sessions in your own workspace.</p>
      <form
        className="form-stack"
        noValidate
        onSubmit={handleSubmit(async ({ displayName, email, password }) => {
          setFailure(undefined)
          try {
            await read(
              api.POST('/api/v1/auth/register', { body: { displayName, email, password } }),
            )
            navigate('/login', { replace: true, state: { registered: true, email } })
          } catch (error) {
            setFailure(error)
          }
        })}
      >
        <Field
          label="Display name"
          autoComplete="name"
          {...register('displayName')}
          error={errors.displayName?.message}
        />
        <Field
          label="Email"
          type="email"
          autoComplete="username"
          {...register('email')}
          error={errors.email?.message}
        />
        <PasswordField
          label="Password"
          autoComplete="new-password"
          {...register('password')}
          error={errors.password?.message}
        />
        <PasswordField
          label="Confirm password"
          autoComplete="new-password"
          {...register('confirmation')}
          error={errors.confirmation?.message}
        />
        {failure !== undefined && <Failure error={failure} />}
        <Button busy={isSubmitting} type="submit">
          Create account
        </Button>
      </form>
      <p>
        Have an account? <Link to="/login">Sign in</Link>
      </p>
    </div>
  )
}
