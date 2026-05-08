#!/usr/bin/env python3
"""Verifies per-lane coverage thresholds from dotnet XPlat Code Coverage output."""

from __future__ import annotations

import argparse
import glob
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass


@dataclass(frozen=True)
class LaneCoverage:
    lane: str
    percent: float


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate lane coverage thresholds from Cobertura reports.")
    parser.add_argument("--unit-threshold", type=float, required=True)
    parser.add_argument("--contracts-threshold", type=float, required=True)
    parser.add_argument("--infrastructure-threshold", type=float, required=True)
    parser.add_argument("--webapi-threshold", type=float, required=True)
    parser.add_argument("--webadmin-threshold", type=float, required=True)
    parser.add_argument("--integration-threshold", type=float, required=True)
    parser.add_argument("--mobile-shared-threshold", type=float, required=True)
    return parser.parse_args()


def find_cobertura_percent(search_root: str) -> float:
    matches = glob.glob(f"{search_root}/**/coverage.cobertura.xml", recursive=True)
    if not matches:
        raise FileNotFoundError(f"No coverage.cobertura.xml found under '{search_root}'.")

    total_covered = 0
    total_valid = 0

    for file_path in matches:
        root = ET.parse(file_path).getroot()
        lines_covered = int(root.attrib.get("lines-covered", "0"))
        lines_valid = int(root.attrib.get("lines-valid", "0"))
        total_covered += lines_covered
        total_valid += lines_valid

    if total_valid <= 0:
        raise ValueError(f"Coverage report under '{search_root}' has zero valid lines.")

    return (total_covered / total_valid) * 100.0


def validate_lane(lane: str, root: str, threshold: float) -> LaneCoverage:
    percent = find_cobertura_percent(root)
    print(f"{lane}: {percent:.2f}% (threshold {threshold:.2f}%)")
    if percent < threshold:
        raise ValueError(f"{lane} coverage {percent:.2f}% is below threshold {threshold:.2f}%.")

    return LaneCoverage(lane=lane, percent=percent)


def main() -> int:
    args = parse_args()

    lanes = [
        ("unit", "TestResults/unit", args.unit_threshold),
        ("contracts", "TestResults/contracts", args.contracts_threshold),
        ("infrastructure", "TestResults/infrastructure", args.infrastructure_threshold),
        ("webapi", "TestResults/webapi", args.webapi_threshold),
        ("webadmin", "TestResults/webadmin", args.webadmin_threshold),
        ("integration", "TestResults/integration", args.integration_threshold),
        ("mobile-shared", "TestResults/mobile-shared", args.mobile_shared_threshold),
    ]

    try:
        results = [validate_lane(name, root, threshold) for name, root, threshold in lanes]
    except (FileNotFoundError, ValueError, ET.ParseError) as ex:
        print(f"Coverage gate failed: {ex}")
        return 1

    summary = ", ".join(f"{item.lane}={item.percent:.2f}%" for item in results)
    print(f"Coverage gate passed: {summary}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
