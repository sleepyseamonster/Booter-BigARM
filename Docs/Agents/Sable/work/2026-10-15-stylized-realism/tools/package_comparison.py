"""Validate render receipts and lay out unchanged renders for review.

Uses Pillow only for report layout, uniform resizing, and diagnostic grayscale;
all creative texture changes are authored upstream, never as image postfilters.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = Path(__file__).resolve().parent.parent
receipt = json.loads((HERE / 'render_receipt.json').read_text(encoding='utf-8'))
assert len(receipt['renders']) == 24
for row in receipt['renders']:
    path = HERE / 'renders' / row['file']
    assert hashlib.sha256(path.read_bytes()).hexdigest() == row['sha256'], path
    assert row['geometry_verified']
    with Image.open(path) as img:
        assert list(img.size) == receipt['resolution']
for lighting in ['sunset','twilight']:
    for view in ['close','gameplay','orbit','prop']:
        group = [r for r in receipt['renders'] if r['light']==lighting and r['view']==view]
        assert {r['variant'] for r in group} == {'A','B','C'}
        for key in ['camera','pitch','yaw','distance','sun_energy','sun_color','ambient','fill_energy']:
            assert all(r[key] == group[0][key] for r in group), key

font_dir = Path('C:/Windows/Fonts')
font = ImageFont.truetype(str(font_dir / 'segoeui.ttf'), 21)
title = ImageFont.truetype(str(font_dir / 'segoeuib.ttf'), 29)
small = ImageFont.truetype(str(font_dir / 'segoeui.ttf'), 17)
labels = [('A','Realistic baseline'),('B','Subtle painted'),('C','More painterly')]
boards = HERE / 'boards'
boards.mkdir(exist_ok=True)
for lighting in ['sunset','twilight']:
    for view in ['close','gameplay','orbit','prop']:
        board = Image.new('RGB', (2160, 680), '#16191d')
        d = ImageDraw.Draw(board)
        d.text((22,16), 'SABLE  /  STYLIZED REALISM', font=title, fill='#ece5db')
        d.text((22,57), f'{lighting.title()} light  /  {view.title()} view  /  Same geometry, camera and lighting across A, B and C',font=small,fill='#b9b7b3')
        for col,(key,label) in enumerate(labels):
            x=col*720
            d.text((x+16,95), f'{key}  {label}', font=font, fill='#edc9a0')
            with Image.open(HERE / 'renders' / f'{lighting}_{view}_{key}.png') as img:
                thumb=img.convert('RGB').resize((720,540),Image.Resampling.LANCZOS)
            board.paste(thumb,(x,130))
        board.save(boards / f'{lighting}_{view}.png')
        if lighting=='sunset' and view=='close':
            # Analytical companion; never used as a texture or substituted for color approval.
            ImageOps.grayscale(board).save(boards/'sunset_close_grayscale.png')

groups=[]
for lighting in ['sunset','twilight']:
    for view in ['close','gameplay','orbit','prop']:
        cards=''.join(f'<article><h2>{key} · {label}</h2><a href="renders/{lighting}_{view}_{key}.png"><img src="renders/{lighting}_{view}_{key}.png" alt="{label}, {lighting}, {view}"></a></article>' for key,label in labels)
        groups.append(f'<section data-light="{lighting}" data-view="{view}" hidden>{cards}</section>')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Sable stylized realism comparison</title><style>
*{box-sizing:border-box}body{margin:0;background:#15191e;color:#eae5dd;font:16px/1.5 system-ui,sans-serif;padding:30px}header{max-width:1100px}h1{font-size:32px;margin:0}p{color:#bfc2c7}nav{display:flex;gap:20px;flex-wrap:wrap;margin:24px 0}label{display:flex;gap:10px;align-items:center}select{font:inherit;background:#252c35;color:white;border:1px solid #59616a;border-radius:5px;padding:8px}section{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px}section[hidden]{display:none}article{background:#20262e;border-radius:7px;overflow:hidden}h2{font-size:18px;padding:6px 16px;color:#e7c3a0}img{display:block;width:100%;height:auto}a{color:#e7c3a0}footer{max-width:1050px;margin-top:25px}@media(max-width:850px){section{grid-template-columns:1fr}body{padding:15px}}
</style><header><h1>Stylized realism comparison</h1><p>The same basalt rock, dry ground and salvage canister. Geometry, placement, lighting and camera match within each set. Click any image for its full resolution render.</p></header>
<nav><label>Lighting <select id="lighting"><option value="sunset">Brighter sunset</option><option value="twilight">Deeper twilight</option></select></label><label>View <select id="view"><option value="close">Close material review</option><option value="gameplay">Gameplay framing · 22 m / 50°</option><option value="orbit">Alternate orbit · 26°</option><option value="prop">Canister material detail</option></select></label></nav>
'''+''.join(groups)+'''<footer><p>Offline Blender Cycles renders. B and C use AI-authored painted albedo drafts, with reduced fine normal detail. These are look-development candidates, not final manually painted assets or Unity screenshots. The canister is a study prop, not a canon design.</p><p><a href="STUDY_RESULTS.md">Review notes and limitations</a> · <a href="render_receipt.json">Render settings and preservation receipt</a> · <a href="boards/sunset_close_grayscale.png">Grayscale companion</a></p></footer>
<script>const l=document.getElementById('lighting'),v=document.getElementById('view');function show(){document.querySelectorAll('section').forEach(s=>s.hidden=s.dataset.light!==l.value||s.dataset.view!==v.value)}l.addEventListener('change',show);v.addEventListener('change',show);show();</script></html>'''
(HERE/'comparison.html').write_text(html,encoding='utf-8',newline='\n')
for text_path in HERE.rglob('*.json'):
    text_path.write_text(text_path.read_text(encoding='utf-8'),encoding='utf-8',newline='\n')
print('PASS: 24 render hashes and dimensions; geometry checks; 8 matching A/B/C groups. Packaged 9 boards and HTML review.')
