# Computer Vision limitations

- One visible face may belong to another person. Reliable non-detection is
  evidence about an image under the quality filter. Angle, lighting,
  occlusion and camera placement affect the input.
- The approximate generic head pose has no eye-tracking or visual-target
  calibration. A moved screen or camera can invalidate the saved neutral
  angles. Orientation durations can remain unknown even with reliable face presence.
- DirectShow provides no reliable portable distinction between access denied,
  unavailable and in-use for all devices. The UI groups these as Unavailable.
- Session start, pause, consent and end use the existing polling plus upload
  validation. Short unconfirmed boundary pieces can be excluded. An API
  outage stops new capture after the short lease and retries already confirmed
  data within the retention window.
- The current experimental decision is computed on request, while the
  versioned baseline is persisted. Comparative reports currently expose
  the latest overlapping pair for one session.
- Physical hardware, OS device permission behavior and capture-to-display
  latency require the [manual webcam test](manual-verification.md).
