#!/usr/bin/env python3
"""Guard rails for .github/variants.json and the versions it produces.

  validate_variants.py [variants.json]
      Fails when a variant definition is inconsistent:
        - a non-default Steam branch (e.g. "preview") must set "preview": true and
          the default branch must set "preview": false. Preview bits published
          without the -preview<build> suffix land in the STABLE stream and can
          shadow real releases -- that is how 3.26.3.107 / 3.26.4.100 became the
          "latest" .Publicized versions on nuget.org.
        - variant names, dirs and loginIds must be unique.

  validate_variants.py --nuspec <file.nuspec> --preview <true|false>
      Fails when the .nuspec version doesn't match the variant's stream: preview
      variants must publish X.Y.Z.N-preview<build>, stable variants plain X.Y.Z.N.

Exit codes: 0 ok, 1 validation failed, 2 usage error.
"""
import json
import re
import sys

REQUIRED_FIELDS = ("variant", "appId", "depotId", "branch", "dir", "nuspec",
                   "preview", "publicize", "anonymous", "loginId")
STABLE_RE = re.compile(r"^\d+\.\d+\.\d+\.\d+$")
PREVIEW_RE = re.compile(r"^\d+\.\d+\.\d+\.\d+-preview\d+$")


def error(message: str) -> None:
    print(f"::error::{message}")


def validate_variants(path: str) -> int:
    with open(path, encoding="utf-8") as f:
        variants = json.load(f)

    errors = 0
    seen = {"variant": {}, "dir": {}, "loginId": {}}
    for index, v in enumerate(variants):
        name = v.get("variant", f"#{index}")
        missing = [field for field in REQUIRED_FIELDS if field not in v]
        if missing:
            error(f"{path}: variant '{name}' is missing {', '.join(missing)}.")
            errors += 1
            continue

        branch, preview = v["branch"], v["preview"]
        if preview is not (branch != ""):
            expected = branch != ""
            error(f"{path}: variant '{name}' has branch '{branch or '(default)'}' but "
                  f"\"preview\": {json.dumps(preview)}; it must be {json.dumps(expected)}. "
                  "Non-default branches must publish -preview<build> prereleases, "
                  "otherwise their versions pollute the stable stream.")
            errors += 1

        for key in seen:
            value = v[key]
            if value in seen[key]:
                error(f"{path}: {key} {json.dumps(value)} is used by both "
                      f"'{seen[key][value]}' and '{name}'; it must be unique.")
                errors += 1
            else:
                seen[key][value] = name

    if errors == 0:
        print(f"OK: {len(variants)} variants in {path} are consistent.")
    return 1 if errors else 0


def validate_nuspec(nuspec: str, preview: str) -> int:
    if preview not in ("true", "false"):
        print("usage: --preview must be 'true' or 'false'", file=sys.stderr)
        return 2
    with open(nuspec, encoding="utf-8-sig") as f:
        match = re.search(r"<version>\s*([^<\s]+)\s*</version>", f.read())
    if not match:
        error(f"{nuspec}: no <version> element found.")
        return 1

    version = match.group(1)
    if preview == "true" and not PREVIEW_RE.match(version):
        error(f"{nuspec}: preview variant would publish '{version}'; "
              "expected X.Y.Z.N-preview<build>. Refusing to push a preview build into the stable stream.")
        return 1
    if preview == "false" and not STABLE_RE.match(version):
        error(f"{nuspec}: stable variant would publish '{version}'; expected X.Y.Z.N.")
        return 1
    print(f"OK: {nuspec} version '{version}' matches preview={preview}.")
    return 0


def main(argv: list) -> int:
    if len(argv) == 5 and argv[1] == "--nuspec" and argv[3] == "--preview":
        return validate_nuspec(argv[2], argv[4])
    if len(argv) in (1, 2):
        return validate_variants(argv[1] if len(argv) == 2 else ".github/variants.json")
    print("usage: validate_variants.py [variants.json] | --nuspec <file> --preview <true|false>",
          file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
