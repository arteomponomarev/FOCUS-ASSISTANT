"""Exercise the actual CPU model on a synthetic scene, with no camera access."""

import statistics
import ctypes
import sys
import time

from test_vision import synthetic_scene
from vision import Detector

detector = Detector()
frame = synthetic_scene()
detector.observe(frame)
durations = []
cpu = time.process_time()
wall = time.perf_counter()
for _ in range(150):
    started = time.perf_counter()
    detector.observe(frame)
    durations.append(time.perf_counter() - started)
    time.sleep(max(0, .2 - (time.perf_counter() - started)))
elapsed = time.perf_counter() - wall
print(f"fps={len(durations) / elapsed:.2f} mean_ms={statistics.mean(durations) * 1000:.2f} "
      f"p95_ms={statistics.quantiles(durations, n=20)[18] * 1000:.2f} "
      f"cpu_one_core_percent={(time.process_time() - cpu) / elapsed * 100:.2f}")

if sys.platform == "win32":
    class Counters(ctypes.Structure):
        _fields_ = [("size", ctypes.c_uint32), ("page_faults", ctypes.c_uint32),
                    ("peak_working_set", ctypes.c_size_t), ("working_set", ctypes.c_size_t),
                    ("pool_and_commit", ctypes.c_size_t * 6)]

    kernel = ctypes.WinDLL("kernel32")
    kernel.GetCurrentProcess.restype = ctypes.c_void_p
    measure = ctypes.WinDLL("psapi").GetProcessMemoryInfo
    measure.argtypes = [ctypes.c_void_p, ctypes.POINTER(Counters), ctypes.c_uint32]
    measure.restype = ctypes.c_int
    counters = Counters()
    counters.size = ctypes.sizeof(counters)
    if measure(kernel.GetCurrentProcess(), ctypes.byref(counters), counters.size):
        print(f"worker_peak_working_set_mib={counters.peak_working_set / (1024 * 1024):.1f}")
