# Physics and adoption attribution

The first physics contract batch exposes configured gravity through `PhysicsWorld::setGravity`/`gravity`; the existing Jolt world, collision queries, dynamic bodies, contact events and capsule controller all consume the same world setting. Default gravity, mutation and invalid input rejection pass in the focused physics suite.

The streaming attribution pass adds shared telemetry subphases for terrain render preparation and physics adoption. The corrected load/retire/return workload passes with unchanged region buffers and zero returned-image differences. Terrain LOD preparation peaked at about 0.35 ms in the retained frames. Jolt mesh-shape cooking reached about 59 ms and accounts for the measured adoption burst. This is a local Mac diagnostic workload, not a Windows or gameplay benchmark.

The prepared-mesh shape path is now implemented. `PhysicsWorld::prepareMesh` cooks a backend-opaque Jolt shape without mutating the live world; `meshPrepared` and `replaceMeshPrepared` adopt it on the physics owner thread. `StreamingScene` runs one bounded physics preparation queue, tracks per-region tickets, cancels retired/replaced work and keeps the existing collider until adoption succeeds. A worker never mutates the live `PhysicsSystem` or resurrects an unloaded region. Renderer handles, body tokens, stable region/object IDs and persisted deltas remain separate.

The focused physics and region-stream suites pass after this change. The previous 59 ms frame-thread cooking burst is now off the adoption path in the native runtime; a new load/retire timing capture is still needed before claiming a measured end-to-end improvement on representative hardware.

No new lighting, UV, normal-mapping or rock-authoring scope is included in this result. The material semantic contract is documented separately in [MATERIAL_SYSTEM_RESULT.md](./MATERIAL_SYSTEM_RESULT.md).
