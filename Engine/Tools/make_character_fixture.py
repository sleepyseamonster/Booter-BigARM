"""Create our own neutral skinned calibration proxy. No external art or game canon."""
import json
import math
from pathlib import Path
import struct
from common import ROOT


def build():
    data=bytearray();views=[];accessors=[]
    def accessor(values,kind,components,code=5126):
        while len(data)%4:data.append(0)
        offset=len(data);fmt={5126:'f',5123:'H',5125:'I'}[code]
        flat=[x for value in values for x in (value if isinstance(value,(list,tuple)) else [value])]
        data.extend(struct.pack('<'+fmt*len(flat),*flat));views.append({'buffer':0,'byteOffset':offset,'byteLength':len(data)-offset})
        a={'bufferView':len(views)-1,'componentType':code,'count':len(values),'type':kind}
        if kind=='VEC3':a.update(min=[min(v[i] for v in values) for i in range(3)],max=[max(v[i] for v in values) for i in range(3)])
        accessors.append(a);return len(accessors)-1
    nodes=[{'name':'scene','children':[1,2]},{'name':'proxy_mesh','mesh':0,'skin':0},
        {'name':'hips','translation':[0,.95,0],'children':[3,7,8]},
        {'name':'torso','translation':[0,.25,0],'children':[4,5,6]},
        {'name':'head','translation':[0,.4,0]},
        {'name':'arm_left','translation':[-.35,.25,0]},
        {'name':'arm_right','translation':[.35,.25,0]},
        {'name':'leg_left','translation':[-.13,-.05,0]},
        {'name':'leg_right','translation':[.13,-.05,0]}]
    joints=list(range(2,9));centers=[[0,.95,0],[0,1.2,0],[0,1.6,0],[-.35,1.45,0],[.35,1.45,0],[-.13,.9,0],[.13,.9,0]]
    parts=[([.18,.12,.12],[0,0,0]),([.25,.25,.13],[0,.05,0]),([.15,.18,.15],[0,.02,0]),
           ([.09,.28,.09],[0,-.25,0]),([.09,.28,.09],[0,-.25,0]),([.1,.45,.12],[0,-.45,0]),([.1,.45,.12],[0,-.45,0])]
    positions=[];normals=[];uvs=[];tangents=[];weights=[];joint_values=[];indices=[]
    # Outward CCW quads, with explicit +Y tangent-space orientation.
    faces=[([0,0,1],[1,0,0]),([0,0,-1],[-1,0,0]),([1,0,0],[0,0,-1]),([-1,0,0],[0,0,1]),([0,1,0],[1,0,0]),([0,-1,0],[1,0,0])]
    for joint,(half,offset) in enumerate(parts):
        center=[centers[joint][i]+offset[i] for i in range(3)]
        for normal,tangent in faces:
            bitangent=[normal[1]*tangent[2]-normal[2]*tangent[1],normal[2]*tangent[0]-normal[0]*tangent[2],normal[0]*tangent[1]-normal[1]*tangent[0]]
            start=len(positions)
            for u,v in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                p=[center[i]+half[i]*(normal[i]+tangent[i]*u+bitangent[i]*v) for i in range(3)]
                positions.append(p);normals.append(normal);uvs.append([(u+1)/2,(v+1)/2]);tangents.append(tangent+[1])
                # Blend the lower torso into the hip to exercise multiple influences.
                blend=.3 if joint==1 and p[1]<1.2 else 0
                weights.append([1-blend,blend,0,0]);joint_values.append([joint,0,0,0])
            indices.extend([start,start+1,start+2,start,start+2,start+3])
    attributes={name:accessor(values,kind,components,code) for name,values,kind,components,code in [
        ('POSITION',positions,'VEC3',3,5126),('NORMAL',normals,'VEC3',3,5126),('TEXCOORD_0',uvs,'VEC2',2,5126),
        ('TANGENT',tangents,'VEC4',4,5126),('JOINTS_0',joint_values,'VEC4',4,5123),('WEIGHTS_0',weights,'VEC4',4,5126)]}
    mesh_indices=accessor(indices,'SCALAR',1,5123)
    inverse=[]
    for x,y,z in centers:inverse.append([1,0,0,0,0,1,0,0,0,0,1,0,-x,-y,-z,1])
    inverse_accessor=accessor(inverse,'MAT4',16)
    animations=[]
    for name,duration,targets in [('idle',2,[(3,.05,2)]),('walk',1,[(7,.65,0),(8,-.65,0),(5,-.5,0),(6,.5,0)])]:
        samplers=[];channels=[];times=[0,duration*.25,duration*.5,duration*.75,duration]
        time_accessor=accessor(times,'SCALAR',1)
        for node,amplitude,axis in targets:
            rotations=[]
            for i in range(5):
                angle=amplitude*math.sin(i*math.pi/2);q=[0,0,0,math.cos(angle/2)];q[axis]=math.sin(angle/2);rotations.append(q)
            samplers.append({'input':time_accessor,'output':accessor(rotations,'VEC4',4),'interpolation':'LINEAR'})
            channels.append({'sampler':len(samplers)-1,'target':{'node':node,'path':'rotation'}})
        animations.append({'name':name,'samplers':samplers,'channels':channels})
    asset={'asset':{'version':'2.0','generator':'Booter & BigARM engine calibration fixture'},'scene':0,'scenes':[{'nodes':[0]}],
        'nodes':nodes,'meshes':[{'primitives':[{'attributes':attributes,'indices':mesh_indices,'material':0}]}],
        'skins':[{'joints':joints,'inverseBindMatrices':inverse_accessor,'skeleton':2}],
        'materials':[{'pbrMetallicRoughness':{'baseColorFactor':[.14,.4,.65,1],'metallicFactor':0,'roughnessFactor':.75}}],
        'animations':animations,'buffers':[{'byteLength':len(data)}],'bufferViews':views,'accessors':accessors}
    doc=json.dumps(asset,separators=(',',':')).encode();doc+=b' '*((-len(doc))%4);data.extend(b'\0'*((-len(data))%4))
    glb=struct.pack('<III',0x46546c67,2,12+8+len(doc)+8+len(data))+struct.pack('<II',len(doc),0x4e4f534a)+doc+struct.pack('<II',len(data),0x004e4942)+data
    out=ROOT/'Assets/Models/Calibration';out.mkdir(parents=True,exist_ok=True);(out/'character.glb').write_bytes(glb)
    print('Wrote',out/'character.glb',len(glb),'bytes')

if __name__=='__main__':build()
