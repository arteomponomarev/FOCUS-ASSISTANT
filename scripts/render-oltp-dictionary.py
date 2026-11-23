"""Render a PostgreSQL catalog dictionary produced by docs/database/oltp-introspect.sql."""

import argparse
import json
from pathlib import Path


def cell(value):
    return "`" + value.replace("|", "\\|") + "`" if value is not None else "-"


def render(schema):
    output = ["# PostgreSQL OLTP physical data dictionary", "",
              "Generated from the disposable PostgreSQL 17 catalog after applying all EF migrations.",
              "Column nullability and defaults come from the catalog. Refer to the [3NF and integrity analysis](oltp-verification.md) for domains and semantics.",
              "UNIQUE indexes are listed even when the catalog represents them as indexes rather than named constraints.", ""]
    for table in schema["tables"]:
        output.extend([f"## {table}", "", "| Column | PostgreSQL type | Nullable | Default |",
                       "| --- | --- | --- | --- |"])
        for _, name, kind, not_null, default in (row for row in schema["columns"] if row[0] == table):
            output.append(f"| {cell(name)} | {cell(kind)} | {'No' if not_null else 'Yes'} | {cell(default)} |")
        output.extend(["", "| Constraint | Definition |", "| --- | --- |"])
        for _, name, definition in (row for row in schema["constraints"] if row[0] == table):
            output.append(f"| {cell(name)} | {cell(definition)} |")
        output.extend(["", "| Index | Definition |", "| --- | --- |"])
        for _, name, definition in (row for row in schema["indexes"] if row[0] == table):
            output.append(f"| {cell(name)} | {cell(definition)} |")
        output.append("")
    return "\n".join(output)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("catalog", type=Path)
    args = parser.parse_args()
    schema = json.loads(args.catalog.read_text(encoding="utf-8-sig"))
    Path("docs/database/oltp-data-dictionary.md").write_text(render(schema), encoding="utf-8")
    print(*(f"{name}: {len(rows)}" for name, rows in schema.items()), sep="\n")


if __name__ == "__main__":
    main()
