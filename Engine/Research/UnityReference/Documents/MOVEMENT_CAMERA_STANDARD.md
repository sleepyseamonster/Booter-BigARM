# Movement And Camera Standard

This is the preserved baseline for the legacy top-down 2D path. The accepted perspective top-down 3D lane overrides its body, projection, pixel-grid, and sorting-specific rules while retaining the separation between input, movement physics, and camera framing.

For the perspective lane:

- use a 3D `Rigidbody` on XZ with Y as elevation;
- project movement through the camera basis onto the traversal plane;
- use an elevated perspective camera with serialized pitch, yaw, distance, field of view, damping, and obstruction values;
- use right-stick horizontal orbit and constrained vertical pitch by default; while the gamepad left trigger is held, suppress orbit and use the right stick to translate the framing target across the XZ plane without blocking player movement;
- limit look-ahead translation to `12` meters, move outward at `12.6 m/s` (three times normal run speed), and recenter at `37.8 m/s` (three times the outward speed) whenever the stick or modifier is released;
- use 3D depth, lighting, colliders, materials, and renderer topology instead of pixel snapping and sprite sorting;
- keep one collision owner for each walkable surface; decorative crag faces must not overlap the authoritative terrain collider;
- use a capsule with a low-friction movement material, a volume cast for ground proximity, and a raycast for the true triangle normal before applying the serialized slope limit;
- the current perspective prototype accepts terrain through `48` degrees and projects movement along accepted surfaces;
- sample the authoritative support normal beneath the capsule centerline, then time-filter accepted normals before driving slope velocity so terrain triangle seams do not shake the controller;
- apply the `48` degree rejection to the unsmoothed measured normal, and rate-limit grounded vertical correction so filtering cannot make steep terrain climbable or produce instant vertical velocity changes;
- create weight through bounded momentum rather than input latency: keep movement input immediate while targeting `0.45-0.60` seconds to normal run, `0.80-1.00` seconds to sprint, and `0.30-0.42` seconds from normal run to rest;
- expose a read-only locomotion snapshot from the Rigidbody motor; animation may read current/desired planar velocity, acceleration, actual facing, measured yaw rate, support normal, sprint, traversal ownership, alignment, and heading error but may not move the gameplay body;
- keep one project-owned `TopDown3DLocomotionClipProfile` as the clip, contact, transition, and playback-bound authority used by the scene, builder, runtime, validator, and tests;
- drive sustained walk, run, sprint, and directional gaits from one semantic locomotion phase with continuous normalized weights; remap that phase through each clip's measured left/right contacts instead of assuming identical raw normalized time;
- select medium directional gait from local desired trajectory and measured Rigidbody yaw, and use authored non-looping, planted-side start, stop, and pivot clips for the major weight transfers; none of these states may lock steering or apply displacement;
- keep the Playables mixer as the sole Humanoid pose authority during grounded-locomotion recovery. Root motion, clip Foot IK, procedural foot goals, pelvis translation, foot rotation, and the failed downstream pose job remain disabled;
- emit grounded contacts from calibrated semantic phase crossings or explicit one-shot plant metadata. Footstep dust consumes those contacts and must not maintain a second distance cadence, guessed-foot alternation, or runtime auto-installer;
- keep hands-on feel and controller acceptance with the user unless the user explicitly delegates it.

## Weighty Locomotion Reference Findings

The first weight profile was researched on 2026-08-16 and deliberately separates visible mass from control latency:

