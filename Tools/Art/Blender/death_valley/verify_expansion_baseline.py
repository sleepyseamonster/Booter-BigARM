"""Verify and archive an isolated retained-terrain readback, without editing Unity assets."""
import argparse
import json
from pathlib import Path
import sys
import numpy as np
from prepare_region import sha256

ROOT = Path(__file__).resolve().parents[4]

def verify(capture, output):
    if output.exists(): raise ValueError("Use a fresh baseline archive directory")
    report = json.loads((capture/"baseline.json").read_text())
    if report["schema_version"]!=1 or len(report["tiles"])!=256 or report["scene_guid"]!="4de5dd018ee194314a00fd369e2d3eeb":
        raise ValueError("Unexpected baseline schema/identity")
    arrays={};keys=set();maximum=0.
    sys.path.insert(0,str(ROOT/"Docs/DeathValley"))
    for tile in report["tiles"]:
        key=tile["id"]
        row,col=int(key[1:3]),int(key[5:7])
        if key!=f"r{row:02}_c{col:02}" or not 0<=row<16 or not 0<=col<16 or key in arrays:
            raise ValueError("Invalid or duplicate local tile")
        expected=f"epsg26911/e{520400+256*col}/n{4010296-256*(row+1)}/size256"
        if tile["geographic_key"]!=expected or expected in keys:raise ValueError("Invalid geographic identity")
        keys.add(expected)
        if not tile["collider_matches"] or tile["resolution"]!=257:raise ValueError("Baseline collider/grid mismatch")
        file=(capture/tile["heights_file"]).resolve()
        if capture.resolve() not in file.parents or sha256(file)!=tile["heights_sha256"]:raise ValueError("Readback changed or escaped capture")
        heights=np.fromfile(file,dtype="<f4").reshape(257,257)
        if not np.isfinite(heights).all() or heights.min()<0 or heights.max()>1:raise ValueError("Invalid normalized readback")
        for name,hash_name in [("data_path","data_sha256"),("source_file","source_sha256")]:
            path=(ROOT/tile[name]).resolve()
            if ROOT not in path.parents or sha256(path)!=tile[hash_name]:raise ValueError("Live retained source changed: "+str(path))
        if sha256(ROOT/(tile["data_path"]+".meta"))!=tile["meta_sha256"]:raise ValueError("Retained metadata changed")
        position=tile["position"];size=tile["size"]
        if position!={"x":col*256-2048,"y":-100,"z":(15-row)*256-2048} or size!={"x":256,"y":1800,"z":256}:
            raise ValueError("Retained bounds mismatch")
        n=257 if row in (12,13) and col in (1,2) else 129
        source=np.fromfile(ROOT/tile["source_file"],dtype="<u2").reshape(n,n)[::-1].astype("float32")/np.float32(65535)
        if n==129:
            error=np.abs(heights[::2,::2]-source)
        else:
            error=np.abs(heights-source)
            # Original documented mixed-resolution focus perimeters only.
            if row==12:error[256,1:256:2]=0
            if row==13:error[0,1:256:2]=0
            if col==1:error[1:256:2,0]=0
            if col==2:error[1:256:2,256]=0
        maximum=max(maximum,float(error.max())*1800)
        arrays[key]=heights
    edges=0
    for r in range(16):
        for c in range(16):
            a=arrays[f"r{r:02}_c{c:02}"]
            if c<15:
                if not np.array_equal(a[:,-1],arrays[f"r{r:02}_c{c+1:02}"][:,0]):raise ValueError("Retained E/W seam changed")
                edges+=1
            if r<15:
                if not np.array_equal(a[0,:],arrays[f"r{r+1:02}_c{c:02}"][-1,:]):raise ValueError("Retained N/S seam changed")
                edges+=1
    if maximum>.04:raise ValueError("Measured source discrepancy exceeds retained precision")
    output.mkdir(parents=True)
    file=output/"render_heights.npz";np.savez_compressed(file,**arrays)
    report.update({"render_heights_file":file.name,"render_heights_sha256":sha256(file),
                   "baseline_capture_sha256":sha256(capture/"baseline.json"),"shared_edges_checked":edges,
                   "max_retained_sample_error_m":maximum,"status":"verified_readback_baseline",
                   "limits":"Collider data binding only; no new terrain or actor traversal; scene hash identifies captured snapshot"})
    (output/"baseline.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"tiles":256,"shared_edges":edges,"max_source_error_m":maximum,"archive":str(output)}))

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--capture",type=Path,required=True);p.add_argument("--output",type=Path,required=True)
    a=p.parse_args();verify(a.capture.resolve(),a.output.resolve())

if __name__=="__main__":main()
