# Docs Index

This file is a lightweight guide to what the major docs in this repo are for.

It exists to reduce overlap and to make it easier to tell which docs are canonical, which are standards, and which are exploratory reference.

## Canonical Docs

These should be treated as the source of truth unless they are intentionally revised.

- [WORLD_BASIS.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_BASIS.md)
  The setting, tone, and core world fantasy.
- [WORLD_SYSTEMS_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_SYSTEMS_STANDARD.md)
  The procedural generation, chunking, and save/load baseline.
- [PROJECT_STRUCTURE.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/PROJECT_STRUCTURE.md)
  The target layout for `Assets/_Project/`.
- [UNITY_PROJECT_STANDARDS.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/UNITY_PROJECT_STANDARDS.md)
  Compact naming and organization standards.
- [AGENT_AND_UNITY_PRACTICES.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/AGENT_AND_UNITY_PRACTICES.md)
  Working norms for Codex and Unity repo practice.

## Agent Operations

- [Agents/Gottspan/README.md](./Agents/Gottspan/README.md)
  The canonical repo-manager contract and entry point for project coordination.
- [Agents/Gottspan/instructions/MULTI_AGENT_WORKFLOW.md](./Agents/Gottspan/instructions/MULTI_AGENT_WORKFLOW.md)
  Delegation, shared-worktree ownership, handoff, and integration rules.
- [Agents/Gottspan/sops/REPO_AND_UNITY_MANAGEMENT.md](./Agents/Gottspan/sops/REPO_AND_UNITY_MANAGEMENT.md)
  Gottspan's startup, planning, change-control, Unity validation, and closeout SOP.
- [Agents/Gottspan/memory/PROJECT_MEMORY.md](./Agents/Gottspan/memory/PROJECT_MEMORY.md)
  Bounded durable project facts and known management gaps; it does not replace live inspection.

## Program Plans

- [TOP_DOWN_3D_FOUNDATION_PLAN.md](./TOP_DOWN_3D_FOUNDATION_PLAN.md)
  The active perspective top-down 3D direction, bounded first-foundation scope, architecture, proof requirements, and stop conditions.
- [3D_CONVERSION_AUDIT_AND_CHECKLIST.md](./3D_CONVERSION_AUDIT_AND_CHECKLIST.md)
  The original conversion audit, ownership contract, migration matrix, work breakdown, decision gates, asset requirements, and risks. Its orthographic/isometric direction is superseded for new work by the active perspective foundation plan.
- [3D_CONVERSION_START_READINESS.md](./3D_CONVERSION_START_READINESS.md)
  The live Level A and Level B inventory for closing the protected-spike blockers, accepting CP-06, preparing CP-07, and later authorizing production 3D asset work.
- [ISOMETRIC_DIRECTION_BRIEF.md](./ISOMETRIC_DIRECTION_BRIEF.md)
  The historical working contract for the completed protected isometric conversion spike; it remains evidence rather than current production direction.

## Implementation Standards And Seams

These define current preferred approaches or sequencing.

- [BIGARM_COMPANION_STANDARD.md](./BIGARM_COMPANION_STANDARD.md)
  The canonical product and implementation rules for BigARM's companion role, physical follow behavior, and future unloaded-world traversal seam.
- [IMPLEMENTATION_SEQUENCE.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/IMPLEMENTATION_SEQUENCE.md)
- [GAMEPLAY_ARCHITECTURE_BASELINES.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md)
- [INPUT_ARCHITECTURE_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/INPUT_ARCHITECTURE_STANDARD.md)
- [MOVEMENT_CAMERA_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/MOVEMENT_CAMERA_STANDARD.md)
- [URP_2D_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/URP_2D_STANDARD.md)
- [UNITY_AUTOMATION.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/UNITY_AUTOMATION.md)
- [CODEX_EDITOR_STANDARD.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/CODEX_EDITOR_STANDARD.md)

## Project Snapshot Docs

These describe current repo state rather than durable design truth.

- [PROJECT_BASELINE.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/PROJECT_BASELINE.md)
- [PROJECT_STATUS.md](./PROJECT_STATUS.md)
- [ROADMAP.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/ROADMAP.md)
- [RESEARCH_PLAN.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/RESEARCH_PLAN.md)
- [ConversionEvidence/LEGACY_BASELINE_2026-08-12.md](./ConversionEvidence/LEGACY_BASELINE_2026-08-12.md)
  The immutable CP-02 scene, renderer, hierarchy, Build Settings, Git-blob, and SHA-256 preservation anchor for the protected 2D path.
- [ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md](./ConversionEvidence/PROTECTED_SPIKE_REPORT_2026-08-12.md)
  CP-01 through CP-05 implementation evidence, screenshots, corrections, validation results, limitations, and the CP-06 decision boundary.
- [ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md](./ConversionEvidence/TOP_DOWN_3D_FOUNDATION_REPORT_2026-08-13.md)
  Current perspective foundation implementation, automated proof, protected-baseline evidence, deferred scope, and user-owned acceptance boundary.

## Design Evidence And Decisions

- [DECISION_LOG.md](./DECISION_LOG.md)
  Pointers and rationale for consequential design, architecture, and workflow decisions; controlling docs still hold current truth.
- [PLAYTEST_LOG.md](./PLAYTEST_LOG.md)
  Repeatable playtest observations tied to a build or commit and a specific design question.

## Provisional Reference Docs

These are intentionally non-canonical. They preserve ideas, research, or working context that may later be refined, replaced, or discarded.

- [WORLD_GEN_REFERENCE_NOTES.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_GEN_REFERENCE_NOTES.md)
  Distilled world-generation ideas from exploratory discussion.
- [UNITY_TILEMAP_PROCGEN_REFERENCE.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/UNITY_TILEMAP_PROCGEN_REFERENCE.md)
  Unity-facing implementation notes from exploratory discussion.
- [WORLD_GEN_RESEARCH_SUMMARY.md](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/WORLD_GEN_RESEARCH_SUMMARY.md)
  External research findings from Unity docs, developer practice, and similar projects.

## Working Rule

When adding a new doc, decide which bucket it belongs in:

- canonical
- implementation standard
- project snapshot
- provisional reference

If a provisional reference becomes stable enough to drive work repeatedly, move its useful parts into the appropriate canonical or implementation doc instead of letting both drift indefinitely.
