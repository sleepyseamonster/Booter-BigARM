"""Append the verified new rock to the open study, preserving existing objects.

Run via runpy.run_path(..., run_name='__main__') in the live Blender console.
Does not save; inspect the addition before saving the same working file.
"""
from pathlib import Path
import math

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
NAME='ReferenceRock_FracturedSlate_01'


def apply():
    terrain=bpy.data.objects.get('Badlands terrain - study only')
    if terrain is None:
        raise RuntimeError('Open the badlands study before adding this rock')
    if NAME in bpy.data.objects:
        raise RuntimeError('Reference rock is already present; refusing a duplicate')
    if bpy.context.mode!='OBJECT':
        raise RuntimeError('Finish the current edit operation before adding the rock')
    x,y=0.0,-6.0
    origin=terrain.matrix_world.inverted() @ Vector((x,y,100.0))
    direction=(terrain.matrix_world.inverted().to_3x3() @ Vector((0,0,-1))).normalized()
    hit,point,normal,index=terrain.ray_cast(origin,direction)
    if not hit:
        raise RuntimeError('No ground at the planned review placement')
    z=(terrain.matrix_world @ point).z-.045
    with bpy.data.libraries.load(str(ROOT/'ReferenceSlate.blend'),link=False) as (source,target):
        if NAME not in source.objects:raise RuntimeError('Source rock is missing')
        target.objects=[NAME]
    rock=target.objects[0]
    collection=bpy.data.collections.new('Reference rock - low polygon study')
    bpy.context.scene.collection.children.link(collection)
    collection.objects.link(rock)
    rock.location=(x,y,z)
    rock.rotation_euler=(0,0,-.15)
    # Bury the closed underside beneath the lowest sampled footprint contact.
    contact_heights=[]
    for vertex in rock.data.vertices:
        if vertex.co.z>.01:continue
        px=x+math.cos(-.15)*vertex.co.x-math.sin(-.15)*vertex.co.y
        py=y+math.sin(-.15)*vertex.co.x+math.cos(-.15)*vertex.co.y
        local_origin=terrain.matrix_world.inverted() @ Vector((px,py,100))
        found,contact,_,_=terrain.ray_cast(local_origin,direction)
        if found:contact_heights.append((terrain.matrix_world @ contact).z)
    if contact_heights:rock.location.z=min(contact_heights)-.035
    rock['reference_rock_added_2026_10_01']=True
    for obj in bpy.context.selected_objects:obj.select_set(False)
    rock.select_set(True);bpy.context.view_layer.objects.active=rock
    for screen in bpy.data.screens:
        for area in screen.areas:
            for space in area.spaces:
                if space.type=='VIEW_3D':
                    region=space.region_3d
                    if region:
                        region.view_location=rock.location+Vector((0,0,1.55))
                        region.view_distance=7.5
                        region.view_rotation=Vector((1,-1.7,.65)).to_track_quat('Z','Y')
                        region.view_perspective='PERSP'
    print('ADDED REFERENCE ROCK',NAME,'triangles',sum(len(p.vertices)-2 for p in rock.data.polygons),'location',tuple(rock.location))


if __name__=='__main__':
    apply()
