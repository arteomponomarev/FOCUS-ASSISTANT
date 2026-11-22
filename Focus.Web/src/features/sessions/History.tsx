import { Plus } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { calendarRange } from '../../ui/format'
import { Button, Field, PageTitle, Pagination, Section } from '../../ui/primitives'
import { sessionStatuses, useSessionListQuery } from './data'
import { SessionList } from './SessionList'

export default function History() {
  const [search, setSearch] = useSearchParams()
  const start = search.get('from') ?? ''
  const end = search.get('through') ?? ''
  const status = sessionStatuses.find((value) => value === search.get('status'))
  const sort = search.get('sort') === 'asc' ? 'asc' : 'desc'
  const page = Math.max(1, Math.min(100_000, Math.floor(Number(search.get('page')) || 1)))
  const range = calendarRange(start, end)
  const query = { ...range, Status: status, Sort: sort, Page: page, PageSize: 10 } as const
  const history = useSessionListQuery(query, { skip: !range })
  const data = history.currentData

  function setFilter(key: string, value: string) {
    const updated = new URLSearchParams(search)
    if (value) updated.set(key, value)
    else updated.delete(key)
    if (key !== 'page') updated.delete('page')
    setSearch(updated, { replace: true })
  }

  return (
    <>
      <PageTitle
        title="Sessions"
        action={
          <Link className="button primary" to="/sessions/new">
            <Plus size={19} aria-hidden="true" /> New session
          </Link>
        }
      >
        Work history, goals, and self-reported outcomes.
      </PageTitle>
      <Section title="Session history">
        <div className="form-grid filters">
          <Field
            label="From date"
            type="date"
            value={start}
            onChange={(event) => setFilter('from', event.target.value)}
          />
          <Field
            label="Through date"
            type="date"
            value={end}
            onChange={(event) => setFilter('through', event.target.value)}
          />
          <div className="field">
            <label htmlFor="history-status">Status</label>
            <select
              id="history-status"
              value={status ?? ''}
              onChange={(event) => setFilter('status', event.target.value)}
            >
              <option value="">All statuses</option>
              {sessionStatuses.map((value) => (
                <option key={value}>{value}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label htmlFor="history-sort">Date order</label>
            <select
              id="history-sort"
              value={sort}
              onChange={(event) => setFilter('sort', event.target.value)}
            >
              <option value="desc">Newest first</option>
              <option value="asc">Oldest first</option>
            </select>
          </div>
        </div>
        {!range ? (
          <p className="field-error" role="alert">
            Choose a valid date range.
          </p>
        ) : history.isError ? (
          <Failure error={history.error} retry={() => void history.refetch()} />
        ) : !data ? (
          <Skeleton label="Loading sessions…" />
        ) : (
          <>
            {data.items.length > 0 ? (
              <SessionList items={data.items} />
            ) : (
              <Empty
                title="No matching sessions"
                action={
                  <Button kind="secondary" onClick={() => setSearch({})}>
                    Clear filters
                  </Button>
                }
              >
                Try another date range or create a new session.
              </Empty>
            )}
            <Pagination
              page={page}
              size={10}
              total={data.totalCount}
              setPage={(next) => setFilter('page', String(next))}
            />
          </>
        )}
      </Section>
    </>
  )
}
