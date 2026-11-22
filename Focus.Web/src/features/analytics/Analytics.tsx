import { useQuery } from '@tanstack/react-query'
import { useId, useMemo, useState } from 'react'
import type { Model } from '../../api/client'
import { Distribution, Trend } from '../../ui/charts'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { calendarRange, dayLabel, duration, recentRange } from '../../ui/format'
import { Field, PageTitle, Section } from '../../ui/primitives'
import { reports, timeZone } from './data'
import { SessionResearch } from './SessionResearch'

const periods = ['Day', 'Week', 'Month'] as const satisfies Model['ReportPeriod'][]

export default function Analytics() {
  const initial = useMemo(() => recentRange(30), [])
  const [from, setFrom] = useState(initial.from)
  const [through, setThrough] = useState(initial.through)
  const [period, setPeriod] = useState<Model['ReportPeriod']>('Day')
  const filterId = useId()
  const range = calendarRange(from, through)
  const valid =
    !!range?.From &&
    !!range?.To &&
    (Date.parse(range.To) - Date.parse(range.From)) / 86_400_000 <= 366
  const params = { ...range, TimeZone: timeZone }
  const summary = useQuery({
    queryKey: ['analytics', 'summary', params],
    enabled: valid,
    queryFn: ({ signal }) => reports.summary(params, signal),
  })
  const chart = useQuery({
    queryKey: ['analytics', 'statistics', params, period],
    enabled: valid,
    queryFn: ({ signal }) => reports.statistics({ ...params, Period: period }, signal),
  })
  const buckets = chart.data?.buckets ?? []
  const points = (select: (value: Model['ReportMetrics']) => number | null) =>
    buckets.map((bucket) => ({ label: dayLabel(bucket.from), value: select(bucket.metrics) }))
  const total = summary.data?.metrics

  return (
    <>
      <PageTitle title="Analytics">
        Review activity signals, estimated states, and your own ratings.
      </PageTitle>
      <div className="stack roomy">
        <Section title="Report period" subtitle={'Calendar time zone: ' + timeZone}>
          <div className="form-grid">
            <Field
              label="From date"
              type="date"
              value={from}
              onChange={(event) => setFrom(event.target.value)}
            />
            <Field
              label="Through date"
              type="date"
              value={through}
              onChange={(event) => setThrough(event.target.value)}
            />
            <div className="field">
              <label htmlFor={filterId}>Group by</label>
              <select
                id={filterId}
                value={period}
                onChange={(event) =>
                  setPeriod(periods.find((item) => item === event.target.value) ?? 'Day')
                }
              >
                {periods.map((item) => (
                  <option key={item}>{item}</option>
                ))}
              </select>
            </div>
          </div>
          {!valid && (
            <p className="field-error" role="alert">
              Choose a valid date range of up to 366 days.
            </p>
          )}
        </Section>
        <p className="research-note">
          Computer activity provides behavioral signals. Estimated states are heuristic.
          Concentration and productivity ratings are self-reported.
        </p>
        {valid &&
          (chart.isPending || summary.isPending ? (
            <div className="card">
              <Skeleton label="Loading report…" />
            </div>
          ) : (
            <>
              {chart.isError && <Failure error={chart.error} retry={() => void chart.refetch()} />}
              {chart.data &&
                total?.sessions === 0 &&
                total.trackedActiveSeconds === 0 &&
                !Object.values(total.estimatedStateSeconds).some((value) => value > 0) && (
                  <div className="card">
                    <Empty title="No sessions in this period">
                      Select another period or add a session.
                    </Empty>
                  </div>
                )}
              {chart.data && (
                <div className="panel-grid evenly">
                  <Section title="Estimated focused time" subtitle="Duration classified as Focused">
                    <Trend
                      label="Estimated focused time"
                      unit="Minutes"
                      points={points((m) =>
                        Object.values(m.estimatedStateSeconds).some((value) => value > 0)
                          ? (m.estimatedStateSeconds.Focused ?? 0) / 60
                          : null,
                      )}
                    />
                  </Section>
                  <Section title="Goal completion" subtitle="Based on recorded outcomes">
                    <Trend
                      label="Goal completion"
                      unit="Percent"
                      maximum={100}
                      alt
                      points={points((m) => (m.goals.total ? m.goals.completionPercentage : null))}
                    />
                  </Section>
                  <Section title="Self-reported concentration">
                    <Trend
                      label="Concentration"
                      unit="Rating out of 5"
                      maximum={5}
                      points={points((m) => m.averageSelfReportedConcentration)}
                    />
                  </Section>
                  <Section title="Self-reported productivity">
                    <Trend
                      label="Productivity"
                      unit="Rating out of 5"
                      maximum={5}
                      alt
                      points={points((m) => m.averageSelfReportedProductivity)}
                    />
                  </Section>
                  <Section title="Window switches">
                    <Trend
                      label="Window switches"
                      unit="Switches"
                      points={points((m) => m.windowSwitches || null)}
                    />
                  </Section>
                  <Section title="Observed idle time">
                    <Trend
                      label="Observed idle time"
                      unit="Minutes"
                      alt
                      points={points((m) =>
                        m.observedIdleSeconds > 0 ? m.observedIdleSeconds / 60 : null,
                      )}
                    />
                  </Section>
                </div>
              )}
              <Section title="Period totals">
                {summary.isError ? (
                  <Failure error={summary.error} retry={() => void summary.refetch()} />
                ) : (
                  <div className="panel-grid evenly">
                    <dl className="kv">
                      <div>
                        <dt>Active time</dt>
                        <dd>{duration(summary.data.metrics.trackedActiveSeconds)}</dd>
                      </div>
                      <div>
                        <dt>Keyboard events</dt>
                        <dd>{summary.data.metrics.keyboardEvents}</dd>
                      </div>
                      <div>
                        <dt>Mouse clicks</dt>
                        <dd>{summary.data.metrics.mouseClicks}</dd>
                      </div>
                      <div>
                        <dt>Window switches</dt>
                        <dd>{summary.data.metrics.windowSwitches}</dd>
                      </div>
                      <div>
                        <dt>Idle time</dt>
                        <dd>{duration(summary.data.metrics.observedIdleSeconds)}</dd>
                      </div>
                    </dl>
                    <Distribution seconds={summary.data.metrics.estimatedStateSeconds} />
                  </div>
                )}
              </Section>
              <SessionResearch key={range.From + ':' + range.To} range={range} />
            </>
          ))}
      </div>
    </>
  )
}
