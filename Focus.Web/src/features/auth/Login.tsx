import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import type { z } from 'zod'
import { login } from '../../api/client'
import { Failure } from '../../ui/feedback'
import { Button, Field, PasswordField } from '../../ui/primitives'
import { loginForm } from './forms'

export default function Login() {
  const location = useLocation()
  const navigate = useNavigate()
  const [failure, setFailure] = useState<unknown>()
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof loginForm>>({
    resolver: zodResolver(loginForm),
    defaultValues: {
      email: typeof location.state?.email === 'string' ? location.state.email : '',
      password: '',
    },
  })
  return (
    <div className="auth-card">
      <h1>Welcome back</h1>
      <p className="muted">Sign in to your Focus Assistant workspace.</p>
      {location.state?.registered && (
        <p className="notice" role="status">
          Account created. Sign in to continue.
        </p>
      )}
      {typeof location.state?.notice === 'string' && (
        <p className="notice" role="status">
          {location.state.notice}
        </p>
      )}
      <form
        className="form-stack"
        noValidate
        onSubmit={handleSubmit(async (values) => {
          setFailure(undefined)
          try {
            const from: unknown = location.state?.from
            await login(values)
            navigate(
              typeof from === 'string' &&
                /^\/(dashboard|sessions|analytics|settings)(?:[/?]|$)/.test(from)
                ? from
                : '/dashboard',
              { replace: true },
            )
          } catch (error) {
            setFailure(error)
          }
        })}
      >
        <Field
          label="Email"
          type="email"
          autoComplete="username"
          {...register('email')}
          error={errors.email?.message}
        />
        <PasswordField
          label="Password"
          autoComplete="current-password"
          {...register('password')}
          error={errors.password?.message}
        />
        {failure !== undefined && <Failure error={failure} />}
        <Button busy={isSubmitting} type="submit">
          Sign in
        </Button>
      </form>
      <p>
        New here? <Link to="/register">Create an account</Link>
      </p>
    </div>
  )
}
