"""Compare native geometry with the original project C# planner and mesher."""
from pathlib import Path
import json,subprocess,sys,math
native=Path(sys.argv[1]);reference=Path(sys.argv[2]);out=Path(sys.argv[3]);out.mkdir(exist_ok=False)
rows=[]
for n in sorted(native.glob('*.native.json')):
 recipe=native/n.name.removesuffix('.native.json');result=out/(recipe.name+'.reference.json')
 subprocess.run(['dotnet',str(reference),str(recipe),str(result)],check=True,stdout=subprocess.DEVNULL)
 a=json.loads(n.read_text())['payload'];b=json.loads(result.read_text())
 pose=max((abs(x-y) for p,q in zip(a['poses'],b['poses']) for key in ['center','scale'] for x,y in zip(p[key],q[key])),default=0)
 qerror=max((min(max(abs(x-y) for x,y in zip(p['orientation'],q['orientation'])),max(abs(x+y) for x,y in zip(p['orientation'],q['orientation']))) for p,q in zip(a['poses'],b['poses'])),default=0)
 counts=len(a['vertices'])==len(b['vertices']) and len(a['triangles'])==len(b['triangles'])
 distance=max((math.dist(p,q) for p,q in zip(a['vertices'],b['vertices'])),default=0) if counts else None
 row=dict(case=recipe.name,counts_match=counts,native_vertices=len(a['vertices']),reference_vertices=len(b['vertices']),max_pose_error=pose,max_quaternion_error=qerror,max_corresponding_vertex_distance=distance)
 def oriented(triangles):
  return sorted(min(tuple(triangles[i+j:i+3]+triangles[i:i+j]) for j in range(3)) for i in range(0,len(triangles),3))
 topology=counts and oriented(a['triangles'])==oriented(b['triangles'])
 sources=len(a['poses'])==len(b['poses']) and all(p['primitive']==q['primitive'] and p['seed']==q['seed'] for p,q in zip(a['poses'],b['poses']))
 row['source_identity_match']=sources
 row['topology_match']=topology
 row['passed']=sources and topology and pose<2e-5 and qerror<2e-5 and distance<.0001
 rows.append(row);print(row,flush=True)
summary=dict(passed=bool(rows) and all(r['passed'] for r in rows),cases=rows,proof='Original project C# algorithm bodies with test-only math adapters; no Unity Editor execution; captured Unity pose also checked by native tests')
(out/'result.json').write_text(json.dumps(summary,indent=2)+'\n')
if not summary['passed']:sys.exit(1)
