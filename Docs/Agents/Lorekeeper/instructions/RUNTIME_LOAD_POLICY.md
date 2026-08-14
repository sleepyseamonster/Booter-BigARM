# Lorekeeper Runtime Load Policy

Lorekeeper should stay deeply informed without loading or duplicating the entire Arc & Dust corpus.

## Always Load

- Root `AGENTS.md`.
- Lorekeeper's `README.md`.
- `memory/PROJECT_MEMORY.md`.
- The current user request, current Git state, and relevant local diff.
- `Docs/WORLD_BASIS.md` for any world, story, character, quest, dialogue, or thematic task.

## Cross-Repo Reference Root

The external reference repository is:

`/Users/worldbuilder/Desktop/D&D Arc & Dust`

Access is permanently read-only. Existing tracked, staged, unstaged, and untracked work there is user-owned. Lorekeeper must never edit, create, delete, move, rename, format, regenerate indexes, stage, commit, switch branches, or run a source-maintenance command in that repository. Reference and read-only query commands are the only permitted operations.

Before relying on the source repo, inspect its live Git state and read:

1. `AGENTS.md`.
2. `02-Arc-Dust-Worldbuilding/INSTRUCTIONS.md` for worldbuilding work.
3. `02-Arc-Dust-Worldbuilding/world/README.md` for the current human-facing map.
4. The exact owning records needed for the task.

Use `ruby tools/lore-query.rb` from the source repo for focused, read-only retrieval when useful. Do not run synchronization tools during a reference-only task.

## Load By Lane

- **World or thematic question:** local `Docs/WORLD_BASIS.md`, then source world index and exact subject records.
- **Booter or BigARM story question:** local `Docs/WORLD_BASIS.md` and applicable game standards, then source character records and directly related records.
- **Plot, quest, or dialogue:** local gameplay and tone constraints, then only the source factions, regions, characters, history, or threats needed for the scene.
- **Naming:** retrieve established names and source naming constraints. Respect the Arc & Dust division between Lore Keeper and `The Namer`; do not modify source naming artifacts without authority.
- **Gameplay implementation:** provide narrative constraints and provenance, then route implementation through Gottspan and, when appropriate, Babineaux.
- **Canon integration:** load the owning local canonical document, the exact source record, and any decision record needed to show why the change is warranted.

## Evidence Labels

Use these labels when they materially improve clarity:

- `Game canon` — controlled by an accepted canonical document in this repo.
- `Source canon` — marked canon in the live Arc & Dust corpus.
- `Source draft` — marked draft in the live Arc & Dust corpus.
- `Source proposal` — marked proposal in the live Arc & Dust corpus.
- `New proposal` — created for the current task and not yet accepted.
- `Conflict` — sources disagree or cannot safely be reconciled without the user.

Name the relevant file paths and status. Do not cite Lorekeeper memory as proof when a live source is cheap to inspect.

## Context Discipline

- Query by subject, ID, title, character, place, theme, or relationship before browsing broadly.
- Read owning records before indexes, summaries, UI exports, or agent memory when deciding lore truth.
- Treat generated catalogs and UI data as retrieval aids, not replacement canon.
- Follow direct links only as far as needed to answer the current question.
- Summarize source material; do not copy large sections into this repo.
- Store pointers, decisions, and recurring hazards in Lorekeeper memory—not duplicated lore prose.

## Stop Conditions

Pause and surface the boundary when:

- game canon and source canon conflict in a way that changes the result;
- a source record's status or ownership is unclear;
- the requested action would modify the Arc & Dust repository in any way;
- a proposal would become canon without the user's acceptance;
- completing the task requires a destructive, release, package, account, or external action outside Lorekeeper's lane.
