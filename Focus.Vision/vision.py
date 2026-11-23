"""Local YuNet observations. The worker output is a fixed vocabulary of coarse codes."""

import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import sys
import threading
import time

import cv2
import numpy as np

HERE = Path(__file__).resolve().parent
MODEL = HERE / "models" / "face_detection_yunet_2023mar.onnx"
MANIFEST = HERE / "model.json"
CALIBRATION = HERE / "calibration.local.json"
FACE_TEMPLATE = np.array([[-31, -25, 22], [31, -25, 22], [0, 0, 0],
                          [-24, 26, 18], [24, 26, 18]], dtype=np.float64)


def orientation(angles, neutral=(0, 0, 0)):
    yaw, pitch, roll = (value - center for value, center in zip(angles, neutral))
    if not all(math.isfinite(value) for value in (yaw, pitch, roll)) or abs(roll) > 27:
        return "UNKNOWN"
    if abs(yaw) >= 24 and abs(yaw) >= abs(pitch):
        return "TURNED_LEFT" if yaw < 0 else "TURNED_RIGHT"
    if abs(pitch) >= 24:
        return "LOOKING_UP_ESTIMATE" if pitch > 0 else "LOOKING_DOWN_ESTIMATE"
    if abs(yaw) <= 14 and abs(pitch) <= 14:
        return "SCREEN_FACING_ESTIMATE"
    return "UNKNOWN"


def pose(points, width, height):
    points = np.asarray(points, dtype=np.float64).reshape(5, 2)
    eye_span = np.linalg.norm(points[1] - points[0])
    if not np.isfinite(points).all() or eye_span < 16:
        return None
    focal = max(width, height)
    camera = np.array([[focal, 0, width / 2], [0, focal, height / 2], [0, 0, 1]], dtype=np.float64)
    success, rotation, translation = cv2.solvePnP(
        FACE_TEMPLATE, points, camera, None, flags=cv2.SOLVEPNP_EPNP)
    if not success:
        return None
    rotation, translation = cv2.solvePnPRefineLM(FACE_TEMPLATE, points, camera, None, rotation, translation)
    if translation[2, 0] <= 0:
        return None
    projected, _ = cv2.projectPoints(FACE_TEMPLATE, rotation, translation, camera, None)
    error = np.linalg.norm(projected.reshape(5, 2) - points, axis=1).mean() / eye_span
    if not np.isfinite(error) or error > .12:
        return None
    matrix = cv2.Rodrigues(rotation)[0]
    pitch, yaw, roll = cv2.RQDecomp3x3(matrix)[0]
    return (yaw, pitch, roll) if max(abs(yaw), abs(pitch), abs(roll)) < 85 else None


class Detector:
    def __init__(self, model=MODEL):
        expected = json.loads(MANIFEST.read_text(encoding="utf-8"))["sha256"]
        with model.open("rb") as contents:
            if hashlib.file_digest(contents, "sha256").hexdigest() != expected:
                raise ValueError("Unexpected YuNet model digest")
        cv2.setNumThreads(1)
        self.model = cv2.FaceDetectorYN.create(str(model), "", (320, 240), .60, .30, 16,
                                               cv2.dnn.DNN_BACKEND_OPENCV, cv2.dnn.DNN_TARGET_CPU)

    def observe(self, frame):
        height, width = frame.shape[:2]
        if width > 320:
            frame = cv2.resize(frame, (320, max(1, round(height * 320 / width))), interpolation=cv2.INTER_AREA)
        height, width = frame.shape[:2]
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        if not usable(gray):
            return "UNKNOWN", None
        self.model.setInputSize((width, height))
        _, faces = self.model.detect(frame)
        return observation(faces, width, height)


def usable(gray):
    return 35 <= float(gray.mean()) <= 220 and float(gray.std()) >= 14


def observation(faces, width, height):
    if faces is None or len(faces) == 0:
        return "NOT_DETECTED", None
    if len(faces) != 1 or faces[0, 14] < .85 or min(faces[0, 2:4]) < 40:
        return "UNKNOWN", None
    return "PRESENT", pose(faces[0, 4:14], width, height)


def read_calibration(path, index):
    if not path.is_file() or path.stat().st_size > 512:
        return None
    try:
        saved = json.loads(path.read_text(encoding="utf-8"))
        angles = tuple(float(value) for value in saved["neutral"])
        if saved["device"] == index and len(angles) == 3 and all(abs(value) <= 35 for value in angles):
            return angles
    except (OSError, KeyError, ValueError, TypeError):
        return None
    return None


