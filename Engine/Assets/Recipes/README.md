# Native World Recipe Fixtures

These are technical inputs for the new engine, not final art, world coordinates or BigARM dimensions.

`wasteland-rock.json` controls the first native faceted rock generator. Inputs use integer millimetres and permille values. `wasteland-placement.json` reserves a spawn exclusion and a short route for both calibration agent sizes. Region span is currently 256 m. Placement checks use the generated rock's full horizontal footprint.

From `Engine/`:

```sh
build/foundation/engine_rock_cook Assets/Recipes/wasteland-rock.json out/rocks/example
build/foundation/engine_workbench --model out/rocks/example/model.json
```

Choose a new cook directory each time. The result contains the existing engine model format, a recipe copy and a rock identity/bounds/surface sidecar. The current workbench reads the generated model for inspection; material/collision/LOD adapters and live recipe editing follow next. The placement fixture is consumed by the placement API; it is not yet a populated or streamed world.

`wasteland-terrain.json` supplies the provisional seed/amplitude for the CPU open-ground generator. The [terrain result](../../Docs/TERRAIN_JOBS_RESULT.md) documents placement, corridor and streaming limits.
