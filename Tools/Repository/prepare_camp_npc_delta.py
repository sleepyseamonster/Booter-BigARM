"""Prepare an additive NPC scene payload without writing the author scene.

All existing serialized records except the root list remain byte-identical. New
local IDs are remapped so concurrent additions cannot collide with candidate IDs.
The applying process must exclusively lock the scene and compare source_sha256.
"""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)[^\r\n]*", re.M)
LOCAL = re.compile(r"\{fileID: (-?\d+)\}")
SCENE = Path("Assets/_Project/Scenes/Reference/CampLoopTesting.unity")


def documents(text):
    matches = list(HEADER.finditer(text))
    result = {}
    for i, match in enumerate(matches):
        identity = match.group(2)
        if identity in result:
            raise ValueError("Duplicate scene document ID")
        result[identity] = (int(match.group(1)), text[match.start(): matches[i + 1].start() if i + 1 < len(matches) else len(text)])
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candidate", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--refresh-candidate", action="store_true", help="Copy current source/dependency closure into the isolated candidate, then adopt the prepared scene there only")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    candidate = Path(args.candidate).resolve()
    if candidate.parent != Path("D:/BooterBigArmValidation").resolve():
        raise ValueError("Candidate must be a dedicated validation child")
    raw = (root / SCENE).read_bytes()
    source = raw.decode("utf-8")
    target = (candidate / SCENE).read_bytes().decode("utf-8")
    current = documents(source)
    authored = documents(target)
    prefab_meta = candidate / "Assets/_Project/Prefabs/TopDown3D/NPC/LagerSkittish.prefab.meta"
    guid = re.search(r"^guid: (\w+)$", prefab_meta.read_text(), re.M).group(1)
    seeds = [key for key, (kind, text) in authored.items() if kind == 1001 and re.search(r"m_SourcePrefab:.*guid: " + guid, text)]
    if len(seeds) != 1 or any("guid: " + guid in text for _, text in current.values()):
        raise ValueError("Expected one candidate NPC and none in author scene")
    seed = seeds[0]
    owned = {seed}
    for key, (_, text) in authored.items():
        if re.search(r"m_PrefabInstance: \{fileID: " + seed + r"\}", text):
            owned.add(key)
    owned_go = {key for key in owned if authored[key][0] == 1}
    for key, (_, text) in authored.items():
        if any(re.search(r"m_GameObject: \{fileID: " + go + r"\}", text) for go in owned_go):
            owned.add(key)
    external = set()
    for key in owned:
        external.update(LOCAL.findall(authored[key][1]))
    external -= owned | {"0"}
    if not external.issubset(current):
        raise ValueError("Current scene lacks candidate external bindings: " + str(external - current.keys()))
    roots = [(key, text) for key, (_, text) in current.items() if text.splitlines()[1] == "SceneRoots:"]
    candidate_roots = [text for _, text in authored.values() if text.splitlines()[1] == "SceneRoots:"]
    if len(roots) != 1 or len(candidate_roots) != 1:
        raise ValueError("Ambiguous scene roots")
    added_roots = set(LOCAL.findall(candidate_roots[0])) & owned
    # Unity 6 root lists can reference the PrefabInstance directly, not a stripped Transform.
    if len(added_roots) != 1 or authored[next(iter(added_roots))][0] not in (4, 1001):
        raise ValueError("NPC requires exactly one scene root")
    remap = {}
    identity = 9001410000000
    for key in sorted(owned, key=int):
        while str(identity) in current or str(identity) in remap.values():
            identity += 1
        remap[key] = str(identity)
        identity += 1
    newline = "\r\n" if "\r\n" in source else "\n"
    records = []
    for key in sorted(owned, key=int):
        record = authored[key][1]
        record = re.sub(r"^(--- !u!\d+ &)-?\d+", lambda m: m.group(1) + remap[key], record, count=1)
        record = LOCAL.sub(lambda m: "{fileID: " + remap.get(m.group(1), m.group(1)) + "}", record)
        # Empty Unity fields remain empty while avoiding whitespace-only diff noise.
        record = "\n".join(line.rstrip(" \t") for line in record.splitlines()) + "\n"
        records.append(record.replace("\n", newline))
    root_id, root_record = roots[0]
    amended_roots = root_record + ("" if root_record.endswith("\n") else newline) + "  - {fileID: " + remap[next(iter(added_roots))] + "}" + newline
    result = source.replace(root_record, "".join(records) + amended_roots, 1)
    verified = documents(result)
    for key, value in current.items():
        if key != root_id and verified[key] != value:
            raise ValueError("Existing serialized record changed: " + key)
    if len(verified) != len(current) + len(owned):
        raise ValueError("Unexpected added record count")
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    payload = result.encode("utf-8")
    (output / "CampLoopTesting.npc-candidate.unity").write_bytes(payload)
    receipt = {"source_sha256": hashlib.sha256(raw).hexdigest(), "result_sha256": hashlib.sha256(payload).hexdigest(),
               "added_documents": len(owned), "preserved_documents": len(current) - 1, "remapped_ids": remap,
               "existing_external_bindings": sorted(external), "prefab_guid": guid,
               "scope": "Only new NPC records and one appended root entry; all other author-scene documents preserved byte-for-byte."}
    (output / "scene-delta.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.refresh_candidate:
        # A frozen current-working-source proof must not rely on an old validation copy's scripts.
        guid_map = {}
        for meta in (root / "Assets").rglob("*.meta"):
            match = re.search(rb"(?m)^guid: ([a-f0-9]{32})", meta.read_bytes())
            if match:
                guid_map[match.group(1).decode()] = meta.with_suffix("")
        paths = set()
        for folder in (root / "Assets/_Project/Scripts", root / "Assets/_Project/Tests"):
            paths.update(p for p in folder.rglob("*") if p.is_file())
        queue = [root / "Assets/_Project/Settings/NPC/LagerDefinition.asset"]
        queue += [guid_map[g] for g in re.findall(r"guid: ([a-f0-9]{32})", result) if g in guid_map]
        seen = set()
        while queue:
            asset = queue.pop()
            if asset in seen or not asset.is_file():
                continue
            if asset.suffix.lower() == ".blend":
                raise ValueError("Excluded Blender source is not a runtime dependency")
            seen.add(asset)
            paths.add(asset)
            meta = Path(str(asset) + ".meta")
            if meta.is_file():
                paths.add(meta)
            for serial in (asset, meta):
                if serial.is_file() and serial.suffix.lower() in {".meta", ".asset", ".prefab", ".mat", ".anim", ".controller", ".unity", ".shadergraph"}:
                    for guid_ref in re.findall(rb"guid: ([a-f0-9]{32})", serial.read_bytes()):
                        if guid_ref.decode() in guid_map:
                            queue.append(guid_map[guid_ref.decode()])
        manifest = []
        for asset in sorted(paths):
            relative = asset.relative_to(root)
            contents = asset.read_bytes()
            destination = candidate / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(contents)
            if asset.read_bytes() != contents:
                raise ValueError("Concurrent source write during capture: " + str(relative))
            manifest.append({"path": relative.as_posix(), "sha256": hashlib.sha256(contents).hexdigest()})
        if (root / SCENE).read_bytes() != raw:
            raise ValueError("Author scene changed during closure capture; prepare again")
        (candidate / SCENE).write_bytes(payload)
        (output / "current-source-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
        print("Current candidate closure refreshed:", len(manifest), "files; author scene not written.")
    print(json.dumps(receipt, indent=2))


if __name__ == "__main__":
    main()
