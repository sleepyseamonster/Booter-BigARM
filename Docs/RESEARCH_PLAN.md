# Research Plan

This is the prioritized research roadmap for Booter & BigARM. It focuses on high-value Unity and Codex topics that affect project quality, editor automation, and long-term maintainability.

## Priority 1: Unity Build And Automation

Goal:
- Make repeatable build, import, and validation workflows from the command line.

Research questions:
- What are the recommended `-batchmode`, `-executeMethod`, `-logFile`, and build-target patterns for Unity 6?
- What should be automated in editor scripts versus left to manual editor use?
- What is the safest way to surface build/test commands for Codex?

Expected output:
- A stable automation pattern for builds and validation.
- Minimal editor entry points that are safe to call from CLI.

Current status:
- The project already has a CLI build bridge in `Assets/_Project/Scripts/Editor/BuildAutomation.cs`.
- `Docs/UNITY_AUTOMATION.md` documents the current command-line workflow and now reflects explicit build-target validation.

## Priority 2: 2D URP Pipeline For Top-Down Games

Goal:
- Keep the render setup clean, performant, and readable for a 2D top-down game.

Research questions:
- What URP 2D settings matter most for top-down readability?
- How should camera, lighting, sorting layers, and render assets be organized?
- What should be kept in project settings versus asset-level settings?

Expected output:
- A project-specific 2D URP checklist.
- Clear settings conventions for scenes, lights, and render assets.

Current status:
- The project already uses URP and the 2D Renderer assets.
- `Docs/URP_2D_STANDARD.md` captures the compact rendering baseline for this repo.

## Priority 3: Project Structure And Naming

Goal:
- Keep the repo easy to navigate and safe to change.

Research questions:
- What folder and naming conventions are most maintainable in Unity teams?
- How should runtime, editor, and test code be separated?
- What is the best practice for asmdef boundaries in a project like this?

Expected output:
- A small, opinionated structure standard.
- Naming rules that prevent churn and ambiguity.

Current status:
- `Docs/PROJECT_STRUCTURE.md` and `Docs/UNITY_PROJECT_STANDARDS.md` already define the repo's folder and naming baseline.
- The remaining work here is to keep those standards aligned with new gameplay systems and test assemblies as the codebase grows.

## Priority 4: Codex And Editor Integration

Goal:
- Make agent-driven work predictable and low risk.

Research questions:
- What task sizes are best for Codex?
- What repo instructions should live in `AGENTS.md` versus a supporting doc?
- What commands and checks should be the default feedback loop for changes?

Expected output:
- A clear agent workflow standard.
- Short, reliable command-line hooks for project tasks.

## Priority 5: Core Game Architecture Topics

Goal:
- Lay the foundation for the actual game systems in a controlled order.

Research questions:
- How should save/load be structured?
- How should procedural generation be chunked and seeded?
- What movement and camera approach best fits a constrained top-down survival game?
- What input architecture best supports keyboard, mouse, and controller?

Expected output:
- An implementation order for the first gameplay systems.
- Data and runtime boundaries that avoid redesign later.

## Research Rules

- Research one priority at a time unless there is a hard dependency.
- Favor primary sources first, then project-specific inference.
- Turn research into a short repo doc only when it changes how the project should be built.
- Avoid expanding the plan unless the new topic will directly reduce rework or risk.
