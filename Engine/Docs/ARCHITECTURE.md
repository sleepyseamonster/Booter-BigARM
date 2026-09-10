# Proposed Engine Ownership Boundaries

Planning contract, not implemented runtime. The [requirements](./REQUIREMENTS.md) and [accepted direction](./DIRECTION.md) control scope.

```mermaid
flowchart LR
    Recipe[Versioned recipe document] --> Generator[World and rock core]
    Generator --> Result[CPU geometry and semantic result]
    Result --> Renderer[Renderer resources]
    Result --> Collision[Collision adapter]
    Editor[Workbench controls] --> Recipe
    Delta[Persistent overrides] --> Generator
    Stream[Region lifecycle owner] --> Generator
    Stream --> Renderer
    Stream --> Collision
```

## Ownership

| Owner | Owns | Must not own |
|---|---|---|
| Core | IDs, resource lifetime primitives, logging, scheduling interfaces | Game canon or platform GPU objects |
| Platform adapter | Events, window, OS paths, device notifications | Movement rules or editable recipe state |
| Recipe document | Validated settings, version, edit history and dirty state | Live renderer/physics handles |
| World/rock core | Deterministic planning and CPU results | Window/UI access or GPU submission |
| Region lifecycle | Request epochs, cancellation, current desired state and budgets | Permanent identity derived from load order |
| Renderer | Mesh/material handles, visibility, uploads and frame passes | Authoritative world edits or physics state |
| Collision adapter | Shapes and physical/query resources | A second terrain surface that disagrees with generated data |
| Persistence | Versioned durable deltas and atomic document writes | Transient pointers, handles or every regenerable mesh |
| Workbench | Commands applied to the document, selection and diagnostics | A second recipe copy that drifts from saved data |

## Critical Lifecycles

Generation request identity includes stable owner ID, recipe/generator versions and a request epoch. A completed job is accepted only if its owner and epoch are still current. Cancelling an owner prevents future adoption even when computation cannot stop immediately. GPU destruction waits for the rendering API's lifetime rules; CPU cache ownership is independent. Reopening a region reconstructs resources from authoritative data and deltas.

Worker jobs produce data; the render owner submits uploads. Initial budgets must be measured and explicit. Do not solve a burst by running unbounded generation or collision cooking on the frame thread.

Authoring edits validate before replacing the active recipe. Failed generation retains the prior valid preview with an explicit error. Later undo/redo operates on document commands, not arbitrary UI widgets. Save writes should be transactional and versioned; define migration before changing persisted meaning.

## Procedural Concerns

- World identity: explicit seed, algorithm/version namespace and replaceable coordinate authority.
- Stable object identity: stable source/member keys and placement ownership, separate from resource handles.
- Streaming: lifecycle and late-result checks above; an isolated renderer fixture is explicitly temporary.
- Authored constraints: recipe bounds, source envelopes and exclusions are inputs to generation.
- Persisted deltas: authoritative changes survive resource destruction and regeneration.

Use camera-local floating-point coordinates for rendering while keeping durable geographic identity independent. Local axis/units conventions will be documented in the first fixture; they do not canonize the world's map. Tests must separately cover projection/depth, winding and transforms across the actual backends.

## Initial Module Layout

Only create modules when needed: `Source/Core`, `Source/Platform`, `Source/Rendering`, `Source/World`, `Apps/Workbench`, `Tests`, and `Tools`. Third-party implementation stays in ignored build/cache locations, with source/version/license records tracked separately. The preparation experiment under `Research/Experiments` is disposable evidence, not the production module layout.
