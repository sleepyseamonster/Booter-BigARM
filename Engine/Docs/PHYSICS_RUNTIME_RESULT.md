# Physics and adoption attribution

The first physics contract batch exposes configured gravity through `PhysicsWorld::setGravity`/`gravity`; the existing Jolt world, collision queries, dynamic bodies, contact events and capsule controller all consume the same world setting. Default gravity, mutation and invalid input rejection pass in the focused physics suite.

The streaming attribution pass adds shared telemetry subphases for terrain render preparation and physics adoption. The corrected load/retire/return workload passes with unchanged region buffers and zero returned-image differences. Terrain LOD preparation peaked at about 0.35 ms in the retained frames. Jolt mesh-shape cooking reached about 59 ms and accounts for the measured adoption burst. This is a local Mac diagnostic workload, not a Windows or gameplay benchmark.

The next implementation is a prepared-mesh shape path: cook the Jolt shape in a bounded worker job, validate its request epoch and byte budget, then adopt the finished shape on the physics owner thread. Existing valid bodies must remain until replacement adoption succeeds. Renderer handles, body tokens, stable region/object IDs and persisted deltas remain separate. A worker must never mutate the live `PhysicsSystem` or resurrect an unloaded region.

No new lighting, UV, normal-mapping or rock-authoring scope is included in this result. The material semantic contract is documented separately in [MATERIAL_SYSTEM_RESULT.md](./MATERIAL_SYSTEM_RESULT.md).
