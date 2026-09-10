# Cook and Verify Textures

Prepared 2026-09-10. This procedure governs the planned texture cooker; it does not imply that the cooker or runtime loader exists. The executable offline research procedure is [TEXTURE-001](../Research/Experiments/TEXTURE-001/README.md).

1. Read [current status](../Docs/STATUS.md), [texture research](../Research/TEXTURE_SYSTEM_RESEARCH.md) and the applicable stage of [the implementation plan](../Docs/TEXTURE_SYSTEM_PLAN.md). Confirm the exact asset subset and proof boundary. Inspect Git and preserve unrelated work.
2. Verify the surface manifest and selected source hashes. Use only Engine copies. Check material references, importer conventions and channel semantics; treat filename guesses as unresolved. Source art stays separate from runtime inputs.
3. Declare the recipe before conversion: source/hash, role, color transfer, channel mapping, alpha/normal convention, resolution and edge policy, mip algorithm and target format. Reject silent resize, channel loss and unexpected color metadata.
4. Verify pinned dependency trees. Build tools into an isolated ignored build directory; retain tool hashes, compiler/configuration and source lock. Do not change upstream sources or the application tool options to run a research probe.
5. Write outputs into a new ignored directory, then validate before publishing the cooked record atomically. Preserve originals. Record command argv, duration, output hashes, dimensions, every mip level and estimated decoded/GPU payload bytes. Never use PNG size as VRAM usage.
6. Compare with independent semantic oracles: color averages in linear space, numerical channels without transfer conversion, alpha separately, normalized vectors with angular error, odd sizes and repeating borders. A successful encode or `--validate` is only structural/round-trip evidence. Stock texturec mip generation currently fails selected semantic checks.
7. During runtime implementation, capture channel/UV/normal/mip fixtures through the production upload and material path. Record backend, device, resolution, exposure and resource counts. Check sharing, replacement failure, final release and stale requests. Add native Windows evidence separately.
8. Compare real assets against uncompressed references before accepting compressed formats. Record per-channel and angular error plus close, distant and oblique views. Keep height/parallax quality and creative approval distinct from a scalar checksum.
9. Update status with exactly what passed and what remains open. Run focused checks, review the exact stage manifest and commit task-owned work locally. Pushing or distributing assets/tools requires its own current authority and applicable provenance/license review.

Research completion means a defensible design and retained evidence. Runtime completion requires the implementation-stage tests; visual acceptance remains the user's decision.
