import type { Model } from '../../api/client'
import { Empty } from '../../ui/feedback'

function ratio(value: number, total: number) {
  return total > 0 ? `${Math.round((100 * value) / total)}%` : 'Unavailable'
}

function seconds(milliseconds: number) {
  return `${(milliseconds / 1000).toLocaleString('en', { maximumFractionDigits: 3 })} s`
}

export function VisionSummary({
  report,
  enabled,
}: {
  report: Model['VisionReport']
  enabled?: boolean
}) {
  const oriented = report.facingMs + report.awayMs
  return (
    <div className="stack">
      <p className="muted">
        Camera consent: {enabled === undefined ? 'unknown' : enabled ? 'enabled' : 'disabled'}.
        Screen-facing estimates use head orientation after local calibration.
      </p>
      {report.observedMs + report.unknownMs === 0 ? (
        <Empty title="No camera observations uploaded">
          Camera totals will appear here after an upload.
        </Empty>
      ) : (
        <dl className="kv">
          <div>
            <dt>Usable observation</dt>
            <dd>{seconds(report.observedMs)}</dd>
          </div>
          <div>
            <dt>Unknown duration</dt>
            <dd>{seconds(report.unknownMs)}</dd>
          </div>
          <div>
            <dt>Face detected in usable observations</dt>
            <dd>{ratio(report.presentMs, report.observedMs)}</dd>
          </div>
          <div>
            <dt>Possibly screen-facing when orientation is available</dt>
            <dd>{ratio(report.facingMs, oriented)}</dd>
          </div>
          <div>
            <dt>Valid observation samples</dt>
            <dd>{report.validSamples}</dd>
          </div>
        </dl>
      )}
      {report.comparison && (
        <p>
          Last comparable window: baseline <strong>{report.comparison.baseline.state}</strong> (
          {report.comparison.baseline.ruleVersion}), experimental{' '}
          <strong>{report.comparison.experimental.state}</strong> (
          {report.comparison.experimental.ruleVersion}). {report.comparison.experimental.reason}
        </p>
      )}
    </div>
  )
}
