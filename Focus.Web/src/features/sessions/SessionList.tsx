import { Link } from 'react-router-dom'
import { duration, percent, rating, timestamp } from '../../ui/format'
import { sessionTitle } from './data'
import type { Session } from './data'

export function Badge({ status }: { status: string }) {
  return (
    <span className="badge" data-state={status}>
      {status}
    </span>
  )
}

export function SessionList({ items }: { items: Session[] }) {
  return (
    <>
      <table className="session-table responsive">
        <thead>
          <tr>
            <th scope="col">Session</th>
            <th scope="col">Status</th>
            <th scope="col">Active / planned</th>
            <th scope="col">Goals</th>
            <th scope="col">Concentration</th>
            <th scope="col">Productivity</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>
                <Link to={'/sessions/' + item.id}>{sessionTitle(item)}</Link>
                <br />
                <small className="muted">{timestamp(item.startedAt ?? item.createdAt)}</small>
              </td>
              <td>
                <Badge status={item.status} />
              </td>
              <td>
                {duration(item.activeSeconds)} / {duration(item.plannedDurationSeconds)}
              </td>
              <td>
                {item.goalStatistics.total === 0
                  ? '—'
                  : percent(item.goalStatistics.completionPercentage)}
              </td>
              <td>{rating(item.selfReportedConcentration)}</td>
              <td>{rating(item.selfReportedProductivity)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="session-tile-list">
        {items.map((item) => (
          <article className="tile" key={item.id}>
            <div className="row space-between">
              <Link className="button ghost" to={'/sessions/' + item.id}>
                {sessionTitle(item)}
              </Link>
              <Badge status={item.status} />
            </div>
            <small className="muted">{timestamp(item.startedAt ?? item.createdAt)}</small>
            <dl className="kv">
              <div>
                <dt>Active / planned</dt>
                <dd>
                  {duration(item.activeSeconds)} / {duration(item.plannedDurationSeconds)}
                </dd>
              </div>
              <div>
                <dt>Goals</dt>
                <dd>
                  {item.goalStatistics.total === 0
                    ? '—'
                    : percent(item.goalStatistics.completionPercentage)}
                </dd>
              </div>
              <div>
                <dt>Concentration</dt>
                <dd>{rating(item.selfReportedConcentration)}</dd>
              </div>
              <div>
                <dt>Productivity</dt>
                <dd>{rating(item.selfReportedProductivity)}</dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
    </>
  )
}
