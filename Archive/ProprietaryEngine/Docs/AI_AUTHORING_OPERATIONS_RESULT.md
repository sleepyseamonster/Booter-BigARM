# UI-independent authoring operations

Implemented 2026-09-14 as a generic engine boundary for AI-driven authoring. It contains no terrain, rock, landscape or game-specific handler.

`AuthoringOperations` accepts bounded JSON requests with a stable operation ID and expected domain version. A caller-owned `process(budget)` adopts at most the requested number of operations, keeping model/network work outside the real-time loop. Registered handlers receive the payload and expected version, then return an object result and the resulting version. Unknown types are rejected; handler exceptions become failed receipts; a handler cannot move a version backwards. Receipts retain status, result, version and a bounded history.

This is deliberately an operation seam rather than a transport or widget system. The authoritative domain owner still validates identity, procedural invariants, stale edits, persistence and undo before applying a change. UI and AI clients can share this queue later without creating a competing world model.

Proof: the existing native core suite exercises successful processing, bounded budget adoption, unknown-operation rejection, stale-version failure and receipt data. It builds without a new dependency or project-wide setting change.