- Capcom describes the `Monster Hunter Wilds` player-animation pipeline as a combination of revised legacy motion, motion capture, and hand-authored work, with the player animation lead explicitly balancing in-game performance against visually striking movement. The project inference is to preserve readable body mechanics and gait transitions without globally slowing input response. See [Autodesk's Capcom production interview](https://blogs.autodesk.com/media-and-entertainment/2026/04/23/the-production-infrastructure-behind-capcoms-monster-hunter-wilds/).
- Rockstar devoted a GDC session to the creative and technical systems behind Arthur Morgan's locomotion, but later made Red Dead Online's on-foot movement quicker and more responsive while preserving speed through vaults and climbs. The project inference is to borrow momentum and follow-through without copying the most sluggish response characteristics. See [GDC's RDR2 locomotion overview](https://gdconf.com/article/learn-the-secrets-of-red-dead-redemption-2-s-player-locomotion-at-gdc/) and [Rockstar's movement update notes](https://support.rockstargames.com/articles/6dT8UroC7aKslsqA38oaxj/red-dead-redemption-2-title-update-1-11-notes-ps4-xbox-one).
- Pearl Abyss' official `Crimson Desert` update specifically improved short-distance turning responsiveness and removed a brief movement stop in another traversal mode. The project inference is that realistic momentum must still yield promptly to clear player intent. See [Crimson Desert update 1.01.00](https://crimsondesert.pearlabyss.com/en-US/News/Notice/Detail?_boardNo=76).
- Motion-matching research frames responsive natural locomotion as matching both the current pose and the desired future trajectory with short blends. This prototype does not adopt motion matching, but it applies the transferable pieces: travel-aligned facing, gait phase continuity, hysteresis, and transition-specific blend timing. See [GDC: Motion Matching and the Road to Next-Gen Animation](https://www.gdcvault.com/play/1023280/Motion-Matching-and-The-Road).

## Pixel Scale Baseline

- Treat the world as a 32px art scale.
- Author tiles and most world-facing sprites to align cleanly to that grid.
- Use a consistent pixels-per-unit convention that preserves the 32px baseline instead of mixing sprite scales ad hoc.
- Keep the camera and follow motion compatible with pixel-perfect rendering so the scene stays crisp.

## Core Movement Rule

- Use a `Rigidbody2D`-backed movement motor as the authoritative body for any object that collides or interacts with the world.
- Do not move colliders directly by changing `Transform.position` every frame.
- Keep the movement decision layer separate from the physics application layer.

Unity's Rigidbody2D docs are explicit that moving collider-bearing bodies by Transform causes problems, and that Rigidbody2D movement should be used instead. See:
- [Rigidbody 2D](https://docs.unity3d.com/ru/2019.4/Manual/class-Rigidbody2D.html)
- [Rigidbody2D.MovePosition](https://docs.unity3d.com/kr/2022.2/ScriptReference/Rigidbody2D.MovePosition.html)
- [Rigidbody2D.MovePosition API](https://docs.unity3d.com/es/530/ScriptReference/Rigidbody2D.MovePosition.html)

## Movement Design Rule

- Treat input as a desired movement vector, not as immediate world motion.
- Apply movement through the physics step, not through ad hoc transform updates.
- Keep the movement motor swappable so future mechanics can override speed, turn behavior, or control ownership without rewriting the whole controller.
- If camera follow jitter appears on physics-driven actors, enable `Rigidbody2D` interpolation rather than coupling the camera to raw transform motion.

## Baseline Player Feel

- Gamepad input should drive an analog movement vector.
- Keyboard input should map cleanly to the same movement model.
- The movement layer should support acceleration, slowdown, stun, dash, and other modifiers as data-driven changes.
- Keep movement code deterministic enough that future systems can reason about it.

## Camera Rule

- Default the top-down camera to orthographic.
- Keep the camera follow and framing logic separate from the player movement code.
- Use camera data and camera state changes for tuning, not hard-coded movement coupling.
- Use a pixel-perfect camera path for the prototype and future gameplay scenes so follow motion snaps cleanly to the render grid.
- Prefer smoothing that respects the pixel grid over raw fractional camera movement.

Unity's Cinemachine docs state that the 2D setup works with an orthographic camera and that the virtual camera drives the Unity camera. See:
- [Cinemachine 2D graphics](https://docs.unity3d.com/ja/Packages/com.unity.cinemachine%402.6/manual/Cinemachine2D.html)
- [Cinemachine Virtual Camera properties](https://docs.unity3d.com/ja/Packages/com.unity.cinemachine%402.6/manual/CinemachineVirtualCamera.html)

## Readability Rule

- Use Sorting Layers and Order in Layer as the primary way to express depth and overlap.
- Use Sorting Group for multi-sprite characters, equipment rigs, and any prefab with more than one renderer that should stay visually together.
- Keep camera logic simple; do not rely on camera distance alone to solve 2D readability.

Unity's sorting docs are clear that Sorting Layer and Order in Layer are the primary 2D sorting controls, and that Sorting Group keeps grouped renderers together. See:
- [Sorting Group](https://docs.unity3d.com/es/2021.1/Manual/class-SortingGroup.html)
- [2D Sorting](https://docs.unity3d.com/ru/2021.1/Manual/2DSorting.html)

## Flexibility Rule

- Put camera tuning values in data, not hard-coded constants.
- Keep the camera able to change mode later, such as exploration, combat, interior, or event focus.
- Keep the follow rig able to expand from one target to a target group later, instead of hardwiring all framing to a single actor.
- Keep movement and camera baselines permissive enough to support future traversal mechanics.
- Keep the player motor interface narrow so future traversal types can swap body behavior, camera framing, or input response without reworking the full controller.

## Practical Rule Set

1. Rigidbody2D is the authoritative movement body.
2. Transform motion is not the primary gameplay movement path.
3. Camera is orthographic by default.
4. Cinemachine or an equivalent camera rig handles follow and framing.
5. Sorting layers and sorting groups enforce readability.
6. Pixel-perfect rendering is part of the baseline, not an optional polish pass.
