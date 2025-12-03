import argparse
import fnmatch
import json
import sys
from pathlib import Path
from typing import Iterable, List, Set


def load_json(path: Path) -> dict:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def normalize_path(path: str) -> str:
    return path.replace("\\", "/")


def load_patterns(files: List[Path], inline_patterns: List[str]) -> List[str]:
    patterns: List[str] = []
    for entry in inline_patterns:
        normalized = entry.strip()
        if normalized:
            patterns.append(normalized)

    for file_path in files:
        if not file_path.is_file():
            continue

        for raw_line in file_path.read_text(encoding="utf-8").splitlines():
            line = raw_line.strip()
            if not line or line.startswith("#"):
                continue
            patterns.append(line)

    return patterns


def load_changed_paths(path: Path) -> Set[str]:
    if not path.is_file():
        return set()

    changed: Set[str] = set()
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        normalized = normalize_path(raw_line.strip())
        if not normalized:
            continue
        changed.add(normalized)
        if normalized.endswith(".meta"):
            changed.add(normalized[:-5])
    return changed


def is_whitelisted(path: str, patterns: Iterable[str]) -> bool:
    normalized = normalize_path(path)
    for pattern in patterns:
        if fnmatch.fnmatch(normalized, pattern) or normalized.startswith(pattern.rstrip("*/")):
            return True
    return False


def format_asset(asset: dict) -> str:
    labels = asset.get("labels") or []
    label_display = "(no labels)" if len(labels) == 0 else ", ".join(labels)
    return f"- `{asset.get('path', '(unknown)')}` [{asset.get('type', 'Unknown')}] labels: {label_display}"


def write_outputs(path: Path, values: dict) -> None:
    with path.open("a", encoding="utf-8") as handle:
        for key, value in values.items():
            handle.write(f"{key}={value}\n")


def main() -> int:
    parser = argparse.ArgumentParser(description="Analyze AssetUsageAuditor output for CI gating.")
    parser.add_argument("--audit-json", type=Path, required=True, help="Path to asset audit JSON report.")
    parser.add_argument("--changed-files", type=Path, required=False, help="Optional path to file containing changed files, one per line.")
    parser.add_argument("--whitelist", type=Path, action="append", default=[], help="Path(s) to whitelist pattern files.")
    parser.add_argument("--whitelist-pattern", action="append", default=[], help="Inline whitelist pattern (glob).")
    parser.add_argument("--summary-markdown", type=Path, required=True, help="Where to write the rendered summary markdown.")
    parser.add_argument("--summary-json", type=Path, required=True, help="Where to write the computed summary JSON.")
    parser.add_argument("--github-output", type=Path, help="Optional path to $GITHUB_OUTPUT for sharing values.")
    parser.add_argument("--max-listed-assets", type=int, default=20, help="Maximum number of assets to list explicitly in summaries.")
    parser.add_argument("--fail-on-new-unused", action="store_true", help="Return a non-zero exit code when new unused assets are detected.")
    args = parser.parse_args()

    report = load_json(args.audit_json)
    zero_reference_assets = [
        {
            "path": normalize_path(asset.get("path", "")),
            "type": asset.get("type", "Unknown"),
            "labels": asset.get("labels", []),
        }
        for asset in report.get("zeroReferenceAssets", [])
    ]

    whitelist_patterns = load_patterns(args.whitelist, args.whitelist_pattern)
    changed_paths = load_changed_paths(args.changed_files) if args.changed_files else set()
    has_changed_files = len(changed_paths) > 0

    new_unused_assets = []
    if has_changed_files:
        new_unused_assets = [
            asset
            for asset in zero_reference_assets
            if asset["path"] in changed_paths
            and not is_whitelisted(asset["path"], whitelist_patterns)
        ]

    summary = {
        "generatedFrom": str(args.audit_json),
        "changedFilesConsidered": has_changed_files,
        "zeroReferenceCount": len(zero_reference_assets),
        "newUnusedCount": len(new_unused_assets),
        "whitelistPatterns": whitelist_patterns,
        "listedAssets": new_unused_assets[: args.max_listed_assets],
    }

    args.summary_json.parent.mkdir(parents=True, exist_ok=True)
    args.summary_json.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    lines = [
        "## Asset Usage Audit",
        f"- Zero-reference assets: {len(zero_reference_assets)}",
        f"- New unused assets in this change: {len(new_unused_assets)}",
    ]

    if whitelist_patterns:
        lines.append("- Whitelisted paths: " + ", ".join(whitelist_patterns))

    lines.append("")

    if not has_changed_files:
        lines.append("No changed assets detected; reporting only for visibility.")
    elif len(new_unused_assets) == 0:
        lines.append("No new unused assets detected in the touched files.")
    else:
        lines.append("New unused assets outside whitelist:")
        for asset in new_unused_assets[: args.max_listed_assets]:
            lines.append(format_asset(asset))
        if len(new_unused_assets) > args.max_listed_assets:
            lines.append(f"- ...and {len(new_unused_assets) - args.max_listed_assets} more")

    args.summary_markdown.parent.mkdir(parents=True, exist_ok=True)
    args.summary_markdown.write_text("\n".join(lines) + "\n", encoding="utf-8")

    outputs = {
        "zero_reference_count": len(zero_reference_assets),
        "new_unused_count": len(new_unused_assets),
        "summary_markdown": str(args.summary_markdown),
        "summary_json": str(args.summary_json),
    }

    if args.github_output:
        write_outputs(args.github_output, outputs)

    if args.fail_on_new_unused and len(new_unused_assets) > 0:
        return 2

    return 0


if __name__ == "__main__":
    sys.exit(main())
