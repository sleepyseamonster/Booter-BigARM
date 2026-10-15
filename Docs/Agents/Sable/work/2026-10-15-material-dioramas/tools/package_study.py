"""Verify raw frame matrix and package unfiltered review images/boards/HTML.
Python package_study.py ROOT [--partial] (partial contact sheets only, for ongoing QA).
Pillow is used for resizing, captions and contact sheets, never to repaint a render.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageStat
import hashlib,json,sys,itertools,html,math

ROOT=Path(sys.argv[1]).resolve();HERE=Path(__file__).resolve().parent.parent
RAW=ROOT/'outputs/Sable/MaterialDioramas20261015'
PARTIAL='--partial' in sys.argv
receipt=json.loads((RAW/'render_receipt.json').read_text(encoding='utf-8'))
rows=receipt['renders'];styles=receipt['styles'];labels=receipt['materials']
FONT=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24)
TITLE=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',32)
SMALL=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
BOARD=HERE/'boards';IMAGES=HERE/'images';BOARD.mkdir(exist_ok=True);IMAGES.mkdir(exist_ok=True)
GROUPS={'metal':['M01','M02','M03','M04','M05','M06'],'stone':['S01','S02','S03','S04','O01'],'ground':['G01','G02','G03','C01']}
def key(r):return (r['scene'],r['style'],r['light'],r['view'],r['kind'],r.get('group'))
assert len({key(r) for r in rows})==len(rows),'Duplicate frame keys'
checks=[]
for row in rows:
    path=RAW/'renders'/row['file'];assert path.exists(),path
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],path
    with Image.open(path) as im:assert list(im.size)==row['resolution'];assert im.mode=='RGB'
    if row['kind']=='controlled':assert row['geometry']==receipt['geometry'][row['scene']]
for sn,light,view in itertools.product(['yard','gate','quarry'],['sunset','twilight'],['gameplay','close','orbit']):
    group=[r for r in rows if (r['scene'],r['light'],r['view'],r['kind'])==(sn,light,view,'controlled')]
    if len(group)!=4:
        assert PARTIAL,f'Missing controlled group: {sn}/{light}/{view}'
        continue
    assert {r['style'] for r in group}==set(styles)
    assert all(r['camera']==group[0]['camera'] for r in group)
    assert all(r['resolution']==group[0]['resolution'] for r in group)
    checks.append((sn,light,view))
if not PARTIAL:
    assert receipt.get('complete') and len(rows)==97,(receipt.get('complete'),len(rows))
    for kind,count in [('controlled',72),('material',12),('neutral_diagnostic',4),('accepted_ground',1),('whole_style',8)]:assert sum(r['kind']==kind for r in rows)==count,kind
    for sn in ['yard','gate','quarry']:
        assert {o['material'] for o in receipt['objects'][sn]}==set(labels),sn
    for r in rows:
        if r['view']=='gameplay':
            c=r['camera']
            actual=[c['distance'],c['pitch'],c['yaw'],*c['target'],c['vertical_fov'],c['aspect']]
            assert all(math.isclose(a,b,rel_tol=0,abs_tol=1e-6) for a,b in zip(actual,[22,50,40,0,0,1.15,48,16/9])),c
    for rel,digest in receipt['sources'].items():assert hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()==digest,rel
    for source in receipt.get('native_sources',[]):assert hashlib.sha256((ROOT/source['file']).read_bytes()).hexdigest()==source['sha256'],source['file']

derivatives=[]
for row in rows:
    source=RAW/'renders'/row['file'];out=IMAGES/(source.stem+'.jpg')
    with Image.open(source) as im:
        if row['kind']=='material':
            ids=GROUPS[row['group']]
            canvas=Image.new('RGB',(im.width,im.height+160),'#15191c');canvas.paste(im,(0,76));d=ImageDraw.Draw(canvas)
            d.text((36,18),f"{row['style']}  /  {styles[row['style']]}  /  {row['group'].upper()} — matching diagnostic geometry",font=TITLE,fill='#eee8dd')
            for n,part in enumerate([ids[3:],ids[:3]]):
                txt=('Back/top: ' if n==0 else 'Front/bottom: ')+ '   |   '.join(f'{i} {labels[i]}' for i in part)
                d.text((36,im.height+82+n*32),txt,font=FONT,fill='#ded0bd')
            canvas.save(out,quality=92,subsampling=0)
        elif row['kind']=='accepted_ground':
            canvas=Image.new('RGB',(im.width,im.height+110),'#15191c');canvas.paste(im,(0,70));d=ImageDraw.Draw(canvas)
            d.text((30,14),'Original B ground control (left) / phase-blended B derivative (right)',font=TITLE,fill='#eee8dd')
            d.text((30,im.height+76),'Identical geometry, aligned metre coordinates, retained source pixels and grading. Neutral light.',font=FONT,fill='#ded0bd');canvas.save(out,quality=92,subsampling=0)
        else:im.save(out,quality=92,subsampling=0)
    derivatives.append({'source':row['file'],'image':str(out.relative_to(HERE)).replace('\\','/'),'sha256':hashlib.sha256(out.read_bytes()).hexdigest()})
    row['review']=str(out.relative_to(HERE)).replace('\\','/')

def board(selected,name,title,note):
    selected=sorted(selected,key=lambda r:r['style']);w,h=960,540
    canvas=Image.new('RGB',(1920,1280),'#14191d');d=ImageDraw.Draw(canvas)
    d.text((25,15),title,font=TITLE,fill='#eee8dd');d.text((25,59),note,font=SMALL,fill='#c6bba9')
    for i,row in enumerate(selected):
        with Image.open(RAW/'renders'/row['file']) as im:thumb=im.resize((w,h),Image.Resampling.LANCZOS)
        x=(i%2)*w;y=125+(i//2)*575
        canvas.paste(thumb,(x,y));d.text((x+18,y-29),f"{row['style']}  {styles[row['style']]}",font=FONT,fill='#e4c299')
    out=BOARD/(name+'.jpg');canvas.save(out,quality=92,subsampling=0)
for sn,light,view in checks:
    selected=[r for r in rows if (r['scene'],r['light'],r['view'],r['kind'])==(sn,light,view,'controlled')]
    board(selected,f'{sn}_{light}_{view}',f'{sn.upper()} / {light} / {view}','Same geometry, placement, camera, wear coverage and light. Surface treatments differ.')
for view in ['gameplay','close']:
    selected=[r for r in rows if (r['kind'],r['view'])==('whole_style',view)]
    if len(selected)==4:board(selected,'whole_style_'+view,'WORKYARD / whole-style pilot / '+view,'C/D also change rock planes and selected housing proportions. A/B retain the common form.')

# Compact contact sheets cover every retained primary frame for completeness review.
for sn in ['yard','gate','quarry','plates','control']:
    selected=[r for r in rows if r['scene']==sn]
    if not selected:continue
    tw,th=480,270;nrows=(len(selected)+3)//4
    sheet=Image.new('RGB',(1920,nrows*310+70),'#15191c');d=ImageDraw.Draw(sheet);d.text((20,15),sn.upper()+' / every captured frame',font=TITLE,fill='white')
    for i,row in enumerate(selected):
        with Image.open(RAW/'renders'/row['file']) as im:thumb=im.resize((tw,th),Image.Resampling.LANCZOS)
        x=(i%4)*tw;y=(i//4)*310+70;sheet.paste(thumb,(x,y))
        caption=f"{row['style']} {row['light']} {row['view']} / {row['kind']}"+((' '+row['group']) if row.get('group') else '')
        d.text((x+8,y+273),caption,font=SMALL,fill='#e4c299')
    sheet.save(BOARD/(sn+'_contact.jpg'),quality=90)
if PARTIAL:
    print(json.dumps({'partial_frames':len(rows),'matching_groups':len(checks)}));sys.exit(0)

metrics=[]
for row in rows:
    if row['kind'] not in ['controlled','neutral_diagnostic']:continue
    with Image.open(RAW/'renders'/row['file']) as im:
        gray=im.convert('L');hist=gray.histogram();n=im.width*im.height
        metrics.append({'file':row['file'],'mean_luma_8bit':ImageStat.Stat(gray).mean[0],'near_black_fraction':sum(hist[:5])/n,'near_white_fraction':sum(hist[251:])/n})
verification={'primary_images':len(rows),'controlled_matching_groups':len(checks),'material_coverage':{sn:sorted({o['material'] for o in receipt['objects'][sn]}) for sn in ['yard','gate','quarry']},'native_resolution':[2560,1440],'source_and_render_hashes_verified':True,'camera_match_verified':True,'review_images':derivatives,'image_metrics':metrics,'limits':'Metrics diagnose clipping but do not prove visual quality. Offline Cycles only. Visual review and clean-source reproduction recorded separately.'}
(HERE/'render_receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
(HERE/'verification.json').write_text(json.dumps(verification,indent=2),encoding='utf-8')
data=json.dumps(rows)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Sable | Material Dioramas</title>
<style>*{box-sizing:border-box}body{margin:0;background:#101719;color:#efe8dc;font:16px system-ui}header{padding:28px 4vw 18px;border-bottom:1px solid #3d4545}h1{font-size:32px;letter-spacing:-.8px;margin:0 0 8px}p{color:#c4c6bf;max-width:1000px;line-height:1.5}nav{display:flex;gap:12px;flex-wrap:wrap;margin:20px 0}label{display:flex;flex-direction:column;gap:6px;font-size:13px;color:#b5bfb9}select,button{background:#253033;border:1px solid #5e6b66;border-radius:5px;color:#efe8dc;padding:10px;font:inherit}main{padding:20px 3vw}#grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}figure{margin:0;background:#1a2224;border:1px solid #3c4948;border-radius:8px;overflow:hidden}figcaption{padding:14px;color:#dfb986}figure img{width:100%;height:auto;display:block}a{color:#e8ba7a}#note{min-height:45px}#legend{margin-top:18px;font-size:14px}footer{padding:25px 4vw;color:#abb6b0}@media(max-width:850px){#grid{grid-template-columns:1fr}h1{font-size:26px}}</style>
<header><h1>Material dioramas</h1><p>Three places. Four original treatments. Compare materials on identical forms, then inspect a separate form pilot. Click any image for its full review resolution.</p>
<nav><label>Comparison<select id="mode"><option value="controlled">Common geometry / materials</option><option value="whole_style">Whole-style workyard pilot</option><option value="material">Neutral material plates</option><option value="neutral_diagnostic">Neutral workyard diagnostic</option><option value="accepted_ground">Accepted ground control</option></select></label>
<label>Place<select id="scene"><option value="yard">Salvage workyard</option><option value="gate">Collapsed service gate</option><option value="quarry">Quarry relay cut</option></select></label>
<label>Light<select id="light"><option value="sunset">Brighter sunset</option><option value="twilight">Deeper twilight</option></select></label>
<label>View<select id="view"><option value="close">Material close view</option><option value="gameplay">Elevated gameplay reference</option><option value="orbit">Alternate orbit</option></select></label>
<label>Plate<select id="group"><option value="metal">Metal / 6 finishes</option><option value="stone">Stone and ore / 5 surfaces</option><option value="ground">Ground and ceramic / 4 surfaces</option></select></label></nav><p id="note"></p></header><main><div id="grid"></div><p id="legend"></p></main>
<footer>Offline Blender Cycles study. Procedural materials and selected retained generated texture drafts; not hand-painted production assets. Fixed exposure and framing within matched groups. No Unity production content changed. <a href="RESULTS.md">Art review and limits</a> · <a href="PLAN.md">Plan</a></footer>
<script>const rows=__DATA__, names=__NAMES__; const $=id=>document.getElementById(id);function update(){const mode=$('mode').value;let scene=$('scene').value,light=$('light').value,view=$('view').value;const group=$('group').value;
$('scene').disabled=mode!=='controlled';$('light').disabled=mode!=='controlled';$('view').disabled=!['controlled','whole_style'].includes(mode);$('group').disabled=mode!=='material';
if(mode==='whole_style'){scene='yard';light='sunset';if(view==='orbit'){view='gameplay';$('view').value=view}}if(mode==='material'){scene='plates';light='neutral';view='plate'}if(mode==='neutral_diagnostic'){scene='yard';light='neutral';view='gameplay'}if(mode==='accepted_ground'){scene='control';light='neutral';view='control'}
const selected=rows.filter(r=>r.kind===mode&&r.scene===scene&&r.light===light&&r.view===view&&(mode!=='material'||r.group===group));$('grid').replaceChildren();selected.forEach(r=>{const f=document.createElement('figure'),c=document.createElement('figcaption'),a=document.createElement('a'),im=new Image();c.textContent=r.style+' — '+names[r.style];im.src=r.review;im.alt=c.textContent+' | '+r.scene+' '+r.light+' '+r.view;im.loading='eager';a.href=r.review;a.target='_blank';a.append(im);f.append(c,a);$('grid').append(f)});
$('note').textContent=mode==='controlled'?'Identical geometry, placement, wear coverage, camera and lights. Color, painted marks and fine relief differ.':mode==='whole_style'?'Separate pilot: C/D also change rock planes and selected housing proportions. A/B retain the common forms. Not a texture-only comparison.':mode==='accepted_ground'?'Left: retained original B panel and grading. Right: phase-blended derivative. Both use aligned diagnostic coordinates.':mode==='material'?'Matching flat/bevelled and curved coupons. Same scale, orientation and neutral reflection lighting. IDs and row order are printed on each plate.':'Shared neutral light checks whether sunset/twilight are concealing differences.';
$('legend').textContent='B is the preferred-direction extension: old B ground and B–C rock/prop intent. It is not simply the old B atlas applied to all new materials. G02/G03, new metals and stone families remain proposals.';document.body.dataset.frames=selected.length;}
document.querySelectorAll('select').forEach(s=>s.addEventListener('change',update));update();</script></html>'''
page=page.replace('__DATA__',data).replace('__NAMES__',json.dumps(styles))
(HERE/'comparison.html').write_text(page,encoding='utf-8')
print(json.dumps({'frames':len(rows),'matching_groups':len(checks),'review_bytes':sum(p.stat().st_size for p in IMAGES.glob('*.jpg')),'boards':len(list(BOARD.glob('*.jpg')))}))
