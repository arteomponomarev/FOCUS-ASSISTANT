import { useState } from 'react'
import { z } from 'zod'
import type { Model } from '../../api/client'
import { ApiError } from '../../api/errors'
import { Button, Field, Section } from '../../ui/primitives'
import {
  useAvatarQuery,
  usePomodoroQuery,
  useSaveAvatarMutation,
  useSavePomodoroMutation,
} from './data'
import { fieldValue, MutationFeedback, PendingData } from './shared'

const pomodoro = z
  .object({
    focusMinutes: z.number().int().min(1).max(240),
    shortBreakMinutes: z.number().int().min(1).max(120),
    longBreakMinutes: z.number().int().min(1).max(120),
    intervalsBeforeLongBreak: z.number().int().min(1).max(12),
  })
  .refine((value) => value.shortBreakMinutes <= value.longBreakMinutes)

const fields = [
  { key: 'focusMinutes', label: 'Focus duration (minutes)', max: 240 },
  { key: 'shortBreakMinutes', label: 'Short break (minutes)', max: 120 },
  { key: 'longBreakMinutes', label: 'Long break (minutes)', max: 120 },
  { key: 'intervalsBeforeLongBreak', label: 'Intervals before a long break', max: 12 },
] as const

export function Pomodoro() {
  const query = usePomodoroQuery()
  const [savePomodoro, save] = useSavePomodoroMutation()
  const [validation, setValidation] = useState<ApiError>()

  function submit(form: FormData) {
    const values = Object.fromEntries(fields.map(({ key }) => [key, Number(fieldValue(form, key))]))
    const result = pomodoro.safeParse(values)
    if (!result.success) {
      return setValidation(
        new ApiError(
          'Check the durations. The long break must be at least as long as the short break.',
          400,
        ),
      )
    }
    setValidation(undefined)
    void savePomodoro(result.data)
  }
  return (
    <Section title="Pomodoro preferences">
      <PendingData query={query}>
        {(data) => (
          <form
            className="form-stack"
            onSubmit={(event) => {
              event.preventDefault()
              submit(new FormData(event.currentTarget))
            }}
          >
            <div className="form-grid">
              {fields.map(({ key, label, max }) => (
                <Field
                  key={key}
                  name={key}
                  label={label}
                  type="number"
                  inputMode="numeric"
                  defaultValue={data[key]}
                  min={1}
                  max={max}
                  required
                />
              ))}
            </div>
            <MutationFeedback error={validation ?? save.error} saved={save.isSuccess} />
            <Button type="submit" busy={save.isLoading}>
              Save Pomodoro settings
            </Button>
          </form>
        )}
      </PendingData>
    </Section>
  )
}

const visibility = {
  AlwaysVisible: 'Always visible',
  OnlyWhenNeeded: 'Only when needed',
  NotificationsOnly: 'Notifications only',
  Hidden: 'Hidden',
} satisfies Record<Model['AvatarVisibility'], string>
const intensities = [
  'Quiet',
  'Balanced',
  'Active',
] as const satisfies Model['NotificationIntensity'][]
const avatarForm = z.object({
  visibility: z.enum(['AlwaysVisible', 'OnlyWhenNeeded', 'NotificationsOnly', 'Hidden']),
  intensity: z.enum(intensities),
  avatarId: z
    .string()
    .trim()
    .max(80)
    .regex(/^[A-Za-z0-9_-]*$/),
})

export function Assistant() {
  const query = useAvatarQuery()
  const [saveAvatar, save] = useSaveAvatarMutation()
  const [validation, setValidation] = useState<ApiError>()

  function submit(form: FormData) {
    const result = avatarForm.safeParse(Object.fromEntries(form))
    if (!result.success) {
      return setValidation(
        new ApiError(
          'Check the preferences and use only letters, digits, hyphens or underscores for the avatar identifier.',
          400,
        ),
      )
    }
    setValidation(undefined)
    void saveAvatar({ ...result.data, avatarId: result.data.avatarId || null })
  }
  return (
    <Section
      title="Assistant preferences"
      subtitle="These settings are stored for a compatible desktop avatar."
    >
      <PendingData query={query}>
        {(data) => (
          <form
            className="form-stack"
            onSubmit={(event) => {
              event.preventDefault()
              submit(new FormData(event.currentTarget))
            }}
          >
            <div className="form-grid">
              <div className="field">
                <label htmlFor="avatar-visibility">Visibility</label>
                <select id="avatar-visibility" name="visibility" defaultValue={data.visibility}>
                  {Object.entries(visibility).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label htmlFor="avatar-intensity">Notification intensity</label>
                <select id="avatar-intensity" name="intensity" defaultValue={data.intensity}>
                  {intensities.map((value) => (
                    <option key={value}>{value}</option>
                  ))}
                </select>
              </div>
            </div>
            <Field
              label="Avatar identifier"
              name="avatarId"
              defaultValue={data.avatarId ?? ''}
              maxLength={80}
              hint="Optional. Use letters, digits, hyphens, or underscores."
            />
            <MutationFeedback error={validation ?? save.error} saved={save.isSuccess} />
            <Button type="submit" busy={save.isLoading}>
              Save assistant settings
            </Button>
          </form>
        )}
      </PendingData>
    </Section>
  )
}
