#!/usr/bin/env python3
"""Validate TruthGate's repository documentation using only the standard library."""

from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote

REPO_ROOT = Path(__file__).resolve().parents[1]
DOCS_ROOT = REPO_ROOT / "docs"
MARKDOWN_FILES = [REPO_ROOT / "README.md", REPO_ROOT / "DOCKER.md", *DOCS_ROOT.rglob("*.md")]

LINK_PATTERN = re.compile(r"(?<!!)\[[^\]]*\]\(([^)]+)\)|!\[[^\]]*\]\(([^)]+)\)")
STALE_PHRASES = (
    "TruthOrigin",
    "LetMeInBro123",
    "truthgate-run.sh",
    "Why TruthGate Cannot Be Dockerized",
)

errors: list[str] = []

if not DOCS_ROOT.is_dir():
    errors.append("docs/ does not exist")
else:
    for directory in [DOCS_ROOT, *(path for path in DOCS_ROOT.rglob("*") if path.is_dir())]:
        if not (directory / "index.md").is_file():
            errors.append(f"{directory.relative_to(REPO_ROOT)} is missing index.md")

if not (REPO_ROOT / "README.md").read_text(encoding="utf-8").find("(docs/index.md)") >= 0:
    errors.append("README.md must link to docs/index.md")

for markdown_file in MARKDOWN_FILES:
    if not markdown_file.is_file():
        errors.append(f"missing Markdown file: {markdown_file.relative_to(REPO_ROOT)}")
        continue

    content = markdown_file.read_text(encoding="utf-8")

    for stale_phrase in STALE_PHRASES:
        if stale_phrase.casefold() in content.casefold():
            errors.append(
                f"{markdown_file.relative_to(REPO_ROOT)} contains stale phrase: {stale_phrase}"
            )

    for match in LINK_PATTERN.finditer(content):
        raw_target = next(group for group in match.groups() if group is not None).strip()
        target = raw_target.split(maxsplit=1)[0].strip("<>")
        target = unquote(target.split("#", 1)[0])

        if not target or target.startswith(("http://", "https://", "mailto:")):
            continue

        resolved = (markdown_file.parent / target).resolve()
        try:
            resolved.relative_to(REPO_ROOT.resolve())
        except ValueError:
            errors.append(
                f"{markdown_file.relative_to(REPO_ROOT)} links outside repository: {raw_target}"
            )
            continue

        if not resolved.exists():
            errors.append(
                f"{markdown_file.relative_to(REPO_ROOT)} has broken link: {raw_target}"
            )

if errors:
    print("Documentation validation failed:", file=sys.stderr)
    for error in errors:
        print(f"- {error}", file=sys.stderr)
    raise SystemExit(1)

print(
    f"Documentation validation passed for {len(MARKDOWN_FILES)} Markdown files."
)
