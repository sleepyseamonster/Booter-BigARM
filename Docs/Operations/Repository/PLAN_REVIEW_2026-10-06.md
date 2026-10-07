# Reorganization plan review — 2026-10-06

**Scope:** audit of the execution plan against current code, paths, dependency contracts, prior receipts, and active work. **Verdict:** the target layout is sound, but the original remaining-batch instructions were insufficient for safe execution. The [execution plan](./REORGANIZATION_EXECUTION_PLAN.md) has been corrected. This review moves no files and changes no runtime, assets, package versions, or project settings.

## Findings and corrections

| Priority | Finding | Evidence | Correction |
| --- | --- | --- | --- |
| High | The disabled generated prototype is a live authoring template, not only an archival example | `BadwaterPlayableSceneBuilder.BuildFromCli` opens it and transfers configured player, companion, input, camera, lighting and HUD roots | ORG-07 archival is conditional on replacing/preserving/retiring every consumer; primary scene relocation and renderer rename are separate sub-batches |
| High | Removing legacy references from the main editor/test assemblies cannot precede ownership of their dependent code | Isometric builder imports `BooterBigArm.Runtime`; two historical test files remain under the production test assembly | ORG-04 first creates historical Editor/test assembly boundaries, splits mixed validation, and proves each intermediate state compiles |
| High | Renderer reindexing conflicts with the existing validation contract and can invalidate still-imported historical scenes | `ConversionBaselineValidator` requires Renderer2D at index 0 and the 3D renderer above 0; camera/index tests and builders encode those assumptions | ORG-05 prepares a compatible candidate; ORG-06 switches consumers and extraction atomically; validation is replaced before its old requirement is removed |
| High | A restoration manifest alone does not establish that extracted Unity work still functions | YAML/assembly scans omit binary dependencies and runtime path/load contracts; historical scenes depend on shared settings and packages | Require Unity dependency enumeration plus assembly/Resources/path inventory and an actual offline restored-project import/compile check |
| Medium | The baseline CSV is stale after ORG-01 and later feature work | It still names `Engine/` and `Unreal/`; the working tree contains newer atlas tooling and design work | Rebase an owned candidate subset against current HEAD and receipts before each batch; never replay completed moves or include another chat's work |
| Medium | Moving tool/receipt folders requires code changes beyond repairing Markdown links | The receipt verifier uses `parents[4]`, currently has no relocation resolver, and validates old destination paths | Root-discovery and relocation support must be implemented and tested first; original receipts stay immutable |
| Medium | Source-art/resource acceptance was aspirational despite known local tool/data gaps | Prior Death Valley checks did not locate Blender; full-precision GIS source recovery remains incomplete | Study moves wait for real resource-reopening capability; unavailable source data stays explicitly unavailable, not “recovered” by creating manifests |
| Medium | Validation and rollback were broad labels rather than candidate-bound acceptance contracts | Earlier table named “focused checks” without naming the affected interfaces or intermediate-state constraints | Added per-surface validation, exact owned baselines, binary/native dependency scope, reverse candidate checks, and explicit evidence limits |

## Concrete consumer checks

The prototype dependency is visible in [the playable assembler](../../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterPlayableSceneBuilder.cs). Additional consumers include [repository validation](../../../Assets/_Project/Scripts/Editor/Validation/RepositoryPlayabilityAudit.cs), [World Creator path validation](../../../Assets/_Project/Scripts/Editor/Validation/WorldCreatorProductionPathValidator.cs), [scene audit tests](../../../Assets/_Project/Tests/Editor/ScenePlayabilityAuditTests.cs), source capture tooling, and the Windows workspace preflight. Moving the scene and only changing Build Settings would leave working tools broken.

The current renderer requirement is explicit in [conversion validation](../../../Assets/_Project/Scripts/Editor/Validation/ConversionBaselineValidator.cs). The [isometric builder](../../../Archive/Unity/HistoricalProject/Assets/_Project/Scripts/Editor/Isometric/IsometricConversionLabBuilder.cs) and [historical structure tests](../../../Archive/Unity/HistoricalProject/Assets/_Project/Legacy2D/Tests/Editor/ConversionAssetStructureTests.cs) also consume these contracts. No assertions were relaxed and no dependency was removed by this review.

The current [receipt verifier](../../../Tools/Repository/verify-transfer-manifest.py) lacks the proposed relocation/root-discovery capability. The plan now requires adding and proving that capability before moving it. This is an implementation prerequisite, not a capability claimed present.

New active C# atlas consumers and their tests contain `Docs/DeathValley` path contracts while another chat is developing them. ORG-02 must refresh and coordinate that ownership before relocating the package. This review preserves those files.

## Completed batch evidence

ORG-01's [receipt](./Relocations/hosts-2026-10-06.json) accounts for 1,287 files: 1,279 byte-identical moves and eight declared navigation/tool repairs. Its proof is path/source preservation, planning tests, and archived asset/binding checks. It does not establish native host execution or performance. The optional comparison against current Unity source reports historical shader drift; this remains explicitly disclosed and is not treated as a migration regression or a green source-parity claim.

The original [layout audit](./LAYOUT_PLAN_2026-10-06.md) is now labeled as a dated design snapshot, with current execution and review links. It no longer competes with the execution plan's completed-host status.

## Readiness after review

- ORG-01 remains complete within its recorded proof boundary.
- ORG-02 can proceed only from a refreshed, owned candidate and its concrete path/consumer checks.
- ORG-03 requires tool/root/receipt support and source-resource inspection before study moves.
- ORG-04–06 require compile-safe intermediate boundaries and actual production/restoration validation.
- ORG-07's prototype archival is blocked until its current template/tool dependency is resolved. Primary-scene movement and renderer naming are individually verified operations, not permission to retire template behavior.
- ORG-08 reviews actual runtime load contracts and final ownership; it does not remove packages or activate deferred generation.

Lorekeeper's disconnected external source, local canon ownership, background-only work, exact staging, and independent publication authority were already correctly represented and remain intact. No external lore connection is required to complete the organization plan.
