# AGENTS Guidance

## Purpose
- This file sets project-wide expectations for contributors and explains how AGENTS.md files provide scoped instructions.
- Follow these guidelines to keep contributions consistent, reviewable, and easy to maintain.

## How to use
- Scope: This root AGENTS.md applies to the entire repository unless a deeper AGENTS.md overrides parts of it.
- Precedence: The most deeply nested AGENTS.md governing a file takes priority. When editing files, read every AGENTS.md from the repository root to the file's directory and follow the most specific rules.
- Contribution checklist: before submitting, confirm that you followed the applicable AGENTS.md files, ran relevant tests, and captured any PR messaging requirements below.

## Coding and style conventions
- Prefer clear, maintainable code that matches existing patterns; avoid introducing unnecessary dependencies.
- Keep functions and classes small and purposeful; favor readability over premature optimization.
- Validate inputs defensively and handle error cases explicitly; avoid silent failures.
- Document non-obvious decisions in comments; keep comments current with the code.
- Do not wrap imports in try/catch blocks.

## Testing expectations
- Run the narrowest relevant test suites for your changes (unit, integration, or playmode/editmode for Unity).
- Note any tests you skip and why in the PR/testing notes.
- Ensure automated checks are green before merging when possible.

## PR message requirements
- Summarize key changes and rationale succinctly.
- Call out any migrations, configuration updates, or follow-up work required.
- List tests executed (or note if none were run) and any known limitations.

## Additional notes
- Deeper AGENTS.md files refine or override these rules; always consult them for files in their scope.
- Asset auditing protocols: follow the asset auditing steps outlined in scoped AGENTS.md files whenever you add, modify, or remove assets (especially under `Assets/StreamingAssets/`), and call out compliance in PR/testing notes.
