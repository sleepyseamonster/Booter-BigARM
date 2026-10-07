# Architecture audit evidence

Source under audit: `f6dc2ad72511706d2f4c694318013e378238fc3a`. See the [audit findings and correction sequence](../../Docs/ENGINE_ARCHITECTURE_AUDIT_2026-09-15.md).

- [observations.json](./observations.json): bounded native probes, compiler/platform, command and relevant input fingerprints.
- [verification.json](./verification.json): registered-test result, test registration gaps and application/source/shader inventory fingerprints. The inventory is provenance, not a claim that every line has been exhaustively audited.
- [native-tests.txt](./native-tests.txt): retained CTest log for the 15 registered tests.
- [workspace-check.json](./workspace-check.json): document checker output; 44 pre-existing errors are confined to preserved Unity reference imports, with zero task-document errors.

Reproduce the observations from `Engine/` on the Mac with pinned dependency sources available:

```sh
python3 Tools/audit_architecture.py --output out/architecture-audit-repeat.json
```

Choose a new output path for each run. The runner compiles only the audit probe and current authoring/document sources into ignored `build/architecture-audit/`. It does not launch the engine, alter saved scenes or change runtime source. It uses a bounded allocation-failure injector inside the standalone probe process only.

These are observations of defects, not regression acceptance tests. Exit zero means evidence collection succeeded; `reproduced_issue: true` identifies a problem. After corrections, add normal acceptance tests with independent expected results, then repeat this diagnostic against the corrected source in a new evidence location. Timing values describe one synthetic hierarchy on this machine and must not be interpreted as target-game performance.
