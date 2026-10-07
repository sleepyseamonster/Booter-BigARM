# Shared Player Skeleton

P12 implemented 2026-09-10. `engine_player` now runs the same `CalibrationRuntime`, model, renderer, physics, input actions and audio adapter as the workbench. It shows the imported animated proxy, follows it with the third-person camera and provides a nearby marker interaction. Its minimal controls/status overlay uses bgfx debug text; the player has no ImGui include or linked dependency. This is the connected skeleton, not the final game UI.

`CalibrationRuntime` owns the fixed clock, stable player/marker entities, physical ground/marker, capsule motor, camera and character animation instance. Applications provide action state and consume presentation frames. The previous workbench-specific world/motor loop was removed. Movement advances at 60 Hz; model blending follows actual horizontal displacement. Physics owns position, while animation remains presentation. Pause and lost focus stop the clock. Clip 0/1 remain the temporary idle/walk mapping from P11.

E toggles the marker only within 2.5 m and with an unobstructed physics ray. A press edge changes runtime state and emits a monotonically numbered cue with the marker's stable authored identity. Holding the key and repeated event drains cannot replay it. The marker changes from turquoise to gold when active. This is a debug interaction, not a salvage/cargo system or world canon.

`engine_audio` integrates [miniaudio](https://miniaud.io/docs/manual/index.html) 0.11.25, pinned at `9634bedb5b5a2ca38c1ee7108a9358a4e233f14d` through the existing source lock; its license alternatives are retained. The cue is a repository-generated, quiet 0.2-second tone in an owned PCM buffer. One voice restarts on each accepted new event; duplicate/older sequence numbers are ignored. Buffers, sound and engine have explicit teardown. A missing audio device reports an error and leaves the application usable silently. Workbench audio starts only when character mode is enabled. Spatialization, buses, audio assets, device recovery and polished sound remain later work.

World seeds and generated identities are unchanged. These authored calibration entities are local and always resident for this bounded fixture. Future chunk owners will manage their own generated entities and collision resources. Neither GPU/audio handles nor cue playback cursors are persistent identities. Authored placement supplies the initial scene; player/marker runtime deltas are not saved yet. P13 adds snapshot ownership and restart, followed by native world/rock and streaming work.

## Run

From `Engine/`, after building and cooking the P11 proxy:

```sh
build/foundation/engine_player --model out/models/calibration/model.json
```

WASD/left stick moves; Space/South jumps; Shift/stick click runs; right mouse drag/right stick orbits; the wheel changes camera distance; E interacts; P/Start pauses; Escape exits. `--silent` disables device initialization. The workbench's **Shared simulation** panel uses this same runtime. Ground remains finite; walking off it falls until restart. Recovery is P13 work.

## Focused evidence

[Native runtime/audio cases](../Evidence/P12-runtime/result.json) passed for authored identity, reachable-target interaction, press/hold event behavior, drain semantics, pause, camera output and cue deduplication. The real miniaudio offline graph produced finite nonzero PCM (energy 28.918) and returned to silence after the tone ended. This proves mixer output, not audible speaker/device behavior.

[One native player render](../Evidence/P12-player-render/result.json) produced [the player capture](../Evidence/P12-player-capture/player.png) on Metal with zero reported GPU errors and normal shutdown. This render-only mode did not advance simulation or inject gameplay input. Both applications compiled; the retained player link command confirms no ImGui dependency. Existing P09/P10 and P11 evidence covers the motor/camera and GPU skinning primitives. No gameplay smoke test, physical-controller test, Windows test, repeated rendering suite or package rebuild was run for P12.

Next: P13 minimal snapshot/restart and a relocatable player package. The remaining first-pass engine goal is still active.
