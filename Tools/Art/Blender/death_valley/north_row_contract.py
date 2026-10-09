"""Sealed five-step northern row contract; no arbitrary footprints or owner chains."""
from pathlib import Path
import json
import hashlib
import numpy as np
def sha256(path):
    digest=hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):digest.update(chunk)
    return digest.hexdigest()
ROOT=Path(__file__).resolve().parents[4]
AABB=[499920,4006200,524496,4018488]
SOUTH_IDS=("northwest_far","northwest_next","northwest_outer","northwest","north")
LEGACY_BATCHES=("BadwaterFourSlices","WestNorthNorthwest2026-10-07","WestPair2026-10-08","NextWestPair2026-10-08","FarWestPair2026-10-08","WestRidgePair2026-10-08","NorthRidge2026-10-09")
LEGACY_BOUNDS={"existing":[520400,4006200,524496,4010296],"west":[516304,4006200,520400,4010296],"north":[520400,4010296,524496,4014392],"northwest":[516304,4010296,520400,4014392],"west_outer":[512208,4006200,516304,4010296],"northwest_outer":[512208,4010296,516304,4014392],"west_next":[508112,4006200,512208,4010296],"northwest_next":[508112,4010296,512208,4014392],"west_far":[504016,4006200,508112,4010296],"northwest_far":[504016,4010296,508112,4014392],"west_ridge":[499920,4006200,504016,4010296],"northwest_ridge":[499920,4010296,504016,4014392],"north_ridge":[499920,4014392,504016,4018488]}
def contract(step,batch=None):
    if type(step)!=int or not 1<=step<=5:raise ValueError("Only ordered steps 1..5 are accepted")
    east=504016+4096*(step-1);default=f"NorthRowE{east}2026-10-09"
    if batch is not None and batch!=default:raise ValueError("Batch must match sealed step owner")
    section=f"north_row_e{east}";bounds=[east,4014392,east+4096,4018488]
    previous="NorthRidge2026-10-09" if step==1 else f"NorthRowE{east-4096}2026-10-09"
    retained=dict(LEGACY_BOUNDS)
    for k in range(1,step):
        e=504016+4096*(k-1);retained[f"north_row_e{e}"]=[e,4014392,e+4096,4018488]
    roots=[f"Assets/_Project/Art/Terrain/{b}" for b in LEGACY_BATCHES]+[f"Assets/_Project/Art/Terrain/NorthRowE{504016+4096*(k-1)}2026-10-09" for k in range(1,step)]
    return dict(step=step,batch=default,section=section,bounds=bounds,retained=retained,south=SOUTH_IDS[step-1],west="north_ridge" if step==1 else f"north_row_e{east-4096}",retained_count=3328+256*(step-1),total=3328+256*step,baseline_edges=6512+512*(step-1),edges=6512+512*step,roots=roots,predecessor=f"Assets/_Project/Art/Terrain/{previous}/Source/manifest.json",data=ROOT/f"SourceData/Terrain/DeathValley/{default}",art=ROOT/f"SourceArt/Blender/Studies/DeathValley/{default}",asset=f"Assets/_Project/Art/Terrain/{default}")
def color_path(name):
    base=ROOT/"SourceData/Terrain/DeathValley"
    if name=="existing":return base/"MacSnapshot2026-10-07/four_slices_v1/geographic_color.png"
    if name in ("west","north","northwest"):owner,version="WestNorthNorthwest2026-10-07","Prepared03"
    elif name.endswith("_outer"):owner,version="WestPair2026-10-08","Prepared02"
    elif name.endswith("_next"):owner,version="NextWestPair2026-10-08","Prepared01"
    elif name.endswith("_far"):owner,version="FarWestPair2026-10-08","Prepared01"
    elif name in ("west_ridge","northwest_ridge"):owner,version="WestRidgePair2026-10-08","Prepared01"
    elif name=="north_ridge":owner,version="NorthRidge2026-10-09","Prepared01"
    elif name.startswith("north_row_e"):owner,version=f"NorthRowE{name.removeprefix('north_row_e')}2026-10-09","Prepared01"
    else:raise ValueError("Unknown retained color owner")
    return base/owner/version/(name+"_geographic_color.png")
