"""Verify Blender proof resources within the current expansion checkout."""
import hashlib
from pathlib import Path

def sha256(path):
    digest=hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024*1024),b""):
            digest.update(block)
    return digest.hexdigest()

def verify_blender_resources(proof, proof_directory, root):
    """Resolve archived paths against this checkout without trusting the old drive."""
    root=Path(root).resolve()
    parts=proof['blend_file'].replace('\\','/').split('/')
    prefix=['SourceArt','Blender','Studies','DeathValley','WestNorthNorthwest2026-10-07']
    starts=[i for i in range(len(parts)) if parts[i:i+len(prefix)]==prefix]
    if len(starts)!=1:
        raise ValueError('Blender source is outside the expansion source archive')
    relative=parts[starts[0]:]
    if any(p in ('','.', '..') or ':' in p for p in relative):
        raise ValueError('Unsafe Blender source path')
    blend=root.joinpath(*relative).resolve()
    if not blend.is_relative_to(root.joinpath(*prefix).resolve()) or blend.suffix!='.blend':
        raise ValueError('Blender source escapes the expansion source archive')
    directory=Path(proof_directory).resolve()
    export=(directory/proof['export_file']).resolve()
    if not export.is_relative_to(directory) or export.suffix!='.npz':
        raise ValueError('Unsafe Blender proof export path')
    if sha256(blend)!=proof['blend_sha256'] or sha256(export)!=proof['export_sha256']:
        raise ValueError('Blender source/export hash changed')
    return blend,export
