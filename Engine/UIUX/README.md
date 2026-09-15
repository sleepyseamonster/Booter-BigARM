# Engine interface workspace

Dedicated home for the UI/UX agent working on the native scene and viewport tool.

- [Agent instructions](AGENTS.md)
- [Interface change SOP](SOPs/CHANGE_INTERFACE.md)
- [Desktop fullscreen check](Tools/check_windowed_fullscreen.py)
- [Testing-menu interaction check](Tools/check_menu.sh)
- [Current minimal viewer and verification](MINIMAL_VIEWER.md)
- [Scene selection and gizmo engine contract](GIZMO_ENGINE_CONTRACT.md)
- [First change and verification](MENU_CHANGE.md)

Shipping code remains in the canonical engine source tree: [menu shell](../Source/Tools/EngineMenu.h), [workbench integration](../Apps/Workbench/main.cpp), and [rock controls](../Source/Tools/RockWorkbench.cpp). This folder owns specialist workflow, not a parallel runtime or roadmap.
