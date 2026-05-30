#!/usr/bin/env python3
"""Compare two RocketModFix.Unturned.Redist NuGet versions.

Prints one of: lt | eq | gt  (how NEW compares to OLD).

Handles the two version shapes this repo produces:
  - stable:     X.Y.Z.N                 (e.g. 3.26.3.2)
  - preview:    X.Y.Z.N-preview<build>  (e.g. 3.26.4.100-preview23479280)

NuGet/SemVer ordering: a stable release outranks any prerelease of the same
core version, and prereleases are ordered by their numeric build id.

Usage: compare_nuget_version.py <new> <old>
Exit codes: 0 on success, 2 on an unrecognized version string.
"""
import re
import sys

_VERSION_RE = re.compile(r"^(\d+)\.(\d+)\.(\d+)\.(\d+)(?:-preview(\d+))?$")


def parse(version: str):
    match = _VERSION_RE.match(version.strip())
    if not match:
        print(f"::error::Unrecognized NuGet version format: {version}", file=sys.stderr)
        sys.exit(2)
    core = tuple(int(x) for x in match.groups()[:4])
    pre = match.group(5)
    # (core, is_release, prerelease_build): release (1) sorts above prerelease (0).
    return (core, 1, 0) if pre is None else (core, 0, int(pre))


def main() -> None:
    if len(sys.argv) != 3:
        print("usage: compare_nuget_version.py <new> <old>", file=sys.stderr)
        sys.exit(2)
    new, old = parse(sys.argv[1]), parse(sys.argv[2])
    print("lt" if new < old else ("eq" if new == old else "gt"))


if __name__ == "__main__":
    main()
