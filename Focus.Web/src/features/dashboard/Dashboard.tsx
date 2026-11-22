import { useQuery } from '@tanstack/react-query'
import { CheckCircle2, Clock3, ListChecks, Plus, Sparkles, Target, TrendingUp } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useProfile } from '../../app/layout'
import { Distribution, Trend } from '../../ui/charts'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { calendarRange, dayLabel, duration, percent, rating, recentRange } from '../../ui/format'
import { PageTitle, Section } from '../../ui/primitives'
import { reports, timeZone } from '../analytics/data'
import { sessions } from '../sessions/data'
import { SessionList } from '../sessions/SessionList'

export default function Dashboard() {
  const profile = useProfile()
  const [days, setDays] = useState(7)
  const params = useMemo(() => {
    const dates = recentRange(days)
    return { ...calendarRange(dates.from, dates.through), TimeZone: timeZone }
  }, [days])
  const overview = useQuery({
    queryKey: ['analytics', 'summary', params],
    queryFn: ({ signal }) => reports.summary(params, signal),
  })
  const series = useQuery({
    queryKey: ['analytics', 'statistics', params, 'Day'],
    queryFn: ({ signal }) => reports.statistics({ ...params, Period: 'Day' }, signal),
  })
  const recent = useQuery({
    queryKey: ['sessions', 'recent'],
    queryFn: ({ signal }) => sessions.list({ PageSize: 5, Sort: 'desc' }, signal),
  })
  const metrics = overview.data?.metrics
  const cards = metrics && [
    { label: 'Active session time', value: duration(metrics.trackedActiveSeconds), Icon: Clock3 },
    { label: 'Sessions completed', value: String(metrics.completedSessions), Icon: CheckCircle2 },
    {
      label: 'Self-reported concentration',
      value: rating(metrics.averageSelfReportedConcentration),
      Icon: Target,
    },
    {
      label: 'Self-reported productivity',
      value: rating(metrics.averageSelfReportedProductivity),
      Icon: TrendingUp,
    },
    {
      label: 'Goal completion',
      value: metrics.goals.total ? percent(metrics.goals.completionPercentage) : '—',
      Icon: ListChecks,
    },
    {
      label: 'Estimated focused time',
      value: Object.values(metrics.estimatedStateSeconds).some((value) => value > 0)
        ? duration(metrics.estimatedStateSeconds.Focused ?? 0)
        : '—',
      Icon: Sparkles,
    },
  ]

  return (
    <>
      <PageTitle
        title="Overview"
        action={
          <div className="field">
            <label htmlFor="overview-range" className="sr-only">
              Date range
            </label>
            <select
              id="overview-range"
              value={days}
              onChange={(event) => setDays(Number(event.target.value))}
            >
              <option value="7">Last 7 days</option>
              <option value="30">Last 30 days</option>
              <option value="90">Last 90 days</option>
            </select>
          </div>
        }
      >
        Your sessions, goals, and research signals at a glance.
      </PageTitle>
      <div className="stack roomy">
        <section className="hero">
          <div className="rings" aria-hidden="true" />
          <div>
            <h2>
              {profile.data?.displayName
                ? 'Welcome, ' + profile.data.displayName
                : 'Make time for focus'}
            </h2>
            <p>Plan a session, give it a goal, and reflect on how it went.</p>
            <Link className="button" to="/sessions/new">
              <Plus size={19} aria-hidden="true" /> New session
            </Link>
          </div>
        </section>
        {profile.isError && <Failure error={profile.error} retry={() => void profile.refetch()} />}
        {overview.isPending ? (
          <div className="card">
            <Skeleton label="Loading overview…" />
          </div>
        ) : overview.isError ? (
          <Failure error={overview.error} retry={() => void overview.refetch()} />
        ) : (
          <div className="metrics">
            {cards?.map(({ label, value, Icon }) => (
              <article className="metric card" key={label}>
                <span className="icon-tile">
                  <Icon size={22} aria-hidden="true" />
                </span>
                <strong>{value}</strong>
                <span>{label}</span>
              </article>
            ))}
          </div>
        )}
        {!overview.isPending && (
          <>
            <div className="panel-grid">
              <Section
                title="Session activity"
                subtitle="Active minutes by day"
                action={
                  <Link to="/analytics" className="button ghost">
                    View analytics
                  </Link>
                }
              >
                {series.isPending ? (
                  <Skeleton />
                ) : series.isError ? (
                  <Failure error={series.error} retry={() => void series.refetch()} />
                ) : series.data.buckets.every(
                    ({ metrics }) => metrics.trackedActiveSeconds === 0,
                  ) ? (
                  <Empty title="No active time yet" />
                ) : (
                  <Trend
                    label="Active session time"
                    unit="Minutes"
                    points={series.data.buckets.map((bucket) => ({
                      label: dayLabel(bucket.from),
                      value: bucket.metrics.trackedActiveSeconds / 60,
                    }))}
                  />
                )}
              </Section>
              <Section title="Estimated states" subtitle="Based on aggregated signals">
                {overview.isError ? (
                  <Failure error={overview.error} retry={() => void overview.refetch()} />
                ) : (
                  <Distribution seconds={overview.data.metrics.estimatedStateSeconds} />
                )}
              </Section>
            </div>
            <Section
              title="Recent sessions"
              action={
                <Link className="button ghost" to="/sessions">
                  View history
                </Link>
              }
            >
              {recent.isPending ? (
                <Skeleton />
              ) : recent.isError ? (
                <Failure error={recent.error} retry={() => void recent.refetch()} />
              ) : recent.data.items.length > 0 ? (
                <SessionList items={recent.data.items} />
              ) : (
                <Empty
                  title="Your first session starts here"
                  action={
                    <Link className="button primary" to="/sessions/new">
                      Plan a session
                    </Link>
                  }
                >
                  Set a goal and save a reflection when you finish.
                </Empty>
              )}
            </Section>
          </>
        )}
      </div>
    </>
  )
}
