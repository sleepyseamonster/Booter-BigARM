# Single-rock reference comparison

This is a **test-only** C# adapter, not an engine dependency. The native C++ build,
cooker and packaged viewer do not use .NET, Unity assemblies or a Unity process.
`prepare_reference.py` extracts the preserved project's deterministic planner,
mesher and topology method bodies into ignored Engine-local output. Editor-only
entry points are omitted; the original inputs are SHA-256 recorded.

`UnityMath.cs` supplies the small numeric/type surface needed to run those bodies
outside Unity. It is not a full Unity emulation. The native test independently
checks reconstructed source poses against all three actual saved Unity family
recipes. Together these checks constrain adapter error; they do not constitute
a live Unity Editor screenshot comparison.

Run from the repository root, choosing new output directories:

```sh
cmake --build Engine/build/foundation --target engine_single_rock_tests
Engine/build/foundation/engine_single_rock_tests Engine/Assets/GoldenRockPresets Engine/out/rock-check-native
python3 Engine/Tests/ReferenceRock/prepare_reference.py Engine/out/rock-check-reference
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build Engine/out/rock-check-reference/Reference.csproj
python3 Engine/Tests/ReferenceRock/compare.py Engine/out/rock-check-native Engine/out/rock-check-reference/bin/Debug/net9.0/Reference.dll Engine/out/rock-check-comparison
python3 Engine/Tests/ReferenceRock/silhouettes.py Engine/out/rock-check-native Engine/out/rock-check-comparison Engine/out/rock-check-silhouettes
```

The reference runner needs .NET 9; only the silhouette rasterizer needs NumPy and
Pillow. The C++ test needs neither. All generated source copies remain under
`Engine/out/`; the preserved Unity sources are read-only.

Comparison checks source identity/count, position/scale/quaternion tolerances,
equal vertex/triangle counts, oriented triangle connectivity and corresponding
vertex distance below 0.1 mm. Rasterization compares nine orthographic views per
case at 512×512, requiring silhouette intersection-over-union of at least 0.9995.
The 21 cases cover three captured layouts, a fresh Golden Rock plan, Auto and all
eight explicit profiles, signed seed boundaries, seeded dimensions and extreme
body proportions. This is a bounded regression set, not proof of all possible
seeds or all Unity settings.
