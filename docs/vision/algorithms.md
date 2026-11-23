# Observation and evaluation rules

## Local observations

YuNet runs at an intended 5 FPS on CPU, adjustable from 1 to 10. Dark,
overexposed or low-contrast sampled frames become unknown. A usable frame
without a detection at the candidate threshold yields `NOT_DETECTED`.
Multiple candidates, a weak candidate or a small face yield `UNKNOWN`.
One sufficiently large candidate scored at least .85 yields `PRESENT`.
The score is the detector's threshold input, without probability calibration.
A missing or abandoned device gives `CAMERA_UNAVAILABLE`; denied local
permission or account consent prevents capture.

The five landmarks are fitted against an anonymous generic head template
with solvePnP and a nominal focal length. Fits with an interocular distance
below 16 pixels, reprojection error above .12 of that distance or implausible
angles are rejected. Local Euler yaw, pitch and roll drive orientation
categories. After subtracting a user-provided calibration, both yaw and
pitch inside 14 degrees produce a screen-facing estimate. Deviations of at
least 24 degrees classify the dominant axis as left, right, down or up.
Ambiguous values and excessive roll yield unknown. The pipeline emits weak
possible-screen-facing or possible-away evidence only after calibration.

Agent time is UTC. Consecutive readings are combined only when the gap is
within 1.45 times the target frame period. Equal classifications support the
interval between them. Differing head directions can support face presence
without supporting an orientation. Loss of detection and skipped frames
produce unknown time, without filling the gap from the previous state.
The Agent holds a short sampling lag to finish spans and commits them only
after confirming session boundaries. A sample that straddles a pause,
consent or end boundary is discarded. The confirmed partial final window
uploads through the same 24-hour deadline as existing activity.

The API accepts `presentMs + notDetectedMs + unknownMs` equal to the
integer-millisecond window duration. `facingMs + awayMs` must fit within
`presentMs`. The valid sample count is bounded by the physical observation
rate. In the report usable time is present plus not-detected time. Face
presence percentage uses usable time as its denominator. Possible
screen-facing percentage uses only time with usable orientation.

## Interpretation

The persisted desktop baseline stays `rules-1.0.0`. The experimental mode
can change a silent Idle window to Absent only when a camera window covers
at least 80% of both windows, lasts at least 12 seconds, has at least 12
valid samples and contains at least 90% usable non-detection. The explanation
reports uncertainty about physical presence. Changed estimates are tagged
`rules-vision-experimental-1.0.0`. Unknowns, facing, looking away and
camera-only sessions cannot change the baseline. No reported data is a
productivity score.

Compare both states through the owned vision report and account export. The
latest comparable persisted windows are evaluated by the two documented
rules without changing baseline history. The current-state endpoint selects
the experiment only when enabled and consented. Use [the tests](testing.md)
to reproduce on synthetic inputs. These inputs lack concentration ground truth.
