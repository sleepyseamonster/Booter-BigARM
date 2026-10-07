# TEXTURE-001 — Offline texture semantics

Executed 2026-09-10 on the current Mac. This is a bounded CPU conversion/decode experiment using the existing pinned sources. It launches no renderer or Unity process and changes no application sources, dependency pins or original assets.

## Question and result

Can the pinned texturec be our universal mip cooker, and can its containers enter the generic bgfx parser directly?

**No universal mip path at this pin.** RGBA8 color mips failed the sRGB average oracle; BC7 linear-data mips failed the arithmetic average oracle. KTX 1 parsed through the generic core path. KTX2 written and validated by texturec was rejected by that entry point. A separate KTX2 parser exists in bimg but is not exercised by this inspector.

The [research](../../TEXTURE_SYSTEM_RESEARCH.md) records values, interpretation and implementation choices. Probe `false` semantic fields are measured incompatibilities, not hidden test failures. The process succeeds when it collects those measurements and its operational assertions pass.

## Reproduce

Run from `Engine/`. Prepare the pinned cache first; `--fetch` allows missing archives to be downloaded. Skip that flag when the cache is already present. Choose fresh evidence names because receipts never overwrite prior runs.

```sh
python3 Tools/prepare_probe.py --fetch
python3 Tools/record.py --out Evidence/TEXTURE-local-configure --input Research/Experiments/TEXTURE-001/CMakeLists.txt -- cmake -S Research/Experiments/TEXTURE-001 -B build/texture-research -DCMAKE_BUILD_TYPE=Release
python3 Tools/record.py --out Evidence/TEXTURE-local-build --input Research/Experiments/TEXTURE-001/CMakeLists.txt --input Research/Experiments/TEXTURE-001/inspect.cpp -- cmake --build build/texture-research --target texturec texture_inspect --parallel 4
python3 Tools/record.py --out Evidence/TEXTURE-local-run --input Research/Experiments/TEXTURE-001/probe.py --input Research/Experiments/TEXTURE-001/inspect.cpp --input Research/probe-lock.json --input Assets/SurfaceLibrary/manifest.json -- python3 Research/Experiments/TEXTURE-001/probe.py --out Evidence/TEXTURE-local-measurements
```

The driver currently locates the Mac/single-configuration executable layout. Windows reproduction needs explicit executable/layout support before use; these commands are not Windows proof. Generated PNG/KTX inputs/outputs go to ignored `out/texture-research`. Stop if source verification fails; never overwrite a changed dependency cache.

The probe generates a 32² alternating black/white image and opposing-X normal image with Python's standard library. It uses C++ core bimg parsing/decoding, prints every mip's channel means/first pixel, and converts one manifest-selected 1254² height source. It tests repeated BC7 output and invalid input rejection. It computes full-chain memory estimates for the collection. It is not a production parser, general quality metric or adversarial-file test harness.

## Durable evidence

- [Configure](../../../Evidence/TEXTURE-001-configure/result.json) and [build](../../../Evidence/TEXTURE-001-build/result.json).
- [Source archive/tree verification](../../../Evidence/TEXTURE-001-sources.json): all six exact pinned trees verified after building.
- [Final run receipt](../../../Evidence/TEXTURE-001-final-run/result.json), [measurements](../../../Evidence/TEXTURE-001-final-measurements/measurements.json) and [exact commands/output](../../../Evidence/TEXTURE-001-final-measurements/commands.json).
- [Initial KTX2 attempt](../../../Evidence/TEXTURE-001-run/result.json): retained failed receipt that led to inspecting dispatch. The final experiment measures this rejection explicitly and uses KTX 1 for semantic comparisons.
- [Intermediate KTX run](../../../Evidence/TEXTURE-001-run-ktx/result.json): predates adding explicit color-pass fields; final receipt identifies current probe source.

No source images were edited. No GPU format support, material orientation, artistic quality, target-PC performance or cross-platform bit identity follows from these results. Upstream build warnings remain in the logs. The tools include bundled codec dependencies; a complete transitive distribution audit is still outside this experiment.
