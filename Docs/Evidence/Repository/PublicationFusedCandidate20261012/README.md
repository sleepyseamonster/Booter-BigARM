# Fused rock candidate verification

This is a technically verified candidate, not an adopted production asset. The seven original Fused files remain unchanged and untracked at their existing paths. Their intended disposition is still an author decision under the [closeout plan](../../../Operations/Repository/PUBLICATION_CLOSEOUT_PLAN_2026-10-12.md).

The proposed repair replaces only the prefab's obsolete participant-script GUID `0e50320d49327094f9aaf000cfb07799` with `6209eff4b58d40142830d68f3faacb87`, taken from the existing `SculptedRockFusionParticipant.cs.meta`. The serialized class identifier already names that component. No mesh, material reference, transform, field value or asset metadata is regenerated. The corrected prefab and review diff are retained locally under `Logs/PublicationImplementation20261012/FusedCandidate/`.

The isolated background Unity editor imported the corrected candidate and resolved the participant. The shared mesh/collider, two material slots, static concave collider and component relationships passed. Imported geometry has 4,645 render vertices, 2,675 welded positions, 5,346 triangles, one connected component and zero boundary/nonmanifold edges, degenerate triangles or loose positions. Signed volume matches the original fusion report within `7.63e-10` cubic metres. UVs, normals, tangents and vertices are finite and complete.

All seven `SculptedRockFusionTests` passed with zero skips using installed Blender 5.2.2 LTS. The candidate was hash-preserved outside the isolated output folder before the bake/Undo fixture and restored afterward; the test's deletion affected only its newly generated isolated output. The production project and its authoring sources were untouched.

[Verification receipt](verification.json) seals original file and candidate hashes. No production scene was saved, no source was moved, no foreground application was activated, and no gameplay smoke test or visual acceptance was claimed. A second background production-editor read timed out, so unsaved scene state remains unknown. Keeping existing paths and GUIDs avoids needing a speculative reference-removal decision.
