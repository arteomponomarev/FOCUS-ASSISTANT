import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { duration, percent, rating, timestamp } from '../../ui/format'
import { Pagination, Section } from '../../ui/primitives'
import { Badge } from '../sessions/SessionList'
import { useResearchQuery } from './data'

export function SessionResearch({ range }: { range: { From?: string; To?: string } }) {
  const [page, setPage] = useState(1)
  const [expanded, setExpanded] = useState(false)
  const query = useResearchQuery({ range, page }, { skip: !expanded })
  const data = query.currentData
  return (
    <Section title="Session comparison">
      <details onToggle={(event) => setExpanded(event.currentTarget.open)}>
        <summary className="chart-summary">Compare reported ratings, estimates and goals</summary>
        {expanded &&
          (query.isError ? (
            <Failure error={query.error} retry={() => void query.refetch()} />
          ) : !data ? (
            <Skeleton label="Loading session comparison…" />
          ) : data.items.length === 0 && page === 1 ? (
            <Empty title="No sessions in this date range" />
          ) : (
            <div className="stack">
              {data.items.map((row) => {
                const estimated = Object.values(row.estimatedStateSeconds).some(
                  (value) => value > 0,
                )
                return (
                  <article className="tile" key={row.sessionId}>
                    <div className="row space-between">
                      <Link className="button ghost" to={'/sessions/' + row.sessionId}>
                        Session {row.sessionId.slice(0, 8)}
                      </Link>
                      <Badge status={row.status} />
                    </div>
                    <p className="muted">
                      {row.startedAt ? timestamp(row.startedAt) : 'Start time unavailable'}
                    </p>
                    <dl className="kv">
                      <div>
                        <dt>Active work</dt>
                        <dd>{duration(row.activeSeconds)}</dd>
                      </div>
                      <div>
                        <dt>Estimated focused time</dt>
                        <dd>
                          {estimated ? duration(row.estimatedStateSeconds.Focused ?? 0) : '—'}
                        </dd>
                      </div>
                      <div>
                        <dt>Self-reported concentration</dt>
                        <dd>{rating(row.selfReportedConcentration)}</dd>
                      </div>
                      <div>
                        <dt>Self-reported productivity</dt>
                        <dd>{rating(row.selfReportedProductivity)}</dd>
                      </div>
                      <div>
                        <dt>Goal completion</dt>
                        <dd>{row.goals.total ? percent(row.goals.completionPercentage) : '—'}</dd>
                      </div>
                      <div>
                        <dt>Window switches</dt>
                        <dd>{row.windowSwitches}</dd>
                      </div>
                    </dl>
                  </article>
                )
              })}
              <Pagination
                page={page}
                size={data.pageSize}
                total={data.totalCount}
                setPage={setPage}
              />
            </div>
          ))}
      </details>
    </Section>
  )
}
