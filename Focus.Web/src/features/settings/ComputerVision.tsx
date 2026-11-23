import { timestamp } from '../../ui/format'
import { Section } from '../../ui/primitives'
import { usePrivacyQuery, useSaveVisionMutation } from './data'
import { MutationFeedback, PendingData } from './shared'

export function ComputerVision() {
  const query = usePrivacyQuery()
  const [save, change] = useSaveVisionMutation()

  return (
    <Section
      title="Computer Vision"
      subtitle="The Windows Agent processes camera frames locally during an active session."
    >
      <PendingData query={query}>
        {(data) => (
          <div className="form-stack">
            <label className="toggle-field">
              <input
                type="checkbox"
                checked={
                  change.isLoading
                    ? (change.originalArgs?.cameraEnabled ?? data.cameraEnabled)
                    : data.cameraEnabled
                }
                disabled={change.isLoading || query.isFetching}
                onChange={(event) => void save({ cameraEnabled: event.target.checked })}
              />
              <span>Allow coarse camera observation totals</span>
            </label>
            <label className="toggle-field">
              <input
                type="checkbox"
                checked={
                  change.isLoading
                    ? (change.originalArgs?.visionExperimental ?? data.visionExperimental)
                    : data.visionExperimental
                }
                disabled={!data.cameraEnabled || change.isLoading || query.isFetching}
                onChange={(event) => void save({ visionExperimental: event.target.checked })}
              />
              <span>Enable experimental camera-assisted estimates</span>
            </label>
            <p className="muted">
              {data.cameraConsentedAt
                ? `Camera consent recorded ${timestamp(data.cameraConsentedAt)}.`
                : 'Camera observation is disabled for this account.'}
            </p>
            <p>
              {data.cameraEnabled &&
                'Start the Windows Agent with --vision and confirm camera use there. The desktop panel shows whether it is processing. '}
              Choose a device and calibrate on Windows. This browser displays account consent and
              uploaded totals. Signing out here withdraws camera consent. Use Ctrl+C in the Agent
              terminal for an immediate local stop.
            </p>
            <p className="muted">
              Images, video and face landmarks stay out of the data sent to your account. Head
              orientation is an estimate and cannot measure attention.
            </p>
            <MutationFeedback error={change.error} saved={change.isSuccess} />
          </div>
        )}
      </PendingData>
    </Section>
  )
}
