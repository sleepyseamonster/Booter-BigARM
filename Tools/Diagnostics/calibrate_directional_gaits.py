"""Standing directional loop recipes and stance proposals from private equipped tapes."""
import argparse
import json
from pathlib import Path
import numpy as np
from calibrate_forward_gaits import calibrate

SELECTION = (
    ("WalkLeft", "B", "SidewaysWalkOriginalBaked", (-1, 0)),
    ("RunLeft", "B", "ExistingRunLeftOriginalBaked", (-1, 0)),
    ("WalkRight", "C", "SidewaysWalkMirrorOriginalBaked", (1, 0)),
    ("RunRight", "C", "ExistingRunLeftMirrorOriginalBaked", (1, 0)),
    ("WalkBackward", "E", "NeutralBackwardWalkOriginalBaked", (0, -1)),
    ("RunBackward", "C", "BackwardRunOriginalBaked", (0, -1)),
)


def window(data, side, axis, seconds):
    t = np.array([s["time"] for s in data["samples"]]); xyz = np.array([s["points"] for s in data["samples"]])
    ids = np.array([i for i, v in enumerate(data["vertices"]) if any(side in b["bone"] for b in v)])
    count = round(seconds / float(np.median(np.diff(t)))); best = None
    axis = np.array(axis); perpendicular = np.array([axis[1], -axis[0]])
    for first in range(len(t)-count):
        stop = first+count; part=xyz[first:stop+1]; low=part[:, ids, 1].min(axis=1)
        if max(low-part[:, :, 1].min(axis=1)) > .002:
            continue
        times=t[first:stop+1]-t[first]; centered=times-times.mean()
        for vertex in ids:
            height=part[:, vertex, 1]-low
            if height.max() > .008:
                continue
            planar=part[:, vertex][:, [0, 2]]; along=planar@axis
            speed=-float(centered@along/(centered@centered))
            if not .4 < speed < 7:
                continue
            world=planar+np.outer(times,axis*speed)
            excursion=float(np.linalg.norm(world-world[0],axis=1).max())
            travel=float(np.linalg.norm(np.diff(world,axis=0),axis=1).sum())
            score=excursion+.15*travel+float(height.mean())
            if best is None or score<best[0]:
                best=(score,dict(time=float((t[first]+t[stop])/2),speed=speed,vertex=int(vertex),excursion=excursion,travel=travel))
    if best is None:
        raise ValueError(f"No native {side} stance window: {data['path']}")
    return best[1]


def prepare(root, output):
    if output.exists():
        raise FileExistsError(output)
    rows=[]; observations=[]
    for role, folder, name, axis in SELECTION:
        data=json.loads((root/f"DirectionalSourceTapes{folder}"/(name+".json")).read_text(encoding="utf-8-sig"))
        seconds=.1 if role.startswith("Walk") else 8/120
        right=window(data,".R",axis,seconds);left=window(data,".L",axis,seconds)
        origin=(right["time"]/data["length"]-.16)%1
        r=((right["time"]/data["length"]-origin)%1)/2;l=((left["time"]/data["length"]-origin)%1)/2
        if not r<l<r+.5<l+.5<1:
            raise ValueError(f"Review alternating native contacts for {role}: {r}, {l}")
        rows.append(dict(source=data["path"],name="StandingDirectional"+role,originPhase=origin))
        observations.append(dict(role=role,axis=axis,seeds=[r,l,r+.5,l+.5],window=seconds,right=right,left=left))
    output.write_bytes((json.dumps(rows,indent=2)+"\n").encode())
    output.with_name(output.stem+"-seeds.json").write_bytes((json.dumps(observations,indent=2)+"\n").encode())


def propose(root, plan, output):
    if output.exists():
        raise FileExistsError(output)
    seeds=json.loads(plan.with_name(plan.stem+"-seeds.json").read_text())
    converted=root/(output.stem+"-inputs");converted.mkdir(exist_ok=False)
    sources=[]
    for row in seeds:
        name="StandingDirectional"+row["role"]
        data=json.loads((root/(name+".json")).read_text(encoding="utf-8-sig"))
        axis=np.array(row["axis"]);perp=np.array([axis[1],-axis[0]])
        def rotate(p):
            p=np.array(p);return [float(p[[0,2]]@perp),float(p[1]),float(p[[0,2]]@axis)]
        sides=[0 if any(".L" in b["bone"] for b in v) else 1 for v in data["vertices"]]
        frames=[]
        for sample in data["samples"]:
            points=np.array(sample["points"])
            frames.append(dict(seconds=sample["time"],hips=[0,0,0],left=rotate(points[np.array(sides)==0].mean(axis=0)),
                right=rotate(points[np.array(sides)==1].mean(axis=0)),vertices=[rotate(p) for p in points]))
        converted.joinpath(name+".json").write_bytes(json.dumps(dict(path=data["path"],floor=-.91,globalSkinWeights="FourBones",
            ids=[dict(side=s,vertex=i) for i,s in enumerate(sides)],frames=frames)).encode())
        nominal=(1.1 if row["role"].startswith("Walk") else 1.8) if row["role"].endswith("Backward") else (1.4 if row["role"].startswith("Walk") else 3.)
        sources.append((row["role"],name,nominal,row["seeds"],row["window"]))
    report=calibrate(converted,False,sources,maximum_lift=.08,regularization=.08,minimum_native_speed=.4,contact_centres_only=True)
    # The backward walk's unequal stance intervals need the measured local
    # distance fit; contact-centre interpolation alone left visible axial slip.
    report["gaits"][4]=calibrate(converted,False,[sources[4]],maximum_lift=.08,regularization=.5,minimum_native_speed=.4)["gaits"][0]
    output.write_bytes((json.dumps(report,indent=2)+"\n").encode())
    for gait in report["gaits"]:
        print(gait["role"],"native",round(gait["nativeContactSpeed"],4),"stride",round(gait["metresPerCycle"],4),
              "rate",round(gait["pureMinimumEffectiveRate"],3),round(gait["pureMaximumEffectiveRate"],3))


if __name__=="__main__":
    parser=argparse.ArgumentParser();parser.add_argument("mode",choices=("prepare","propose"));parser.add_argument("directory",type=Path)
    parser.add_argument("output",type=Path);parser.add_argument("--plan",type=Path);args=parser.parse_args()
    if args.mode=="prepare":prepare(args.directory,args.output)
    else:propose(args.directory,args.plan,args.output)
