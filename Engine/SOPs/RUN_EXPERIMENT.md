# Run a Technical Experiment

Before running, record: question, requirement IDs, exact input files/revisions, permitted writes, command, hardware/backend, pass conditions and proof limits. Keep experiment code in `Research/Experiments/`, output in `.cache/` or `out/`, and durable evidence in `Evidence/`.

Use `Tools/record.py` with `--input` for the experiment source and lock. Record configure, build and run separately. The recorder provides provenance and exit results, not semantic validation. Inspect compiler diagnostics, test assertions, backend identity and any output artifact.

On failure, preserve the original receipt. Explain the cause, make the smallest relevant repair, and produce a new receipt against the repaired inputs. Do not call an old result current after an input change. A timeout, missing output or incomplete test result is not success.

For performance, name CPU/GPU, native platform, backend, resolution, build configuration, visible workload and measurement method. Report frame-time distribution and loading transitions. Noop rendering, compilation and virtualized/translated execution have narrower proof than native GPU execution.

Keep foreground applications undisturbed. Technical integration checks are distinct from user-owned gameplay smoke testing. Do not launch Unity or alter reference assets. A displayed interactive experiment needs to be appropriate to the current user task; automated background checks should avoid stealing focus.

At the end write a short result: observed behavior, failure/repair sequence, what passed, what remains untested, exact evidence links and the next decision. See [the foundation plan](../Docs/FOUNDATION_PLAN.md).
