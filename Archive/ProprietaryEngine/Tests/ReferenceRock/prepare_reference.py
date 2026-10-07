"""Extract original project C# math algorithms into an isolated, test-only .NET runner."""
from pathlib import Path
import re,sys,hashlib,json
repo=Path(__file__).resolve().parents[3];out=Path(sys.argv[1]).resolve()
assert repo/'Engine' in out.parents and not out.exists()
out.mkdir(parents=True)
base=repo/'Assets/_Project/Scripts/Editor/TopDown3D'
source=(base/'TopDown3DRockWorkbenchBaseRockGenerator.cs').read_text()
# Remove only editor interaction methods; original deterministic bodies are unchanged.
remove=['CreateNewSeed','GenerateNewIntoWorkbench','GenerateIntoWorkbench','CreatePlanForAuthoring','GetOrCreateSourceGroup','GetShapeLabel','GetVolumeName']
def strip_methods(s,names):
 for name in names:
  while (m:=re.search(r'        (?:internal|private|public) static [^\n]+ '+name+r'\(',s)):
   a=s.index('{',m.start());i=a+1;depth=1
   while depth:depth+=(s[i]=='{')-(s[i]=='}');i+=1
   s=s[:m.start()]+s[i:]
 return s
source=strip_methods(source,remove).replace('using UnityEditor;','')
(out/'Planner.cs').write_text(source)
(out/'Mesher.cs').write_text((base/'TopDown3DRockWorkbenchMesher.cs').read_text())
top=(base/'RockFusion/TopDown3DRockMeshTopology.cs').read_text();top=strip_methods(top,['Get','ResetCache']);top=top.replace('[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]','')
(out/'Topology.cs').write_text(top)
for name in ['UnityMath.cs','Program.cs']:(out/name).write_text((Path(__file__).parent/name).read_text())
(out/'Reference.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><CheckForOverflowUnderflow>false</CheckForOverflowUnderflow></PropertyGroup></Project>')
files=[base/'TopDown3DRockWorkbenchBaseRockGenerator.cs',base/'TopDown3DRockWorkbenchMesher.cs',base/'RockFusion/TopDown3DRockMeshTopology.cs']
(out/'source-hashes.json').write_text(json.dumps({str(p.relative_to(repo)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2))
