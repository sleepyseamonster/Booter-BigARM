"""Append one organized native section while preserving all predecessor scene documents."""
import argparse
from pathlib import Path
import re
from preserve_expansion_scene import split_documents

def preserve(original, current, parent_id):
    prefix, old = split_documents(original)
    _, new = split_documents(current)
    if set(old) - set(new) or parent_id not in old:
        raise ValueError("Existing scene document removed or parent absent")
    pattern = r"(?m)^  m_Children:\r?\n(?:  - \{fileID: \d+\}\r?\n)*"
    before, after = re.search(pattern, old[parent_id]), re.search(pattern, new[parent_id])
    if before is None or after is None:
        raise ValueError("Missing common-parent child list")
    previous = re.findall(r"fileID: (\d+)", before[0])
    now = re.findall(r"fileID: (\d+)", after[0])
    added = {k: v for k, v in new.items() if k not in old}
    if now[:len(previous)] != previous or len(now) != len(previous) + 1 or now[-1] not in added or len(added) != 1314:
        raise ValueError("Expected one section, sixteen sectors and 256 five-document tiles")
    if sum(v.startswith("--- !u!218 ") for v in added.values()) != 256 or sum(v.startswith("--- !u!154 ") for v in added.values()) != 256:
        raise ValueError("Incorrect added native terrain/collider documents")
    old[parent_id] = old[parent_id][:before.start()] + after[0] + old[parent_id][before.end():]
    return prefix + "".join(old.values()) + "".join(added.values())

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--before", type=Path, required=True)
    p.add_argument("--scene", type=Path, required=True)
    p.add_argument("--parent-transform-id", required=True)
    a = p.parse_args()
    a.scene.write_bytes(preserve(a.before.read_bytes().decode("utf-8"), a.scene.read_bytes().decode("utf-8"), a.parent_transform_id).encode("utf-8"))
