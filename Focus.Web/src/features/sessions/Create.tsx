import { zodResolver } from '@hookform/resolvers/zod'
import { useInfiniteQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import type { z } from 'zod'
import { Failure } from '../../ui/feedback'
import { Button, Field, PageTitle, Section } from '../../ui/primitives'
import { profiles } from '../settings/data'
import { refreshSessionCollections, sessions } from './data'
import { createForm } from './session-form'

export default function Create() {
  const navigate = useNavigate()
  const cache = useQueryClient()
  const [error, setError] = useState<unknown>()
  const form = useForm<z.infer<typeof createForm>>({
    resolver: zodResolver(createForm),
    defaultValues: { minutes: 25, profile: '', goals: [] },
  })
  const goals = useFieldArray({ control: form.control, name: 'goals' })
  const options = useInfiniteQuery({
    queryKey: ['profiles', 'picker'],
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) => profiles.list(pageParam, signal),
    getNextPageParam: (last, pages) =>
      last.items.length && pages.length * last.pageSize < last.totalCount
        ? pages.length + 1
        : undefined,
  })
  const profileOptions = options.data?.pages.flatMap((page) => page.items)
  const { errors, isSubmitting } = form.formState

  return (
    <>
      <PageTitle title="New session">
        Set aside time for a task and define what you want to accomplish.
      </PageTitle>
      <div className="settings-main">
        <Section title="Session plan">
          <form
            className="form-stack"
            noValidate
            onSubmit={form.handleSubmit(async (values) => {
              setError(undefined)
              try {
                const item = await sessions.create({
                  plannedDurationSeconds: values.minutes * 60,
                  workProfileId: values.profile || null,
                  goals: values.goals.map((goal) => goal.description),
                })
                refreshSessionCollections(cache)
                navigate('/sessions/' + item.id)
              } catch (e) {
                setError(e)
              }
            })}
          >
            <div className="form-grid">
              <Field
                label="Planned duration (minutes)"
                type="number"
                min="1"
                max="1440"
                inputMode="numeric"
                {...form.register('minutes', { valueAsNumber: true })}
                error={errors.minutes?.message}
              />
              <div className="field">
                <label htmlFor="work-profile">Work profile</label>
                <select id="work-profile" {...form.register('profile')}>
                  <option value="">
                    {options.isPending ? 'Loading profiles…' : 'No work profile'}
                  </option>
                  {profileOptions?.map((profile) => (
                    <option key={profile.id} value={profile.id}>
                      {profile.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            {options.hasNextPage && (
              <Button
                type="button"
                kind="secondary"
                busy={options.isFetchingNextPage}
                onClick={() => void options.fetchNextPage()}
              >
                Load more work profiles
              </Button>
            )}
            {options.isError && (
              <Failure error={options.error} retry={() => void options.refetch()} />
            )}
            <fieldset>
              <legend>Goals</legend>
              {goals.fields.map((goal, index) => (
                <div className="row" key={goal.id}>
                  <div className="grow">
                    <Field
                      label={'Goal ' + (index + 1)}
                      maxLength={500}
                      {...form.register(`goals.${index}.description`)}
                      error={errors.goals?.[index]?.description?.message}
                    />
                  </div>
                  <Button
                    kind="ghost"
                    type="button"
                    aria-label={'Remove goal ' + (index + 1)}
                    onClick={() => goals.remove(index)}
                  >
                    <Trash2 size={19} aria-hidden="true" />
                  </Button>
                </div>
              ))}
              {errors.goals?.message && (
                <p className="field-error" role="alert">
                  {errors.goals.message}
                </p>
              )}
              <div>
                <Button
                  type="button"
                  kind="secondary"
                  disabled={goals.fields.length >= 100}
                  onClick={() => goals.append({ description: '' })}
                >
                  <Plus size={18} aria-hidden="true" /> Add goal
                </Button>
              </div>
            </fieldset>
            {error !== undefined && <Failure error={error} />}
            <div className="actions">
              <Button type="submit" busy={isSubmitting}>
                Create session
              </Button>
              <Link className="button ghost" to="/sessions">
                Cancel
              </Link>
            </div>
          </form>
        </Section>
      </div>
    </>
  )
}
