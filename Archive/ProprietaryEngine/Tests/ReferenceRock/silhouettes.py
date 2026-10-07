"""Rasterize matched orthographic views of native and C# reference meshes."""
from pathlib import Path
import math,json,sys
import numpy as np
from PIL import Image,ImageDraw,ImageChops
native,reference,out=map(Path,sys.argv[1:]);out.mkdir(exist_ok=False)
rows=[];sheet=Image.new('RGB',(1080,7*300+50),'#f3f4f5');draw=ImageDraw.Draw(sheet)
for x,title in [(10,'Native C++'),(370,'Original C# algorithms'),(730,'Overlay: matching silhouettes')]:draw.text((x,15),title,fill='#102336')
selected=['golden.json','profile-1.json','profile-2.json','profile-4.json','profile-5.json','profile-6.json','profile-8.json'];worst=1
for n in sorted(native.glob('*.native.json')):
 name=n.name.removesuffix('.native.json');a=json.loads(n.read_text())['payload'];b=json.loads((reference/(name+'.reference.json')).read_text())
 av=np.asarray(a['vertices']);bv=np.asarray(b['vertices']);at=np.asarray(a['triangles']).reshape(-1,3);bt=np.asarray(b['triangles']).reshape(-1,3)
 center=(bv.min(axis=0)+bv.max(axis=0))/2;radius=max(np.linalg.norm(bv-center,axis=1).max(),.001)
 ious=[]
 for view in range(9):
  yaw=view*math.pi/4;pitch=.35 if view<8 else 1.4
  right=np.array([math.cos(yaw),0,-math.sin(yaw)]);up=np.array([-math.sin(yaw)*math.sin(pitch),math.cos(pitch),-math.cos(yaw)*math.sin(pitch)])
  def render(v,t):
   v=v-center;p=np.stack([v@right,-v@up],axis=1)*(220/radius)+256
   im=Image.new('1',(512,512));d=ImageDraw.Draw(im)
   for indices in t:d.polygon([tuple(x) for x in p[indices]],fill=1)
   return im
  ai=render(av,at);bi=render(bv,bt);aa=np.asarray(ai);bb=np.asarray(bi);iou=np.logical_and(aa,bb).sum()/np.logical_or(aa,bb).sum();ious.append(float(iou));worst=min(worst,float(iou))
  if name in selected and view==1:
   row=selected.index(name);y=50+row*300
   for col,im in enumerate([ai,bi]):
    pane=Image.new('RGB',(512,512),'#f3f4f5');pane.paste('#233f56',mask=im);sheet.paste(pane.resize((280,280)),(col*360+35,y))
   pane=Image.new('RGB',(512,512),'#f3f4f5');pane.paste('#2b7d72',mask=ai);pane.paste('#e76132',mask=ImageChops.logical_xor(ai,bi));sheet.paste(pane.resize((280,280)),(755,y))
   ImageDraw.Draw(sheet).text((12,y+270),name.removesuffix('.json'),fill='#102336')
 rows.append(dict(case=name,minimum_iou=min(ious),views=9))
sheet.save(out/'silhouette-comparison.png')
result=dict(passed=worst>=.9995,minimum_iou=worst,views=len(rows)*9,threshold=.9995,cases=rows)
(out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k!='cases'}))
if not result['passed']:sys.exit(1)
