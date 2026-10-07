# Shared Simulation Foundation

P08 implemented 2026-09-10. The engine now has a reusable simulation library connected to the workbench, with fixed ticks, entity components, ordered commands and platform-independent input actions. Physics and character control are the next consumers; this pass does not claim a playable character.

`engine_simulation` depends on the core document/identity library and EnTT's registry headers. It has no SDL, rendering or ImGui dependency. EnTT v4.0.0 is pinned at `85c6bba014049b5de8fad49d25424df2f1f6a8c1` in `Research/runtime-lock.json`; its archive/content verification uses the existing acquisition tool. The exact MIT notice is retained in `Research/Licenses/EnTT-LICENSE.txt`. [Upstream registry documentation](https://github.com/skypjack/entt/wiki/Entity-Component-System) informed the component/lifetime adapter. The engine owns scheduling and identity rather than relying on registry iteration order.

## Runtime contract

- `FixedClock` runs at 60 Hz with at most four ticks per displayed frame. It reports discarded overload seconds and frame count. Pause/focus loss clears fractional backlog. Simulation time advances only through accepted ticks; later travel/pressure must use that clock.
- `World` owns transforms, previous transforms and velocities in EnTT. A stable string ID maps to a live token containing world ownership and a 64-bit incarnation in addition to the underlying registry entity. Stable IDs survive reload; live tokens do not. Entity iteration sorts by stable ID.
- Commands have unique monotonically advancing sequence numbers, are adopted in sorted order at the tick boundary, and target an incarnation. Removed/reloaded targets reject stale commands rather than resolving them to a replacement. The queue admits at most 1,024 commands; the default live budget is 8,192 entities. Velocity, pose, queue and ID bounds reject invalid candidates. Immediate create/remove/unload are single-owner boundary operations.
- Initial velocity integration is free kinematic motion. This supplies a real transform consumer without introducing pretend collision. Jolt and the character motor will own physical motion in P09/P10. This is not cross-platform deterministic physics or an atomic transaction spanning every system in a tick.
- Actions separate gameplay, UI confirm/cancel and system pause. Press/release edges survive short taps between ticks and are consumed once across catch-up ticks. Focus changes, context changes and remapping inhibit held controls until neutral. Controller removal cancels pending edges; newly adopted controller controls must first return to neutral. Named binding documents are bounded, validated and atomically replaced through the existing document layer.
- The SDL adapter reconciles event edges and held keyboard/gamepad state, applies a 20-percent axis dead zone, opens one available controller, and handles replacement/removal. Physical-device and Windows behavior remain unverified; [SDL's axis contract](https://wiki.libsdl.org/SDL3/SDL_GetGamepadAxis) is the platform source.

## Workbench integration

Expand **Shared simulation**, enable **Simulate proxy**, then click outside the inspector. WASD or the left stick drives the temporary proxy; P or controller Start toggles pause. The inspector shows tick count and discarded time. The rendered proxy/following inspection view uses interpolated runtime positions. This is free motion on the existing calibration fixture, not the finished third-person motor or camera. The existing rendering verification modes leave simulation disabled.

`--bindings <file>` loads named bindings; `--save-bindings <file>` writes them on exit. Binding edits are available through the document/API first; a live remapping UI is later work. The current fixture snapshot remains an inspection document, not a player save. Entity snapshots and persisted gameplay deltas follow in P13/P21.

## Procedural contract

Generated identity uses the existing structural `GeneratedId` text; authored calibration IDs are explicit client inputs. Positions normalize across negative and positive region boundaries using configured region span. Region unload removes transient component storage and invalidates tokens; reloading the same stable ID obtains a fresh lifetime. Authored constraints enter through validated poses/IDs and commands, without defining final geography. The runtime does not generate chunks or preserve gameplay deltas yet: the future world/save owner must adopt authoritative deltas before unload. GPU/physics/entity handles must never become persistent IDs. Rendering still uses a bounded calibration origin, not a claim of streamed-world presentation.

## Evidence and next work

[Native build and focused simulation tests](../Evidence/P08-runtime/result.json) passed. The test advances the same world at 30/60/144 render rates, checks bounded overload/pause, quick taps, focus/UI/remap/disconnect/hotplug transitions, binding roundtrip and rejection, sequence ordering, region crossing/unload/reload, stale/foreign tokens and queue bounds. A review caught the need to inhibit controls held during controller adoption; that correction and its focused case passed in the final build.

The workbench integration compiled on the current Mac. No gameplay smoke test, physical-controller test, repeated render capture, package rebuild or Windows run was performed for this batch. These are deliberate proof limits, not completion claims for P09/P10/P12. Continue with Jolt static collision and queries, then capsule motion and camera obstruction. Rendering polish remains deferred.
