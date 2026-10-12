"""Read-only terrain storage classification; writes inventories only to --output.

No Unity launch, asset move, LFS conversion, staging, upload or source deletion.
Git blob identities pin tracked bytes; declared SHA256 values are not fresh proof.
"""
import argparse
import collections
import csv
import datetime
import gzip
import hashlib
import io
import json
import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
TERRAIN = 'Assets/_Project/Art/Terrain/'
SCENE = 'Assets/_Project/Scenes/Production/GreaterWasteland.unity'
FINAL = TERRAIN + 'NorthRow4E520400N4026680_2026-10-11/Source/manifest.json'
GUID = re.compile(rb'guid: ([0-9a-f]{32})')
RULES = {
    'GIT_RUNTIME_TEXT': 'Keep native text assets and metadata in ordinary Git.',
    'GIT_REFERENCE_SCENE': 'Keep active dressing/performance reference scenes and metadata in ordinary Git.',
    'LFS_RUNTIME_BINARY': 'Keep at the identical Unity path; candidate for exact-path LFS conversion.',
    'GIT_EDITOR_CONTRACT': 'Keep source manifests and active editor map contracts in ordinary Git.',
    'LFS_EDITOR_MAP': 'Keep active editor map payloads accessible; candidate for exact-path LFS.',
    'LFS_EDITOR_CONTRACT': 'Keep large sealed manifests at the identical path; candidate for exact-path LFS.',
    'ARCHIVE_GATED_SOURCE': 'Archive candidate only after validator/rebuild restore contract passes.',
    'ARCHIVE_GATED_SCENE': 'Retained candidate scene; archive only with builder/test path compatibility.',
    'GIT_EVIDENCE': 'Keep canonical notes and small provenance/verification receipts in Git.',
    'ARCHIVE_GATED_EVIDENCE': 'Bulk evidence; archive after receipt links and consumers resolve.',
    'LOCAL_ONLY': 'Preserve locally; no Git, LFS or cloud publication.',
    'REVIEW_KEEP': 'Keep in current location until additional dependencies are resolved.',
}

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def rel(p):
    return p.relative_to(ROOT).as_posix()

