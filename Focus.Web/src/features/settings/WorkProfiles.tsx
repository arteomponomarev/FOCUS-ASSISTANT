import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { z } from 'zod'
import type { Model } from '../../api/client'
import { Empty, Failure } from '../../ui/feedback'
import { Button, Dialog, Field, Pagination, Section } from '../../ui/primitives'
import {
  useDeleteWorkProfileMutation,
  useSaveWorkProfileMutation,
  useWorkProfilesQuery,
} from './data'
import { PendingData } from './shared'

const categories = [
  'Allowed',
  'Neutral',
  'Distracting',
] as const satisfies Model['ProcessCategory'][]
const profileForm = z
  .object({
    name: z.string().trim().min(1, 'Enter a profile name.').max(100),
    description: z.string().max(500),
    focusMinutes: z.number().int().min(1).max(240),
    breakMinutes: z.number().int().min(1).max(120),
    rules: z
      .array(
        z.object({
          processName: z
            .string()
            .trim()
            .max(200)
            .regex(/^[a-zA-Z0-9][a-zA-Z0-9._-]*$/, 'Use a process name without a file path.'),
          category: z.enum(categories),
        }),
      )
      .max(100),
  })
  .refine(
    (form) =>
      new Set(form.rules.map((rule) => rule.processName.toLowerCase())).size === form.rules.length,
    { path: ['rules'], message: 'Use each process name only once.' },
  )

type Profile = Model['WorkProfileResponse']
function Editor({ profile, close }: { profile?: Profile; close: () => void }) {
  const form = useForm<z.infer<typeof profileForm>>({
    resolver: zodResolver(profileForm),
    defaultValues: profile
      ? {
          ...profile,
          description: profile.description ?? '',
          rules: profile.rules.map((rule) => ({ ...rule, category: rule.category ?? 'Neutral' })),
        }
      : { name: '', description: '', focusMinutes: 25, breakMinutes: 5, rules: [] },
  })
  const items = useFieldArray({ name: 'rules', control: form.control })
  const [saveProfile, save] = useSaveWorkProfileMutation()
  const errors = form.formState.errors

  return (
    <Dialog
      title={profile ? 'Edit work profile' : 'Create work profile'}
      close={close}
      busy={save.isLoading}
    >
      <form
        className="form-stack"
        noValidate
        onSubmit={form.handleSubmit(async (values) => {
          const result = await saveProfile({
            body: { ...values, description: values.description.trim() || null },
            id: profile?.id,
          })
          if ('data' in result) close()
        })}
      >
        <Field
          label="Profile name"
          {...form.register('name')}
          error={errors.name?.message}
          maxLength={100}
        />
        <Field
          label="Description"
          {...form.register('description')}
          error={errors.description?.message}
          maxLength={500}
        />
        <div className="form-grid">
          <Field
            label="Focus duration (minutes)"
            type="number"
            {...form.register('focusMinutes', { valueAsNumber: true })}
            error={errors.focusMinutes?.message}
          />
          <Field
            label="Break duration (minutes)"
            type="number"
            {...form.register('breakMinutes', { valueAsNumber: true })}
            error={errors.breakMinutes?.message}
          />
        </div>
        <fieldset>
          <legend>Application rules</legend>
          {items.fields.map((item, index) => (
            <div className="tile" key={item.id}>
              <div className="form-grid">
                <Field
                  label={'Process name ' + (index + 1)}
                  {...form.register(`rules.${index}.processName`)}
                  error={errors.rules?.[index]?.processName?.message}
                />
                <div className="field">
                  <label htmlFor={'category-' + item.id}>Category {index + 1}</label>
                  <select id={'category-' + item.id} {...form.register(`rules.${index}.category`)}>
                    {categories.map((category) => (
                      <option key={category}>{category}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div>
                <Button
                  type="button"
                  kind="ghost"
                  onClick={() => items.remove(index)}
                  aria-label={'Remove rule ' + (index + 1)}
                >
                  <Trash2 size={18} aria-hidden="true" /> Remove rule
                </Button>
              </div>
            </div>
          ))}
          {errors.rules?.message && (
            <p role="alert" className="field-error">
              {errors.rules.message}
            </p>
          )}
          <div>
            <Button
              type="button"
              kind="secondary"
              disabled={items.fields.length >= 100}
              onClick={() => items.append({ processName: '', category: 'Neutral' })}
            >
              <Plus size={18} aria-hidden="true" /> Add rule
            </Button>
          </div>
        </fieldset>
        {save.isError && <Failure error={save.error} />}
        <Button type="submit" busy={save.isLoading}>
          Save profile
        </Button>
      </form>
    </Dialog>
  )
}

export function WorkProfiles() {
  const [page, setPage] = useState(1)
  const [edit, setEdit] = useState<Profile | 'new'>()
  const [remove, setRemove] = useState<Profile>()
  const query = useWorkProfilesQuery(page)
  const [deleteProfile, deletion] = useDeleteWorkProfileMutation()
  async function confirmRemoval(id: string) {
    const wasLast = query.currentData?.items.length === 1
    const result = await deleteProfile(id)
    if ('data' in result) {
      if (wasLast && page > 1) setPage(page - 1)
      setRemove(undefined)
    }
  }
  return (
    <Section
      title="Work profiles"
      subtitle="Set default work and break durations and classify processes."
      action={
        <Button type="button" onClick={() => setEdit('new')}>
          <Plus size={18} aria-hidden="true" /> New profile
        </Button>
      }
    >
      <PendingData query={query}>
        {(data) =>
          data.items.length === 0 ? (
            <Empty title="No work profiles">
              Create a profile to reuse duration and application preferences.
            </Empty>
          ) : (
            <>
              <ul className="rules-list">
                {data.items.map((profile) => (
                  <li key={profile.id}>
                    <div className="row space-between">
                      <h3>{profile.name}</h3>
                      <div className="actions">
                        <Button
                          type="button"
                          kind="secondary"
                          onClick={() => setEdit(profile)}
                          aria-label={'Edit profile: ' + profile.name}
                        >
                          Edit
                        </Button>
                        <Button
                          type="button"
                          kind="ghost"
                          onClick={() => {
                            deletion.reset()
                            setRemove(profile)
                          }}
                          aria-label={'Delete profile: ' + profile.name}
                        >
                          Delete
                        </Button>
                      </div>
                    </div>
                    {profile.description && <p>{profile.description}</p>}
                    <small className="muted">
                      {profile.focusMinutes} min focus · {profile.breakMinutes} min break ·{' '}
                      {profile.rules.length} rules
                    </small>
                  </li>
                ))}
              </ul>
              <Pagination page={page} size={25} total={data.totalCount} setPage={setPage} />
            </>
          )
        }
      </PendingData>
      {edit && (
        <Editor profile={edit === 'new' ? undefined : edit} close={() => setEdit(undefined)} />
      )}
      {remove && (
        <Dialog title="Delete profile" close={() => setRemove(undefined)} busy={deletion.isLoading}>
          <div className="form-stack">
            <p>Delete “{remove.name}” and its application rules?</p>
            {deletion.isError && <Failure error={deletion.error} />}
            <div className="actions">
              <Button
                kind="danger"
                busy={deletion.isLoading}
                onClick={() => void confirmRemoval(remove.id)}
              >
                Delete profile
              </Button>
              <Button
                kind="secondary"
                disabled={deletion.isLoading}
                onClick={() => setRemove(undefined)}
              >
                Keep profile
              </Button>
            </div>
          </div>
        </Dialog>
      )}
    </Section>
  )
}
