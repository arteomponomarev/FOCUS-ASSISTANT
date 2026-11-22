"""Exercise a published API over local TLS against a fresh disposable PostgreSQL database."""

import argparse
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
import re
import secrets
import socket
import ssl
import subprocess
import tempfile
from threading import Thread
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path


def ports():
    with socket.socket() as first, socket.socket() as second:
        first.bind(("127.0.0.1", 0))
        second.bind(("127.0.0.1", 0))
        return first.getsockname()[1], second.getsockname()[1]


def test_connection():
    value = os.environ["FOCUS_TEST_DATABASE"]
    if not re.search(r"(?:^|;)Host=(?:127\.0\.0\.1|localhost)(?:;|$)", value, re.I):
        raise SystemExit("Set FOCUS_TEST_DATABASE to a disposable loopback server.")
    value, count = re.subn(r"((?:^|;)Database=)focus_test_[A-Za-z0-9_]+(?=;|$)",
                           lambda match: match[1] + "focus_test_release_" + uuid.uuid4().hex,
                           value, flags=re.I)
    if count != 1:
        raise SystemExit("Use a focus_test_ database name in the test connection.")
    return value


def request(base, context, method, path, payload=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    data = None if payload is None else json.dumps(payload).encode()
    message = urllib.request.Request(base + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(message, context=context, timeout=4) as response:
            raw = response.read()
            is_json = response.headers.get_content_type().endswith("json")
            return response.status, json.loads(raw) if is_json and raw else None
    except urllib.error.HTTPError as error:
        return error.code, None


def wait_ready(process, send):
    for _ in range(40):
        if process.poll() is not None:
            raise RuntimeError("The production test process terminated during startup.")
        try:
            if send("GET", "/health/ready")[0] == 200:
                return
        except (urllib.error.URLError, TimeoutError):
            pass
        time.sleep(0.25)
    raise RuntimeError("Readiness probe timed out.")


def stop(process):
    if process.poll() is None:
        process.terminate()
        process.wait(timeout=10)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("artifact", type=Path)
    parser.add_argument("--openssl", default="openssl")
    args = parser.parse_args()
    command = ["dotnet", str(args.artifact.resolve(strict=True))]
    fixture_prefixes = (
        "OTEL_", "OBSERVABILITY__", "AUTHENTICATION__", "HTTP__", "KESTREL__",
        "CONNECTIONSTRINGS__", "ASPNETCORE_", "DOTNET_ENVIRONMENT",
    )
    env = {key: value for key, value in os.environ.items() if not key.upper().startswith(fixture_prefixes)}
    env.update(ASPNETCORE_ENVIRONMENT="Production", DOTNET_ENVIRONMENT="Production",
               AllowedHosts="127.0.0.1;localhost",
               ConnectionStrings__PostgreSQL=test_connection())
    subprocess.run(command + ["--migrate"], cwd=args.artifact.resolve().parent, env=env, check=True)

    with tempfile.TemporaryDirectory(prefix="focus-release-smoke-") as directory:
        records = {"/v1/traces": [], "/v1/metrics": []}

        class Receiver(BaseHTTPRequestHandler):
            def do_POST(self):
                if self.path not in records:
                    self.send_error(404)
                    return
                records[self.path].append(self.rfile.read(int(self.headers["Content-Length"])))
                self.send_response(200)
                self.send_header("Content-Type", "application/x-protobuf")
                self.send_header("Content-Length", "0")
                self.end_headers()

            def log_message(self, *_):
                pass

        with ThreadingHTTPServer(("127.0.0.1", 0), Receiver) as collector:
            worker = Thread(target=collector.serve_forever, daemon=True)
            worker.start()
            env.update(Observability__ExportOtlp="true",
                       OTEL_EXPORTER_OTLP_ENDPOINT=f"http://127.0.0.1:{collector.server_port}",
                       OTEL_EXPORTER_OTLP_PROTOCOL="http/protobuf",
                       OTEL_BSP_SCHEDULE_DELAY="100", OTEL_METRIC_EXPORT_INTERVAL="1000")
            try:
                run(command, args.openssl, env, Path(directory), records)
            finally:
                collector.shutdown()
                worker.join()


def run(command, openssl, env, directory, records):
    cert = directory / "release.crt"
    key = directory / "release.key"
    pfx = directory / "release.pfx"
    log = directory / "host.jsonl"
    https_port, http_port = ports()
    https = f"https://127.0.0.1:{https_port}"
    http = f"http://127.0.0.1:{http_port}"
    pfx_password = secrets.token_hex(24)
    env.update(FOCUS_SMOKE_PFX_PASSWORD=pfx_password,
               Authentication__KeyDirectory=str(directory / "keys"),
               Authentication__KeyEncryptionCertificatePath=str(pfx),
               Authentication__KeyEncryptionCertificatePassword=pfx_password,
               Kestrel__Endpoints__Https__Certificate__Path=str(pfx),
               Kestrel__Endpoints__Https__Certificate__Password=pfx_password,
               Kestrel__Endpoints__Https__Url=https, Kestrel__Endpoints__Http__Url=http)
    subprocess.run([openssl, "req", "-x509", "-newkey", "rsa:2048", "-sha256", "-days", "1",
                    "-nodes", "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost,IP:127.0.0.1",
                    "-keyout", str(key), "-out", str(cert)], check=True,
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run([openssl, "pkcs12", "-export", "-out", str(pfx), "-inkey", str(key),
                    "-in", str(cert), "-passout", "env:FOCUS_SMOKE_PFX_PASSWORD"], check=True, env=env)
    context = ssl.create_default_context(cafile=str(cert))

    def send(method, path, payload=None, token=None):
        return request(https, context, method, path, payload, token)

    def start():
        with log.open("a", encoding="utf-8") as output:
            return subprocess.Popen(command, cwd=Path(command[1]).parent, env=env, stdout=output, stderr=output,
                                    creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))

    process = start()
    try:
        wait_ready(process, send)
        assert request(http, None, "GET", "/api/v1/sessions")[0] == 400
        assert send("GET", "/api/v1/sessions")[0] == 401
        credentials = {"email": "smoke@example.invalid", "password": secrets.token_hex(24)}
        assert send("POST", "/api/v1/auth/register", credentials | {"displayName": "Smoke"})[0] == 201
        code, tokens = send("POST", "/api/v1/auth/login", credentials)
        assert code == 200
        token = tokens["accessToken"]
        assert send("GET", "/api/v1/auth/me", token=token)[1]["email"] == credentials["email"]
        assert send("PATCH", "/api/v1/settings/privacy", {"telemetryEnabled": True}, token)[0] == 200
        code, session = send("POST", "/api/v1/sessions", {"plannedDurationSeconds": 600}, token)
        assert code == 201
        path = "/api/v1/sessions/" + session["id"]
        code, goal = send("POST", path + "/goals", {"description": "Verify persistent release data"}, token)
        assert code == 201
        assert send("POST", path + "/start", token=token)[0] == 200
        started = datetime.now(timezone.utc).isoformat()
        time.sleep(1)
        item = {
            "clientEventId": str(uuid.uuid4()),
            "startedAt": started,
            "endedAt": datetime.now(timezone.utc).isoformat(),
            "activeProcessName": "editor",
            "keyboardEventCount": 7,
            "mouseClickCount": 2,
            "windowSwitchCount": 0,
            "idleSeconds": 0,
        }
        batch = {"items": [item]}
        assert send("POST", path + "/activity/batch", batch, token) == (200, {"created": 1, "duplicates": 0})
        assert send("POST", path + "/activity/batch", batch, token) == (200, {"created": 0, "duplicates": 1})
        assert send("GET", path + "/focus-states", token=token)[1]["totalCount"] == 1
        assert send("POST", path + "/pause", token=token)[0] == 200
        assert send("POST", path + "/resume", token=token)[0] == 200
        completion = {"selfReportedConcentration": 4, "selfReportedProductivity": 3,
                      "goalOutcomes": [{"goalId": goal["id"], "status": "Completed"}]}
        assert send("POST", path + "/complete", completion, token)[0] == 200
        assert send("POST", path + "/complete", completion, token)[0] == 409
        assert send("GET", path, token=token)[0] == 200
        for _ in range(50):
            if records["/v1/traces"] and any(b"focus.activity.persisted_windows" in item for item in records["/v1/metrics"]):
                break
            time.sleep(0.1)
        else:
            raise RuntimeError("The loopback receiver did not receive the expected OTLP trace and activity metric.")
        assert all(session["id"].encode() not in trace for trace in records["/v1/traces"])
        stop(process)
        process = start()
        wait_ready(process, send)
        code, loaded = send("GET", path, token=token)
        assert code == 200 and loaded["id"] == session["id"] and loaded["status"] == "Completed"
        assert loaded["selfReportedConcentration"] == 4 and loaded["selfReportedProductivity"] == 3
        assert loaded["goals"][0]["status"] == "Completed"
        assert send("GET", "/api/v1/sessions", token=token)[1]["totalCount"] == 1
        assert send("GET", path + "/timeline", token=token)[1]["events"]["totalCount"] == 2
        assert send("GET", path + "/activity", token=token)[1]["totalCount"] == 1
        metrics = send("GET", "/api/v1/analytics/summary", token=token)[1]["metrics"]
        assert metrics["completedSessions"] == 1 and metrics["keyboardEvents"] == 7
        assert send("POST", "/api/v1/auth/logout", token=token)[0] == 204
        assert send("GET", path, token=token)[0] == 401
    finally:
        stop(process)
    print("PASS: production HTTPS workflow, OTLP export, and durable credentials and research data across restart.")


if __name__ == "__main__":
    main()
