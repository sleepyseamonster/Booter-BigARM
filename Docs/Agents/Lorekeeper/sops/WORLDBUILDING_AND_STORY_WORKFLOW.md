# Lorekeeper Worldbuilding And Story Workflow

Use this SOP for lore retrieval, thematic development, plot work, character writing, quests, dialogue, environmental storytelling, and canon-alignment tasks.

## 1. Establish The Contract

Define:

- the requested outcome;
- whether the task is retrieval, synthesis, invention, critique, or canon integration;
- the local owning document, if one exists;
- whether Arc & Dust access is reference-only or includes explicit write authority;
- the proof needed for the result;
- the point where a creative or canon decision returns to the user.

## 2. Inspect Both Workspaces Safely

- Inspect Git state in Booter & BigARM.
- If the source repo is needed, inspect its Git state without changing it.
- Treat every unrelated modification and untracked file as user-owned.
- Do not let source-repo dirtiness become permission to edit, clean, sync, or commit it.

## 3. Establish The Local Game Baseline

Read `Docs/WORLD_BASIS.md` and the smallest applicable set of character, gameplay, visual, or narrative documents. Record which statements are canonical, provisional, implementation facts, or merely historical evidence.

## 4. Retrieve The Smallest Useful Source Slice

From `/Users/worldbuilder/Desktop/D&D Arc & Dust`:

1. Read the source governance files required by the runtime policy.
2. Query the relevant term, ID, or relationship.
3. Read the owning world records rather than relying only on indexes or agent memory.
4. Capture each record's `canon_status` and any unresolved language.
5. Stop expanding once the current question is supported.

## 5. Build An Alignment View

Separate findings into:

- already established game canon;
- compatible source material that adds useful depth;
- source material that remains draft or proposal;
- new ideas created for this task;
- contradictions or decisions that need the user.

Do not smooth over differences in terminology, survival logic, character history, tone, or chronology.

## 6. Produce The Requested Work

For retrieval, answer with concise source-backed context.

For creative work, make the smallest coherent proposal that satisfies the request and mark invention clearly.

For plot or dialogue, preserve world constraints and character agency while avoiding premature canon claims.

For game-facing design, translate lore into usable narrative constraints, hooks, beats, environmental cues, or content requirements without silently implementing Unity changes.

## 7. Integrate Only With Authority

- If the user asked only for ideas, analysis, or retrieval, do not edit canon documents.
- If the user approved a local canon change, update the canonical owning file rather than Lorekeeper memory alone.
- If the correct owning record is in Arc & Dust, stop unless the user also authorized writing to that repository.
- Coordinate repo-wide documentation placement through Gottspan.
- Route Unity-facing execution through Gottspan and Babineaux as appropriate.

## 8. Verify And Close Out

- Re-read changed local documents and check links and status language.
- Run only relevant documentation or repository checks.
- Confirm no unrelated source-repo changes were made.
- Report what is canon, what is proposed, what source files informed the result, and what remains the user's decision.
- Update Lorekeeper memory only with durable pointers, accepted decisions, and recurring hazards; never use memory as a substitute for the owning lore record.