def is_scoped(p):
    return p.startswith((TERRAIN, 'SourceData/Terrain/', 'SourceArt/Blender/Studies/DeathValley/',
                         'Docs/DeathValley/', 'Docs/Evidence/DeathValley/', 'Docs/Evidence/Badwater/',
                         'wetransfer_death-valley-terrain-data_2026-10-07_0218/'))

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if output == ROOT or output.is_relative_to(ROOT / 'Assets'):
        raise ValueError('Inventory output must not be the root or an imported Assets folder')
    output.mkdir(parents=True, exist_ok=True)
    head = git('rev-parse', 'HEAD').decode().strip()
    index = {}
    for entry in git('ls-tree', '-rlz', head).split(b'\0'):
        if not entry:
            continue
        info, name = entry.split(b'\t', 1)
        mode, kind, oid, size = info.split()
        if kind == b'blob':
            index[name.decode()] = (oid.decode(), int(size))
    print('Committed tree indexed', len(index), flush=True)
    paths = {p for p in index if is_scoped(p)}
    disk_errors = []
    for folder in ['SourceData/Terrain', 'SourceArt/Blender/Studies/DeathValley',
                   'wetransfer_death-valley-terrain-data_2026-10-07_0218']:
        if (ROOT / folder).exists():
            paths.update(rel(p) for p in (ROOT / folder).rglob('*') if p.is_file())
    paths.update(p for p in index if p == TERRAIN.rstrip('/') + '.meta')

    guid_paths = {}
    duplicates = []
    terrain_guids = {}
    for p in sorted(index):
        if not p.startswith('Assets/') or not p.endswith('.meta'):
            continue
        try:
            raw = (ROOT / p).read_bytes()
        except OSError as e:
            disk_errors.append(str(e))
            continue
        match = GUID.search(raw)
        if not match:
            continue
        g = match[1].decode()
        if g in guid_paths and guid_paths[g] != p[:-5]:
            duplicates.append([g, guid_paths[g], p[:-5]])
        guid_paths[g] = p[:-5]
        if p.startswith(TERRAIN):
            terrain_guids[p[:-5]] = g
    print('Metadata indexed', len(terrain_guids), flush=True)
    asset_guids = {name: guid for guid, name in guid_paths.items()}

    guid_consumers = collections.defaultdict(set)
    production_guids = set()
    text_scanned = []
    binary_unparsed = []
    candidate_scenes = []
    for p in sorted(index):
        if not p.startswith(('Assets/', 'ProjectSettings/')) or pathlib.PurePosixPath(p).suffix not in {
                '.unity', '.prefab', '.mat', '.asset', '.terrainlayer', '.meta', '.controller', '.overrideController', '.playable'}:
            continue
        if p.endswith('.meta'):
            continue
        with (ROOT / p).open('rb') as f:
            first = f.read(80)
            if not first.startswith(b'%YAML'):
                if p.endswith('.asset'):
                    binary_unparsed.append(p)
                continue
            data = first + f.read()
        text_scanned.append(p)
        refs = {v.decode() for v in GUID.findall(data)}
        if p == SCENE:
            production_guids = refs
        hit = False
        for g in refs:
            target = guid_paths.get(g, '')
            if target.startswith(TERRAIN):
                guid_consumers[target].add(p)
                hit = True
        if hit and p.endswith('.unity') and '/Scenes/Reference/' in p:
            candidate_scenes.append(p)
            paths.add(p)
            paths.add(p + '.meta')
    paths.update([SCENE, SCENE + '.meta'])
    print('Serialized text references scanned', len(text_scanned), flush=True)

    manifest_consumers = collections.defaultdict(set)
    protected_consumers = collections.defaultdict(set)
    expected_hashes = collections.defaultdict(set)
    tile_layers = {}
    manifests = []
    latest = None
    active_native_refs = set()
    active_new_snapshots = set()
    active_sources = set()
    for p in sorted(index):
        if not p.startswith(TERRAIN) or not p.endswith('/Source/manifest.json'):
            continue
        m = json.loads((ROOT / p).read_text(encoding='utf-8-sig'))
        owner = p.removesuffix('/Source/manifest.json')
        mid = len(manifests)
        manifests.append({'id': mid, 'path': p, 'family': m.get('family', ''), 'tile_count': m.get('tile_count'),
                          'protected_count': len(m.get('protected_files', [])), 'bytes': index[p][1]})
        for f in m.get('protected_files', []):
            protected_consumers[f['path']].add(mid)
            expected_hashes[f['path']].add(f['sha256'])
        for t in m.get('new_tiles', []) + m.get('retained_tiles', []):
            for key in ['data_path', 'layer_path']:
                if t.get(key):
                    manifest_consumers[t[key]].add(mid)
            if t.get('layer_path'):
                tile_layers[t['data_path']] = t['layer_path']
            if t.get('data_path'):
                for key, target in [('data_sha256', t['data_path']), ('meta_sha256', t['data_path']+'.meta')]:
                    if t.get(key):
                        expected_hashes[target].add(t[key])
            for key in ['render', 'height', 'color']:
                if t.get(key+'_file'):
                    target = owner + '/Source/' + t[key+'_file']
                    manifest_consumers[target].add(mid)
                    if t.get(key+'_sha256'):
                        expected_hashes[target].add(t[key+'_sha256'])
                    if p == FINAL and t in m.get('new_tiles', []):
                        active_sources.add(target)
            ref = t.get('native_reference')
            if ref and ref.get('path'):
                target = ref['path']
                manifest_consumers[target].add(mid)
                expected_hashes[target].add(ref['sha256'])
                if p == FINAL and t in m.get('retained_tiles', []):
                    active_native_refs.add(target)
                if p == FINAL and t in m.get('new_tiles', []):
                    active_new_snapshots.add(target)
        if m.get('predecessor_manifest'):
            manifest_consumers[m['predecessor_manifest']].add(mid)
        if p == FINAL:
            latest = m
    if latest is None:
        raise ValueError('Expected accepted fourth-row final manifest is absent')
    production_data = {guid_paths[g] for g in production_guids if g in guid_paths and '/TerrainData/' in guid_paths[g]}
    declared_data = {t['data_path'] for t in latest['new_tiles'] + latest['retained_tiles']}
    if production_data != declared_data or len(production_data) != 9216:
        raise ValueError('Production scene and accepted 9216 TerrainData paths disagree')
    print('Production scene matches', len(production_data), 'TerrainData assets', flush=True)

    map_payloads = set()
    map_descriptors = []
    for p in paths:
        if pathlib.PurePosixPath(p).name not in {'developer_map.json', 'coverage_catalog.json'}:
            continue
        map_descriptors.append(p)
        if p.endswith('developer_map.json'):
            m = json.loads((ROOT / p).read_text(encoding='utf-8-sig'))
            if m.get('grid_file'):
                map_payloads.add((pathlib.PurePosixPath(p).parent / m['grid_file']).as_posix())
            for t in m.get('textures', []):
                map_payloads.add((pathlib.PurePosixPath(p).parent / t['file']).as_posix())

    code_consumers = []
    for parent in ['Assets/_Project/Scripts', 'Assets/_Project/Tests', 'Tools']:
        for f in (ROOT / parent).rglob('*'):
            if f.suffix not in {'.cs', '.py', '.ps1', '.sh'} or '__pycache__' in f.parts or f.resolve() == pathlib.Path(__file__).resolve():
                continue
            text = f.read_text(encoding='utf-8-sig', errors='replace')
            if '/death_valley/' in rel(f) or any(s in text for s in ['Art/Terrain/', 'BadwaterNorthRow', 'Source/NativeSnapshot', 'developer_map.json', 'coverage_catalog.json']):
                code_consumers.append({'path': rel(f), 'git_blob': index.get(rel(f), ('',0))[0],
                                       'working_sha256': hashlib.sha256(f.read_bytes()).hexdigest(),
                                       'directory_hash_contract': 'VerifyProtectedFiles' in text or 'protected_files' in text})

    rows = []
    formats = collections.Counter()
    for p in sorted(paths):
        file = ROOT / p
        if not file.is_file():
            disk_errors.append('Missing scoped path: ' + p)
            continue
        size = file.stat().st_size
        oid, committed_size = index.get(p, ('', ''))
        suffix = file.suffix.lower()
        asset = p[:-5] if p.endswith('.meta') else p
        g = asset_guids.get(asset, '')
        rule = 'REVIEW_KEEP'
        role = 'unresolved'
        gate = 'dependency_review'
        if p.startswith(('SourceData/Terrain/', 'wetransfer_death-valley-terrain-data_2026-10-07_0218/')) or re.search(r'\.blend(?:\d+)?$', p, re.I):
            rule, role, gate = 'LOCAL_ONLY', 'local_authoring_source', 'local_backup_restore'
        elif p.startswith(TERRAIN) or p == TERRAIN.rstrip('/') + '.meta':
            if suffix == '.meta':
                rule, role, gate = 'GIT_RUNTIME_TEXT', 'unity_identity', 'metadata_pairing'
                if '/Source/' in asset and not (ROOT/asset).is_dir() and asset not in guid_consumers and asset not in map_payloads and not asset.endswith('/manifest.json') and pathlib.PurePosixPath(asset).suffix not in {'.json','.csv'}:
                    rule, role, gate = 'ARCHIVE_GATED_SOURCE', 'source_identity', 'source_restore_and_validation'
            elif '/TerrainData/' in p:
                with file.open('rb') as f:
                    fmt = 'yaml' if f.read(8).startswith(b'%YAML') else 'binary'
                formats[fmt] += 1
                rule = 'LFS_RUNTIME_BINARY' if fmt == 'binary' else 'GIT_RUNTIME_TEXT'
                role, gate = 'production_terrain_data' if p in production_data else 'retained_terrain_data', 'same_path_lfs_roundtrip'
            elif suffix in {'.terrainlayer', '.mat', '.prefab'}:
                rule, role, gate = 'GIT_RUNTIME_TEXT', 'terrain_rendering', 'metadata_pairing'
            elif asset in guid_consumers and suffix not in {'.json', '.csv'}:
                rule, role, gate = 'LFS_RUNTIME_BINARY', 'serialized_texture_or_payload', 'same_path_lfs_roundtrip'
            elif suffix in {'.json', '.csv'}:
                rule = 'LFS_EDITOR_CONTRACT' if suffix == '.json' and size >= 2*1024**2 else 'GIT_EDITOR_CONTRACT'
                role, gate = 'editor_provenance_contract', 'immutable_contract'
            elif '/Source/' in p:
                rule, role, gate = 'ARCHIVE_GATED_SOURCE', 'capture_or_rebuild_input', 'source_restore_and_validation'
            elif suffix == '.png':
                rule, role, gate = 'LFS_RUNTIME_BINARY', 'terrain_texture_conservative_keep', 'same_path_lfs_roundtrip'
        elif p == SCENE or p == SCENE + '.meta':
            rule, role, gate = 'GIT_RUNTIME_TEXT', 'production_scene', 'immutable_scene'
        elif p in candidate_scenes or p.removesuffix('.meta') in candidate_scenes:
            if asset.endswith('Candidate.unity'):
                rule, role, gate = 'ARCHIVE_GATED_SCENE', 'historical_candidate_scene', 'builder_test_restore'
            else:
                rule, role, gate = 'GIT_REFERENCE_SCENE', 'active_reference_scene', 'reference_scene_preservation'
        elif p in map_payloads:
            rule, role, gate = 'LFS_EDITOR_MAP', 'active_developer_map', 'map_restore'
        elif p in map_descriptors:
            rule, role, gate = 'GIT_EDITOR_CONTRACT', 'active_developer_map_contract', 'immutable_contract'
        elif p.startswith('Docs/'):
            rule = 'GIT_EVIDENCE' if suffix in {'.md','.json','.csv','.txt','.xml','.log','.py','.cs'} and size < 2*1024**2 else 'ARCHIVE_GATED_EVIDENCE'
            role, gate = 'terrain_receipt_or_evidence', 'receipt_resolution'
        elif p.startswith('SourceArt/'):
            rule, role, gate = 'ARCHIVE_GATED_EVIDENCE', 'retained_study_resource', 'relative_resource_restore'
        # A source file referred to by serialized GUID always stays available as a Unity asset.
        if rule == 'ARCHIVE_GATED_SOURCE' and asset in guid_consumers:
            rule, role, gate = 'GIT_RUNTIME_TEXT' if suffix == '.meta' else 'LFS_RUNTIME_BINARY', 'serialized_source_dependency', 'same_path_lfs_roundtrip'
        rows.append([p, size, oid, committed_size, g, rule, role, gate,
                     int(asset in production_data), int(asset in active_native_refs or asset in active_new_snapshots), int(asset in active_sources),
                     ','.join(str(n) for n in sorted(manifest_consumers.get(asset, set()))),
                     ','.join(str(n) for n in sorted(protected_consumers.get(p, set()))),
                     '|'.join(sorted(guid_consumers.get(asset, set())))])

    fields = ['path','working_bytes','git_blob','committed_bytes','unity_guid','storage_class','role','gate',
              'production_data','latest_native_reference','latest_new_source','manifest_ids','protected_manifest_ids',
              'serialized_text_consumers']
    stream = io.StringIO(newline='')
    writer = csv.writer(stream, delimiter='\t', lineterminator='\n')
    writer.writerow(fields)
    writer.writerows(rows)
    payload = stream.getvalue().encode('utf-8')
    compressed = gzip.compress(payload, compresslevel=9, mtime=0)
    inventory = output / 'terrain-files.tsv.gz'
    inventory.write_bytes(compressed)
    # The plain view is a local diagnostic, not an additional publication payload.
    local = ROOT / 'Logs/terrain-storage-classification.tsv'
    local.parent.mkdir(exist_ok=True)
    local.write_bytes(payload)
    class_stats = collections.defaultdict(lambda: {'files':0,'bytes':0,'tracked_files':0,'tracked_bytes':0})
    for row in rows:
        c = class_stats[row[5]]
        c['files'] += 1
        c['bytes'] += row[1]
        if row[2]:
            c['tracked_files'] += 1
            c['tracked_bytes'] += int(row[3])
    source_usage = collections.defaultdict(lambda: {'files':0,'bytes':0})
    for row in rows:
        if row[5] == 'ARCHIVE_GATED_SOURCE' and not row[0].endswith('.meta'):
            key = 'latest_direct' if row[9] or row[10] else 'protected_or_historical'
            source_usage[key]['files'] += 1
            source_usage[key]['bytes'] += row[1]
    missing_manifest_refs = sorted(p for p in manifest_consumers if p.startswith(TERRAIN) and p not in index)
    summary = {
        'schema_version':1, 'captured_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'head_start':head,'head_end':git('rev-parse','HEAD').decode().strip(),
        'inventory':{'file':inventory.name,'sha256':hashlib.sha256(compressed).hexdigest(),
                     'uncompressed_sha256':hashlib.sha256(payload).hexdigest(),'rows':len(rows),'columns':fields,
                     'compressed_bytes':len(compressed),'uncompressed_bytes':len(payload)},
        'scope':'Active terrain assets, production/terrain-reference scenes, local terrain authoring sources, terrain docs/evidence and retained transfer inputs; excludes standalone historical projects and external backups.',
        'classification_intent':'Proposed storage classes; currently tracked files remain ordinary Git. No imported asset is cleared for removal by this static audit.',
        'rules':RULES,'classes':dict(class_stats),'archive_source_usage':dict(source_usage),
        'manifests':manifests,'code_consumers':code_consumers,'map_descriptors':map_descriptors,'map_payloads':sorted(map_payloads),
        'production':{'scene':SCENE,'scene_sha256':hashlib.sha256((ROOT/SCENE).read_bytes()).hexdigest(),
                      'manifest':FINAL,'manifest_sha256':hashlib.sha256((ROOT/FINAL).read_bytes()).hexdigest(),
                      'terrain_data_count':len(production_data),'manifest_data_set_matches_scene':production_data==declared_data,
                      'latest_retained_native_sources':len(active_native_refs),'latest_new_source_files':len(active_sources),
                      'latest_new_native_snapshots':len(active_new_snapshots),
                      'terrain_data_formats':dict(formats),'reference_scenes':candidate_scenes},
        'duplicate_guids':duplicates,'disk_errors':disk_errors,'missing_manifest_references':missing_manifest_refs,
        'unparsed_binary_asset_count':len(binary_unparsed),
        'declared_sha256_locations':'Existing sealed source manifests and their protected_files entries; not duplicated in the inventory.',
        'text_scanned_count':len(text_scanned),
        'limitations':['GUID scan covers YAML serialized text; binary external-reference graph is not parsed.',
                       'TerrainData/layers/render textures conservatively kept; fresh Unity dependency proof is a migration gate.',
                       'Declared SHA256 values come from sealed manifests, not a fresh full payload rehash.',
                       'Whole-owner protection means ARCHIVE_GATED files are not safe to remove now.',
                       'Git blob IDs describe committed bytes; local-only files have no fresh content hashes.',
                       'This is a non-atomic disk inventory; start/end HEAD and errors are recorded.']}
    (output/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
    assert len({row[0] for row in rows}) == len(rows)
    assert gzip.decompress(compressed) == payload
    assert not any(row[5].startswith('LFS_') and row[0].endswith('.meta') for row in rows)
    assert not any(row[5] == 'ARCHIVE_GATED_SOURCE' and row[13] for row in rows)
    assert not any(row[5] == 'LOCAL_ONLY' and row[2] for row in rows)
    assert not any(row[5] == 'REVIEW_KEEP' for row in rows), 'Unclassified files need a keep decision'
    assert summary['head_start'] == summary['head_end'], 'HEAD changed during classification'
    assert not disk_errors and not duplicates and not missing_manifest_refs
    scoped_changes = git('diff','--name-only',head,'--',TERRAIN,SCENE,'SourceArt/Blender/Studies/DeathValley/',
                         'SourceData/Terrain/','Docs/DeathValley/','Docs/Evidence/DeathValley/','Docs/Evidence/Badwater/').decode().splitlines()
    assert not scoped_changes, 'Scoped tracked files changed during classification: ' + str(scoped_changes)
    print(json.dumps({'head_stable':summary['head_start']==summary['head_end'], 'classes':dict(class_stats),
                      'rows':len(rows),'inventory_compressed_bytes':len(compressed),
                      'disk_errors':len(disk_errors),'duplicate_guids':len(duplicates),
                      'missing_manifest_references':len(missing_manifest_refs)},indent=2),flush=True)

if __name__ == '__main__':
    main()
