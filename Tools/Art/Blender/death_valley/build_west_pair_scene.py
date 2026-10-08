"""West-pair adapter for the proven native mesh builder and reopen verifier.

The original expansion entry point and archived sources are left unchanged.
Only the new manifest's verified six-section resource contract is substituted.
"""
import argparse
import json
from pathlib import Path
import sys

import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_expansion_scene as native

NEW = {"west_outer", "northwest_outer"}
ALL = NEW | {"existing", "west", "north", "northwest"}


def arguments():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--manifest",type=Path,required=True)
    p.add_argument("--mode",choices=["build","verify","render"],required=True)
    p.add_argument("--section",choices=sorted(NEW)+["combined","pilot"],required=True)
    p.add_argument("--output",type=Path,required=True)
    p.add_argument("--stride",type=int,choices=[2,4],default=2)
    p.add_argument("--view",choices=["overhead","oblique","junction"],default="oblique")
    return p.parse_args(sys.argv[sys.argv.index("--")+1:])


def load(manifest):
    record=json.loads(manifest.read_text())
    if record["schema_version"]!=1 or record["unity_origin_m"]!=[522448,4008248] or record["bounds_m"]!=[512208,4006200,524496,4014392]:
        raise ValueError("Unsupported west-pair footprint")
    if native.sha256(native.ROOT/record["baseline_manifest"])!=record["baseline_manifest_sha256"]:
        raise ValueError("Baseline changed")
    if {s["id"] for s in record["sections"]}!=NEW or {s["id"] for s in record["retained_sections"]}!=ALL-NEW:
        raise ValueError("Incomplete section ownership")
    grids,colors,tiles={},{},[]
    for section in record["sections"]+record["retained_sections"]:
        name=section["id"]
        if len(section["tiles"])!=256:
            raise ValueError("Incomplete section")
        for key,h in (("mesh_grids_file","mesh_grids_sha256"),("color_file","color_sha256")):
            resource=(manifest.parent/section[key]).resolve()
            if not resource.is_relative_to(manifest.parent) or native.sha256(resource)!=section[h]:
                raise ValueError("Changed or escaped Blender resource")
        grids[name]=np.load(manifest.parent/section["mesh_grids_file"])
        colors[name]=manifest.parent/section["color_file"]
        for t in section["tiles"]:
            tiles.append(dict(t,id=name+"/"+t["local_id"],section=name,section_bounds=section["bounds_m"]))
    if len({t["geographic_key"] for t in tiles})!=1536:
        raise ValueError("Duplicate geographic identity")
    return record,grids,colors,tiles


def selected(tiles,section):
    if section=="combined":return tiles
    if section=="pilot":
        ids={"west_outer/r00_c15","northwest_outer/r15_c15","west/r00_c00","northwest/r15_c00"}
        return [t for t in tiles if t["id"] in ids]
    return [t for t in tiles if t["section"]==section]


original_mesh=native.mesh
original_camera=native.setup_camera


def mesh(*args):
    obj=original_mesh(*args)
    retained=args[0]["section"] not in NEW
    obj["retained_reference"]=retained
    obj.hide_select=retained
    return obj


def setup_camera(view,tiles):
    original_camera("oblique" if view=="junction" else view,selected(tiles,"pilot") if view=="junction" else tiles)


if __name__=="__main__":
    native.arguments=arguments
    native.load=load
    native.selected=selected
    native.mesh=mesh
    native.setup_camera=setup_camera
    native.main()
