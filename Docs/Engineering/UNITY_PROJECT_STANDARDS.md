# Unity Project Standards

This is the compact standard for how this project should be organized. It is intentionally short and should stay focused on decisions that reduce Unity churn and reference risk.

## Core Principles

- Keep runtime, editor, and test code separated.
- Keep asset locations stable once content becomes real.
- Favor simple, explicit folder names over clever nesting.
- Prefer project-owned folders under `Assets/_Project/` for new work.
- Use Unity special folders only when Unity assigns them a clear meaning.

## Naming Convention

Use PascalCase for folders, scenes, prefabs, assets, and script files.

Examples:

- `PlayerController.cs`
- `MainMenu.unity`
- `CombatHUD.prefab`
- `BooterBigArm.Runtime.asmdef`

Avoid:

- spaces in file and folder names
- inconsistent abbreviations
- generic names like `New Scene`, `Test2`, or `Stuff`

## Folder Standard

```text
Assets/_Project/
  Art/
  Audio/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Runtime/
    Editor/
  Settings/
    Input/
    Profiles/
    Rendering/
      URP/
    Templates/
  Tests/
  UI/
  VFX/
```

## Code Boundaries

- Runtime gameplay code goes in `Scripts/Runtime/`.
- Editor tools, import helpers, and build automation go in `Scripts/Editor/`.
- Keep editor code in an Editor-only asmdef.
- Add test assemblies when test code becomes real.

## Assembly Naming

- Use `BooterBigArm.Runtime.asmdef` for runtime code.
- Use `BooterBigArm.Editor.asmdef` for editor code.
- Use `BooterBigArm.Runtime.Tests.asmdef` and `BooterBigArm.Editor.Tests.asmdef` for test assemblies when they exist.
- Keep assembly names consistent with folder purpose and avoid inventing unrelated names.

## World Units And Character Size

- One Unity world unit represents one metre. Blender authoring uses Metric with Unit Scale 1.0; imported dimensions must still be measured in Unity.
- On October 13, 2026, the author selected **1.80 metres** for Booter's standing body height, measured from feet to head in a documented standing reference pose. Equipment, hair, broad renderer bounds and collider clearance are separate measurements.
- Keep the physics/controller root and its ancestors at unit scale. Calibrate the visual child from measured imported geometry; do not reuse a different rig's numeric import scale.
- Fit capsule height, radius, centre and grounded visual offset together. Radius follows body clearance rather than outstretched arms. Preserve foot contact when changing capsule dimensions. A collider is not proof of visible body height.
- Qualify movement stride/cadence, crouch/prone shapes, vault/climb probes, hand/foot contacts, props and equipment at the approved size before player cutover. World-space movement speeds and interaction ranges need individual review; do not multiply all distances by a single character scale ratio.
- Preserve fixed terrain dimensions, authored world positions and object IDs. Camera framing is reviewed separately from physical size.
- Measurement evidence and the outstanding correction sequence are in [the scale audit](../Evidence/Art/CharacterScaleAudit20261013/audit.json). Existing production remains at the inherited size until its coordinated cutover passes validation.

## Asset Handling Rules

- Move assets in Unity rather than by hand whenever possible.
- Preserve `.meta` files and keep source-control history intact.
- Do not use `Resources/` for general project organization unless a specific runtime lookup need requires it.
- Prefer Addressables or explicit references over ad hoc resource loading for production systems.

## Scene And Prefab Rules

- Keep scenes focused on a single role: bootstrap, gameplay, UI, or test.
- Put reusable objects in prefabs rather than duplicating them across scenes.
- Keep scene names and prefab names descriptive of their purpose.

## Best Practice Summary

The practical standard is:

1. consistent names
2. stable asset locations
3. clear runtime/editor separation
4. minimal special-folder usage
5. no `Resources` unless justified
