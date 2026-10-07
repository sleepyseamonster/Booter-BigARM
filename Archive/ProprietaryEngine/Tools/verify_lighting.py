"""One short native render pass: sun shadows, real materials, normals and exposure.
No gameplay input, package rebuilds or resource stress loops.
"""
import argparse
import json
from pathlib import Path
import subprocess
import sys
from common import ROOT, inside, write_json
from verify_foundation import png


def difference(a, b):
    if a[:2] != b[:2]:
        raise ValueError("Lighting capture dimensions differ")
    w,h,ca,ra=a
    _,_,cb,rb=b
    changed=darker=brighter=0
    for y in range(h//5,h*9//10,3):
        for x in range(w*2//5,w*9//10,3):
            pa=ra[y][x*ca:x*ca+3];pb=rb[y][x*cb:x*cb+3]
            if max(abs(p-q) for p,q in zip(pa,pb))>3:
                changed+=1
                darker+=sum(pb)<sum(pa)-9
                brighter+=sum(pb)>sum(pa)+9
    return {"changed":changed,"darker":darker,"brighter":brighter}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable",default="build/foundation/engine_workbench")
    parser.add_argument("--shaders",default="build/foundation/Shaders")
    parser.add_argument("--catalog",required=True)
    parser.add_argument("--out",required=True)
    args=parser.parse_args()
    exe,shaders,catalog,out=map(inside,(args.executable,args.shaders,args.catalog,args.out))
    if out.exists(): parser.error("Choose a new evidence directory")
    command=[sys.executable,str(ROOT/"Tools/record.py"),"--out",str(out/"run"),"--timeout","60"]
    for path in [exe,catalog,Path(__file__),*sorted(shaders.glob("*.bin"))]:
        command += ["--input",str(path)]
    command += ["--",str(exe),"--shaders",str(shaders),"--catalog",str(catalog),"--verify-lighting",str(out/"captures")]
    subprocess.run(command,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
    report=json.loads((out/"captures/lighting.json").read_text())["payload"]
    if not report["passed"] or not report["real_surfaces"]: raise ValueError("Missing native material evidence")
    names=["unshadowed","shadowed","sun-moved","surfaces","flat-normal","smooth","exposure","camera-bias"]
    images={name:png(out/"captures"/(name+".png")) for name in names}
    comparisons={name:difference(images[a],images[b]) for name,a,b in [
        ("cast_shadow","unshadowed","shadowed"),("sun_direction","shadowed","sun-moved"),
        ("real_materials","shadowed","surfaces"),("normal_mapping","surfaces","flat-normal"),
        ("roughness","surfaces","smooth"),("exposure","surfaces","exposure"),
        ("camera_bias","surfaces","camera-bias")]}
    if any(result["changed"]<100 for result in comparisons.values()):
        raise ValueError("Ineffective lighting control: "+str(comparisons))
    if comparisons["cast_shadow"]["darker"]<100 or comparisons["cast_shadow"]["brighter"]>10:
        raise ValueError("Shadow pass must selectively darken, not brighten the scene")
    if comparisons["exposure"]["darker"]<1000 or comparisons["exposure"]["brighter"]>10:
        raise ValueError("Negative exposure must darken the lit scene")
    result={"result":"passed","backend":report["backend"],"comparisons":comparisons,
            "limits":"Bounded fixture on this Mac; no Windows, gameplay, large-world shadow quality or final art acceptance."}
    write_json(out/"result.json",result)
    print(json.dumps(result,indent=2))


if __name__=="__main__": main()
