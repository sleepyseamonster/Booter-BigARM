# Evaluate a Dependency

1. Name the requirement it serves and what project-owned responsibility remains. Reuse [the dependency inventory](../Research/dependencies.json); avoid introducing a second owner for the same subsystem.
2. Read primary documentation and the relevant build/source files. Record immutable revisions where possible, retrieval date, backend constraints, license path and known integration dependencies.
3. Compare only alternatives that could change this decision. Define the smallest technical question, expected evidence and stop condition before downloading/building.
4. Pin an experiment cohort. Include wrapper/build scripts, subdependencies, shader tools and UI configuration where relevant. Never combine unverified latest versions and call the collection selected.
5. Fetch into ignored engine cache; verify hashes. Preserve source modifications if discovered rather than overwriting them. Retain notices; mark transitive redistribution review open until actually completed.
6. Run the experiment through [the technical-experiment SOP](./RUN_EXPERIMENT.md). Keep failures and subsequent repairs in separate evidence directories.
7. Update [decisions](../Docs/DECISIONS.md), inventory and the relevant audit. Distinguish rejected, proposed, experiment-only and accepted-for-production states. State the next evidence needed.

Stop when the question is answered or a concrete blocker requires a different decision. A successful library sample does not establish the game's performance, visual quality, or shipping readiness. Library selection should reduce total work for the current requirements, including maintenance and authoring.
