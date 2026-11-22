import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import type { Model } from '../../api/client'
import { Empty, Failure, Skeleton } from '../../ui/feedback'
import { clockLabel, duration, percent, rating, timestamp } from '../../ui/format'
import { Button, Dialog, PageTitle, Section } from '../../ui/primitives'
import { Completion } from './Completion'
import { detailKey, refreshSessionCollections, sessionTitle, sessions } from './data'
import { Goals } from './Goals'
import { SessionActions } from './SessionActions'
import type { SessionAction } from './SessionActions'
import { Badge } from './SessionList'
import { Signals } from './Signals'

function useElapsed(active: boolean, revision: number) {
  const [tick, setTick] = useState({ revision: 0, seconds: 0 })
  useEffect(() => {
    if (!active) return
    const start = performance.now()
    const id = window.setInterval(() => {
      setTick({ revision, seconds: (performance.now() - start) / 1000 })
    }, 1000)
    return () => window.clearInterval(id)
  }, [active, revision])
  return active && tick.revision === revision ? tick.seconds : 0
}

export default function Detail() {
  const { id = '' } = useParams()
  const valid = /^[a-f\d]{8}(-[a-f\d]{4}){3}-[a-f\d]{12}$/i.test(id)
  const cache = useQueryClient()
  const [modal, setModal] = useState<'complete' | 'cancel'>()
  const query = useQuery({
    queryKey: detailKey(id),
    enabled: valid,
    queryFn: ({ signal }) => sessions.get(id, signal),
    refetchInterval: (state) => (state.state.data?.status === 'Active' ? 20_000 : false),
  })
  const session = query.data
  const elapsed = useElapsed(session?.status === 'Active', query.dataUpdatedAt)

  function changed() {
    void cache.invalidateQueries({ queryKey: detailKey(id) })
    refreshSessionCollections(cache)
    void cache.invalidateQueries({ queryKey: ['signals', id] })
  }

  const transition = useMutation({
    mutationFn: (action: 'start' | 'pause' | 'resume' | 'cancel') =>
      sessions.transition(id, action),
    onSuccess: (result) => {
      cache.setQueryData(detailKey(id), result)
      setModal(undefined)
      changed()
    },
    onError: () => {
      void query.refetch()
    },
  })

  function act(action: SessionAction) {
    transition.reset()
    if (action === 'complete' || action === 'cancel') setModal(action)
    else transition.mutate(action)
  }

  async function complete(body: Model['CompleteSessionRequest']) {
    try {
      const result = await sessions.complete(id, body)
      cache.setQueryData(detailKey(id), result)
      setModal(undefined)
      changed()
    } catch (error) {
      void query.refetch()
      throw error
    }
  }

  if (!valid)
    return (
      <>
        <PageTitle title="Session unavailable" />
        <Empty
          title="This session link is invalid"
          action={
            <Link to="/sessions" className="button secondary">
              View session history
            </Link>
          }
        />
      </>
    )
  if (!session)
    return (
      <>
        <PageTitle title="Session details" />
        {query.isError ? (
          <Failure error={query.error} retry={() => void query.refetch()} />
        ) : (
          <Skeleton label="Loading session…" />
        )}
      </>
    )

  const activeTime = session.activeSeconds + elapsed
  return (
    <>
      <PageTitle
        title={sessionTitle(session)}
        action={
          <Link to="/sessions" className="button secondary">
            Session history
          </Link>
        }
      >
        Created {timestamp(session.createdAt)}
      </PageTitle>
      <div className="stack roomy">
        {query.isError && <Failure error={query.error} retry={() => void query.refetch()} />}
        <div className="session-focus">
          <Section title="Focus timer" action={<Badge status={session.status} />}>
            <div className="timer-wrap">
              <div className="timer-value" aria-label="Active session time">
                {clockLabel(activeTime)}
              </div>
              <p className="muted">
                Active time · {duration(session.plannedDurationSeconds)} planned
              </p>
              <progress
                value={Math.min(activeTime, session.plannedDurationSeconds)}
                max={session.plannedDurationSeconds}
                aria-label="Planned time progress"
              />
              <SessionActions status={session.status} busy={transition.isPending} onAction={act} />
              {transition.isError && !modal && <Failure error={transition.error} />}
            </div>
          </Section>
          <Section title={session.status === 'Completed' ? 'Completed session' : 'Session summary'}>
            <dl className="kv">
              <div>
                <dt>Elapsed time</dt>
                <dd>{duration(session.elapsedSeconds + elapsed)}</dd>
              </div>
              <div>
                <dt>Paused time</dt>
                <dd>{duration(session.pausedSeconds)}</dd>
              </div>
              <div>
                <dt>Concentration</dt>
                <dd>{rating(session.selfReportedConcentration)}</dd>
              </div>
              <div>
                <dt>Productivity</dt>
                <dd>{rating(session.selfReportedProductivity)}</dd>
              </div>
              <div>
                <dt>Goal completion</dt>
                <dd>
                  {session.goalStatistics.total
                    ? percent(session.goalStatistics.completionPercentage)
                    : '—'}
                </dd>
              </div>
            </dl>
            {session.reflection && (
              <div className="reflection">
                <h3>Reflection</h3>
                <p>{session.reflection}</p>
              </div>
            )}
          </Section>
        </div>
        <Goals session={session} changed={changed} />
        <Signals key={id} id={id} status={session.status} endedAt={session.endedAt} />
      </div>
      {modal === 'complete' && (
        <Completion session={session} close={() => setModal(undefined)} save={complete} />
      )}
      {modal === 'cancel' && (
        <Dialog
          title="Cancel session"
          close={() => setModal(undefined)}
          busy={transition.isPending}
        >
          <div className="form-stack">
            <p>End this session with a cancelled status? It will remain in your history.</p>
            {transition.isError && <Failure error={transition.error} />}
            <div className="actions">
              <Button
                type="button"
                kind="danger"
                busy={transition.isPending}
                onClick={() => transition.mutate('cancel')}
              >
                Confirm cancellation
              </Button>
              <Button
                type="button"
                kind="secondary"
                disabled={transition.isPending}
                onClick={() => setModal(undefined)}
              >
                Keep session
              </Button>
            </div>
          </div>
        </Dialog>
      )}
    </>
  )
}
