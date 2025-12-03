# AGENTS Guidance — Assets/StreamingAssets

## Scope and inheritance
- Applies to all files under `Assets/StreamingAssets/`; inherits the root AGENTS.md for general process notes.
- Use these rules to keep streaming content organized and performant.

## Allowed formats and size limits
- Allowed file types: JSON, CSV, binary data bundles, audio (WAV/OGG), and localization tables; avoid executable or scriptable content.
- Default max file size: 25 MB per asset unless a platform constraint requires smaller; flag exceptions in the PR/testing notes.

## Folder structure and naming
- Group assets by feature domain (e.g., `Localization/`, `Configs/`, `Audio/`); avoid flat dumps.
- Name data bundles with semantic versions or dates (e.g., `loot_tables_v1.2.json`); include locale codes where relevant (e.g., `strings_en-US.json`).
- Keep manifests/index files alongside their bundles when applicable (e.g., `configs_manifest.json`).

## Localization and cadence
- Store localized variants side-by-side with locale suffixes; ensure default locale files exist.
- When updating live content, prefer additive updates over destructive replacements; document cadence expectations in the PR/testing notes when changing update frequency.

## Adding or removing assets
- Update relevant manifests or registries whenever assets are added, renamed, or removed.
- Validate references in code or config to avoid dangling asset names.
- Remove superseded files promptly to avoid clutter; archive large historical assets outside this folder if needed.
