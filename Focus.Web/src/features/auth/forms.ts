import { z } from 'zod'

export const loginForm = z.object({
  email: z.email('Enter a valid email address.').max(320),
  password: z.string().min(1, 'Enter your password.').max(128),
})

export const passwordPolicy = z.string().min(12, 'Use at least 12 characters.').max(128)

export const registrationForm = loginForm
  .extend({
    displayName: z.string().trim().min(1, 'Enter your display name.').max(100),
    password: passwordPolicy,
    confirmation: z.string(),
  })
  .refine((value) => value.password === value.confirmation, {
    path: ['confirmation'],
    message: 'Passwords do not match.',
  })
