# Local authoring protocol v1

The [R2 implementation](./LIVE_AUTHORING_R2_RESULT.md) exposes an optional local authoring host in the Workbench. It edits a generic source scene. Rendering that scene is R3; a live command does not yet imply a visible fixture change. No commands write game content or save source files.

## Launch and connect

From `Engine/`, choose an unused, short absolute socket path. Create its private directory once (the tool never removes or replaces existing endpoints):

```sh
mkdir -m 700 "$PWD/build/authoring-local"
./build/foundation/engine_workbench --authoring-socket "$PWD/build/authoring-local/s"
```

Use `--authoring-scene /absolute/source-scene.json` to load a supported authoring document at startup. Otherwise the host starts with an empty `workbench` document. Initial loading precedes the frame loop. The app does not autosave this document. For a bounded CPU-only technical check, add `--authoring-headless-ms 5000` (100–60,000 ms); this uses the real Workbench host without SDL or gameplay. Normal viewer controls remain owned by the UI lane.

```sh
python3 Tools/author_scene.py --endpoint "$PWD/build/authoring-local/s"
python3 Tools/author_scene.py --endpoint "$PWD/build/authoring-local/s" --request /absolute/request.json --wait 3
```

The first call describes the selected endpoint: protocol, document ID, live epoch, current revision, next entity ID, accepted request-ID high-water mark, supported commands, bounds and owner timings. Copy the actual identity into requests. The CLI never guesses which running application to target. `--request -` reads bounded JSON from stdin; `--wait` polls a retained receipt for up to 60 seconds. Exit 0 means a successful exchange/result, not necessarily completion: inspect `status` if the wait expires. Rejection/expired/conflicting ID exits 1; connection/client failure exits 2. After uncertain delivery, retry the **same complete request**, not a new ID.

Protocol messages are UTF-8 JSON followed by one newline; each connection carries one request/response. Unknown fields, duplicate keys, invalid numeric types and excessive depth/bytes reject. There is no public TCP listener, model inference, live save/load or remote publication API.

## Request shapes

Replace `EPOCH` with the advertised value. Positive IDs/revisions are JSON integers. Request IDs are strictly increasing for new admissions within an epoch; a caller may retry a retained ID only with the identical request. Skipped/evicted old IDs are explicitly unknown/expired. On an ID conflict, inspect the existing receipt and current scene before choosing another ID.

```json
{"action":"describe"}
```

```json
{"action":"inspect_scene","document_id":"workbench","epoch":"EPOCH","revision":1,"offset":0,"limit":64}
```

The page contains `entities`, `total`, exact `revision` and `next_offset` (null at the end). Continue with that offset and the **same revision**. Four revisions are retained; if it expires, describe and restart inspection. Each entity includes local `transform` and a column-major affine `world_matrix` (translation entries 12–14).

```json
{"action":"inspect_entity","document_id":"workbench","epoch":"EPOCH","revision":2,"entity":1}
```

```json
{
  "action":"submit",
  "document_id":"workbench",
  "epoch":"EPOCH",
  "request_id":1,
  "expected_revision":1,
  "operation":"apply_transaction",
  "payload":{"operations":[{"type":"create","name":"Example"}]}
}
```

`operation` is `apply_transaction`, `preview_transaction`, `undo` or `redo`. Undo/redo require an empty object payload. Transactions contain 1–128 operations. Created IDs are provisional until applied; the result lists created root IDs and every changed entity ID. `duplicate` returns the new subtree root; inspect its descendants to obtain their IDs. A single atomic batch can reference fresh IDs starting at the described `next_entity_id`, provided its expected revision remains current. Prefer using returned IDs in a subsequent transaction when a batch does not require atomic cross-references.

| Transaction type | Fields in addition to `type` |
|---|---|
| `create` | `name`, optional `parent` (positive entity ID or null) |
| `duplicate` | `entity`; duplicates the whole subtree with fresh IDs |
| `delete` | `entity`; deletes the whole subtree |
| `set_transform` | `entity`, `transform` containing `translation` (3), quaternion `rotation` xyzw (4), `scale` (3) |
| `reparent` | `entity`, optional `parent` (null means root), optional boolean `preserve_world` (defaults true) |
| `metadata` | `entity`, `mesh`, `material`, `collider`, integer `lod` 0–31, boolean `visible`, array of string `tags`; all metadata fields required |

This is local TRS editing with full derived affine hierarchy matrices. Preserve-world reparenting rejects unsupported local shear or singular/nonfinite results. Whole transactions reject before publication if any operation or required inverse/receipt is invalid or over capacity. Group/ungroup are compositions of create/reparent/delete. Preview validates and describes a candidate without applying it; it does not render an ephemeral drag preview.

```json
{"action":"receipt","document_id":"workbench","epoch":"EPOCH","request_id":1}
```

```json
{"action":"cancel","document_id":"workbench","epoch":"EPOCH","request_id":1}
```

Cancel may return `cancellation_requested`; poll the receipt for `cancelled_before_apply` or an already-applied result. Applied receipts report source revision, not disk durability. Restart creates a new epoch and discards unsaved live state and receipt history. Reconcile against a deliberately loaded saved document after restart; do not assume the previous receipt journal survived.

## Ownership and bounds

`AuthoringHost::snapshot()` atomically loads a shared immutable, history-free document view, independently of the ingress/receipt mutex. Its entities and world matrices remain valid while that shared view is retained. C++ frame consumers should acquire a pointer briefly, consume cached values, and release old snapshots; they must not call synchronous JSON inspection from the render loop. The host's four revision slots do not bound arbitrary external leases. Only the dedicated authoring owner adopts edits.

Default ingress is 256 KiB/request, 128 pending requests and 8 MiB aggregate encoded pending bytes. Response/page maximum is 64 KiB/64 entities. Retention is 512 records and 8 MiB of encoded original-request plus receipt/reserved-result bytes, so large requests may reach the byte cap before the count cap. Pending work is never evicted. These are payload/accounting limits; allocator overhead, history, snapshots and future GPU leases have separate ownership and budgets.

Mac sockets require a private directory owned by the current user and have mode 0600. Existing paths are never overwritten. On orderly shutdown the endpoint removes only its own socket inode. After a crash, choose a new path or deliberately inspect/remove the stale socket yourself. Windows transport currently rejects explicitly; named pipes and native acceptance belong to the Windows gate.
