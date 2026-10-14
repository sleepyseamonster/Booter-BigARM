"""Derive technical PBR channels from the already-approved limestone source; no new art."""
from pathlib import Path
import shutil,sys
import numpy as np
from PIL import Image
root=Path(sys.argv[1]).resolve()
src=root/'SourceArt/Blender/Studies/BooterGroundSetup/LimestoneRock01/ApprovedWhiteV2/SourceTextures'
dst=root/'Assets/_Project/Art/Environment/Ground/FlatStoneGroundPatch01'
prefix='FlatStoneGroundPatch01_Limestone'
shutil.copy2(src/'WhiteLimestone_ApprovedAlbedo.png',dst/(prefix+'_BaseColor.png'))
height=np.asarray(Image.open(src/'WhiteLimestone_Height16.png'),dtype=np.float32)/65535
gy,gx=np.gradient(height,1.1/height.shape[0])
normals=np.stack([-gx*.0006,gy*.0006,np.ones_like(height)],axis=-1)
normals/=np.linalg.norm(normals,axis=-1,keepdims=True)
Image.fromarray(np.uint8(np.clip((normals+1)*127.5,0,255)),'RGB').save(dst/(prefix+'_Normal.png'))
rough=np.asarray(Image.open(src/'WhiteLimestone_Roughness.png').convert('L'))
packed=np.zeros((*rough.shape,4),dtype=np.uint8);packed[:,:,3]=255-rough
Image.fromarray(packed,'RGBA').save(dst/(prefix+'_MetallicSmoothness.png'))
