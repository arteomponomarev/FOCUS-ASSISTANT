import { z } from 'zod'

export const goalDescription = z
  .string()
  .trim()
  .min(1, 'Enter a goal.')
  .max(500, 'Use at most 500 characters.')

export const createForm = z.object({
  minutes: z
    .number()
    .int('Enter a whole number.')
    .min(1, 'Enter at least one minute.')
    .max(1440, 'Use at most 1440 minutes.'),
  profile: z.string(),
  goals: z.array(z.object({ description: goalDescription })).max(100, 'Use at most 100 goals.'),
})

export const completionForm = z.object({
  concentration: z.number().int().min(1, 'Select a concentration rating.').max(5),
  productivity: z.number().int().min(1, 'Select a productivity rating.').max(5),
  reflection: z.string().max(1000, 'Use at most 1000 characters.'),
})
