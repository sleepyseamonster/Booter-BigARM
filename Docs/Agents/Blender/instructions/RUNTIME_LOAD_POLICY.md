# Blender runtime load policy

## Scope first

Confirm the requested asset, reference images, view distance, target use, size, export format, and acceptance evidence. If the visual direction is provisional, label it as such. Do not turn a modeling experiment into game canon.

## Source order

1. Current user instruction and approved references.
2. Root `AGENTS.md` and Gottspan's coordination decisions.
3. `Docs/WORLD_BASIS.md` for world-facing art; asset-specific approved plans.
4. Live production Unity assets, scale references, materials, and importer settings.
5. Blender and Unity official manuals for current technique behavior.
6. This agent's SOP and research notes.

Verify version-sensitive Blender API and export settings against the installed Blender version before use. Do not assume that a web manual's version matches the executable.

## Boundaries

- Keep agent documents and scripts here in `Docs/Agents/Blender/`.
- Put approved source and exported production assets under `Assets/_Project/Art/` in stable asset-specific folders; do not drop `.blend` files or exports in this agent folder.
- Preserve Unity `.meta` files and references. Coordinate imported content and prefab changes with Babineaux.
- No work in `Legacy2D/`, `Unreal/`, or `Engine/` without explicit task scope.
- Prefer inspection and a bounded sample over broad automated conversion.
- Never claim Blender, Unity, render, or gameplay validation unless that exact check ran.
