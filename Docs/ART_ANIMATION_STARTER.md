# Art And Animation Starter

This document defines the first-pass workflow for creating production art and sprite animation in Booter & BigARM.

It is intentionally small. The goal is to start making usable assets without locking the project into a brittle pipeline too early.

## Visual Baseline

- The game is a top-down 2D pixel art game.
- The current render baseline is URP 2D with pixel-perfect presentation.
- The project already uses a 32px art scale as its working reference.
- The world should read as dry, iron-heavy, dead, and sun-burned rather than lush or saturated.
- Favor silhouettes, material readability, and survival-useful clarity over decorative noise.

Use [WORLD_BASIS.md](./WORLD_BASIS.md) when art decisions depend on tone, color language, or world logic.

## What To Make First

Start with assets that unblock gameplay readability and give the project a stable visual language.

Recommended first batch:

1. Booter base sprite set
2. BigARM base silhouette and idle state
3. Harvest node sprites for stone, scrap, ironstone, and algae-related resources
4. Ground tile polish pass for the current prototype terrain
5. Item icons for the current inventory items

## Asset Layout

Use `Assets/_Project/Art/` for source art and exported sprite sheets.

Suggested layout:

```text
Assets/_Project/Art/
  Characters/
    Booter/
      Source/
      Sheets/
    BigARM/
      Source/
      Sheets/
  Environment/
    Ground/
    Props/
    Harvest/
  Items/
    Source/
    Icons/
  FX/
    Source/
    Sheets/
  Animations/
    Characters/
    Props/
    FX/
```

Use `Assets/_Project/Prefabs/` for reusable scene objects that consume the art.

## Folder Intent

- `Source/` is for layered `.aseprite`, `.psd`, or equivalent authoring files.
- `Sheets/` is for exported sprite sheets that Unity imports directly.
- `Icons/` is for item and UI-facing sprites that are meant to render at small sizes.
- `Animations/` is for `.anim`, animator controllers, avatar masks if needed later, and other animation-authored Unity assets.

Keep source art and runtime-ready exports separate when practical.

## Naming Standard

Use PascalCase and keep names descriptive.

Examples:

- `BooterIdleSheet.png`
- `BooterWalkSheet.png`
- `BigArmIdleSheet.png`
- `HarvestNodeIronstoneSheet.png`
- `BooterIdle.anim`
- `Booter.controller`

Avoid spaces, version suffixes in committed filenames, and names like `test`, `final2`, or `new`.

## Sprite Import Baseline

For pixel-art gameplay sprites, use these import settings unless a specific asset needs something else:

- Texture Type: `Sprite (2D and UI)`
- Filter Mode: `Point`
- Compression: `None`
- Generate Mip Maps: `Off`
- Sprite Pixels Per Unit: `32`
- Alpha Is Transparency: `On` for cutout sprites, `Off` for opaque masks or helper textures

For sheet-based animation, use `Multiple` sprite mode when slicing a sheet.

## Animation Baseline

Start simple:

- Build one idle and one walk cycle for Booter first.
- Keep early clips short and readable rather than fluid for their own sake.
- Animate with strong silhouette changes that still read at gameplay zoom.
- Use `Sorting Group` on multi-sprite characters or rigs when child sprites must stay together.
- Keep animation state machines minimal until movement and interaction rules stabilize.

Initial gameplay-facing clip targets:

1. `BooterIdle`
2. `BooterWalk`
3. `BooterHarvest`
4. `BigArmIdle`
5. `ResourceNodeHit`

## Style Guardrails

- Booter should read as trained, burdened, and practical, not heroic or sleek.
- BigARM should read as ancient military hardware rebuilt into something survivable.
- Shapes should feel heavy and repaired rather than pristine.
- Color should stay inside rust, iron, dust, algae, and bone-adjacent ranges.
- Keep the screen readable from gameplay distance before polishing micro-detail.

## Recommended Working Order

1. Lock Booter's body proportions and facing convention.
2. Produce a clean Booter idle sheet.
3. Produce a four-direction or eight-direction walk set, depending on what gameplay immediately needs.
4. Replace current placeholder props with one finished harvestable object family.
5. Build item icons to match the world materials already in the inventory database.

## Practical Unity Rules

- Import new art into stable folders before wiring prefabs or scenes to it.
- Prefer prefab updates over scene-local sprite overrides.
- Do not rename or move imported art casually once prefabs and clips depend on it.
- If an animation asset will be shared by many prefabs, keep it in `Art/Animations/` rather than burying it under one prefab folder.

## Definition Of Done For The First Real Art Pass

The first pass is good enough when:

- Booter is no longer represented by placeholder shapes
- the current harvestables have distinct readable silhouettes
- inventory icons no longer rely on placeholder squares
- the prototype scene reads as part of one visual world instead of mixed placeholders
