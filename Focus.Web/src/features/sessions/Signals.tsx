import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api, read } from '../../api/client'
import type { Model } from '../../api/client'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { duration, timestamp } from '../../ui/format'
import { Button, Pagination, Section } from '../../ui/primitives'
import { sessions } from './data'
import { Badge } from './SessionList'

const tabs = [
  { id: 'timeline', label: 'Timeline' },
  { id: 'estimates', label: 'Estimated states' },
  { id: 'activity', label: 'Activity aggregates' },
] as const
type Tab = (typeof tabs)[number]['id']

export function Signals({
  id,
  status,
  endedAt,
}: {
  id: string
  status: Model['SessionStatus']
  endedAt: string | null
}) {
  const [tab, setTab] = useState<Tab>('timeline')
  const [page, setPage] = useState(1)
  const refetchInterval = () =>
    status === 'Active' ||
    status === 'Paused' ||
    (status === 'Completed' && endedAt && Date.now() - Date.parse(endedAt) < 120_000)
      ? 2_000
      : false
  const privacy = useQuery({
    queryKey: ['settings', 'privacy'],
    queryFn: ({ signal }) => read(api.GET('/api/v1/settings/privacy', { signal })),
    refetchInterval,
  })
  const timeline = useQuery({
    queryKey: ['signals', id, 'timeline', page],
    enabled: tab === 'timeline',
    queryFn: ({ signal }) => sessions.timeline(id, page, signal),
    refetchInterval,
  })
  const estimates = useQuery({
    queryKey: ['signals', id, 'estimates', page],
    enabled: tab === 'estimates',
    queryFn: ({ signal }) => sessions.estimates(id, page, signal),
    refetchInterval,
  })
  const activity = useQuery({
    queryKey: ['signals', id, 'activity', page],
    enabled: tab === 'activity',
    queryFn: ({ signal }) => sessions.activity(id, page, signal),
    refetchInterval,
  })
  const selected = tab === 'timeline' ? timeline : tab === 'estimates' ? estimates : activity
  const total =
    tab === 'timeline'
      ? timeline.data?.events.totalCount
      : tab === 'estimates'
        ? estimates.data?.totalCount
        : activity.data?.totalCount

  return (
    <Section
      title="Session signals"
      subtitle="The Windows Agent supplies desktop activity. This browser only displays received aggregates and estimates."
      action={
        <Button
          type="button"
          kind="secondary"
          onClick={() => {
            void selected.refetch()
            void privacy.refetch()
          }}
        >
          Refresh signals
        </Button>
      }
    >
      {(status === 'Planned' || status === 'Active' || status === 'Paused') && (
        <details>
          <summary className="chart-summary">Connect Windows Agent</summary>
          <p>Sign in to the Agent with this account and enter this session ID.</p>
          <p>
            <code>{id}</code>
          </p>
          <p className="muted">Connection status is unavailable in this browser.</p>
        </details>
      )}
      {privacy.data?.telemetryEnabled === false && (
        <p className="notice">
          Telemetry is disabled.{' '}
          <Link className="button ghost" to="/settings/privacy">
            Open privacy settings
          </Link>
        </p>
      )}
      <div className="actions filters" role="group" aria-label="Signal view">
        {tabs.map(({ id, label }) => (
          <Button
            type="button"
            kind={tab === id ? 'primary' : 'secondary'}
            key={id}
            aria-pressed={tab === id}
            onClick={() => {
              setTab(id)
              setPage(1)
            }}
          >
            {label}
          </Button>
        ))}
      </div>
      {selected.isPending ? (
        <Skeleton label="Loading session signals…" />
      ) : selected.isError ? (
        <Failure error={selected.error} retry={() => void selected.refetch()} />
      ) : (
        <>
          {tab === 'timeline' && timeline.data && (
            <div className="stack">
              <dl className="kv">
                <div>
                  <dt>Created</dt>
                  <dd>{timestamp(timeline.data.createdAt)}</dd>
                </div>
                <div>
                  <dt>Started</dt>
                  <dd>{timestamp(timeline.data.startedAt)}</dd>
                </div>
                <div>
                  <dt>Ended</dt>
                  <dd>{timestamp(timeline.data.endedAt)}</dd>
                </div>
              </dl>
              {timeline.data.pauses.map((pause, index) => (
                <p className="muted" key={index}>
                  Pause: {timestamp(pause.startedAt)} – {timestamp(pause.endedAt)}
                </p>
              ))}
              {timeline.data.events.items.length > 0 && (
                <ol className="timeline">
                  {timeline.data.events.items.map((event) => (
                    <li key={event.id}>
                      <strong>{event.estimatedState ?? 'Observed activity'}</strong>
                      <p className="muted">
                        {timestamp(event.startedAt)} ·{' '}
                        {duration((Date.parse(event.endedAt) - Date.parse(event.startedAt)) / 1000)}
                      </p>
                    </li>
                  ))}
                </ol>
              )}
            </div>
          )}
          {tab === 'estimates' && estimates.data && (
            <ol className="timeline">
              {estimates.data.items.map((estimate) => (
                <li key={estimate.id}>
                  <div className="row">
                    <Badge status={estimate.estimatedState} />
                    <span>{timestamp(estimate.startedAt)}</span>
                  </div>
                  <p>{estimate.reason}</p>
                  <small className="muted">Rule version: {estimate.ruleVersion}</small>
                </li>
              ))}
            </ol>
          )}
          {tab === 'activity' && activity.data && (
            <ol className="timeline">
              {activity.data.items.map((item) => (
                <li key={item.id}>
                  <strong>{item.activeProcessName ?? 'Application unavailable'}</strong>
                  <p className="muted">{timestamp(item.startedAt)}</p>
                  <dl className="kv">
                    <div>
                      <dt>Keyboard events</dt>
                      <dd>{item.keyboardEventCount}</dd>
                    </div>
                    <div>
                      <dt>Mouse clicks</dt>
                      <dd>{item.mouseClickCount}</dd>
                    </div>
                    <div>
                      <dt>Window switches</dt>
                      <dd>{item.windowSwitchCount}</dd>
                    </div>
                    <div>
                      <dt>Idle</dt>
                      <dd>{duration(item.idleSeconds)}</dd>
                    </div>
                  </dl>
                </li>
              ))}
            </ol>
          )}
          {total === 0 && (
            <Empty title="No desktop signals recorded">
              {privacy.data?.telemetryEnabled === false
                ? 'Enable telemetry to collect activity in a current or future session.'
                : selected.isFetching
                  ? 'Checking for incoming uploads.'
                  : 'The Agent may be disconnected or may have pending uploads. Refresh to check again.'}
            </Empty>
          )}
          {total != null && <Pagination page={page} size={10} total={total} setPage={setPage} />}
        </>
      )}
    </Section>
  )
}
