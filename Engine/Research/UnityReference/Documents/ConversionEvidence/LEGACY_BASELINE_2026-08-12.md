# Protected 2D Baseline — 2026-08-12

This snapshot is the CP-02 preservation anchor captured immediately before Unity-side conversion assets are created.

- Baseline commit: `2129bfbb5a9512b0dcadfcb3a73339fb362e8660`
- Branch at capture: `main`, ahead of `origin/main` by 10 local commits
- Active Unity editor: `6000.4.0f1`
- Existing dirty ground-art files were user-owned and excluded from conversion work.

## Enabled Scene Order

1. `Assets/_Project/Scenes/PrototypeScene.unity`
2. `Assets/_Project/Scenes/SampleScene.unity`

The conversion lab must not become an enabled build scene before the cutover gate.

## Renderer Baseline

- Pipeline asset: `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset`
- Default renderer index: `0`
- Renderer at index 0: `Assets/_Project/Settings/Rendering/URP/Renderer2D.asset`
- The conversion renderer must be appended at a non-default index. `Renderer2D.asset` and the default index must remain unchanged.

## Protected Hierarchy State

In `PrototypeScene.unity`:

- `Sand Patch Grid` is inactive.
- `Ground Grid` is inactive.

## Git Blob Anchors

| File | Git blob |
| --- | --- |
| `PrototypeScene.unity` | `f5c360f2135e892b26e5ae3079a2e2bfd44ead98` |
| `SampleScene.unity` | `7b420f6ebd3ce91e13736fea44dc79263dcf2c94` |
| `Renderer2D.asset` | `69d2802a7a73d799e53b09967ce4448bff5eec11` |
| `UniversalRP.asset` | `d9771fcd3efbf32b9fe6d84a49c05d459f8b77a8` |
| `EditorBuildSettings.asset` | `5f3aba73d55536c9cffd65526b64055e7785e605` |

## SHA-256 Anchors

| File | SHA-256 at capture |
| --- | --- |
| `PrototypeScene.unity` | `fab742d6de621ca5414d9f2ca273ce82ba1cbec52ca78e72a3a4437d50b6d9a7` |
| `SampleScene.unity` | `387d8216a517be775b6ecc2ab55b97ece687c3372322da2ba027a96908014912` |
| `Renderer2D.asset` | `0ad61c87afceceb7d18b08c9fe35d2e36a89640d5af073be0e9cec5839bf1516` |
| `UniversalRP.asset` | `7cdcc79dedadbde17803800f44ecb6ffd08b71e83ae56ebbf6db4adb555613e7` |
| `EditorBuildSettings.asset` | `882bc38bca9cd5fe492cafb3583935c997bcbc691e50bcc7451ba2ddfffc8b5a` |

`UniversalRP.asset` is expected to change only by appending the conversion renderer while preserving index 0 and its default. The other four SHA-256 anchors must remain stable through CP-05.
