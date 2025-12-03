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

## Asset auditing steps and required manifests
- Run an asset audit for every new or updated file before committing.
- Confirm formats and sizes meet the rules above; recompress or split assets to stay within the 25 MB guideline unless a documented exception applies.
- Verify folder placement and naming (including locale suffixes) reflect the feature domain and release cadence.
- Update or create the relevant manifest/index files that enumerate the assets for a domain (e.g., `configs_manifest.json`, `audio_manifest.json`, `localization_manifest.json`), keeping them beside the assets they track.
- Capture audit results in PR/testing notes using a short checklist: format/size verified, manifest/index updated, locale coverage confirmed, cadence/rollout risks noted, and any removals paired with cleanup entries.

## Adding or removing assets
- Update relevant manifests or registries whenever assets are added, renamed, or removed.
- Validate references in code or config to avoid dangling asset names.
- Remove superseded files promptly to avoid clutter; archive large historical assets outside this folder if needed.
