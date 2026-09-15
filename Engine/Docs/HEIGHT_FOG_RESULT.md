# Generic height fog result

Implemented 2026-09-14 as an engine presentation pass. Fog is environment state, not terrain or game content.

`EnvironmentSettings` now carries linear fog color, density, height falloff and an enable flag. Inspection documents use environment schema version 2 and migrate version 1 documents by applying explicit fog defaults. Values are bounded before they reach the renderer.

The forward scene shader applies exponential distance fog with a height attenuation term. It runs after direct and ambient lighting, preserves the existing HDR scene/display split, and is disabled by default so current captures remain unchanged. The implementation does not introduce a speculative render graph or screen-space AO; material AO remains active, while shared depth/normal targets are still the correct next step for true screen-space effects.

Proof: inspection roundtrip, version 1 migration and invalid-value checks pass in the native core suite. All scene shader variants and the workbench/player targets rebuild successfully.
