"""Verify the public API before and after an operator-controlled restart."""

import argparse
from datetime import datetime, timedelta
from getpass import GetPassWarning, getpass
import json
from pathlib import Path
import re
import socket
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener
from uuid import UUID, uuid4
import warnings


class SmokeFailure(Exception):
    pass


class NoRedirects(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class Api:
    def __init__(self, origin):
        self.origin = origin
        self.token = None
        self.http = build_opener(NoRedirects())

    def call(self, method, path, expected, payload=None):
        headers = {"Accept": "application/json"}
        body = None
        if payload is not None:
            headers["Content-Type"] = "application/json"
            body = json.dumps(payload).encode()
        if self.token:
            headers["Authorization"] = "Bearer " + self.token

        request = Request(self.origin + path, body, headers, method=method)
        try:
            with self.http.open(request, timeout=15) as response:
                if response.status != expected:
                    raise SmokeFailure(f"{method} {path} returned HTTP {response.status}; expected {expected}.")
                if response.headers.get_content_type() == "application/json":
                    return json.load(response)
                return None
        except HTTPError as error:
            error.close()
            raise SmokeFailure(f"{method} {path} returned HTTP {error.code}; expected {expected}.") from None
        except (URLError, TimeoutError) as error:
            raise SmokeFailure(f"{method} {path} failed TLS or transport checks ({type(error).__name__}).") from None

    def health(self):
        for probe in ("live", "ready"):
            self.call("GET", "/health/" + probe, 200)

    def login(self, credentials):
        result = self.call("POST", "/api/v1/auth/login", 200, credentials)
        token = result["accessToken"]
        if not isinstance(token, str) or not token:
            raise SmokeFailure("Login did not return an access token.")
        self.token = token

    def logout(self):
        self.call("POST", "/api/v1/auth/logout", 204)
        self.token = None


def read_session(api, state):
    session = "/api/v1/sessions/" + state["sessionId"]
    detail = api.call("GET", session, 200)
    analytics = api.call("GET", "/api/v1/analytics/summary", 200)["metrics"]
    if analytics["completedSessions"] != 1 or analytics["keyboardEvents"] != 7:
        raise SmokeFailure("Owner analytics differs from the synthetic fixture.")
    if state.get("vision"):
        report = api.call("GET", session + "/vision", 200)
        if report["presentMs"] != 700 or report["unknownMs"] != 300 or report["validSamples"] != 5:
            raise SmokeFailure("Stored camera aggregates differ from the synthetic fixture.")
    activity = api.call("GET", session + "/activity", 200)
    estimates = api.call("GET", session + "/focus-states", 200)
    if (
        detail["id"] != state["sessionId"]
        or detail["status"] != "Completed"
        or detail["selfReportedConcentration"] != 4
        or detail["selfReportedProductivity"] != 3
        or not any(goal["id"] == state["goalId"] and goal["status"] == "Completed" for goal in detail["goals"])
        or activity["totalCount"] != 1
        or activity["items"][0]["keyboardEventCount"] != 7
        or estimates["totalCount"] != 1
        or not estimates["items"][0]["ruleVersion"]
    ):
        raise SmokeFailure("Stored session, goal, aggregate or estimate differs from the synthetic fixture.")


def prepare(api, state_path, password, include_vision=False):
    state = {"origin": api.origin, "email": f"focus-smoke-{uuid4().hex}@example.invalid"}
    # Reserve a new file before creating an account and keep the recovery email if a step fails.
    with state_path.open("x", encoding="utf-8") as output:
        json.dump(state, output)
    state["vision"] = include_vision
    credentials = {"email": state["email"], "password": password}
    api.call("POST", "/api/v1/auth/register", 201, credentials | {"displayName": "Deployment Smoke Test"})
    api.login(credentials)
    api.call("PATCH", "/api/v1/settings/privacy", 200, {
        "telemetryEnabled": True, "cameraEnabled": include_vision,
    })
    created = api.call("POST", "/api/v1/sessions", 201, {"plannedDurationSeconds": 600})
    state["sessionId"] = str(UUID(created["id"]))
    session = "/api/v1/sessions/" + state["sessionId"]
    goal = api.call("POST", session + "/goals", 201, {"description": "Verify deployment persistence"})
    state["goalId"] = str(UUID(goal["id"]))
    started = api.call("POST", session + "/start", 200)
    start = datetime.fromisoformat(started["startedAt"]) + timedelta(milliseconds=50)
    end = start + timedelta(seconds=1)
    time.sleep(1.2)
    item = {
        "clientEventId": str(uuid4()),
        "startedAt": start.isoformat(),
        "endedAt": end.isoformat(),
        "activeProcessName": "editor",
        "keyboardEventCount": 7,
        "mouseClickCount": 2,
        "windowSwitchCount": 1,
        "idleSeconds": 0,
    }
    items = [item]
    if include_vision:
        items.append(item | {
            "clientEventId": str(uuid4()),
            "activeProcessName": None, "keyboardEventCount": 0, "mouseClickCount": 0,
            "windowSwitchCount": 0,
            "vision": {"presentMs": 700, "notDetectedMs": 0, "unknownMs": 300,
                       "facingMs": 500, "awayMs": 0, "validSamples": 5},
        })
    accepted = api.call("POST", session + "/activity/batch", 200, {"items": items})
    replay = api.call("POST", session + "/activity/batch", 200, {"items": items})
    if (accepted != {"created": len(items), "duplicates": 0}
            or replay != {"created": 0, "duplicates": len(items)}):
        raise SmokeFailure("The aggregate or duplicate replay did not have the expected outcome.")
    api.call(
        "POST", session + "/complete", 200,
        {"selfReportedConcentration": 4, "selfReportedProductivity": 3,
         "goalOutcomes": [{"goalId": state["goalId"], "status": "Completed"}]},
    )
    read_session(api, state)
    api.logout()
    state_path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
    print("PASS: HTTPS, health, authentication, session, goal, telemetry replay, analytics and readback.")
    print("Record and perform the API restart separately, then run verify.")


def verify(api, state_path, password, cleanup):
    state = json.loads(state_path.read_text(encoding="utf-8"))
    if state["origin"] != api.origin or not re.fullmatch(
        r"focus-smoke-[a-f0-9]{32}@example\.invalid", state["email"]
    ):
        raise SmokeFailure("The state file does not belong to this origin and a smoke-test account.")
    for key in ("sessionId", "goalId"):
        state[key] = str(UUID(state[key]))
    api.login({"email": state["email"], "password": password})
    read_session(api, state)
    if cleanup:
        api.call("POST", "/api/v1/settings/privacy/delete-account", 204, {"password": password})
        print("Disposable smoke account removed.")
    else:
        api.logout()
    print("PASS: stored session, goal, aggregate, estimate and analytics retrieved. Pair with the operator's restart evidence.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("phase", choices=("prepare", "verify"))
    parser.add_argument("origin", help="HTTPS origin without credentials, path, query or fragment")
    parser.add_argument("--state", type=Path, required=True, help="Local file for synthetic IDs and account email")
    parser.add_argument("--vision", action="store_true", help="Include synthetic CV aggregates in prepare")
    parser.add_argument("--cleanup", action="store_true", help="Delete the disposable account after verify succeeds")
    args = parser.parse_args()
    try:
        origin = urlsplit(args.origin)
        if (origin.scheme != "https" or not origin.hostname or origin.username is not None
                or origin.password is not None or origin.path not in ("", "/")
                or origin.query or origin.fragment or origin.port == 0):
            raise SmokeFailure("Use an HTTPS origin without credentials, path, query or fragment.")
        if args.phase == "prepare" and args.cleanup:
            raise SmokeFailure("--cleanup applies only to verify.")
        if args.phase == "verify" and args.vision:
            raise SmokeFailure("--vision applies only to prepare; verify reads the saved fixture type.")
        socket.getaddrinfo(origin.hostname, origin.port or 443, type=socket.SOCK_STREAM)
        api = Api(args.origin.rstrip("/"))
        api.health()
        with warnings.catch_warnings():
            warnings.simplefilter("error", GetPassWarning)
            password = getpass("Disposable account password (same in both phases): ")
        if not 12 <= len(password) <= 128:
            raise SmokeFailure("Use a generated password between 12 and 128 characters.")
        if args.phase == "prepare":
            prepare(api, args.state, password, args.vision)
        else:
            verify(api, args.state, password, args.cleanup)
    except SmokeFailure as error:
        sys.exit(str(error))
    except GetPassWarning:
        sys.exit("Run the smoke test in an interactive terminal with hidden password input.")
    except (OSError, ValueError, TypeError, KeyError, IndexError):
        sys.exit("Could not read the expected API response or smoke state. Check the state file and API contract.")


if __name__ == "__main__":
    main()
