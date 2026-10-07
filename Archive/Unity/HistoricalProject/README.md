# Frozen Unity historical restore project

This separate, inactive Unity 6000.4.0f1 project preserves the former 2D/isometric work and compatible rendering, packages, source, and tests outside production `Assets`.

The [restore manifest](./restore-manifest.json) records each source and snapshot hash. Native dependencies were exported with `AssetDatabase.GetDependencies`. The TopDown3D runtime copy is a frozen compile dependency of historical renderer features, not a competing maintained implementation.

An external copy imported and compiled in background and passed all eight historical tests. To restore, copy `Assets`, `Packages`, and `ProjectSettings` into a separate project and use the pinned editor. Keep caches and outputs untracked. Never copy duplicate GUIDs into production `Assets`.

The original compatible profile/renderer configuration remains frozen here. Current production uses its own 3D renderer configuration. Reactivating historical development requires a separate user-directed task.
