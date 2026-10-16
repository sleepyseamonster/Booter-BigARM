"""Verify hashes and package captions/contact sheets; no texture repainting."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,sys
ROOT=Path(sys.argv[1]).resolve();HERE=Path(__file__).resolve().parent;OUT=ROOT/'outputs/Sable/MaterialDioramas20261015/OrganicLibrary'
r=json.loads((HERE/'receipt.json').read_text());assert r['complete'] and len(r['maps'])==25 and len(r['renders'])==15
assert hashlib.sha256((HERE/'build_library.py').read_bytes()).hexdigest()==r['script_sha256']
for row in r['maps']+r['renders']:assert hashlib.sha256((ROOT/row['file']).read_bytes()).hexdigest()==row['sha256']
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24);title=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',32)
names={'WornLeather':'Worn leather','Cloth':'Cloth (visual reference)','Bone':'Weathered bone','Chitin':'Layered chitin','AgedWood':'Aged wood (visual reference)'}
styles={'R':'Restrained realism','E':'Combined direction','P':'Broader painted accents'}
images=HERE/'images';images.mkdir(exist_ok=True);assets=[];unity=[]
def save(im,path):
    im.convert('RGB').save(path,quality=94,subsampling=0);assets.append({'file':str(path.relative_to(HERE)).replace('\\','/'),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
overview=Image.new('RGB',(2500,620),'#151d1f');od=ImageDraw.Draw(overview);od.text((25,15),'E / Combined direction — actual Unity URP materials',font=title,fill='#eee6d9')
for j,(f,label) in enumerate(names.items()):
    board=Image.new('RGB',(1920,760),'#151d1f');d=ImageDraw.Draw(board);d.text((25,18),label+' / Unity URP-Lit',font=title,fill='#eee6d9')
    d.text((25,63),'Shared normal, smoothness and cavity maps. Same geometry, lights and camera; color accents vary.',font=font,fill='#bdc7bf')
    native=Image.new('RGB',(1920,610),'#151d1f');nd=ImageDraw.Draw(native);nd.text((25,18),label+' / Blender flat, curved and context samples',font=title,fill='#eee6d9')
    for i,(s,sl) in enumerate(styles.items()):
        path=OUT/'Unity'/f'{f}_{s}.png';im=Image.open(path);assert im.size==(1024,1024)
        # Reject blank/error frames without mistaking the dark studio background for a material.
        import numpy as np
        a=np.array(im.convert('RGB'));assert a.std()>8;magenta=(a[:,:,0]>220)&(a[:,:,1]<40)&(a[:,:,2]>220);assert magenta.mean()<.01
        unity.append({'file':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
        save(im,images/f'Unity_{f}_{s}.jpg');board.paste(im.convert('RGB').resize((640,640),Image.Resampling.LANCZOS),(640*i,120));d.text((640*i+20,92),s+' / '+sl,font=font,fill='#e1b784')
        if s=='E':overview.paste(im.convert('RGB').resize((500,500),Image.Resampling.LANCZOS),(j*500,100));od.text((j*500+15,65),label,font=font,fill='#e1b784')
        im=Image.open(OUT/f'{f}_{s}.png');save(im,images/f'Blender_{f}_{s}.jpg');native.paste(im.resize((640,480),Image.Resampling.LANCZOS),(640*i,130));nd.text((640*i+20,95),s+' / '+sl,font=font,fill='#e1b784')
    save(board,HERE/f'{f}_comparison.jpg');save(native,HERE/f'{f}_forms.jpg')
    tile=Image.open(OUT/'Textures'/f'{f}_BaseColor_E.png').convert('RGB').resize((512,512),Image.Resampling.LANCZOS)
    repeat=Image.new('RGB',(1024,1024))
    for x in [0,512]:
        for y in [0,512]:repeat.paste(tile,(x,y))
    save(repeat,images/f'{f}_E_repeat.jpg')
save(overview,HERE/'E_overview.jpg')
report={'review_assets':assets,'unity_captures':unity,'source_hashes_verified':True,'primary_comparisons':15,'unity_render_frames':15,'blender_render_frames':15,'no_magenta_error_frames':True}
(HERE/'review_verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
page='<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Sable | Organic Material Library</title><style>body{max-width:1800px;margin:30px auto;padding:0 24px;background:#11191b;color:#eee5d8;font:17px system-ui}p{max-width:1000px;line-height:1.6;color:#bbc5bf}img{width:100%;height:auto}a{color:#e6ba84}h2{margin-top:55px}</style><h1>Organic material library</h1><p>Five materials, three treatments. E continues our combined direction. Unity captures show the actual imported URP/Lit materials. Blender samples add flat and context forms. Lore correction: wood and cloth are excluded from game use. Chitin is the primary usable family; hide is extremely scarce, and bone requires anatomy qualification. Incompatible samples are retained historical references.</p><a href="E_overview.jpg"><img src="E_overview.jpg" alt="Five combined-direction Unity materials"></a>'
for f,label in names.items():
    page+=f'<h2>{label}</h2><a href="{f}_comparison.jpg"><img src="{f}_comparison.jpg" alt="{label}: restrained, combined and broad accents in Unity"></a><p><a href="{f}_forms.jpg">Flat and context forms</a> · <a href="images/{f}_E_repeat.jpg">2 × 2 color tiling check</a></p>'
page+='<p><a href="README.md">Unity setup, art review and validation limits</a></p>'
(HERE/'review.html').write_text(page,encoding='utf-8',newline='\n')
print('PACKAGED',len(assets),'review images; 15 Unity and 15 Blender captures')
