# First Milestone: Runnable Native Rock Generator

The first-pass technical rock-generator milestone is delivered on Mac, 2026-09-10. It generates native rocks, displays the existing rock/ground textures with lighting and shadows, keeps collision on the highest-detail mesh, previews basic detail levels, edits recipes with undo/redo, saves/reloads them, and exports the exact accepted static mesh. This is the usable skeleton milestone; final geological art, gameplay feel and native Windows acceptance remain open.

## Start here

The local package is `Engine/out/rock-generator/`. Double-click **Launch-Rock-Generator.command** inside it. Keep the package in a writable folder. The package contains the executable, shaders, four required cooked material textures, an optional animated character proxy, the native rock cooker, notices and `START-HERE.md`. It launches without Unity or source-relative assets.

The launcher creates `UserData/rock.json` from the bundled example only on first use. Later launches preserve that file. Camera/lighting settings load from `UserData/inspection.json` when present and save on normal exit. The verifier removed only its own newly created UserData after checking the package, leaving the delivered package ready for a fresh first launch.

1. Edit seed, radii, detail, irregularity or bands, then select **Apply recipe**.
2. Right-drag to orbit; use the wheel to zoom. The left inspector controls lighting and materials.
3. **Undo/Redo** restores accepted recipe states. **Save recipe** writes the accepted recipe; **Reload** reads the displayed file. Unsaved recipe edits do not survive exit.
4. Choose a new **Export directory**, then **Export rock**. The default is `UserData/Exports/rock-001`; choose another name for later exports.

Apply draft edits before saving or exporting. Export uses the currently accepted highest-detail preview mesh, even when viewing a coarser level. It writes `model.json`, `mesh.bin`, `recipe.json` and `rock.json` with stable identity, bounds, footprint and triangle surface classifications. Mesh export does not embed textures, prebuild collision or export a whole world. The native cooker and GUI share the same export implementation. Output directories must be new; an interrupted export may leave a partial new directory, and retry requires a new destination.

The workbench can enable the shared third-person character to walk around the collider. Recipe editing is disabled during character mode. Hands-on traversal and creative review were not automated.

## Rebuild and package

From `Engine/` with the already prepared dependencies and cooked assets:

```sh
cmake --preset mac-dev
cmake --build build/foundation --target engine_workbench engine_player engine_rock_cook engine_rock_workbench_tests --parallel 4
python3 Tools/package_workbench.py --build build/foundation --out out/rock-generator-new --catalog out/surfaces-accepted-a/catalog.json --model out/models/calibration/model.json --rock Assets/Recipes/wasteland-rock.json
```

Use a new package directory. The packager selects the four material textures used by this workbench from the existing catalog; it does not recook or modify source art. A fresh package can use the bounded launch check:

```sh
python3 Tools/verify_rock_workbench.py --package out/rock-generator-new --out Evidence/rock-package-new
```

That verifier refuses existing UserData, validates the package inventory, runs the launcher from an unrelated working directory, checks one six-capture render/edit sequence, verifies saved inspection settings and working-recipe preservation, then removes only the UserData it created. It is not a gameplay smoke test. A Windows launcher template is present for future native packages; it has not been executed on Windows.

## Recorded evidence

[The focused native check](../Evidence/ROCK-delivery-native/result.json) passed, including exact export geometry/normals/identity/recipe roundtrip and overwrite rejection, alongside the existing edit/collision/document cases. An initial missing test include was corrected before this final executable ran; the superseded stale-test invocation is excluded from accepted evidence.

[The packaged launch](../Evidence/ROCK-delivery-package/result.json) passed on Metal with all 38 inventoried package files intact, four real material textures, six captures and zero GPU errors. Vertex/index buffer counts returned to 14/7. Undo matched the original scene with zero sampled differences above three code values. Relaunch preserved a deliberately modified working recipe, and inspection settings saved. [The retained package manifest](../Evidence/ROCK-delivery-package/package.json) binds the shipped files and implementation source hashes. The UI export path compiled and its shared implementation was exercised natively; no claim of automated mouse-click testing is made.

This completes the first-pass rock-generator delivery, not the full engine goal. Next is P20 live region residency/readiness, followed by persistent world deltas and the companion/gameplay foundations. No deeper rock polish or additional generator feature set is required before that integration work.
