import { Empty } from './feedback'
import { duration } from './format'
import { estimateDistribution } from '../features/analytics/data'

export type ChartPoint = { label: string; value: number | null }

export function Trend({
  points,
  label,
  unit,
  maximum,
  alt = false,
}: {
  points: ChartPoint[]
  label: string
  unit: string
  maximum?: number
  alt?: boolean
}) {
  if (!points.some(({ value }) => value !== null)) return <Empty title="No data for this period" />
  const ceiling = maximum ?? Math.max(1, ...points.map(({ value }) => value ?? 0))
  return (
    <figure className="chart">
      <div className="chart-plot" aria-hidden="true">
        {points.map((point, index) => (
          <span key={index} className="chart-column">
            {point.value != null && (
              <span
                className={'chart-bar' + (alt ? ' alt' : '')}
                style={{ height: Math.min(100, (100 * point.value) / ceiling) + '%' }}
              />
            )}
          </span>
        ))}
      </div>
      <div className="chart-keys" aria-hidden="true">
        <span>{points[0]?.label}</span>
        <span>{unit}</span>
        <span>{points.at(-1)?.label}</span>
      </div>
      <figcaption>
        <details>
          <summary className="chart-summary">View data: {label}</summary>
          <table className="session-table">
            <caption className="sr-only">{label}</caption>
            <thead>
              <tr>
                <th scope="col">Period</th>
                <th scope="col">{unit}</th>
              </tr>
            </thead>
            <tbody>
              {points.map((point, index) => (
                <tr key={index}>
                  <td>{point.label}</td>
                  <td>
                    {point.value === null
                      ? '—'
                      : point.value.toLocaleString('en', { maximumFractionDigits: 1 })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </details>
      </figcaption>
    </figure>
  )
}

export function Distribution({ seconds }: { seconds: Record<string, number> }) {
  const entries = estimateDistribution(seconds)
  if (entries.every((item) => item.seconds === 0))
    return (
      <Empty title="No estimated states yet">
        Estimates appear after the backend receives aggregate telemetry.
      </Empty>
    )
  return (
    <div className="state-list">
      {entries.map((entry) => (
        <div className="state-row" key={entry.state}>
          <span>{entry.state}</span>
          <span className="state-track" aria-hidden="true">
            <span className="state-fill" style={{ width: entry.percent + '%' }} />
          </span>
          <span>
            {duration(entry.seconds)} · {Math.round(entry.percent)}%
          </span>
        </div>
      ))}
      <details>
        <summary className="chart-summary">How to read these states</summary>
        <dl className="kv">
          <div>
            <dt>Focused</dt>
            <dd>Activity in an allowed app with few switches and little idle time.</dd>
          </div>
          <div>
            <dt>Neutral</dt>
            <dd>Available signals support no stronger estimate.</dd>
          </div>
          <div>
            <dt>Distracted</dt>
            <dd>Frequent switching while using an app flagged in the work profile.</dd>
          </div>
          <div>
            <dt>Idle</dt>
            <dd>A window mostly spent without interaction.</dd>
          </div>
          <div>
            <dt>Break</dt>
            <dd>A window within a recorded pause.</dd>
          </div>
          <div>
            <dt>Absent</dt>
            <dd>Extended inactivity. Physical presence is unknown.</dd>
          </div>
        </dl>
      </details>
    </div>
  )
}
