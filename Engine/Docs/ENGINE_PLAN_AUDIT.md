# Whole-Engine Plan Audit and Rewrite

Audited 2026-09-10. Reviewed [draft V1](./Planning/ENGINE_PLAN_V1.md), SHA-256 `776b5bc636d6b854c5cf73e89322c043f66eaad667a20e4eb4bcdf94591265ed`, against the [live system audit](../Research/ENGINE_SYSTEM_AUDIT.md), [research update](../Research/ENGINE_ARCHITECTURE_RESEARCH.md), accepted direction and game constraints. This is a self-audit, not an independent reviewer or runtime test.

## Verdict on V1

The draft covers the needed system families and preserves the engine/game direction. It is not yet sufficient for autonomous implementation: large milestones hide dependencies, several services arrive too late, and some verification language is too broad. Rewrite before treating it as the execution authority.

## Findings and required corrections

| ID | Severity | Finding in V1 | Correction in rewritten plan |
|---|---|---|---|
| A-01 | High | A feature-family list does not tell the agent which concrete work can start or what it must prove. | P01–P37 work packages with explicit dependencies, owned modules, deliverables, proof and completion evidence. Machine-check graph/coverage; master plan remains the human authority. |
| A-02 | High | M4 is the first explicit durable world-state milestone, after IDs, animated entities and generated rocks have appeared. | P01 defines identity/version/document rules; P13 proves a minimal restartable snapshot before M3; P21 extends the same persistence owner to region deltas and consistent save generations. |
| A-03 | High | “Streaming” combines rendering, collision, nav and logical existence. | Explicit readiness/retirement contracts and owner epochs in P19/P20. A visible chunk is not automatically safe to enter; invisible BigARM is not deleted. |
| A-04 | High | M5 navigation could arrive after terrain/placement choices make BigARM's routes impossible. | P14 establishes both agent profiles and route/exclusion constraints before rock placement; P18 generates macro traversal truth; P23/P24 implement detailed/coarse travel and handoff. |
| A-05 | High | “Atomic saves” is underspecified for player state, companion cargo and multiple region files. | P13/P21/P36 require save-generation manifests, backup/recovery, operation identity, crash fault injection and generator/content-version compatibility. Never silently reset corrupt saves. |
| A-06 | Medium | Audio/HUD appear chiefly in M6, making earlier motor, animation and interaction evaluation visually narrow. | One synthesized/owned cue and debug presentation in P12; production UI/audio bindings in P26 before the first companion expedition. Accessibility/text identity begins there and matures in P33. |
| A-07 | High | M8 packaging postpones discovery of absolute asset paths and editor dependencies. | P07 establishes install-relative paths/build profiles; P13 creates a portable technical game package; P35/P37 mature packaging and exact candidate checks. |
| A-08 | High | “Windows checkpoint” is not a precise gate and can become permanently deferred. | W01 native build, W02 native GPU/input, W03 target workload checks; explicit dependencies for Windows optimization/candidate claims. Mac work may proceed on independent tasks while gaps remain visible. |
| A-09 | Medium | The preferred-library list might be interpreted as authorization to install everything immediately. | Dependency adoption occurs only with the first consumer; proposed versus pinned/verified states remain separate. Each integration records capability, notices and limited proof. |
| A-10 | High | No concrete admission policy prevents memory/upload/job bursts while waiting for a product PC budget. | Configurable provisional engineering limits, bounded queue policy, measured counters and workload corpus. Separate guardrails from final target-PC performance acceptance. |
| A-11 | Medium | V1 promises a shared runtime but leaves the existing FixtureState/Workbench coupling unresolved. | P01/P08/P12 extract only services with real consumers; `Apps/Game` and Workbench share one runtime. No parallel preview simulation or general-purpose framework rewrite. |
| A-12 | Medium | Game content and technical capability completion are not sharply separated. | Distinguish engine-ready, integrated slice, creative acceptance and release approval. Placeholder rigs/cues/content have explicit provenance and do not close final-art or game-design gates. |
| A-13 | High | “Coarse travel” does not yet specify what happens when detailed re-entry is invalid. | P24 single-writer ownership, persisted route/version/progress, readiness-before-handoff and hold/replan on invalid detail. No player-relative snap or fabricated route progress. |
| A-14 | Medium | The prior OR-2 → OR-3 → OR-4 wording could force all architecture work into a renderer-only queue. | Preserve color/shadow/material proof dependencies but permit independent document, cooker, build and runtime work. The whole-engine plan controls scheduling; OR and texture plans remain detail contracts. |
| A-15 | Medium | “No more micro-decisions” could be read as bypassing product or external-action authority. | Agent owns technical sequencing and bounded implementation; product/artistic choices, purchases, external publication and destructive operations retain their existing authority boundaries. Do independent work while a real input is pending. |
| A-16 | Medium | Full future scope could expand into unnecessary commercial-engine features. | Explicit deferred-feature table with concrete review triggers. No multiplayer, canyon, ocean, general destruction, ray-tracing or custom editor framework work by implication. |

## Rewrite acceptance checks

The rewritten [master plan](./FOUNDATION_PLAN.md) must cover all 24 audited capabilities and all existing R-01–R-12 requirements, name the complete M1–M8 path, and expose work-package dependencies without cycles. Every package must have a verifiable deliverable and a proof boundary. Windows gates must remain required for Windows claims, even when another task is ready locally.

Check technical consistency: one world/time owner; stable IDs distinct from handles; physics/nav/render surfaces share versioned source; source/cooked/save state separate; cancellation prevents late adoption; no teleport recovery; regular third person and Mac preference preserved. Check procedural concerns in every shared system. Check document routing so the old foundation ending and narrow next-step guidance no longer compete with the rewritten program.

## Post-rewrite self-audit

The rewrite now provides named work packages, an acyclic dependency record and a capability coverage check. It moves identity, snapshot recovery, input/audio feedback and portable packaging before world scale. It attaches navigation constraints to generation, separates detailed/coarse BigARM ownership, and defines budget/admission and platform gates. Current status remains the verified fixture; none of the new packages is marked implemented merely because its plan exists.

Residual uncertainty is empirical or product-owned: integration pins not yet tested, Windows environment/hardware, target performance/quality, real asset fidelity and first-loop creative acceptance. These are attached to specific gates. They do not prevent starting M1 or justify repeatedly asking the user which basic engine subsystem to build.

Final dependency review also corrected two edge cases: an asset larger than one frame's upload allowance needs a bounded admission path so it cannot starve forever, and M4's navigation-readiness interface cannot be presented as implemented Recast tiles before P23. M4 technical anchors likewise do not establish companion behavior. These corrections are now explicit in the master plan.

The structural checker proves plan consistency only. It cannot prove that the design is complete for all future creative changes or that the engine implementations will work. Re-audit at milestone completion, an invariant failure or a material direction change; avoid replacing execution with continuous planning.
