import type { ReactNode } from 'react'
import { Failure, Skeleton } from '../../ui/feedback'

export function PendingData<T>({
  query,
  children,
}: {
  query: { isPending: boolean; isError: boolean; error: unknown; data?: T; refetch: () => unknown }
  children: (data: T) => ReactNode
}) {
  if (query.isPending) return <Skeleton label="Loading settings…" />
  if (query.isError) return <Failure error={query.error} retry={() => void query.refetch()} />
  return query.data === undefined ? null : children(query.data)
}

export function MutationFeedback({ error, saved }: { error: unknown; saved: boolean }) {
  return error ? (
    <Failure error={error} />
  ) : saved ? (
    <p className="notice" role="status">
      Changes saved.
    </p>
  ) : null
}

export function fieldValue(form: FormData, key: string) {
  const value = form.get(key)
  return typeof value === 'string' ? value : ''
}