def coarse(presence, angles, neutral):
    if presence == "NOT_DETECTED":
        return "N"
    if presence != "PRESENT":
        return "U"
    # A head facing an uncalibrated camera is not evidence of facing the screen.
    if angles is None or neutral is None:
        return "P"
    head = orientation(angles, neutral)
    return "F" if head == "SCREEN_FACING_ESTIMATE" else "P" if head == "UNKNOWN" else "A"


def open_camera(index):
    camera = cv2.VideoCapture(index, cv2.CAP_DSHOW if sys.platform == "win32" else cv2.CAP_ANY)
    if camera.isOpened():
        camera.set(cv2.CAP_PROP_FRAME_WIDTH, 320)
        camera.set(cv2.CAP_PROP_FRAME_HEIGHT, 240)
    return camera


def emit(code):
    print(code, flush=True)


def run_capture(camera, detector, calibration, fps, stopped, send=emit):
    try:
        while not stopped.is_set() and camera.isOpened():
            began = time.monotonic()
            ok, frame = camera.read()
            if not ok or frame is None or stopped.is_set():
                return
            code = coarse(*detector.observe(frame), calibration)
            del frame
            send(code)
            stopped.wait(max(0.0, 1 / fps - (time.monotonic() - began)))
    finally:
        camera.release()


def worker(index, fps):
    detector = Detector()
    calibration = read_calibration(CALIBRATION, index)
    stopped = threading.Event()
    disposed = threading.Event()
    pulse = [time.monotonic()]

    def read_lease():
        while sys.stdin.readline(16) == "KEEP\n":
            pulse[0] = time.monotonic()
        stopped.set()

    def enforce_lease():
        while not stopped.wait(.25) and time.monotonic() - pulse[0] < 2.75:
            pass
        stopped.set()
        if not disposed.wait(.8):
            # Native camera calls may block. Windows releases process-owned handles on exit.
            os._exit(0)

    threading.Thread(target=read_lease, daemon=True).start()
    threading.Thread(target=enforce_lease, daemon=True).start()
    try:
        while not stopped.is_set():
            run_capture(open_camera(index), detector, calibration, fps, stopped)
            if not stopped.is_set():
                emit("X")
                stopped.wait(2)
    finally:
        disposed.set()


def calibrate(index):
    detector = Detector()
    camera = open_camera(index)
    angles = []
    print("Camera active for calibration. Look at your usual screen position.")
    try:
        for _ in range(35):
            ok, frame = camera.read()
            if not ok:
                break
            presence, result = detector.observe(frame)
            if presence == "PRESENT" and result is not None:
                angles.append(result)
            time.sleep(.18)
    finally:
        camera.release()
    median = np.median(angles, axis=0) if len(angles) >= 15 else None
    if median is None or np.max(np.abs(median)) > 35 or np.max(np.std(angles, axis=0)) > 8:
        print("Calibration failed. Use one face with stable lighting and a steady posture.")
        return 1
    neutral = [int(round(value / 5) * 5) for value in median]
    temp = CALIBRATION.with_suffix(".tmp")
    temp.write_text(json.dumps({"device": index, "neutral": neutral}), encoding="utf-8")
    temp.replace(CALIBRATION)
    print("Coarse neutral angle offsets saved locally.")
    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["worker", "list", "calibrate", "clear-calibration"])
    parser.add_argument("--camera", type=int, choices=range(0, 16), default=0)
    parser.add_argument("--fps", type=int, choices=range(1, 11), default=5)
    args = parser.parse_args()
    try:
        if args.mode == "worker":
            # Only the Agent's redirected private subprocess pipes are supported.
            if sys.stdin.isatty() or sys.stdout.isatty() or sys.stdin.readline(32).strip() != "START_SESSION":
                return 2
            sys.stdout.reconfigure(newline="\n")
            worker(args.camera, args.fps)
        elif args.mode == "clear-calibration":
            CALIBRATION.unlink(missing_ok=True)
        elif args.mode == "list":
            if not sys.stdin.isatty() or input("Type PROBE to briefly open local camera devices: ") != "PROBE":
                return 2
            for index in range(6):
                camera = open_camera(index)
                try:
                    if camera.isOpened():
                        print(f"Available index: {index}")
                finally:
                    camera.release()
        elif sys.stdin.isatty() and input("Type CALIBRATE to enable the camera temporarily: ") == "CALIBRATE":
            return calibrate(args.camera)
        else:
            return 2
    except (OSError, ValueError, cv2.error):
        print("Vision initialization or camera processing failed.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
