# Focus.Vision Windows setup

Use x64 Windows, Python 3.12, the .NET SDK in `global.json`, PostgreSQL and the
existing Focus Assistant API. Test with the disposable database in the
[Windows review guide](../frontend/manual-review-checklist.md). Fresh API
databases and existing test databases need the new versioned migration.

From the repository root in PowerShell:

```powershell
python -m venv .\Focus.Vision\.venv
$visionPython = Resolve-Path .\Focus.Vision\.venv\Scripts\python.exe
& $visionPython -m pip install -r .\Focus.Vision\requirements.txt
& .\Focus.Vision\install-model.ps1
```

The installer retrieves the 232,589-byte YuNet March 2023 model from the
pinned OpenCV Zoo revision in [model.json](../../Focus.Vision/model.json).
It checks the model size and SHA-256 against the official Git LFS pointer and
retrieves the project-specific MIT license. The worker checks the digest
again at startup. OpenCV Python 4.12.0.88 and NumPy 2.2.6 are pinned. The
official x64 OpenCV Windows wheel uses CPU inference and includes DirectShow.
YuNet has a compact detection model and a documented MIT notice. Its five
landmarks are sufficient for the deliberately approximate pose fit and keep
the distribution smaller than a full Face Landmarker pipeline.

- [OpenCV Zoo model and license](https://github.com/opencv/opencv_zoo/tree/47534e27c9851bb1128ccc0102f1145e27f23f98/models/face_detection_yunet)
- [Official OpenCV Windows wheel](https://pypi.org/project/opencv-python/4.12.0.88/)

Device probing and calibration are separate interactive actions. They
briefly open the webcam when you confirm the prompt:

```powershell
& $visionPython .\Focus.Vision\vision.py list
& $visionPython .\Focus.Vision\vision.py calibrate --camera 0
& $visionPython .\Focus.Vision\vision.py clear-calibration
```

Type `PROBE` to read the available indices from 0 through 5. Replace `0`
with the chosen DirectShow index. During calibration type `CALIBRATE`, look
at the screen in your usual posture and keep one face in view for several
seconds. Run `clear-calibration` only when you intend to remove the saved
offsets. A camera change or moved monitor may require recalibration.

Camera status **Unavailable** can indicate Windows permission denial,
a busy or disconnected device, an invalid camera index or a missing runtime.
Check **Settings > Privacy & security > Camera**, including desktop app
access. Close competing apps, check the device index and rerun setup when
the model is missing. The existing telemetry path runs without this setup.
