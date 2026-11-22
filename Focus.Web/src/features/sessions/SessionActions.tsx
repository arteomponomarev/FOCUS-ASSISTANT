import { Ban, Check, Pause, Play } from 'lucide-react'
import { Button } from '../../ui/primitives'
import type { Session } from './data'

export type SessionAction = 'start' | 'pause' | 'resume' | 'complete' | 'cancel'

export function availableActions(status: Session['status']): SessionAction[] {
  switch (status) {
    case 'Planned':
      return ['start', 'cancel']
    case 'Active':
      return ['pause', 'complete', 'cancel']
    case 'Paused':
      return ['resume', 'complete', 'cancel']
    default:
      return []
  }
}

const controls = {
  start: { label: 'Start session', Icon: Play, kind: 'primary' },
  pause: { label: 'Pause', Icon: Pause, kind: 'secondary' },
  resume: { label: 'Resume', Icon: Play, kind: 'primary' },
  complete: { label: 'Finish session', Icon: Check, kind: 'primary' },
  cancel: { label: 'Cancel session', Icon: Ban, kind: 'ghost' },
} as const

export function SessionActions({
  status,
  busy = false,
  onAction,
}: {
  status: Session['status']
  busy?: boolean
  onAction: (action: SessionAction) => void
}) {
  const available = availableActions(status)
  return available.length > 0 ? (
    <div className="actions">
      {available.map((action) => {
        const { Icon, label, kind } = controls[action]
        return (
          <Button
            key={action}
            type="button"
            kind={kind}
            disabled={busy}
            onClick={() => onAction(action)}
          >
            <Icon size={18} aria-hidden="true" />
            {label}
          </Button>
        )
      })}
    </div>
  ) : null
}
