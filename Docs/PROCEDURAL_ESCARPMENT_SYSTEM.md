# Procedural Escarpment System

## Purpose

The perspective world needs occasional raised ground and short rocky walls without turning the whole playfield into noise. Escarpments are therefore generated as sparse, deterministic landforms with broad quiet ground between them.

The player remains the creative authority for final density, silhouette, texture, and traversal feel. The values below are implementation defaults, not world canon.

## Chosen Shape Strategy

Each accepted global region cell owns one irregular elliptical landform. A stable seed chooses its center, footprint, rotation, height, and edge profile. Sampling is done in world space, so the same formation crosses chunk borders without seams and does not depend on chunk load order.

The landform has three connected parts:

1. The authoritative height sampler raises the interior into a short plateau.
2. Low-amplitude ridged relief breaks up the plateau surface without making all terrain equally rough.
3. A faceted rock-face strip covers the narrow transition from surrounding ground to the raised lip.

This hybrid keeps physics and placement honest while giving the steep area a dedicated rocky material and silhouette. A purely decorative cliff would not change elevation; a purely displaced terrain slope would stretch the ground material and read like a mound.

## Runtime Contract

- `TopDown3DEscarpmentSampler` owns deterministic feature admission and elevation.
- `TopDown3DHeightSampler` adds that elevation to the existing broad terrain noise.
- `TopDown3DChunkMeshBuilder`, terrain collision, normals, safe spawn, clutter, and dust all consume the same final height.
- `TopDown3DEscarpmentDecorator` builds one faceted face mesh per affected chunk using the regular rock material.
- The streamed terrain `MeshCollider` is the sole collision authority for the raised surface. The overlapping face mesh is presentation-only, avoiding duplicate collider seams that can snag a moving capsule.
- Booter's controller reads the true terrain-triangle normal and accepts surfaces through `48` degrees. Steeper uphill faces remain blocking while lateral and downhill movement remain physical.
- Generated presentation meshes are children of their owning chunk and unload with it.

## Distribution and Art Direction

- Region cells are deliberately much larger than chunks, and fewer than half admit a formation.
- Footprints occupy only part of admitted cells, leaving ample sparse ground.
- Two low-order boundary waves create broad natural irregularity instead of noisy saw teeth.
- The face uses two faceted bands from dust-covered toe to raised lip. Broad triangles and triplanar rock texture supply detail at low polygon cost.
- The face overlaps the terrain slightly to suppress cracks and make the wall appear embedded.
- Small rocks, dust pockets, and surface clusters continue to use the final raised terrain automatically.

## Tuning Surface

`TopDown3DWorldSettings` exposes:

- region size and admission chance;
- minimum and maximum footprint radius;
- minimum and maximum climbable height;
- rocky edge width;
- crag relief amplitude and frequency;
- face segment count. The retained collider-grouping field supports deterministic data compatibility but no longer creates overlapping runtime colliders.

Change the escarpment generation version whenever a deliberate placement-breaking algorithm change is accepted. Ordinary presentation changes should not reshuffle formation ownership.

## Proof Gates

- Same seed, version, cell, and world coordinate produce exactly the same feature and elevation.
- Feature enumeration is stable and contains no duplicate cell owners.
- Samples demonstrate both raised areas and substantially more open ground.
- Face mesh data is deterministic, faceted, and non-empty in affected chunks.
- Planned wall data remains deterministic and bounded for compatibility, but runtime decoration creates no duplicate wall colliders.
- Face meshes are chunk-owned and removed on unload.
- Adjacent terrain chunks continue sharing identical border samples through the common height sampler.
- Unity compilation must be clean before handoff; final visual density and hands-on traversal acceptance remain user-owned.
