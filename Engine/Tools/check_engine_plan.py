"""Check the master plan's dependency/coverage index; this is not runtime proof."""
import json
import re
from pathlib import Path
from common import ROOT


def inspect_plan(data, root, master, audit):
    errors = []
    if data.get("schema_version") != 1 or data.get("plan_revision") != 2:
        errors.append("Unsupported roadmap schema or plan revision")
    rows = data.get("work_packages", [])
    ids = [row.get("id") for row in rows]
    if len(ids) != len(set(ids)):
        errors.append("Duplicate work-package ID")
    indexed = {row["id"]: row for row in rows if isinstance(row.get("id"), str)}
    documented = re.findall(r"^\| ((?:P|W)\d{2}) \|", master, re.M)
    if sorted(ids) != sorted(documented):
        errors.append("Master-plan package rows differ from roadmap index")
    capabilities = set(data.get("capability_ids", []))
    audited = set(re.findall(r"^\| (CAP-\d{2}) \|", audit, re.M))
    if capabilities != audited:
        errors.append("Audited capabilities differ from roadmap index")
    covered = set()
    for row in rows:
        identity = row.get("id", "<missing>")
        for field in ("title", "owner_module", "proof"):
            if not isinstance(row.get(field), str) or not row[field].strip():
                errors.append(f"{identity}: missing {field}")
        if row.get("milestone") not in data.get("milestones", []):
            errors.append(f"{identity}: unknown milestone")
        if row.get("state") not in {"planned", "in_progress", "blocked", "complete"}:
            errors.append(f"{identity}: invalid state")
        for dependency in row.get("depends_on", []):
            if dependency not in indexed:
                errors.append(f"{identity}: unknown prerequisite {dependency}")
        for capability in row.get("capabilities", []):
            covered.add(capability)
            if capability not in capabilities:
                errors.append(f"{identity}: unknown capability {capability}")
        if row.get("state") == "complete":
            if not row.get("evidence"):
                errors.append(f"{identity}: completed without evidence references")
            for reference in row.get("evidence", []):
                path = (root / reference).resolve()
                if root not in path.parents or not path.is_file():
                    errors.append(f"{identity}: missing or external evidence {reference}")
            if any(indexed.get(dep, {}).get("state") != "complete" for dep in row.get("depends_on", [])):
                errors.append(f"{identity}: completed before prerequisites")
    if covered != capabilities:
        errors.append("Uncovered capabilities: " + ", ".join(sorted(capabilities - covered)))
    visiting, visited = set(), set()

    def visit(identity):
        if identity in visiting:
            errors.append(f"Dependency cycle at {identity}")
            return
        if identity in visited or identity not in indexed:
            return
        visiting.add(identity)
        for dependency in indexed[identity].get("depends_on", []):
            visit(dependency)
        visiting.remove(identity)
        visited.add(identity)

    for identity in indexed:
        visit(identity)

    def ancestors(identity, seen=None):
        seen = set() if seen is None else seen
        for dependency in indexed.get(identity, {}).get("depends_on", []):
            if dependency not in seen:
                seen.add(dependency)
                ancestors(dependency, seen)
        return seen

    for identity, required in {"P34": "W02", "P37": "W03"}.items():
        if required not in ancestors(identity):
            errors.append(f"{identity}: missing required Windows gate {required}")
    for identity in ("W01", "W02", "W03"):
        if indexed.get(identity, {}).get("environment") != "windows":
            errors.append(f"{identity}: Windows gate missing or mislabeled")
    ready = [row["id"] for row in rows if row.get("state") == "planned"
             and all(indexed.get(dep, {}).get("state") == "complete" for dep in row.get("depends_on", []))]
    if errors:
        ready = []
    return {"schema_version": 1, "errors": errors, "packages": len(rows),
            "capabilities": len(capabilities),
            "ready_local": [identity for identity in ready if indexed[identity]["environment"] != "windows"],
            "ready_windows": [identity for identity in ready if indexed[identity]["environment"] == "windows"],
            "proof_limit": "Plan graph, coverage and evidence-file presence only; not implementation, creative acceptance or runtime verification."}


def main():
    try:
        data = json.loads((ROOT / "Docs/ENGINE_ROADMAP.json").read_text())
        result = inspect_plan(data, ROOT.resolve(), (ROOT / "Docs/FOUNDATION_PLAN.md").read_text(),
                              (ROOT / "Research/ENGINE_SYSTEM_AUDIT.md").read_text())
    except (OSError, ValueError, KeyError, TypeError) as error:
        print(json.dumps({"errors": [str(error)]}, indent=2))
        return 1
    print(json.dumps(result, indent=2))
    return int(bool(result["errors"]))


if __name__ == "__main__":
    raise SystemExit(main())
