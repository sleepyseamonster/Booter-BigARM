"""Compare a privately sampled loop derivative with its time-mapped source pose."""
import argparse
import json
import math
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('source',type=Path)
parser.add_argument('derived',type=Path)
parser.add_argument('--origin',type=float,required=True)
args=parser.parse_args()
source=json.loads(args.source.read_text())
derived=json.loads(args.derived.read_text())
bones=['hips','head','leftFoot','rightFoot','leftToes','rightToes','leftHand','rightHand']
errors=[]
relative_errors=[]
height_offsets=[]
for frame in derived['frames']:
    phase=(frame['seconds']/source['seconds']+args.origin)%1
    location=phase*(len(source['frames'])-1)
    first=math.floor(location)
    blend=location-first
    a,b=source['frames'][first],source['frames'][first+1]
    expected_hips={axis:a['hips'][axis]*(1-blend)+b['hips'][axis]*blend for axis in ['x','y','z']}
    height_offsets.append(frame['hips']['y']-expected_hips['y'])
    for bone in bones:
        expected={axis:a[bone][axis]*(1-blend)+b[bone][axis]*blend for axis in ['x','y','z']}
        errors.append(math.sqrt(sum((frame[bone][axis]-expected[axis])**2 for axis in expected)))
        relative_errors.append(math.sqrt(sum(((frame[bone][axis]-frame['hips'][axis])-(expected[axis]-expected_hips[axis]))**2 for axis in expected)))
print(json.dumps(dict(source=source['clipName'],derived=derived['clipName'],maximum_metres=max(errors),
                      rms_metres=math.sqrt(sum(value*value for value in errors)/len(errors)),
                      source_seconds=source['seconds'],derived_seconds=derived['seconds'],
                      finite=derived['finite'],seam_gap=derived['seamFootGap'],
                      relative_pose_maximum_metres=max(relative_errors),mean_height_rebase_metres=sum(height_offsets)/len(height_offsets))))
