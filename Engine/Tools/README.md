# Preparation Tools

Run with Python 3.9 or newer from `Engine/`. These tools use only the standard library. They do not install tools or launch Unity.

```sh
python3 Tools/doctor.py
python3 Tools/check_workspace.py
python3 Tools/prepare_probe.py --fetch
python3 -m unittest discover -s Tools/tests -v
python3 Tools/record.py --out Evidence/my-check --input Docs/DIRECTION.md -- python3 Tools/check_workspace.py
```

On Windows use `python` if that is the installed command. Use a Visual Studio Developer Command Prompt when checking MSVC.

- `doctor.py` reports availability, not build readiness. `--output Evidence/environment.json` explicitly saves the inventory inside this folder.
- `check_workspace.py` checks links, JSON syntax, obvious Unity code references and tracked generated output. External documentation links are reported as references. It does not prove the complete runtime dependency graph, Internet-link availability or architecture correctness.
- `record.py` runs an explicit argument list with no shell interpretation. Use `--input` for the exact source/lock files a result depends on. It records nonzero exit results, timeouts and launch failures, and refuses to overwrite a previous evidence directory. A changed input makes the invocation unsuccessful even if the process exits zero.
- `prepare_probe.py` verifies pinned source archives and extracted content. `--fetch` permits missing archive downloads into the ignored cache; it never installs globally or overwrites a modified source tree. It rejects archive traversal and link members.
- Evidence output and CWD are confined to `Engine/`; the command itself is not sandboxed. Choose trusted commands and avoid passing credentials or commands that print secrets. On POSIX, timeout kills the process group; on Windows it kills the immediate process, so do not use it as a Windows build-tree cancellation manager.

Only promote logs worth retaining into tracked `Evidence/`. Use `.cache/` for scratch/test receipts. Evidence never substitutes for reading the result and stating its proof limits.
