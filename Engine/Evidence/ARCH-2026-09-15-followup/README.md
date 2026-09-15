# Lifecycle audit evidence — 2026-09-15

The [follow-up audit](../../Docs/ENGINE_LIFECYCLE_AUDIT_2026-09-15.md) explains four new findings A12–A15. [observations.json](./observations.json) records five diagnostic cases, source hashes, toolchain/build commands and passing outputs from the existing physics and snapshot contracts.

Reproduce from `Engine/`, using a new evidence output path:

```sh
python3 Tools/audit_runtime_lifecycle.py --output build/lifecycle-audit-new.json
```

Requires configured Mac `build/core` and pinned dependencies. The runner rebuilds relevant libraries, uses temporary profiles under `Engine/build/`, then removes its own scratch directory. It never accesses user profiles or launches an application. `reproduced_issue: true` means broken behavior was observed; these diagnostics are not regression acceptance tests. Convert individual cases to corrected contract assertions in the owning implementation batch.

Source baseline: `3170b9f66fbf896801d46ce902b4d216d3908e53`. Runtime unchanged. Listed scratch executable/profile paths were temporary; source and JSON observations are durable. Existing tests passing does not close the findings. Destructor termination is inferred from the confirmed throwing prerequisite and source call path, not an executed viewer crash.
