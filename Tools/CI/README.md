# CI Tooling

This folder contains utilities that support auditing within CI and during local verification.

## Asset usage audit helper

`analyze_asset_audit.py` consumes the JSON produced by `AssetUsageAuditor.RunAuditFromBatch` and summarizes unused assets for CI:

```bash
python3 Tools/CI/analyze_asset_audit.py \
  --audit-json Logs/asset_audits/asset_audit_2024-01-01.json \
  --changed-files /tmp/changed_files.txt \
  --whitelist .github/asset-audit-whitelist.txt \
  --summary-markdown Logs/asset_audits/asset_audit_summary.md \
  --summary-json Logs/asset_audits/asset_audit_summary.json \
  --fail-on-new-unused
```

Key flags:
- `--changed-files` (optional): newline-delimited list of paths to scope the "new unused" calculation.
- `--whitelist`/`--whitelist-pattern`: allow-list unused assets.
- `--fail-on-new-unused`: set a non-zero exit code when new unused assets are found.

Outputs include both Markdown (for PR comments and job summaries) and JSON (for artifacts/automation).

## C# public/serialized usage audit

`analyze_csharp_usage.csx` is a Roslyn-powered script that scans the Unity solution or a fallback ad-hoc workspace for public or serialized methods/fields that are not referenced anywhere else in the codebase. It mirrors the asset audit flow so CI can flag newly introduced legacy-only members.

Requirements:
- .NET SDK (for `dotnet`), plus [`dotnet-script`](https://github.com/dotnet-script/dotnet-script) available on the `PATH`.
- A Unity-generated solution file (`*.sln`). If no solution is present, the script creates an ad-hoc project from the repository's C# sources to keep analysis available.

Example usage:

```bash
# Install dotnet-script if needed
# dotnet tool install -g dotnet-script

# Run the analyzer and emit Markdown/JSON summaries
~/.dotnet/tools/dotnet-script Tools/CI/analyze_csharp_usage.csx \
  --project-root . \
  --solution ARPGCORE.sln \
  --changed-files /tmp/changed_files.txt \
  --whitelist .github/legacy-member-whitelist.txt \
  --summary-markdown Logs/asset_audits/csharp_usage_summary.md \
  --summary-json Logs/asset_audits/csharp_usage_summary.json \
  --fail-on-new-legacy
```

Flags:
- `--project-root`: base path for relative output and display (defaults to the current directory).
- `--solution`: path to the Unity solution (`*.sln`). If omitted or missing, the script attempts to locate one automatically and otherwise falls back to an ad-hoc workspace.
- `--changed-files`: newline-delimited list used to calculate the "new unused" count for gating.
- `--whitelist`/`--whitelist-pattern`: wildcard patterns applied to symbol display names or file paths to ignore known intentional members. The repository keeps patterns in `.github/legacy-member-whitelist.txt`.
- `--fail-on-new-legacy`: exit with a non-zero code when new unused members appear in touched files.
- `--max-listed-members`: cap the number of individual members echoed into Markdown/JSON summaries.

The tool emits:
- `unused_count` and `new_unused_count` values (also written to `$GITHUB_OUTPUT` when provided).
- Markdown and JSON summaries stored next to the asset audit outputs for easy artifact collection.

## CI integration

The `asset-usage-audit` workflow installs `dotnet-script`, runs the C# usage audit alongside the asset audit, uploads both reports as artifacts, and gates builds when new unused members slip in (configurable via `LEGACY_WARN_ONLY`).