def verified_anchor_image(name):
    """Prepared imagery must reproduce the already accepted runtime color pixels."""
    from PIL import Image
    file=color_path(name);image=np.asarray(Image.open(file).convert("RGB"))
    if image.shape!=(2048,2048,3):raise ValueError("Unexpected retained image grid")
    owner=file.parent.parent.name
    runtime=ROOT/f"Assets/_Project/Art/Terrain/{owner}/Source/Colors/{name}"
    for row in range(16):
        for col in range(16):
            tile=np.asarray(Image.open(runtime/f"r{row:02}_c{col:02}.png").convert("RGB"))
            if not np.array_equal(tile,image[row*128:(row+1)*128,col*128:(col+1)*128]):raise ValueError("Prepared anchor differs from accepted runtime imagery")
    return image
def expected_cells(c,include_new=False):
    bounds=list(c["retained"].values())+([c["bounds"]] if include_new else [])
    return {(e,n) for b in bounds for e in range(b[0],b[2],256) for n in range(b[1],b[3],256)}
def validate_occupancy(c,tiles,include_new=False):
    expected=expected_cells(c,include_new);actual=[]
    for t in tiles:
        e,n=t["bounds_m"][:2]
        if t["bounds_m"]!=[e,n,e+256,n+256] or t["geographic_key"]!=f"epsg26911/e{e}/n{n}/size256":raise ValueError("Shifted geometry or geographic identity")
        actual.append((e,n))
    if len(actual)!=len(expected) or set(actual)!=expected:raise ValueError("Missing, duplicate or premature terrain cells")
def protected_inventory(c):
    paths={f.relative_to(ROOT).as_posix() for folder in c["roots"] for f in (ROOT/folder).rglob("*") if f.is_file()}
    if any(not (ROOT/folder).is_dir() for folder in c["roots"]):raise ValueError("Missing retained asset root")
    return paths|{"Packages/manifest.json","Packages/packages-lock.json","ProjectSettings/ProjectVersion.txt","ProjectSettings/EditorBuildSettings.asset"}
def validate_baseline(c,bm):
    validate_occupancy(c,bm["tiles"])
    if {(t["bounds_m"][0],t["bounds_m"][1]) for t in bm["tiles"]}!=expected_cells(c) or len(bm["tiles"])!=c["retained_count"] or bm["edges"]!=c["baseline_edges"]:raise ValueError("Wrong predecessor occupancy")
    if {f["path"] for f in bm["protected_files"]}!=protected_inventory(c):raise ValueError("Incomplete protected ownership")
    for f in bm["protected_files"]:
        if sha256(ROOT/f["path"])!=f["sha256"]:raise ValueError("Protected bytes changed")
    pred=json.loads((ROOT/c["predecessor"]).read_text())
    if pred["tile_count"]!=c["retained_count"]:raise ValueError("Predecessor step count changed")
    if c["step"]>1 and pred.get("row_step")!=c["step"]-1:raise ValueError("Predecessor step missing")
    if sha256(ROOT/bm["scene"])!=bm["scene_sha256"]:raise ValueError("Predecessor scene changed")
def feather_color(matched,south,west,step):
    """South priority only at southwest; all other border pixels are exact."""
    allowed=np.array(([16,16,16] if step==5 else [0,0,0]),dtype='int16')
    return _feather_color_with_bound(matched,south,west,allowed)
def _feather_color_with_bound(matched,south,west,allowed):
    """Internal policy primitive, also tested against historical conflict fixtures."""
    if matched.shape!=(2048,2048,3) or south.shape!=(2048,3) or west.shape!=(2048,3):raise ValueError("Color grid contract")
    conflict=np.abs(south[0].astype('int16')-west[-1].astype('int16'))
    if np.any(conflict>allowed):raise ValueError("Corner conflict exceeds audited anchors")
    result=matched.astype('float32').copy();w=np.clip(1-np.arange(2048)/128,0,1).astype('float32');s=w[::-1]
    result+=(west-result[:,0])[:,None,:]*w[None,:,None]
    result+=(south-result[-1])[None,:,:]*s[:,None,None]
    result=np.clip(np.rint(result),0,255).astype('uint8');result[:,0]=west;result[-1]=south
    if not np.array_equal(result[-1],south) or not np.array_equal(result[:-1,0],west[:-1]):raise ValueError("Color anchor moved")
    return result,{"policy":"south_row_priority_single_southwest_pixel","west_exception_pixels":int(np.any(conflict)),"southwest_difference_rgb":conflict.tolist(),"audited_max_rgb":allowed.tolist(),"south_edge_error":0,"west_edge_error_excluding_corner":0}
