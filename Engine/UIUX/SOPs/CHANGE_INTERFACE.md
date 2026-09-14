# Change the engine interface

1. Read the current request and engine agreements. Identify the exact visible behavior, owning controls and executable the user is running.
2. Inspect Git status and relevant diffs. Preserve unrelated changes and user-owned running applications. Snapshot shared-file baselines under ignored Engine output before editing.
3. Default to zero visible controls. Add a testing control only when the current test requires it, and expose only that subsystem. State the small acceptance contract. Check overlap, resize behavior, keyboard/mouse capture, scrolling, and whether hidden UI performs document bookkeeping. Explain any world/persistence impact before implementation.
4. Edit canonical UI sources. Avoid interface redesigns outside the request and avoid mixing UI visibility with engine data.
5. Build the native workbench. For menu-shell changes run `Engine/UIUX/Tools/check_menu.sh`; it uses real Dear ImGui event handling without a GPU or gameplay. Inspect the native UI in a separate review build when visual layout changes. Check closed, open and dismissed states, smallest supported window and neighboring panels. Keep captures in ignored Engine output.
6. Record which source/build/interaction/visual checks actually ran. A headless menu check does not prove the entire rendered viewport, physical input feel or Windows support.
7. Verify documentation links and whitespace. Stage only task-owned files or exact hunks under Gear Ball's rules, review the staged diff and commit verified work. A commit does not authorize push or release.
8. Tell the user what changed and how to open the updated executable. Never silently restart their active editing session.
