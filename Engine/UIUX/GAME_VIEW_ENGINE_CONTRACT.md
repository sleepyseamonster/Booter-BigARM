# Game View engine contract

The visible Game View is a UI client of the shared engine runtime. It must use `PlaySession`; it must not construct a second preview simulation, physics world, streaming owner or persistence path. The standalone Player and Workbench already exercise this same service.

## Start and source ownership

1. Take one immutable `AuthoringSceneDocument` snapshot from the authoring owner.
2. Call `PlaySession::start` with that snapshot, the selected player snapshot and model resources.
3. Retain the returned session epoch in the Game View controller. Every later session call carries that epoch, so callbacks from an older session reject instead of affecting a restarted session.
4. Treat the recorded source revision and checksum from `inspect()` as the session identity. Runtime changes never mutate the source document.

Starting is an explicit user or Codex operation. Merely opening or focusing the Game View does not restart the session.

## Frame and input ownership

- Call `focus(epoch, true)` only while the Game View owns keyboard/controller input. Call `focus(epoch, false)` before returning input to Scene View, text entry, menus or another window.
- Call `pause(epoch, true/false)` for an explicit play pause. Loss of focus also suspends simulation and clears gameplay input without accumulating a wall-time backlog.
- Feed physical controls through `PlaySession::actions()`. Do not write directly to the character, camera or physics state from widgets.
- Advance through `advance(...)` and obtain the display state through `present(...)`. The returned frame is the Game View's presentation input; the UI does not invent another camera/runtime state.
- Render authoring gizmos only in Scene View. Game View uses the runtime third-person camera and gameplay input context.

## Save and load status

World persistence is asynchronous through `DurabilityService`. A requested save returns an operation ID and observable receipt states such as queued, writing, durable, superseded or rejected. Display `durable` only after the terminal durable receipt; request admission alone is not disk completion. Loaded state is adopted only when its recorded play epoch and source revision still match the current session.

The current local authoring socket remains scene-authoring only. UI code may call the typed in-process services; external Codex play/save/load commands require a later bounded protocol extension.

## Stop and shutdown

Stop streaming and other consumers that hold session-owned physics/render resources before calling `PlaySession::stop`. Stop is idempotent and release-only. It discards the runtime copy and returns to the unchanged authoring snapshot. On application shutdown, drain `DurabilityService::shutdown()` so every admitted explicit save reaches a terminal receipt.

## UI acceptance boundary

The engine-side contract is implemented and covered by the R4 native proof. The UI owner still owns the visible tab or split-view presentation, focus routing, status display and hands-on acceptance. See [the R4 result](../Docs/PLAY_SESSION_DURABILITY_R4_RESULT.md) for tested behavior and remaining platform limits.
