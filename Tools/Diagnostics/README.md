# Greater Wasteland audit tools

These source files are inert outside `Assets` and are not imported into the production Unity project. They support the [October 11 performance audit](../../Docs/Evidence/Performance/GREATER_WASTELAND_AUDIT_2026-10-11.md). No package or Editor version change is needed.

## Measurement modes

`GreaterWastelandFrameAudit.Start()` samples the running Editor loop. In a headless Editor this is simulation evidence only: there may be no camera drawing. Never present its throughput as gameplay FPS.

`StartOffscreen()` renders the main game camera through URP into a 1158 × 495 texture and samples six conditions: normal, distant heightmap drawing off, distant Terrain components off, normal repeated, half render scale and shadows off. Initial renderer warmup precedes five seconds of settling and fifteen seconds of sampling per condition. Raw output is `Logs/greater-wasteland-frame-audit.json` relative to the candidate. Copy it before another run overwrites it.

The rendering result measures that offscreen Editor path, including render-request overhead. It excludes presentation and cannot establish standalone Player FPS. Missing/zero GPU timings and counters must be labelled unavailable. Time markers are in nanoseconds; allocation counters are bytes and rendering counters are counts. Markers can be nested, so do not add their inclusive times together.

## Isolation and restoration

Both entry points reject projects outside a `BooterBigArmValidation/PerformanceAudit...` candidate and require running, unpaused Greater Wasteland with its main camera and URP. A SessionState guard prevents duplicate captures. No terrain data, collider, GameObject active state, scene save or player input is changed. Distant rendering uses X/Z distance from terrain bounds with a radius of at least 1,000 metres or camera far clip plus 512 metres.

Each phase restores terrain enabled/drawing states and the previous quality pipeline. Graphics changes use a transient pipeline clone. Completion, Play Mode exit, cancellation and assembly reload dispose recorders/temporary textures and restore the previous background-execution setting. A result with zero samples is rejected. `Cancel()` supports controller cancellation; an external process kill cannot run managed cleanup, which is another reason to use only disposable candidates.

The renderer-off experiment is diagnostic. `FixedTerrainSurfaceIndex` currently rejects disabled Terrain components, so this flag cannot be shipped as a production optimization without updating and validating that collision-query contract.

## Reproduction prerequisites

The desktop memory-exhaustion alert recorded during the audit means further heavy rendering captures are currently deferred. The accepted measurements are already retained. Before any later rendering run, establish a bounded resource budget and verify that no other rendering Unity session is running. Do not focus or raise Unity. Do not reuse another task's validation directory or overwrite its files.

Create a fresh task-owned candidate containing matching `Assets`, `Packages` and `ProjectSettings`; preserve source hashes and current dirty-work ownership. Any copied Library is an isolated cache, not current import proof. Copy these two `.cs` files into that candidate's existing asmdef-isolated Editor folder, leaving the production Assets tree unchanged. Let the candidate import its own metadata. Create a candidate `Logs` folder.

`IsolatedFrameAuditLaunch.Launch` is the batchmode `-executeMethod` entry point. Use the installed pinned Unity executable with the explicit candidate `-projectPath`, an ignored `-logFile` and `Start-Process -WindowStyle Hidden`. It enters Play Mode only in that candidate and queues background player updates without moving actors or supplying device input. Its default is simulation capture; use `-nographics` for that mode. Rendering requires the additional explicit `-auditRender` argument and must omit `-nographics`. Do not add `-quit`: the controller exits after capture and restoration.

Controller exit codes are 0 for completed/restored capture, 1 for a capture error, and 2 for a timeout. Its watchdog starts after renderer warmup; it polls the report at most twice per second to reduce measurement overhead. A reload after measurement starts exits with an error instead of restarting hot-reloaded gameplay. Keep other eval, import, tests and builds out of the measurement window. An interrupted or partial output is not accepted evidence.

## Compile without running Unity

The final two helpers were checked with the pinned Editor's `Data/NetCoreRuntime/dotnet.exe` and `Data/DotNetSdkRoslyn/csc.dll`. The response file selected reference, define, language, unsafe and framework options from the existing `Library/Bee/artifacts/1900b0aE.dag/BooterBigArm.Editor.rsp`, then compiled only these two source files to an ignored `Logs` DLL. Exit code was 0, with no compiler diagnostics. This checks source compatibility without opening the scene, rendering or accepting gameplay behavior.

The original executed capture precedes the final isolation, duplicate-run, cancellation and zero-sample guards. Those closeout guards were compiled offline; no further heavy Unity run was performed after the crash alert.

## Procedural chunk reference study

`chunk_population_reference_study.py` is a standalone Python standard-library experiment for the [chunk research](../../Docs/Research/Engineering/GREATER_WASTELAND_CHUNK_EXPERIMENTS_2026-10-11.md). It uses synthetic X/Z candidates and a neighbor-priority rule; it never reads terrain assets, starts Unity, renders, runs native physics or accesses production save files. Its six checks cover evaluation order, ownership/IDs, cross-border spacing, a protected-site mask, fresh generation in different work partitions, and a serialized sparse depletion after regeneration. This is not a production generator or Unity performance test.

From the checkout root, run `.venv/Scripts/python.exe Tools/Diagnostics/chunk_population_reference_study.py --output Logs/chunk-population-reference.json`. Output is restricted to a JSON file under the current checkout's ignored Logs directory. The retained example covers a 1,024 metre square, including negative coordinates, with 256 metre identity cells and 64/128/256 metre working partitions. Pairwise spacing verification is intentionally small; it is not suitable as a whole-world runtime algorithm.
