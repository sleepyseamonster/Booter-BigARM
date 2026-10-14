"""Plot private calibrated motion samples. Outputs remain outside repository Assets."""
import argparse
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw

parser = argparse.ArgumentParser()
parser.add_argument("directory", type=Path)
args = parser.parse_args()
names = ["StandardWalk", "StandardRun", "TwoCycleSprint"]
def contact_windows(frames, side):
    """Find measured near-floor regions; merge a stance crossing the cycle boundary."""
    frames = frames[:-1]
    duration = frames[-1]['seconds'] + (frames[1]['seconds']-frames[0]['seconds'])
    values = [frame[side+'Sole']['y'] for frame in frames]
    dt=frames[1]['seconds']-frames[0]['seconds']
    neighbourhood=max(2,round(.12/dt))
    flags=[]
    for index,value in enumerate(values):
        previous=frames[(index-1)%len(frames)]
        following=frames[(index+1)%len(frames)]
        velocity_z=(following[side+'Foot']['z']-previous[side+'Foot']['z'])/(2*dt)
        velocity_y=(following[side+'Sole']['y']-previous[side+'Sole']['y'])/(2*dt)
        local_low=min(values[(index+step)%len(frames)] for step in range(-neighbourhood,neighbourhood+1))
        flags.append(velocity_z < -.2 and abs(velocity_y) < .8 and value <= local_low+.025)
    if all(flags):
        return []
    start = next(i for i,flag in enumerate(flags) if not flag)
    groups, current = [], []
    for step in range(1, len(frames)+1):
        index = (start+step) % len(frames)
        if flags[index]:
            current.append(index)
        elif current:
            groups.append(current)
            current=[]
    if current:
        groups.append(current)
    result=[]
    for group in groups:
        if len(group)*dt < .025:
            continue
        plant=min(group,key=lambda i:values[i])
        speeds=[]
        for index in group:
            previous=frames[(index-1)%len(frames)][side+'Foot']
            following=frames[(index+1)%len(frames)][side+'Foot']
            speeds.append(math.hypot(following['x']-previous['x'],following['z']-previous['z'])/(2*dt))
        result.append(dict(phase=frames[plant]['seconds']/duration,sole_y=values[plant],window_seconds=len(group)*dt,
                           stance_speed=sum(speeds)/len(speeds)))
    return sorted(result,key=lambda value:value['phase'])

def ankle_plants(frames, side, cycle_count):
    """Local ankle minima separated by a measured stride period, not a filled profile quota."""
    frames=frames[:-1]
    period=len(frames)/cycle_count
    radius=round(period*.35)
    candidates=[]
    values=[frame[side+'Foot']['y'] for frame in frames]
    for index,value in enumerate(values):
        if value<=min(values[(index+step)%len(frames)] for step in range(-radius,radius+1)):
            if not candidates or index-candidates[-1]>1:
                candidates.append(index)
    return [dict(phase=index/len(frames),ankle_y=values[index],sole_y=frames[index][side+'Sole']['y']) for index in candidates]

summary=[]
image=Image.new('RGB',(1200,780),'white')
draw=ImageDraw.Draw(image)
parts = ['<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="780" viewBox="0 0 1200 780">',
         '<rect width="1200" height="780" fill="white"/>',
         '<style>text{font-family:Arial,sans-serif;font-size:12px}</style>']
for row, name in enumerate(names):
    data = json.loads((args.directory / (name + ".json")).read_text())
    frames = data["frames"]
    summary.append(dict(clip=name,seconds=data['seconds'],foot_ik=data.get('footIK',False),
                        visual_y_correction=-.9-min(frame[side+'Sole']['y'] for frame in frames for side in ['left','right']),
                        left=contact_windows(frames,'left'),right=contact_windows(frames,'right'),
                        ankle_left=ankle_plants(frames,'left',2 if name=='TwoCycleSprint' else 1),
                        ankle_right=ankle_plants(frames,'right',2 if name=='TwoCycleSprint' else 1)))
    seconds = [frame["seconds"] for frame in frames]
    for col, side in enumerate(["left", "right"]):
        x0, y0 = 65 + col * 590, 40 + row * 255
        width, height = 500, 185
        def point(time, value):
            return x0 + time / data['seconds'] * width, y0 + (value + .2) / -.9 * height
        parts.append(f'<text x="{x0}" y="{y0-12}">{name} · {side}</text>')
        draw.text((x0,y0-17),name+' '+side,fill='black')
        for value in [-1.1, -.9, -.7, -.5, -.3]:
            _, y = point(0, value)
            parts.append(f'<line x1="{x0}" y1="{y}" x2="{x0+width}" y2="{y}" stroke="#ddd"/>')
            parts.append(f'<text x="{x0-45}" y="{y+4}">{value:.1f} m</text>')
            draw.line((x0,y,x0+width,y),fill='#dddddd')
            draw.text((x0-45,y-5),f'{value:.1f} m',fill='black')
        for joint, color in zip(["Foot", "Toes", "Sole"], ['#2274a5', '#ee6c4d', '#28834b']):
            key = side + joint
            if key in frames[0]:
                points = ' '.join(f'{x:.2f},{y:.2f}' for x,y in (point(frame['seconds'],frame[key]['y']) for frame in frames))
                parts.append(f'<polyline fill="none" stroke="{color}" stroke-width="1.5" points="{points}"/>')
                draw.line([point(frame['seconds'],frame[key]['y']) for frame in frames],fill=color,width=2)
                index = ["Foot", "Toes", "Sole"].index(joint)
                parts.append(f'<text x="{x0+index*100}" y="{y0+height+35}" fill="{color}">{joint}</text>')
                draw.text((x0+index*100,y0+height+30),joint,fill=color)
        parts.append(f'<text x="{x0}" y="{y0+height+18}">0 — {data["seconds"]:.3f} seconds; dashed plane is standing feet Y=-0.90 m</text>')
        _, plane = point(0, -.9)
        parts.append(f'<line x1="{x0}" y1="{plane}" x2="{x0+width}" y2="{plane}" stroke="black" stroke-dasharray="5 4"/>')
        draw.line((x0,plane,x0+width,plane),fill='black')
        draw.text((x0,y0+height+12),f'0 to {data["seconds"]:.3f} seconds; metres relative to physics root',fill='black')
parts.append('</svg>')
(args.directory / 'gait-heights.svg').write_text('\n'.join(parts), encoding='utf-8')
(args.directory / 'contact-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
image.save(args.directory/'gait-heights.png')
print(json.dumps(summary))
