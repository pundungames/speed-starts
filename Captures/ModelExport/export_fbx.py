import bpy, json, os, csv, sys
from mathutils import Vector
BASE = os.path.dirname(os.path.abspath(__file__))
with open(os.path.join(BASE,'geometry.json'),encoding='utf-8-sig') as f:
    data=json.load(f)
OUT=os.path.join(BASE,'FBX')
os.makedirs(OUT,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1.0
rows=[]
for model in data['models']:
    if '--quick' in sys.argv and model['name'] not in ('StreetProps/CardboardParcel','Landmarks/Hospital'): continue
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    root=bpy.data.objects.new(model['name'].split('/')[-1],None)
    bpy.context.collection.objects.link(root)
    vertices=triangles=0
    for index,part in enumerate(model['parts']):
        # Unity Y-up to Blender Z-up. Reverse triangle order for the handedness change.
        coords=[(-v['x'],-v['z'],v['y']) for v in part['vertices']]
        tri=part['triangles']
        faces=[(tri[i],tri[i+2],tri[i+1]) for i in range(0,len(tri),3)]
        # Opposite faces used by mesh lettering need separate vertices in FBX.
        # Otherwise Blender's FBX importer removes the coincident back face.
        remap=list(range(len(coords)))
        seen=set()
        separated=[]
        for face in faces:
            key=tuple(sorted(face))
            if key in seen:
                newface=[]
                for vi in face:
                    newface.append(len(coords));coords.append(coords[vi]);remap.append(vi)
                separated.append(tuple(newface))
            else:
                seen.add(key);separated.append(face)
        faces=separated
        mesh=bpy.data.meshes.new('Mesh_%04d'%index)
        mesh.from_pydata(coords,[],faces)
        mesh.update()
        original_uv=part.get('uv',[])
        uv=[original_uv[i] for i in remap] if len(original_uv)==len(part['vertices']) else []
        if len(uv)==len(coords):
            layer=mesh.uv_layers.new(name='UVMap')
            for poly in mesh.polygons:
                for li in poly.loop_indices:
                    entry=uv[mesh.loops[li].vertex_index]
                    layer.data[li].uv=(entry['x'],entry['y'])
        original_normals=part.get('normals',[])
        normals=[original_normals[i] for i in remap] if len(original_normals)==len(part['vertices']) else []
        if len(normals)==len(coords):
            converted=[(-n['x'],-n['z'],n['y']) for n in normals]
            mesh.normals_split_custom_set_from_vertices(converted)
        obj=bpy.data.objects.new(part['name'].split('/')[-1]+'_%04d'%index,mesh)
        bpy.context.collection.objects.link(obj)
        obj.parent=None
        obj.select_set(True)
        vertices+=len(coords);triangles+=len(faces)
    root.select_set(True)
    bpy.context.view_layer.objects.active=root
    path=os.path.join(OUT,model['name']+'.fbx')
    os.makedirs(os.path.dirname(path),exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'MESH'},
        use_mesh_modifiers=False,mesh_smooth_type='OFF',use_custom_props=False,
        add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',
        global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True,path_mode='AUTO',embed_textures=False)
    rows.append([model['name']+'.fbx',model.get('source',''),len(model['parts']),vertices,triangles,model.get('excluded2D',0)])
    print('EXPORTED',model['name'],len(model['parts']),flush=True)
with open(os.path.join(OUT,'ModelList.csv'),'w',newline='',encoding='utf-8-sig') as f:
    writer=csv.writer(f);writer.writerow(['FBX','UnitySource','MeshParts','Vertices','Triangles','Excluded2DParts']);writer.writerows(rows)
print('COMPLETE',len(rows),'FBX models',flush=True)




