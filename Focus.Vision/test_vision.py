import unittest
from threading import Event
from unittest.mock import Mock

import cv2
import numpy as np

from vision import Detector, FACE_TEMPLATE, coarse, observation, orientation, pose, run_capture


class VisionTests(unittest.TestCase):
    def test_pose_with_projected_synthetic_points(self):
        camera = np.array([[320, 0, 160], [0, 320, 120], [0, 0, 1]], np.float64)
        rotation = np.array([[0.0], [.48], [0.0]])
        translation = np.array([[0.0], [0.0], [300.0]])
        points, _ = cv2.projectPoints(FACE_TEMPLATE, rotation, translation, camera, None)
        yaw, pitch, roll = pose(points.reshape(5, 2), 320, 240)
        self.assertAlmostEqual(yaw, 27.5, places=1)
        self.assertAlmostEqual(pitch, 0, places=1)
        self.assertAlmostEqual(roll, 0, places=1)
        self.assertEqual("TURNED_RIGHT", orientation((yaw, pitch, roll)))

    def test_conservative_calibrated_attention_and_ambiguity(self):
        self.assertEqual("SCREEN_FACING_ESTIMATE", orientation((20, 0, 0), (20, 0, 0)))
        self.assertEqual("LOOKING_DOWN_ESTIMATE", orientation((0, -28, 0)))
        self.assertEqual("UNKNOWN", orientation((19, 0, 0)))
        self.assertEqual("UNKNOWN", orientation((0, 0, 45)))
        self.assertEqual("F", coarse("PRESENT", (20, 0, 0), (20, 0, 0)))
        self.assertEqual("A", coarse("PRESENT", (-30, 0, 0), (0, 0, 0)))
        self.assertEqual("P", coarse("PRESENT", (0, 0, 0), None))
        self.assertEqual("P", coarse("PRESENT", None, (0, 0, 0)))
        self.assertEqual("U", coarse("UNKNOWN", None, None))
        self.assertEqual("N", coarse("NOT_DETECTED", None, None))

    def test_application_rejects_multiple_and_weak_faces(self):
        face = np.zeros((1, 15), np.float32)
        face[0, 2:4] = (100, 120)
        face[0, 14] = .67
        self.assertEqual(("UNKNOWN", None), observation(face, 320, 240))
        face[0, 14] = .95
        self.assertEqual(("UNKNOWN", None), observation(np.repeat(face, 2, axis=0), 320, 240))
        self.assertEqual(("PRESENT", None), observation(face, 320, 240))

    def test_actual_model_accepts_synthetic_frame_and_handles_unusable_frame(self):
        detector = Detector()
        blank = np.zeros((240, 320, 3), np.uint8)
        self.assertEqual(("UNKNOWN", None), detector.observe(blank))
        self.assertEqual(("NOT_DETECTED", None), detector.observe(synthetic_scene()))

    def test_actual_model_detects_a_procedurally_drawn_face(self):
        # Generated in memory from geometric shapes. No person's image or image file is used.
        face = np.full((240, 320, 3), (94, 119, 132), np.uint8)
        cv2.ellipse(face, (160, 220), (70, 52), 0, 0, 360, (71, 54, 46), -1)
        cv2.ellipse(face, (160, 108), (58, 82), 0, 0, 360, (43, 34, 32), -1)
        cv2.ellipse(face, (160, 121), (48, 63), 0, 0, 360, (121, 161, 206), -1)
        for x in (141, 179):
            cv2.ellipse(face, (x, 108), (11, 5), 0, 0, 360, (219, 223, 231), -1)
            cv2.circle(face, (x, 108), 4, (58, 60, 60), -1)
            cv2.circle(face, (x, 108), 2, (23, 23, 24), -1)
            cv2.line(face, (x - 10, 96), (x + 9, 95), (52, 60, 72), 2)
        cv2.line(face, (157, 111), (153, 133), (94, 133, 180), 2)
        cv2.ellipse(face, (160, 135), (8, 3), 0, 0, 180, (89, 115, 167), 1)
        cv2.ellipse(face, (160, 149), (13, 4), 0, 0, 180, (64, 69, 142), 2)
        cv2.ellipse(face, (160, 150), (13, 2), 0, 0, 180, (82, 103, 177), 1)
        status, _ = Detector().observe(cv2.GaussianBlur(face, (3, 3), .5))
        self.assertEqual("PRESENT", status)

    def test_capture_attempt_releases_on_disconnect_and_can_resume(self):
        frame = synthetic_scene()
        detector = Mock()
        detector.observe.return_value = ("NOT_DETECTED", None)
        first = Mock()
        first.read.side_effect = [(False, None)]
        recovered = Mock()
        recovered.read.side_effect = [(True, frame), (False, None)]
        letters = []
        for camera in (first, recovered):
            run_capture(camera, detector, None, 10, Event(), letters.append)
            camera.release.assert_called_once()
        self.assertEqual(["N"], letters)

    def test_cancellation_before_read_releases_without_observing(self):
        camera = Mock()
        detector = Mock()
        stopped = Event()
        stopped.set()
        run_capture(camera, detector, None, 5, stopped, self.fail)
        camera.read.assert_not_called()
        detector.observe.assert_not_called()
        camera.release.assert_called_once()


def synthetic_scene():
    tile = np.indices((240, 320)).sum(axis=0) // 24 % 2
    return np.repeat(((tile * 100) + 55).astype(np.uint8)[:, :, None], 3, axis=2)


if __name__ == "__main__":
    unittest.main()
