import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import type { z } from 'zod'
import type { Model } from '../../api/client'
import { Failure } from '../../ui/feedback'
import { Button, Dialog } from '../../ui/primitives'
import { goalLabel, goalOutcomes } from './data'
import type { GoalOutcome, Session } from './data'
import { completionForm } from './session-form'

function RatingOptions() {
  return (
    <>
      <option value="">Select a rating</option>
      <option value={1}>1 — Very low</option>
      <option value={2}>2 — Low</option>
      <option value={3}>3 — Moderate</option>
      <option value={4}>4 — High</option>
      <option value={5}>5 — Very high</option>
    </>
  )
}

export function Completion({
  session,
  close,
  save,
}: {
  session: Session
  close: () => void
  save: (body: Model['CompleteSessionRequest']) => Promise<void>
}) {
  const form = useForm<z.infer<typeof completionForm>>({
    resolver: zodResolver(completionForm),
    defaultValues: { reflection: '' },
  })
  const [outcomes, setOutcomes] = useState<Record<string, GoalOutcome | ''>>(() =>
    Object.fromEntries(
      session.goals.map((goal) => [
        goal.id,
        goalOutcomes.find((value) => value === goal.status) ?? '',
      ]),
    ),
  )
  const [failure, setFailure] = useState<unknown>()
  const { errors, isSubmitting } = form.formState

  return (
    <Dialog title="Finish session" close={close} busy={isSubmitting}>
      <form
        className="form-stack"
        noValidate
        onSubmit={form.handleSubmit(async (values) => {
          setFailure(undefined)
          try {
            await save({
              selfReportedConcentration: values.concentration,
              selfReportedProductivity: values.productivity,
              reflection: values.reflection.trim() || null,
              goalOutcomes: session.goals.flatMap((goal) => {
                const status = outcomes[goal.id]
                return status ? [{ goalId: goal.id, status }] : []
              }),
            })
          } catch (error) {
            setFailure(error)
          }
        })}
      >
        <p className="muted">Rate your own experience on a scale of 1 to 5.</p>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="rating-concentration">Concentration</label>
            <select
              id="rating-concentration"
              {...form.register('concentration', { setValueAs: Number })}
              aria-invalid={!!errors.concentration}
              aria-describedby={errors.concentration ? 'concentration-error' : undefined}
            >
              <RatingOptions />
            </select>
            {errors.concentration && (
              <small id="concentration-error" className="field-error">
                Select a concentration rating.
              </small>
            )}
          </div>
          <div className="field">
            <label htmlFor="rating-productivity">Productivity</label>
            <select
              id="rating-productivity"
              {...form.register('productivity', { setValueAs: Number })}
              aria-invalid={!!errors.productivity}
              aria-describedby={errors.productivity ? 'productivity-error' : undefined}
            >
              <RatingOptions />
            </select>
            {errors.productivity && (
              <small id="productivity-error" className="field-error">
                Select a productivity rating.
              </small>
            )}
          </div>
        </div>
        {session.goals.length > 0 && (
          <fieldset>
            <legend>Goal outcomes</legend>
            {session.goals.map((goal) => (
              <div className="field" key={goal.id}>
                <label htmlFor={'outcome-' + goal.id}>{goal.description}</label>
                <select
                  id={'outcome-' + goal.id}
                  aria-label={'Outcome for ' + goal.description}
                  value={outcomes[goal.id] ?? ''}
                  onChange={(event) =>
                    setOutcomes({
                      ...outcomes,
                      [goal.id]: goalOutcomes.find((value) => value === event.target.value) ?? '',
                    })
                  }
                >
                  <option value="">Leave pending</option>
                  {goalOutcomes.map((value) => (
                    <option key={value} value={value}>
                      {goalLabel(value)}
                    </option>
                  ))}
                </select>
              </div>
            ))}
          </fieldset>
        )}
        <div className="field">
          <label htmlFor="session-reflection">
            Reflection <span className="muted">(optional)</span>
          </label>
          <textarea id="session-reflection" maxLength={1000} {...form.register('reflection')} />
          {errors.reflection && (
            <small className="field-error" role="alert">
              {errors.reflection.message}
            </small>
          )}
        </div>
        {failure !== undefined && <Failure error={failure} />}
        <div className="actions">
          <Button type="submit" busy={isSubmitting}>
            Save completion
          </Button>
          <Button type="button" kind="ghost" disabled={isSubmitting} onClick={close}>
            Back to session
          </Button>
        </div>
      </form>
    </Dialog>
  )
}
