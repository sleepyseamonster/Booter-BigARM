# Verify and Close Out a Change

Run only checks relevant to the changed surface. Documentation/routing changes use `python3 ../../Tools/check_workspace.py`; roadmap changes also use `python3 ../../Tools/check_engine_plan.py`. Runtime changes use the applicable native build and focused tests. Do not launch Unity or perform user-owned gameplay smoke testing unless explicitly requested.

Then inspect the final diff and confirm:

- no unrelated Unity or engine changes were staged;
- generated output stayed in ignored locations;
- links and required files resolve;
- evidence identifies platform/backend, workload and proof limits;
- status, roadmap or decision records were updated only when the implementation or evidence changed.

Commit only verified task-owned work when authorized by the repository workflow. Do not push or publish.
