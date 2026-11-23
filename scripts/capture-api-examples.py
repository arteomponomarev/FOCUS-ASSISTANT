"""Capture redacted OpenAPI examples from a new loopback PostgreSQL/API fixture."""

import argparse
from datetime import datetime, timedelta
import json
import os
from pathlib import Path
import re
import secrets
import socket
import subprocess
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
from uuid import uuid4


def capture(base, operations):
    examples = {}
    token = None

    def call(name, status=200, body=None, path_options=None, query="", sample=True, mime="application/json"):
        method, path = operations[name]
        path = path.format(**(path_options or {}))
        headers = {}
        if token:
            headers["Authorization"] = "Bearer " + token
        if body is not None:
            headers["Content-Type"] = mime
        request = Request(base + path + query, json.dumps(body).encode() if body is not None else None,
                          headers, method=method)
        try:
            with urlopen(request, timeout=15) as response:
                actual, raw = response.status, response.read()
        except HTTPError as error:
            with error:
                actual, raw = error.code, error.read()
        if actual != status:
            raise RuntimeError(f"{name}: expected {status}, got {actual}")
        result = json.loads(raw) if raw else None
        if sample:
            entry = examples.setdefault(name, {})
            if body is not None and status < 300:
                entry["request"] = body
            if result is not None:
                entry.setdefault("responses", {})[str(status)] = result
        return result

    initial_password = secrets.token_urlsafe(24)
    changed_password = secrets.token_urlsafe(24)
    login = {"email": "researcher@example.invalid", "password": initial_password}
    registration = login | {"displayName": "Research Reviewer"}
    call("AuthMe", 401)
    call("AuthRegister", 201, registration)
    call("AuthRegister", 409, registration)
    call("AuthLogin", 401, login | {"password": "wrong-illustrative-password"})
    first_pair = call("AuthLogin", body=login)
    rotated = call("AuthRefresh", body={"refreshToken": first_pair["refreshToken"]})
    call("AuthRefresh", 401, {"refreshToken": first_pair["refreshToken"]})
    token = rotated["accessToken"]
    call("AuthMe")
    call("AuthRename", body={"displayName": "Focus Reviewer"})
    call("PrivacyGet")
    call("PrivacyUpdate", body={"telemetryEnabled": True, "cameraEnabled": True, "visionExperimental": True})
    call("SettingsGetPomodoro")
    call("SettingsSetPomodoro", body={
        "focusMinutes": 35, "shortBreakMinutes": 5, "longBreakMinutes": 15, "intervalsBeforeLongBreak": 4,
    })
    call("SettingsGetAvatar")
    call("SettingsSetAvatar", body={"visibility": "OnlyWhenNeeded", "intensity": "Quiet", "avatarId": None})
    profile = {"name": "Documentation review", "description": "Writing and editing focus",
               "focusMinutes": 35, "breakMinutes": 5, "rules": [{"processName": "editor", "category": "Allowed"}]}
    profile_id = {"id": call("SettingsCreateProfile", 201, profile)["id"]}
    call("SettingsGetProfiles")
    call("SettingsGetProfile", path_options=profile_id)
    call("SettingsReplaceProfile", body=profile | {"breakMinutes": 6}, path_options=profile_id)
    disposable = {"id": call("SettingsCreateProfile", 201, profile | {"name": "Unused profile"}, sample=False)["id"]}
    call("SettingsDeleteProfile", 204, path_options=disposable)
    call("PrivacyExport")
    call("SessionsCreate", 400, {"plannedDurationSeconds": 0})
    call("SessionsCreate", 415, {"plannedDurationSeconds": 2100}, mime="text/plain")
    created = call("SessionsCreate", 201, {"plannedDurationSeconds": 2100, "workProfileId": profile_id["id"]})
    session = {"id": created["id"]}
    call("SessionsGet", 404, path_options={"id": "10000000-0000-4000-8000-000000000000"})
    first_goal = call("SessionsAddGoal", 201, {"description": "Review methods"}, session)
    goal = session | {"goalId": first_goal["id"]}
    call("SessionsGetGoal", path_options=goal)
    call("SessionsUpdateGoal", body={"description": "Review research methods"}, path_options=goal)
    extra_goal = call("SessionsAddGoal", 201, {"description": "Draft notes"}, session, sample=False)
    call("SessionsDeleteGoal", 204, path_options=session | {"goalId": extra_goal["id"]})
    call("SessionsListGoals", path_options=session)
    started = call("SessionsStart", path_options=session)
    call("SessionsStart", 409, path_options=session)
    start = datetime.fromisoformat(started["startedAt"]) + timedelta(milliseconds=100)
    end = start + timedelta(seconds=1)
    time.sleep(1.3)
    window = {"clientEventId": str(uuid4()), "startedAt": start.isoformat(), "endedAt": end.isoformat(),
              "activeProcessName": "editor", "keyboardEventCount": 7, "mouseClickCount": 2,
              "windowSwitchCount": 1, "idleSeconds": 0}
    vision = window | {"clientEventId": str(uuid4()), "activeProcessName": None, "keyboardEventCount": 0,
                       "mouseClickCount": 0, "windowSwitchCount": 0,
                       "vision": {"presentMs": 700, "notDetectedMs": 0, "unknownMs": 300,
                                  "facingMs": 500, "awayMs": 0, "validSamples": 5}}
    upload = {"items": [window, vision]}
    call("ResearchUpload", body=upload, path_options=session)
    replay = call("ResearchUpload", body=upload, path_options=session, sample=False)
    assert replay == {"created": 0, "duplicates": 2}
    call("ResearchUpload", 409, body={"items": [window | {"mouseClickCount": 3}]}, path_options=session)
    call("ResearchUpload", 422, body={"items": [window | {"clientEventId": str(uuid4()),
         "startedAt": (start - timedelta(minutes=1)).isoformat(), "endedAt": (end - timedelta(minutes=1)).isoformat()}]},
         path_options=session)
    call("ResearchActivity", path_options=session)
    call("ResearchEstimates", path_options=session)
    call("ResearchCurrent", path_options=session)
    call("ResearchVision", path_options=session)
    call("SessionsPause", path_options=session)
    call("SessionsResume", path_options=session)
    completion = {"selfReportedConcentration": 4, "selfReportedProductivity": 3,
                  "reflection": "One planned interruption.",
                  "goalOutcomes": [{"goalId": goal["goalId"], "status": "Completed"}]}
    call("SessionsComplete", body=completion, path_options=session)
    call("SessionsComplete", 409, body=completion, path_options=session)
    call("SessionsGet", path_options=session)
    call("SessionsList", query="?Page=1&PageSize=10&Sort=desc")
    call("ResearchTimeline", path_options=session)
    call("AnalyticsSummary", query="?TimeZone=UTC")
    call("AnalyticsStatistics", query="?Period=Day&TimeZone=UTC")
    call("AnalyticsResearch", query="?Page=1&PageSize=10")
    planned = call("SessionsCreate", 201, {"plannedDurationSeconds": 600}, sample=False)
    call("SessionsCancel", path_options={"id": planned["id"]})
    call("SettingsDeleteProfile", 409, path_options=profile_id)
    call("AuthLogout", 204, query="?stopCamera=true")
    token = call("AuthLogin", body=login, sample=False)["accessToken"]
    call("AuthChangePassword", 204, {"currentPassword": initial_password, "newPassword": changed_password})
    token = call("AuthLogin", body=login | {"password": changed_password}, sample=False)["accessToken"]
    call("PrivacyDeleteAccount", 204, {"password": changed_password})
    for name in operations:
        if name not in examples:
            raise RuntimeError(f"No request or response captured for {name}")

    def redact(v, key=""):
        if key in {"accessToken", "refreshToken", "currentPassword", "newPassword", "password"}:
            return { "accessToken": "<access-token>", "refreshToken": "<refresh-token>" }.get(key, "<example-password>")
        if isinstance(v, dict):
            return {k: redact(value, k) for k, value in v.items()}
        if isinstance(v, list):
            return [redact(value) for value in v]
        return v

    return redact(examples)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("api_dll", type=Path)
    parser.add_argument("output", type=Path, help="New capture file outside tracked sources")
    args = parser.parse_args()
    connection = os.environ["FOCUS_TEST_DATABASE"]
    if not re.fullmatch(r"Host=127\.0\.0\.1;Port=([1-9]\d{3,4});Database=focus_test_\w+;Username=[a-zA-Z0-9_]+", connection):
        raise SystemExit("Supply a disposable explicit-port loopback database connection.")
    if ";Port=5432;" in connection:
        raise SystemExit("Use a dedicated PostgreSQL fixture rather than the default port.")
    connection = re.sub(r"Database=focus_test_\w+", "Database=focus_test_contract_" + uuid4().hex, connection)
    spec = json.loads(Path("docs/api/openapi.json").read_text(encoding="utf-8"))
    operations = {op["operationId"]: (verb.upper(), path)
                  for path, verbs in spec["paths"].items() for verb, op in verbs.items()}
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        port = listener.getsockname()[1]
    base = f"http://127.0.0.1:{port}"
    excluded = ("ASPNETCORE_", "DOTNET_ENVIRONMENT", "CONNECTIONSTRINGS__", "KESTREL__", "AUTHENTICATION__", "HTTP__", "OTEL_")
    env = {k: v for k, v in os.environ.items() if not k.upper().startswith(excluded)}
    env.update(ASPNETCORE_ENVIRONMENT="Contract", DOTNET_ENVIRONMENT="Contract",
               ASPNETCORE_URLS=base, AllowedHosts="127.0.0.1",
               ConnectionStrings__PostgreSQL=connection)
    api = args.api_dll.resolve(strict=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.with_suffix(".log").open("w", encoding="utf-8") as log:
        subprocess.run(["dotnet", str(api), "--migrate"], cwd=api.parent, env=env, stdout=log, stderr=log, check=True)
        server = subprocess.Popen(["dotnet", str(api)], cwd=api.parent, env=env, stdout=log, stderr=log)
        try:
            for _ in range(100):
                if server.poll() is not None:
                    raise RuntimeError("Fixture API stopped during startup")
                try:
                    with urlopen(base + "/health/ready", timeout=1) as response:
                        if response.status == 200:
                            break
                except (URLError, TimeoutError):
                    time.sleep(0.15)
            else:
                raise RuntimeError("Fixture API never became ready")
            captured = capture(base, operations)
            with args.output.open("x", encoding="utf-8") as output:
                json.dump(captured, output, indent=2)
                output.write("\n")
            print(f"Captured {len(captured)} real API operations with synthetic data.")
        finally:
            server.terminate()
            server.wait(timeout=10)


if __name__ == "__main__":
    main()
