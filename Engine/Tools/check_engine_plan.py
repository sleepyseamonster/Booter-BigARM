"""Check the master plan's dependency/coverage index; this is not runtime proof."""
import json
import re
from pathlib import Path
from common import ROOT


def inspect_execution(execution, root, packages, errors):
    """Apply the engine-only correction sequence without erasing historical packages."""
    def read_reference(reference, text=True):
        path = (root / reference).resolve()
        if root not in path.parents or not path.is_file():
            errors.append(f"Execution: missing or external reference {reference}")
            return ""
        return path.read_text() if text else ""

    plan = read_reference(execution.get("plan", ""))
    findings = set()
    for reference in execution.get("audits", []):
        findings.update(re.findall(r"^### (A\d{2}) —", read_reference(reference), re.M))
    rows = execution.get("batches", [])
    ids = [row["id"] for row in rows]
    if not rows or len(ids) != len(set(ids)):
        errors.append("Execution: empty or duplicate batch IDs")
    if ids != re.findall(r"^### (R\d+) —", plan, re.M):
        errors.append("Execution: plan batch order differs from index")
    covered = {item for row in rows for item in row.get("findings", [])}
    if not findings or findings != covered:
        errors.append("Execution: audit finding coverage differs from batches")
    indexed = {row["id"]: row for row in rows}
    preceding = set()
    for row in rows:
        identity = row["id"]
        if row.get("state") not in {"planned", "in_progress", "blocked", "complete"}:
            errors.append(f"{identity}: invalid execution state")
        if row.get("environment") not in {"windows", "mac_or_platform_independent"}:
            errors.append(f"{identity}: invalid execution environment")
        if not row.get("title") or not row.get("proof"):
            errors.append(f"{identity}: missing execution title or proof")
        # The documented order is intentionally topological and serial.
        for dependency in row.get("depends_on", []):
            if dependency not in preceding:
                errors.append(f"{identity}: unknown or nonpreceding prerequisite {dependency}")
        preceding.add(identity)
        for gate in row.get("required_gates", []):
            if gate not in packages or packages[gate].get("environment") != "windows":
                errors.append(f"{identity}: unknown Windows gate {gate}")
        prerequisites_complete = all(indexed.get(dep, {}).get("state") == "complete"
                                     for dep in row.get("depends_on", []))
        gates_complete = all(packages.get(gate, {}).get("state") == "complete"
                             for gate in row.get("required_gates", []))
        if row.get("state") == "complete":
            if not prerequisites_complete or not gates_complete:
                errors.append(f"{identity}: completed before execution prerequisites/gates")
            if not row.get("evidence"):
                errors.append(f"{identity}: completed without execution evidence")
            for reference in row.get("evidence", []):
                read_reference(reference, text=False)
    if set(indexed.get("R6", {}).get("required_gates", [])) != {"W01", "W02"}:
        errors.append("R6: missing required Windows gates W01/W02")
    ready = [row for row in rows if row.get("state") == "planned"
             and all(indexed.get(dep, {}).get("state") == "complete" for dep in row.get("depends_on", []))
             and all(packages.get(gate, {}).get("state") == "complete" for gate in row.get("required_gates", []))]
    return {"plan": execution.get("plan"), "batches": len(rows), "findings": len(findings),
            "active_batches": [row["id"] for row in rows if row.get("state") == "in_progress"],
            "blocked_batches": [row["id"] for row in rows if row.get("state") == "blocked"],
            "scope_complete": bool(rows) and all(row.get("state") == "complete" for row in rows),
            "ready_local": [row["id"] for row in ready if row["environment"] != "windows"],
            "ready_windows": [row["id"] for row in ready if row["environment"] == "windows"]}


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
    execution = None
    if "active_execution" in data:
        execution = inspect_execution(data["active_execution"], root, indexed, errors)
    if errors:
        ready = []
        if execution:
            execution["ready_local"] = []
            execution["ready_windows"] = []
    local = [identity for identity in ready if indexed[identity]["environment"] != "windows"]
    windows = [identity for identity in ready if indexed[identity]["environment"] == "windows"]
    return {"schema_version": 1, "errors": errors, "packages": len(rows),
            "capabilities": len(capabilities),
            "ready_local": execution["ready_local"] if execution else local,
            "ready_windows": windows + (execution["ready_windows"] if execution else []),
            "deferred_roadmap_local": local if execution else [],
            "active_execution": execution,
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
