# AGENTS Guidance — Assets/Scripts

## Scope and inheritance
- Applies to all files under `Assets/Scripts/` and inherits root AGENTS.md; follow any more-nested AGENTS.md if present.
- Use these Unity/C# conventions to keep gameplay code consistent and scene-safe.

## C# naming and structure
- Use PascalCase for classes, structs, enums, and public members; camelCase for private fields; prefix serialized private fields with `_` only if consistent with nearby code.
- Keep MonoBehaviours focused; prefer composition over inheritance and avoid God classes.
- Prefer explicit access modifiers; avoid public fields unless intended for scene wiring.

## Serialization and inspector
- Use `[SerializeField]` for private fields that need inspector wiring; pair with `[Tooltip]` or `[Header]` when it aids designers.
- Avoid breaking serialized state: prefer `[FormerlySerializedAs]` when renaming fields and keep backward compatibility in migrations.
- Initialize serialized references in the inspector when possible; avoid runtime `FindObjectOfType`/`Find` for mandatory dependencies.

## MonoBehaviour patterns
- Use `Awake` for internal setup, `OnEnable` for subscriptions, and `Start` for cross-component initialization.
- Null-check serialized dependencies in `Awake`/`OnValidate` with clear error messages; fail fast in development builds.
- Keep update loops lean; cache component lookups and avoid per-frame allocations.

## Logging and metrics
- Use `Debug.Log`/`LogWarning`/`LogError` sparingly; include context (component and key parameters).
- Avoid noisy logs in production paths; gate verbose logs behind developer flags or defines.
- Ensure metrics/events include stable identifiers; avoid logging sensitive or personally identifiable data.

## Async, coroutines, and timing
- Prefer coroutines for Unity lifecycle work; stop coroutines in `OnDisable`/`OnDestroy` when ownership ends.
- When using async/await, avoid `async void`; use `CancellationToken` or `CancellationTokenSource` tied to object lifetime.
- Use `WaitForSeconds`/`WaitForSecondsRealtime` responsibly; avoid unbounded loops without escape conditions.

## Scene/prefab coupling
- Favor serialized references over scene lookups; document any hard scene/prefab dependencies in comments.
- Keep prefab changes intentional; avoid coupling gameplay scripts to specific scene names unless necessary.
- When adding new public APIs, consider designer usability and inspector clarity.

## Testing guidance
- For gameplay logic, add editmode tests where possible; for lifecycle/physics/UI flows, add playmode tests.
- Keep tests deterministic; avoid real-time waits or randomness without seeding.
- Document any manual test steps in the PR/testing notes when automated coverage is not feasible.
