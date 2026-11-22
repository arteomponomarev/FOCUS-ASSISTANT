"""Validate one isolated Cobertura run and enforce the business logic floor."""

import argparse
from pathlib import Path
from xml.etree import ElementTree


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("report_directory", type=Path)
    parser.add_argument("--minimum", type=float, default=80)
    args = parser.parse_args()

    if not 0 <= args.minimum <= 100:
        raise SystemExit("The coverage threshold must be between 0 and 100.")

    # VSTest can attach a byte-identical copy of the report to the TRX result.
    reports = {path.read_bytes() for path in args.report_directory.rglob("coverage.cobertura.xml")}
    if len(reports) != 1:
        raise SystemExit(f"Expected one unique Cobertura report in a fresh results directory, found {len(reports)}.")

    root = ElementTree.fromstring(reports.pop())
    packages = {package.attrib["name"]: package for package in root.findall("./packages/package")}
    for name in ("Focus.Domain", "Focus.Application", "Focus.Engine"):
        if name not in packages or not packages[name].findall(".//line"):
            raise SystemExit(f"Missing measured package: {name}")
        package = packages[name]
        print(f"{name}: {100 * float(package.attrib['line-rate']):.2f}% lines; "
              f"{100 * float(package.attrib['branch-rate']):.2f}% branches")

    covered = int(root.attrib["lines-covered"])
    measurable = int(root.attrib["lines-valid"])
    if not measurable:
        raise SystemExit("Coverage report contains no measurable lines.")

    rate = 100 * covered / measurable
    print(f"Business logic: {rate:.2f}% ({covered}/{measurable} lines); "
          f"{100 * float(root.attrib['branch-rate']):.2f}% branches")
    if rate < args.minimum:
        raise SystemExit(f"Business logic coverage is below {args.minimum:g}%.")


if __name__ == "__main__":
    main()
