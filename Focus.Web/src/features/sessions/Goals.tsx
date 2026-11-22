import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import type { Model } from '../../api/client'
import { Empty, Failure } from '../../ui/feedback'
import { Button, Dialog, Field, Section } from '../../ui/primitives'
import { goalLabel, goalOutcomes, useChangeGoalMutation } from './data'
import type { Session } from './data'
import { goalDescription } from './session-form'

type Edit = { type: 'add' } | { type: 'edit' | 'remove'; goal: Model['GoalResponse'] }

export function Goals({ session }: { session: Session }) {
  const [editor, setEditor] = useState<Edit>()
  const [description, setDescription] = useState('')
  const [validation, setValidation] = useState<string>()
  const [changeGoal, action] = useChangeGoalMutation()
  const [changeOutcome, outcome] = useChangeGoalMutation()
  const planned = session.status === 'Planned'
  const inProgress = session.status === 'Active' || session.status === 'Paused'

  function open(next: Edit) {
    setEditor(next)
    setDescription(next.type === 'edit' ? next.goal.description : '')
    setValidation(undefined)
    action.reset()
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!editor) return
    const id = session.id
    if (editor.type === 'remove') {
      const result = await changeGoal({ kind: 'remove', id, goalId: editor.goal.id })
      if ('data' in result) setEditor(undefined)
      return
    }
    const parsed = goalDescription.safeParse(description)
    if (!parsed.success) {
      setValidation(parsed.error.issues[0]?.message ?? 'Enter a valid goal.')
      return
    }
    const result = await changeGoal(
      editor.type === 'add'
        ? { kind: 'add', id, description: parsed.data }
        : {
            kind: 'edit',
            id,
            goalId: editor.goal.id,
            body: { description: parsed.data, outcome: null },
          },
    )
    if ('data' in result) setEditor(undefined)
  }

  return (
    <Section
      title="Goals"
      subtitle={
        session.goalStatistics.total > 0
          ? session.goalStatistics.completed + ' of ' + session.goalStatistics.total + ' completed'
          : undefined
      }
      action={
        planned && (
          <Button
            kind="secondary"
            type="button"
            onClick={() => open({ type: 'add' })}
            disabled={session.goals.length >= 100}
          >
            <Plus size={18} aria-hidden="true" /> Add goal
          </Button>
        )
      }
    >
      {session.goals.length === 0 ? (
        <Empty title="No goals">Goals can be added while a session is planned.</Empty>
      ) : (
        <ul className="goal-list">
          {session.goals.map((goal) => (
            <li key={goal.id}>
              <div className="row space-between">
                <strong>{goal.description}</strong>
                <span className="badge">{goalLabel(goal.status)}</span>
              </div>
              {planned && (
                <div className="actions">
                  <Button
                    kind="ghost"
                    onClick={() => open({ type: 'edit', goal })}
                    aria-label={'Edit goal: ' + goal.description}
                  >
                    <Pencil size={18} aria-hidden="true" /> Edit
                  </Button>
                  <Button
                    kind="ghost"
                    onClick={() => open({ type: 'remove', goal })}
                    aria-label={'Delete goal: ' + goal.description}
                  >
                    <Trash2 size={18} aria-hidden="true" /> Delete
                  </Button>
                </div>
              )}
              {inProgress && (
                <div className="field">
                  <label htmlFor={'goal-status-' + goal.id}>Update outcome</label>
                  <select
                    id={'goal-status-' + goal.id}
                    aria-label={'Outcome: ' + goal.description}
                    value={goal.status}
                    disabled={outcome.isLoading}
                    onChange={(event) => {
                      const value = goalOutcomes.find((item) => item === event.target.value)
                      if (value)
                        void changeOutcome({
                          kind: 'edit',
                          id: session.id,
                          goalId: goal.id,
                          body: { description: null, outcome: value },
                        })
                    }}
                  >
                    {goal.status === 'Pending' && (
                      <option value="Pending" disabled>
                        Pending
                      </option>
                    )}
                    {goalOutcomes.map((value) => (
                      <option key={value} value={value}>
                        {goalLabel(value)}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
      {outcome.isError && <Failure error={outcome.error} />}
      {editor && (
        <Dialog
          title={
            editor.type === 'remove'
              ? 'Delete goal'
              : editor.type === 'add'
                ? 'Add goal'
                : 'Edit goal'
          }
          close={() => setEditor(undefined)}
          busy={action.isLoading}
        >
          <form className="form-stack" onSubmit={submit}>
            {editor.type === 'remove' ? (
              <p>Delete “{editor.goal.description}” from this session?</p>
            ) : (
              <Field
                label="Goal description"
                value={description}
                maxLength={500}
                onChange={(event) => setDescription(event.target.value)}
                error={validation}
              />
            )}
            {action.isError && <Failure error={action.error} />}
            <div className="actions">
              <Button
                type="submit"
                busy={action.isLoading}
                kind={editor.type === 'remove' ? 'danger' : 'primary'}
              >
                {editor.type === 'remove' ? 'Delete goal' : 'Save goal'}
              </Button>
              <Button
                type="button"
                kind="ghost"
                disabled={action.isLoading}
                onClick={() => setEditor(undefined)}
              >
                Cancel
              </Button>
            </div>
          </form>
        </Dialog>
      )}
    </Section>
  )
}
