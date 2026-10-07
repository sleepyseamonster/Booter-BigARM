# Scene selection and gizmo engine contract

R3 supplies the engine behavior that a visible Scene View gizmo consumes. The UI owner still owns drawing the handles, cursor feedback, shortcuts, discoverability and hands-on acceptance. Do not implement a second scene document or mutate renderer state directly from a widget.

## Selection and picking

Acquire one `RenderSceneSnapshot` for the viewport frame. Construct a world-space `PickRay` from that viewport's camera and call `pickRenderScene`. The result is the nearest entity whose transformed local mesh bounds intersect the ray; equal-distance results use entity ID for deterministic ordering. Selection is transient UI/session state. Hidden entities are absent from the snapshot. A magenta diagnostic cube remains selectable and reports its unresolved asset identity through `RenderSceneDraw::diagnosticMessage`.

The initial widget should support one or more entity IDs, replace selection on an ordinary click, and add/remove with the platform's conventional modifier. Empty-space click clears selection unless a drag is active. Do not persist selection into authored source, world identity, generated IDs or runtime deltas.

## Manipulation lifecycle

1. On handle press, acquire `AuthoringHost::snapshot()` and call `SceneGizmoSession::begin` with the selected IDs, tool, space and current snap settings. The session pins scene ID, revision, original affine world matrices and the shared selection pivot.
2. During pointer motion, convert the handle motion to an absolute `GizmoDelta` relative to drag start and call `update`. Render `GizmoPreview::worlds` as ephemeral overrides. Do not enqueue source edits per frame. Repeated updates never accumulate from a prior preview.
3. On pointer release, call `commitPayload` and submit one `apply_transaction` request with the session's `baseRevision`. Retain and reconcile that request ID using the [local authoring protocol](../Docs/LIVE_AUTHORING_PROTOCOL.md). One successful drag creates one undo entry.
4. Escape, focus loss, window suspension, selection replacement or tool destruction calls `cancel`/`focusLost` and removes the overrides. The source document remains unchanged.
5. If another human or AI operation changes the scene during the drag, the host rejects the stale revision. Remove the preview, refresh selection from the current snapshot and report the conflict. Never silently rebase a pointer gesture over an AI edit.

`Translate`, `Rotate` and `Scale` support world and local space. World rotate/scale use the shared selection pivot. Local rotate/scale use each selected object's basis and origin. Translation/rotation/scale snap steps are zero when disabled. Parent and child may be selected together: the domain's `setWorldTransforms` conversion prevents the parent's delta from being applied twice to its child.

## View and resource ownership

The renderer consumes immutable `RenderSceneSnapshot` data. Mesh/material records use typed IDs and shared leases; a catalog replacement cannot destroy a resource while an in-flight snapshot references it. UI code must not retain raw renderer or GPU handles.

Scene and Game viewports need unique scene/display view IDs and independent dimensions/target generations through `RenderViewportRegistry`. Resize or removal of one viewport must not rewrite another viewport's camera or target state. The R3 CPU contract proves this isolation; R4 supplies the complete Game View session and GPU target lifecycle.

## Current acceptance boundary

The engine contract, simulated drags, picking, conflict handling and renderer consumption are implemented and tested. Actual visible handles and physical mouse interaction are still an explicit UI-owned acceptance item. Until that UI work lands, Codex can create and manipulate source entities through the same world-transform transaction path, and the Workbench can render those authored snapshots when its authoring endpoint is enabled.
