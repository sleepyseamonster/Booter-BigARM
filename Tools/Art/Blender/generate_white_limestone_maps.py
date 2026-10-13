"""Derive restrained material data from the owner-approved generated albedo.
The albedo is copied byte-for-byte. Relief is an artistic interpretation, not measured data.
Run with the local Python environment: SCRIPT CHECKOUT_ROOT APPROVED_PREVIEW.
"""
import hashlib, json, shutil, sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

root=Path(sys.argv[1]).resolve(); preview=Path(sys.argv[2]).resolve()
base=root/'SourceArt/Blender/Studies/BooterGroundSetup/LimestoneRock01/ApprovedWhiteV2'
source=base/'SourceTextures'
if base.exists(): raise FileExistsError('Preserve the existing approved material version before rebuilding')
source.mkdir(parents=True)
albedo=source/'WhiteLimestone_ApprovedAlbedo.png'
shutil.copy2(preview,albedo)
image=Image.open(albedo).convert('RGB')
rgb=np.asarray(image,dtype=np.float32)/255
lum=rgb@np.array([.2126,.7152,.0722],dtype=np.float32)
gray=Image.fromarray(np.round(lum*255).astype(np.uint8))
blur=np.asarray(gray.filter(ImageFilter.GaussianBlur(6)),dtype=np.float32)/255
relief=lum-blur
scale=max(float(np.percentile(np.abs(relief),98)),.005)
height=np.clip(.5+.25*relief/scale,.1,.9)
# Fine pores are gently rougher than the worn raised mineral grain.
rough=np.clip(.80+.10*(.5-height),.74,.86)
ao=np.clip(1-.12*np.clip((.5-height)*2,0,1),.88,1)
Image.fromarray(np.round(height*65535).astype(np.uint16)).save(source/'WhiteLimestone_Height16.png')
for name,array in [('Roughness',rough),('Occlusion',ao)]:
    Image.fromarray(np.round(array*255).astype(np.uint8)).save(source/f'WhiteLimestone_{name}.png')
report=dict(approved_preview=str(preview),copied_albedo_sha256=hashlib.sha256(albedo.read_bytes()).hexdigest(),
            original_preview_sha256=hashlib.sha256(preview.read_bytes()).hexdigest(),source_dimensions=list(image.size),
            method='Approved albedo retained exactly; small-scale luminance deviation supplies interpreted shallow grit and pore relief, not scan measurements.',
            roughness_range=[float(rough.min()),float(rough.max())],occlusion_range=[float(ao.min()),float(ao.max())],
            height_amplitude_metres=.0006,projection_metres_per_tile=1.1,
            generated_with='Built-in image_gen, approved in this chat on October 12, 2026',
            prompt="Use case: photorealistic-natural. Asset type: preview-only game material texture swatch, single square image. Generate a fresh white limestone surface texture, seen perfectly straight-on, filling the entire frame edge to edge. The user's direction is 'gritty, gritty yet smooth limestone white': dense very fine mineral grit and tiny shallow pores give tactile sandy microtexture, while the larger surface remains smooth, softly weather-worn, gently rounded and calm. Chalky limestone white and very pale warm gray, predominantly white with restrained subtle ivory mineral variation. The grit must be visible up close and irregular, not a uniform dotted pattern; the broad surface should feel smooth, matte and solid stone. Natural realistic limestone, not concrete, stucco or plaster. Only sparse hairline mineral traces; no prominent cracks, no deep holes, no chunky fragments, no pronounced strata, no rippled or heavily wrinkled rock pattern. Flat, even diffuse neutral illumination suitable for an albedo concept; no directional shading, no glossy highlights, no vignette or perspective. Keep brightness and texture density consistent across the image; make edges visually compatible for eventual tiling. Produce only this one high-resolution texture preview, not a PBR map set, not a rendered rock, not a texture sheet montage. No objects, lettering, border or watermark.")
(base/'source_provenance.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
print(json.dumps({k:v for k,v in report.items() if k!='prompt'},indent=2))
