# Preparation Tools

Run with Python 3.9 or newer from `Engine/`. These tools use only the standard library. They do not install tools or launch Unity. `verify_foundation.py` launches our native application for bounded technical verification; see [the run instructions](../Docs/RUN_FOUNDATION.md).

```sh
python3 Tools/doctor.py
python3 Tools/check_workspace.py
python3 Tools/check_engine_plan.py
python3 Tools/prepare_probe.py --fetch
python3 -m unittest discover -s Tools/tests -v
python3 Tools/record.py --out Evidence/my-check --input Docs/DIRECTION.md -- python3 Tools/check_workspace.py
```

On Windows use `python` if that is the installed command. Use a Visual Studio Developer Command Prompt when checking MSVC.

- `doctor.py` reports availability, not build readiness. `--output Evidence/environment.json` explicitly saves the inventory inside this folder.
- `check_workspace.py` checks links, JSON syntax, obvious Unity code references and tracked generated output. External documentation links are reported as references. It does not prove the complete runtime dependency graph, Internet-link availability or architecture correctness.
- `check_engine_plan.py` checks the [master plan](../Docs/FOUNDATION_PLAN.md) against its derived package index and system-audit capability rows. It detects dependency cycles, missing coverage/prerequisites, unsupported completion claims without evidence references and removed Windows gates, then reports ready local/Windows work. Evidence-file presence is not validation of runtime behavior or creative acceptance. Focused tests: `python3 -m unittest discover -s Tools/tests -p test_engine_plan.py -v`.
- `record.py` runs an explicit argument list with no shell interpretation. Use `--input` for the exact source/lock files a result depends on. It records nonzero exit results, timeouts and launch failures, and refuses to overwrite a previous evidence directory. A changed input makes the invocation unsuccessful even if the process exits zero.
- `package_workbench.py --build build/foundation --out out/my-package` installs a new local technical package and records payload/source hashes; it does not build, test or publish it.
- `verify_foundation.py` also measures the linear/HDR/display path and UI boundary. `--portable` launches from an unrelated Engine-local working directory using executable-relative shaders.
- `prepare_probe.py` verifies pinned source archives and extracted content from both the preserved experiment lock and the runtime extension lock. `--fetch` permits missing archive downloads into the ignored cache; it never installs globally or overwrites a modified source tree. It rejects archive traversal and link members.
- `verify_surface_assets.py` checks the [surface library](../Assets/SurfaceLibrary/README.md): copied-image hashes, PNG chunks/PSD headers, inventory and material texture references. Optional `--sources` also compares the preserved Unity source hashes; default verification needs only `Engine/`. Both modes are read-only and do not establish rendered material parity.
- Evidence output and CWD are confined to `Engine/`; the command itself is not sandboxed. Choose trusted commands and avoid passing credentials or commands that print secrets. On POSIX, timeout kills the process group; on Windows it kills the immediate process, so do not use it as a Windows build-tree cancellation manager.

Only promote logs worth retaining into tracked `Evidence/`. Use `.cache/` for scratch/test receipts. Evidence never substitutes for reading the result and stating its proof limits.
