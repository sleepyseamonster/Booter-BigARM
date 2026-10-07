# Game Engine Agent Tools

The agent currently reuses the canonical tools in `Engine/Tools/` so there is one implementation and one evidence path. Start with:

```sh
cd Engine
python3 Tools/check_workspace.py
python3 Tools/check_engine_plan.py
python3 -m unittest discover -s Tools/tests -v
```

Agent-specific checks may be added here when they have a distinct responsibility. Do not clone canonical build, packaging or verification scripts merely to give them a new path.
