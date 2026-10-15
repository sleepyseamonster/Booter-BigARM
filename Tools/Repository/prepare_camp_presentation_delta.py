"""Merge only the verified presentation migration's scene records/properties.

Input before/after are SaveScene(saveAsCopy:true) snapshots of the same author
scene. Target may be the current disk scene or HEAD for task-only staging.
Never installs the payload; emits a receipt and checks local reference closure.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)[^\r\n]*", re.M)
PROPERTY = re.compile(r"^  ([A-Za-z_][A-Za-z_0-9]*):", re.M)
LOCAL = re.compile(r"\{fileID: (-?\d+)\}")
ALLOWED = {1: {"m_Component"}, 4: {"m_Father", "m_Children", "m_LocalPosition", "m_LocalRotation", "m_LocalEulerAnglesHint"},
           224: {"m_Father", "m_Children", "m_LocalPosition", "m_LocalRotation", "m_LocalEulerAnglesHint"},
           1001: {"m_Modification"}, 114: {"environmentModules", "atmosphereProfile", "cameraOpticsProfile"}, 1660057539: {"m_Roots"}}


def normalized(value):
    return re.sub(r"(?<![\w.])-0(?=[,\s}])", "0", value) if value is not None else None


def documents(text):
    matches = list(HEADER.finditer(text))
    result = {}
    for i, match in enumerate(matches):
        key = match.group(2)
        if key in result:
            raise ValueError("Duplicate local ID")
        result[key] = (int(match.group(1)), text[match.start():matches[i + 1].start() if i + 1 < len(matches) else len(text)])
    return result


def properties(text):
    matches = list(PROPERTY.finditer(text))
    return {m.group(1): text[m.start():matches[i + 1].start() if i + 1 < len(matches) else len(text)] for i, m in enumerate(matches)}


def merge(before, after, target, index=False):
    old, new, current = documents(before), documents(after), documents(target)
    if old.keys() - new.keys():
        raise ValueError("Migration removed existing records")
    additions = new.keys() - old.keys()
    if additions & current.keys():
        raise ValueError("New local ID collides with target")
    changes = []
    available = current.keys() | additions
    for key in old:
        if old[key] == new[key]:
            continue
        a, b = properties(old[key][1]), properties(new[key][1])
        names = {name for name in a.keys() | b.keys() if normalized(a.get(name)) != normalized(b.get(name))}
        if names - ALLOWED.get(old[key][0], set()):
            raise ValueError(f"Unexpected property delta {key}: {names}")
        if key not in current:
            if index:
                continue  # User-only pre-existing object stays outside this commit.
            raise ValueError("Target lacks author record " + key)
        record = current[key][1]
        fields = properties(record)
        for name in names:
            value = b.get(name)
            if value is None:
                raise ValueError("Existing field removed")
            if name == "m_Modification":
                parent = re.compile(r"^    m_TransformParent:.*\n", re.M)
                previous_parent = parent.search(a[name])
                next_parent = parent.search(value)
                if previous_parent is None or next_parent is None or normalized(parent.sub("", a[name])) != normalized(parent.sub("", value)):
                    raise ValueError("Prefab override changed beyond parenting: " + key)
                if parent.search(fields[name]).group(0) != previous_parent.group(0):
                    raise ValueError("Concurrent prefab parenting conflict: " + key)
                value = parent.sub(lambda _: next_parent.group(0), fields[name], count=1)
            elif name in {"m_Component", "m_Children", "m_Roots"}:
                source_ids = set(LOCAL.findall(a.get(name, "")))
                next_ids = set(LOCAL.findall(value))
                removed, added = source_ids - next_ids, next_ids - source_ids
                lines = fields.get(name, value.splitlines()[0] + "\n").splitlines(keepends=True)
                lines = [line for line in lines if not (set(LOCAL.findall(line)) & removed)]
                retained = set(LOCAL.findall("".join(lines)))
                for line in value.splitlines(keepends=True)[1:]:
                    refs = set(LOCAL.findall(line))
                    if refs & added and not refs & retained and (not index or refs <= available):
                        lines.append(line)
                value = "".join(lines)
            elif name in fields and name in a and fields[name] != a[name]:
                raise ValueError(f"Concurrent owned-property conflict {key}.{name}")
            if name in fields:
                record = record.replace(fields[name], value, 1)
            else:
                record = record.rstrip("\n") + "\n" + value
            changes.append({"record": key, "property": name})
        current[key] = (current[key][0], record)
    appended = []
    for key in sorted(additions, key=int):
        kind, record = new[key]
        if index and kind == 4:
            for name, value in properties(record).items():
                if name == "m_Children":
                    filtered = "".join(line for line in value.splitlines(keepends=True) if not LOCAL.findall(line) or set(LOCAL.findall(line)) <= available)
                    if filtered.strip() == "m_Children:":
                        filtered = "  m_Children: []\n"
                    record = record.replace(value, filtered, 1)
        record = "\n".join(line.rstrip(" \t") for line in record.splitlines()) + "\n"
        appended.append(record)
    prefix = target[:HEADER.search(target).start()]
    result = prefix + "".join(item[1] for item in current.values()) + "".join(appended)
    parsed = documents(result)
    dangling = {ref for _, record in parsed.values() for ref in LOCAL.findall(record) if ref != "0" and ref not in parsed}
    original = documents(target)
    prior_dangling = {ref for _, record in original.values() for ref in LOCAL.findall(record) if ref != "0" and ref not in original}
    if dangling - prior_dangling:
        raise ValueError("Introduced dangling local references: " + str(dangling - prior_dangling))
    return result, {"added_records": len(additions), "modified_properties": changes, "preexisting_dangling": sorted(prior_dangling), "new_dangling": []}


def main():
    parser = argparse.ArgumentParser()
    for name in ["before", "after", "target", "output"]:
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--index", action="store_true")
    args = parser.parse_args()
    paths = {key: Path(getattr(args, key)) for key in ["before", "after", "target", "output"]}
    inputs = {key: paths[key].read_bytes() for key in ["before", "after", "target"]}
    result, receipt = merge(*(inputs[key].decode("utf-8").replace("\r\n", "\n") for key in ["before", "after", "target"]), index=args.index)
    newline = "\r\n" if b"\r\n" in inputs["target"] else "\n"
    output = result.replace("\n", newline).encode("utf-8")
    paths["output"].write_bytes(output)
    receipt.update({key + "_sha256": hashlib.sha256(value).hexdigest() for key, value in inputs.items()})
    receipt["output_sha256"] = hashlib.sha256(output).hexdigest()
    paths["output"].with_suffix(".receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps({"added_records": receipt["added_records"], "changed_properties": len(receipt["modified_properties"]), "new_dangling": receipt["new_dangling"]}))


if __name__ == "__main__":
    main()
