"""Validate rendered Compose DSNs against the two host libpq services without logging values."""

import configparser
import json
import re
import sys


FIELD = re.compile(r'''\s*([^=;]+?)\s*=\s*("(?:[^"]|"")*"|'(?:[^']|'')*'|[^;"']*)\s*(?:;|$)''')
KEYS = {"host", "port", "database", "username", "password", "ssl mode", "root certificate"}
CA = "/etc/focus/postgres-ca.crt"


def parse_dsn(value):
    fields = {}
    cursor = 0
    while cursor < len(value):
        match = FIELD.match(value, cursor)
        if match is None:
            raise ValueError
        key, text = match.groups()
        key, text = key.strip().lower(), text.strip()
        if key in fields or key not in KEYS or not text:
            raise ValueError
        if text[0] in "\"'":
            text = text[1:-1].replace(text[0] * 2, text[0])
        fields[key] = text
        cursor = match.end()
    if fields.keys() != KEYS or not fields["password"]:
        raise ValueError
    return fields


def verify(config, pg_services):
    endpoints = set()
    for service, kind in ("api", "runtime"), ("migrate", "migrate"):
        role = "focus_" + kind
        env = config["services"][service]["environment"]
        dsn = parse_dsn(env["ConnectionStrings__PostgreSQL"])
        pg = pg_services["focus-" + kind]
        if not re.fullmatch(r"private-[a-z0-9.-]+\.db\.ondigitalocean\.com", dsn["host"]):
            raise ValueError
        if (dsn["host"] != pg["host"] or dsn["port"] != pg["port"]
                or not 1 <= int(dsn["port"]) <= 65535
                or dsn["database"] != "focus_production" or pg["dbname"] != dsn["database"]
                or dsn["username"] != role or pg["user"] != role
                or dsn["ssl mode"].lower() != "verifyfull" or pg["sslmode"] != "verify-full"
                or dsn["root certificate"] != CA or pg["sslrootcert"] != CA):
            raise ValueError
        endpoints.add((dsn["host"], dsn["port"]))
    if len(endpoints) != 1:
        raise ValueError


if __name__ == "__main__":
    try:
        services = configparser.ConfigParser(interpolation=None)
        with open(sys.argv[1], encoding="utf-8") as source:
            services.read_file(source)
        verify(json.load(sys.stdin), services)
    except (IndexError, KeyError, OSError, TypeError, ValueError, configparser.Error):
        sys.exit("Managed connection configuration failed the private endpoint, role or TLS policy.")
