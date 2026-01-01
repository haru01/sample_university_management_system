#!/usr/bin/env python3
"""
EF Core Slow Query Detector

Scans C# codebases for common Entity Framework Core performance anti-patterns.
"""

import os
import re
import sys
from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Generator


class Severity(Enum):
    CRITICAL = "CRITICAL"
    WARNING = "WARNING"
    INFO = "INFO"


@dataclass
class Issue:
    severity: Severity
    pattern: str
    file: str
    line: int
    code: str
    suggestion: str


def find_cs_files(root: Path) -> Generator[Path, None, None]:
    """Find all .cs files recursively."""
    for path in root.rglob("*.cs"):
        if "obj" not in path.parts and "bin" not in path.parts:
            yield path


def analyze_file(file_path: Path) -> list[Issue]:
    """Analyze a single C# file for performance issues."""
    issues = []

    try:
        content = file_path.read_text(encoding="utf-8")
        lines = content.split("\n")
    except Exception as e:
        print(f"Warning: Could not read {file_path}: {e}", file=sys.stderr)
        return issues

    in_foreach = False
    foreach_start_line = 0

    for i, line in enumerate(lines, 1):
        stripped = line.strip()

        # Pattern 1: N+1 - foreach with await inside
        if "foreach" in stripped and "await" not in stripped:
            in_foreach = True
            foreach_start_line = i

        if in_foreach and "await" in stripped and ("Repository" in stripped or "_context" in stripped or "Async" in stripped):
            issues.append(Issue(
                severity=Severity.CRITICAL,
                pattern="N+1 Query",
                file=str(file_path),
                line=i,
                code=stripped[:100],
                suggestion="Move query outside loop or use Include/batch loading"
            ))

        if in_foreach and "}" in stripped and "{" not in stripped:
            in_foreach = False

        # Pattern 2: ToListAsync followed by LINQ in-memory operations
        if re.search(r"ToListAsync\(\).*\.(Max|Min|Sum|Average|Count|Where|Select|OrderBy)", stripped):
            issues.append(Issue(
                severity=Severity.CRITICAL,
                pattern="In-Memory Aggregation",
                file=str(file_path),
                line=i,
                code=stripped[:100],
                suggestion="Use database-level aggregation (MaxAsync, CountAsync, etc.)"
            ))

        # Pattern 3: ToListAsync() - check for full table load
        if re.search(r"\.ToListAsync\(\s*\)", stripped):
            context_start = max(0, i - 5)
            context = "\n".join(lines[context_start:i])
            if not re.search(r"\.(Take|Skip|First|Single)\s*\(", context):
                if re.search(r"(GetNext|NextId|MaxId)", "".join(lines[max(0,i-10):i+5])):
                    issues.append(Issue(
                        severity=Severity.CRITICAL,
                        pattern="Full Table Load for ID",
                        file=str(file_path),
                        line=i,
                        code=stripped[:100],
                        suggestion="Use MaxAsync() or database sequence instead"
                    ))
                else:
                    issues.append(Issue(
                        severity=Severity.WARNING,
                        pattern="Unbounded Query",
                        file=str(file_path),
                        line=i,
                        code=stripped[:100],
                        suggestion="Consider adding Take() or pagination"
                    ))

        # Pattern 4: Missing AsNoTracking on queries
        if re.search(r"await\s+_context\.\w+\.(Where|First|Single|ToList)", stripped):
            context_start = max(0, i - 3)
            context = "\n".join(lines[context_start:i+1])
            if "AsNoTracking" not in context:
                issues.append(Issue(
                    severity=Severity.INFO,
                    pattern="Missing AsNoTracking",
                    file=str(file_path),
                    line=i,
                    code=stripped[:100],
                    suggestion="Add AsNoTracking() for read-only queries"
                ))

        # Pattern 5: String concatenation in queries
        if re.search(r'(Where|FromSql).*\+.*"', stripped) or re.search(r'(Where|FromSql).*\$"', stripped):
            issues.append(Issue(
                severity=Severity.WARNING,
                pattern="String in Query",
                file=str(file_path),
                line=i,
                code=stripped[:100],
                suggestion="Use parameterized queries or LINQ expressions"
            ))

    return issues


def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_efcore.py <path-to-src>")
        print("Example: python analyze_efcore.py ./backend/src")
        sys.exit(1)

    root = Path(sys.argv[1])
    if not root.exists():
        print(f"Error: Path does not exist: {root}")
        sys.exit(1)

    print(f"Scanning {root} for EF Core performance issues...\n")

    all_issues: list[Issue] = []
    files_scanned = 0

    for cs_file in find_cs_files(root):
        files_scanned += 1
        issues = analyze_file(cs_file)
        all_issues.extend(issues)

    severity_order = {Severity.CRITICAL: 0, Severity.WARNING: 1, Severity.INFO: 2}
    all_issues.sort(key=lambda x: (severity_order[x.severity], x.file, x.line))

    print(f"Scanned {files_scanned} files\n")

    if not all_issues:
        print("No issues detected.")
        return

    critical = sum(1 for i in all_issues if i.severity == Severity.CRITICAL)
    warning = sum(1 for i in all_issues if i.severity == Severity.WARNING)
    info = sum(1 for i in all_issues if i.severity == Severity.INFO)

    print(f"Found {len(all_issues)} issues: {critical} critical, {warning} warnings, {info} info\n")
    print("=" * 80)

    current_severity = None
    for issue in all_issues:
        if issue.severity != current_severity:
            current_severity = issue.severity
            print(f"\n## {current_severity.value}\n")

        rel_path = issue.file
        try:
            rel_path = str(Path(issue.file).relative_to(root))
        except ValueError:
            pass

        print(f"[{issue.pattern}] {rel_path}:{issue.line}")
        print(f"  Code: {issue.code}")
        print(f"  Fix: {issue.suggestion}")
        print()


if __name__ == "__main__":
    main()
