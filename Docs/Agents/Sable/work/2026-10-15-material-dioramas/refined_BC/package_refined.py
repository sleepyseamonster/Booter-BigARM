"""Package render derivatives without repainting or recoloring them."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageChops,ImageStat
import json,hashlib,sys
ROOT=Path(sys.argv[1]).resolve();HERE=Path(__file__).resolve().parent
r=json.loads((HERE/'receipt.json').read_text());assert r['complete'] and len(r['renders'])==6
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24)
title=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',32)
assets=[]
for row in r['renders']:
    p=ROOT/row['file'];assert hashlib.sha256(p.read_bytes()).hexdigest()==row['sha256']
    out=HERE/(p.stem+'.jpg')
    with Image.open(p) as im:
        assert im.size==(2560,1440)
        if row['group']:
            canvas=Image.new('RGB',(2560,1600),'#15191c');canvas.paste(im,(0,76));d=ImageDraw.Draw(canvas)
            d.text((30,18),'E / Refined B-C / '+row['group'].upper()+' / neutral light',font=title,fill='#eee8dd')
            lines=['Back/top: blackened cast iron | partial rust | heavy rust','Front/bottom: processed metal | brushed metal | bare forged iron'] if row['group']=='metal' else ['Back/top: limestone | iron-rich rock','Front/bottom: dense basalt | porous volcanic stone | granite']
            for i,t in enumerate(lines):d.text((30,1520+i*32),t,font=font,fill='#ded0bd')
            canvas.save(out,quality=94,subsampling=0)
        else:im.save(out,quality=94,subsampling=0)
    assets.append({'file':out.name,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()})
for view in ['close','gameplay']:
    board=Image.new('RGB',(2880,680),'#15191c');d=ImageDraw.Draw(board)
    d.text((25,15),'B / REFINED B-C / C — same workyard, camera and sunset light',font=title,fill='#eee8dd')
    d.text((25,62),'Refined candidate keeps exact B ground and B material response, with selected C pigment accents.',font=font,fill='#cfc4b6')
    for i,(code,label) in enumerate([('B','B / painted realism'),('E','E / refined B-C'),('C','C / expressive painterly')]):
        path=ROOT/'outputs/Sable/MaterialDioramas20261015'/('refined_BC' if code=='E' else 'renders')/(f'yard_E_sunset_{view}.png' if code=='E' else f'yard_{code}_sunset_{view}_controlled.png')
        with Image.open(path) as im:board.paste(im.resize((960,540),Image.Resampling.LANCZOS),(i*960,140))
        d.text((i*960+20,103),label,font=font,fill='#dfb986')
    out=HERE/f'comparison_{view}.jpg';board.save(out,quality=94,subsampling=0);assets.append({'file':out.name,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()})
v=json.loads((HERE/'verification.json').read_text())
a=Image.open(ROOT/v['reopened_render']);b=Image.open(ROOT/r['renders'][0]['file']);delta=ImageChops.difference(a,b)
v['reopened_pixel_comparison']={'identical':delta.getbbox() is None,'mean_absolute_rgb':ImageStat.Stat(delta).mean,'max_channel_error':max(x[1] for x in delta.getextrema())}
assert v['reopened_pixel_comparison']['max_channel_error']<=1
v['review_assets']=assets
(HERE/'verification.json').write_text(json.dumps(v,indent=2),encoding='utf-8',newline='\n')
sections=[('Combined direction','yard_E_sunset_close.jpg'),('B / combined / C','comparison_close.jpg'),('Gameplay comparison','comparison_gameplay.jpg'),('Deeper twilight','yard_E_twilight_close.jpg'),('Neutral diagnostic','yard_E_neutral_gameplay.jpg'),('Metals','plates_E_neutral_plate_metal.jpg'),('Stone and ore','plates_E_neutral_plate_stone.jpg')]
page='<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Sable | Refined B-C</title><style>body{margin:30px auto;max-width:1500px;padding:0 20px;background:#11191b;color:#eee5d8;font:17px system-ui}p{max-width:900px;line-height:1.6;color:#bcc5bf}img{width:100%;height:auto}a{color:#e2b989}h2{margin-top:50px}</style><h1>Refined B-C workyard</h1><p>Exact B ground. Believable B material response. Broader C pigment accents on exposed rock and prop planes. Click images for full size. This is an offline Blender candidate for review.</p>'
for heading,path in sections:page+=f'<h2>{heading}</h2><a href="{path}"><img src="{path}" alt="{heading}"></a>'
page+='<p><a href="README.md">Review and provenance</a> · <a href="../comparison.html">Original four-treatment study</a></p>'
(HERE/'review.html').write_text(page,encoding='utf-8',newline='\n')
print('REFINED_PACKAGED',len(assets),v['reopened_pixel_comparison'])
