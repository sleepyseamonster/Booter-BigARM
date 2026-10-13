# Material serialization verification

The four materials retain their rendered `_BaseColor`, shader, textures and GUIDs. Pinned URP Lit uses `_BaseColor` for surface data; its shader GUI synchronizes the hidden legacy `_Color` property. This batch retains that synchronization and removes only trailing whitespace.

The isolated background editor imported all four without shader errors. `BadwaterPlayableSceneBuilder.ValidateFromCli` passed the 9,216-terrain production contract; the authored-camp asset and character-identity-color tests both passed. [Receipt](verification.json) seals exact material and metadata hashes. Original dirty bytes remain in ignored local preservation storage. The Fused prefab is outside this batch. No scene save, source move, package change, foreground activation or gameplay smoke test occurred.
