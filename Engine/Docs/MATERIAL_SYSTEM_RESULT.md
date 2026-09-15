# Material semantic contract

Implemented 2026-09-14 as a small engine-owned material layer. This does not add new lighting or texture formats. It makes the existing material pipeline explicit and fail-fast.

`MaterialDefinition` describes a stable material family, its base color/normal/surface channels and optional layered slots. The layered wasteland rock family is defined once in native code instead of relying on parallel slot arithmetic in the application. Validation checks that every required catalog record exists, has the expected semantic role and uses the correct transfer function (sRGB only for color channels). The workbench validates the contract before acquiring GPU resources.

The renderer still owns transient GPU handles and `TextureStore` leases; material definitions contain logical IDs only. Missing optional ground channels continue to use explicit role-aware fallback textures. Material edits therefore cannot silently bind a color image as a normal or surface map.

Focused proof is `material_definition_contract`: a complete layered family passes, while role, transfer-function and missing-record mutations fail. Existing semantic texture catalog and texture-store lifecycle checks remain unchanged. This is a contract and ownership improvement, not final art acceptance, texture compression or a new shader feature.

Physics also exposes configured gravity through `PhysicsWorld::setGravity`/`gravity`, with finite local bounds and the existing Jolt character/dynamic body path consuming the same world gravity. The physics suite verifies the default, mutation and invalid input rejection.

World identity, generator versions, stable objects, streaming epochs and persisted deltas are unaffected. Material definitions reference logical asset IDs; physics tokens remain transient handles. The next engine batch remains streaming adoption attribution and bounded off-thread preparation.
