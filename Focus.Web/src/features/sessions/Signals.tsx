import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { Model } from '../../api/client'
import { useAppDispatch } from '../../app/hooks'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { duration, timestamp } from '../../ui/format'
import { Button, Pagination, Section } from '../../ui/primitives'
import { usePrivacyQuery } from '../settings/data'
import { useActivityQuery, useEstimatesQuery, useTimelineQuery, useVisionQuery } from './data'
import { Badge } from './SessionList'
import { signalsClosed, signalsViewed } from './signalsSaga'
import { VisionSummary } from './VisionSummary'

const tabs = [
  { id: 'timeline', label: 'Timeline' },
  { id: 'estimates', label: 'Estimated states' },
  { id: 'activity', label: 'Activity aggregates' },
  { id: 'vision', label: 'Computer Vision' },
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
  const dispatch = useAppDispatch()
  useEffect(() => {
    dispatch(signalsViewed({ id, status, endedAt }))
    return () => {
      dispatch(signalsClosed())
    }
  }, [dispatch, id, status, endedAt])
  const privacy = usePrivacyQuery()
  const args = { id, page }
  const timeline = useTimelineQuery(args, { skip: tab !== 'timeline' })
  const estimates = useEstimatesQuery(args, { skip: tab !== 'estimates' })
  const activity = useActivityQuery(args, { skip: tab !== 'activity' })
  const vision = useVisionQuery(id, { skip: tab !== 'vision' })
  const selected =
    tab === 'vision'
      ? vision
      : tab === 'timeline'
        ? timeline
        : tab === 'estimates'
          ? estimates
          : activity
  const total =
    tab === 'vision'
      ? undefined
      : tab === 'timeline'
        ? timeline.currentData?.events.totalCount
        : tab === 'estimates'
          ? estimates.currentData?.totalCount
          : activity.currentData?.totalCount

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
      {selected.isError ? (
        <Failure error={selected.error} retry={() => void selected.refetch()} />
      ) : !selected.currentData ? (
        <Skeleton label="Loading session signals…" />
      ) : (
        <>
          {tab === 'timeline' && timeline.currentData && (
            <div className="stack">
              <dl className="kv">
                <div>
                  <dt>Created</dt>
                  <dd>{timestamp(timeline.currentData.createdAt)}</dd>
                </div>
                <div>
                  <dt>Started</dt>
                  <dd>{timestamp(timeline.currentData.startedAt)}</dd>
                </div>
                <div>
                  <dt>Ended</dt>
                  <dd>{timestamp(timeline.currentData.endedAt)}</dd>
                </div>
              </dl>
              {timeline.currentData.pauses.map((pause, index) => (
                <p className="muted" key={index}>
                  Pause: {timestamp(pause.startedAt)} – {timestamp(pause.endedAt)}
                </p>
              ))}
              {timeline.currentData.events.items.length > 0 && (
                <ol className="timeline">
                  {timeline.currentData.events.items.map((event) => (
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
          {tab === 'estimates' && estimates.currentData && (
            <ol className="timeline">
              {estimates.currentData.items.map((estimate) => (
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
          {tab === 'activity' && activity.currentData && (
            <ol className="timeline">
              {activity.currentData.items.map((item) => (
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
          {tab === 'vision' && vision.currentData && (
            <VisionSummary report={vision.currentData} enabled={privacy.data?.cameraEnabled} />
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
