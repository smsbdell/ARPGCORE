# High-churn asset labeling

This project standardizes lifecycle labels for heavily edited asset folders.

## Label definitions
- **shipping** – final or near-final content that should stay stable.
- **prototype** – experimental assets under active iteration.
- **test-only** – fixtures or mock assets safe to remove when unused.
- **deprecated** – slated for removal; auditor reviews after 30 days without updates.

## Coverage
Apply at least one label to every asset in:
- `Assets/Data`
- `Assets/Prefabs`
- `Assets/Resources`
- `Assets/Scenes`

## Review cadence
Assets labeled `deprecated` are expected to be reviewed after 30 days (based on last modified time). The asset auditor flags unlabeled high-churn assets and `deprecated` items past the retention window so they can be cleaned up or archived.
