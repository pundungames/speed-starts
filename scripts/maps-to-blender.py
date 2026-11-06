"""Run with Blender --background --python scripts/maps-to-blender.py."""
import bpy, bmesh, json, math, pathlib, collections, hashlib, sys
from mathutils import Vector

ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Deliverables/Maps'
SOURCE=OUT/'_source'
records=collections.defaultdict(list)
for p in sorted(SOURCE.glob('*.json')):
    d=json.loads(p.read_text(encoding='utf-8')); records[d['theme']].append(d)
selected_theme=next((arg.split('=',1)[1] for arg in sys.argv if arg.startswith('--theme=')),None)
report=json.loads((OUT/'export-report.json').read_text()) if selected_theme and (OUT/'export-report.json').exists() else []

def surface(s):
    key=s.get('source') or s['name']
    if key in materials: return materials[key]
    m=bpy.data.materials.new(s['name']); m.use_nodes=True
    c=s['color']; rgba=(c['r'],c['g'],c['b'],c['a'])
    m.diffuse_color=rgba
    shader=m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=rgba
    shader.inputs['Roughness'].default_value=max(0,min(1,s['roughness']))
    shader.inputs['Metallic'].default_value=max(0,min(1,s['metallic']))
    if s.get('texture'):
        path=SOURCE/s['texture']
        if path.exists():
            image=bpy.data.images.load(str(path),check_existing=True);image.pack()
            tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
            m.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
            m.node_tree.links.new(tex.outputs['Alpha'],shader.inputs['Alpha'])
    m['Unity Shader']=s.get('shader',''); m['Unity Source']=s.get('source','')
    materials[key]=m;return m

def create_part(p,collection,index):
    verts=[]; normals=[]; uvs=[]; colors=[]; remap=[]; lookup={}
    for i,v in enumerate(p['vertices']):
        n=p['normals'][i] if len(p['normals'])==len(p['vertices']) else {'x':0,'y':1,'z':0}
        uv=p['uv'][i] if len(p['uv'])==len(p['vertices']) else {'x':0,'y':0}
        col=p['colors'][i] if len(p['colors'])==len(p['vertices']) else {'r':1,'g':1,'b':1,'a':1}
        # Unity left-handed Y-up to Blender right-handed Z-up.
        co=(v['x'],v['z'],v['y']); no=(n['x'],n['z'],n['y']); tex=(uv['x'],uv['y']); rgba=tuple(col.get(k,1) for k in 'rgba')
        # Generated Unity triangles often differ only in the last float bits.
        # Micrometre position and small normal tolerances recover their shared
        # corners while retaining intentional UV, paint and hard-edge splits.
        key=tuple(round(x,6) for x in co)+tuple(round(x,5) for x in no)+tuple(round(x,6) for x in tex+rgba)
        if key not in lookup:
            lookup[key]=len(verts);verts.append(co);normals.append(no);uvs.append(tex);colors.append(rgba)
        remap.append(lookup[key])
    t=p['triangles'];faces=[];face_materials=[];seen_faces=set()
    for i in range(0,len(t),3):
        face=(remap[t[i]],remap[t[i+2]],remap[t[i+1]])
        coordinates=[tuple(round(x,6) for x in verts[j]) for j in face]
        mi=p['materials'][i//3]
        signature=(tuple(sorted(coordinates)),mi)
        if len(set(coordinates))<3 or signature in seen_faces:continue
        a,b,c=map(Vector,coordinates)
        if (b-a).cross(c-a).length_squared<1e-18:continue
        seen_faces.add(signature);faces.append(face);face_materials.append(mi)
    name=p['name'].split('/')[-1] or 'Mesh'
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj)
    for s in p['surfaces']:mesh.materials.append(surface(s))
    if not mesh.materials:mesh.materials.append(surface({'name':'Default','color':dict(r=1,g=1,b=1,a=1),'roughness':.8,'metallic':0}))
    uv=mesh.uv_layers.new(name='UVMap')
    color=mesh.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
    for face,mi in zip(mesh.polygons,face_materials):face.material_index=min(mi,max(0,len(mesh.materials)-1))
    for loop in mesh.loops:uv.data[loop.index].uv=uvs[loop.vertex_index];color.data[loop.index].color=colors[loop.vertex_index]
    # Keep unusual degenerate/overlapping source triangles verbatim.
    can_join=all(len(set(f))==3 for f in faces) and len({tuple(sorted(f)) for f in faces})==len(faces)
    if can_join:
        bm=bmesh.new();bm.from_mesh(mesh)
        bmesh.ops.join_triangles(bm,faces=list(bm.faces),angle_face_threshold=.0001,angle_shape_threshold=math.pi,cmp_seam=True,cmp_sharp=True,cmp_uvs=True,cmp_vcols=True,cmp_materials=True)
        bm.to_mesh(mesh);bm.free();mesh.update()
    assert sum(len(f.vertices)-2 for f in mesh.polygons)==len(faces), f'Topology changed: {name}'
    mesh.validate(verbose=False,clean_customdata=False)
    mesh.update()
    # Blender 5.1 can crash in its custom-normal setter after BMesh face joins.
    # Split vertices already preserve the source normal boundaries; compute
    # shading within those boundaries and retain source vectors for editing.
    for polygon in mesh.polygons:polygon.use_smooth=True
    if len(mesh.vertices)==len(normals):
        normal_attribute=mesh.attributes.new(name='UnityNormals',type='FLOAT_VECTOR',domain='POINT')
        for item,n in zip(normal_attribute.data,normals):item.vector=n
    obj['Unity Path']=p['name']
    return obj,len(t)//3,sum(len(f.vertices)==4 for f in mesh.polygons)

for theme,models in sorted(records.items()):
    if selected_theme and theme!=selected_theme:continue
    models.sort(key=lambda model:model['name'].casefold())
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0;materials={}
    folder=OUT/theme; (folder/'FBX').mkdir(parents=True,exist_ok=True)
    theme_report={'theme':theme,'models':len(models),'parts':0,'source_triangles':0,'quads':0,'remaining_triangles':0,'removed_degenerate_or_duplicate_triangles':0}
    index_records=[];used_names=set()
    for index,d in enumerate(models):
        coll=bpy.data.collections.new(d['name']);bpy.context.scene.collection.children.link(coll)
        root=bpy.data.objects.new(d['name'],None);coll.objects.link(root);root['Unity Source']=d['source']
        objects=[root]
        for p in d['parts']:
            obj,tris,quads=create_part(p,coll,index);obj.parent=root;objects.append(obj)
            theme_report['parts']+=1;theme_report['source_triangles']+=tris;theme_report['quads']+=quads
            theme_report['remaining_triangles']+=sum(len(f.vertices)==3 for f in obj.data.polygons)
            theme_report['removed_degenerate_or_duplicate_triangles']+=tris-sum(len(f.vertices)-2 for f in obj.data.polygons)
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        safe=''.join(c if c not in '<>:"/\\|?*' else '_' for c in d['name'])
        if safe in used_names:safe+='_'+hashlib.sha256(d['source'].encode()).hexdigest()[:8]
        used_names.add(safe)
        bpy.ops.export_scene.fbx(filepath=str(folder/'FBX'/f'{safe}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=False,mesh_smooth_type='OFF',use_triangles=False,add_leaf_bones=False,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
        index_records.append({'name':d['name'],'source':d['source'],'fbx':f'FBX/{safe}.fbx','meshes':len(objects)-1,'triangles_equivalent':sum(len(f.vertices)-2 for o in objects if o.type=='MESH' for f in o.data.polygons),'source_triangles':sum(len(p['triangles'])//3 for p in d['parts'])})
        # Catalogue layout only: each individual FBX retains its original pivot.
        root.location=(index%8*65,index//8*65,0)
        print('EXPORTED',theme,d['name'],flush=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1
    # Save a useful starting view and a material-colour preview.
    points=[o.matrix_world @ Vector(c) for o in bpy.context.scene.objects if o.type=='MESH' for c in o.bound_box]
    bpy.context.view_layer.update()
    points=[o.matrix_world @ Vector(c) for o in bpy.context.scene.objects if o.type=='MESH' for c in o.bound_box]
    lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
    center=(lo+hi)/2;size=(hi-lo).length
    cam_data=bpy.data.cameras.new('Preview Camera');cam=bpy.data.objects.new('Preview Camera',cam_data);bpy.context.scene.collection.objects.link(cam)
    cam.location=center+Vector((.8,-1,.9)).normalized()*max(size,10);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam_data.type='ORTHO';cam_data.ortho_scale=max(size*1.1,10);cam_data.clip_end=max(size*5,1000)
    scene=bpy.context.scene;scene.camera=cam
    scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='MATERIAL';scene.display.shading.light='STUDIO';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.background_type='VIEWPORT';scene.display.shading.background_color=(.16,.18,.22)
    scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    scene.render.filepath=str(folder/f'{theme}_Preview.png')
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                area.spaces.active.region_3d.view_location=center;area.spaces.active.region_3d.view_distance=max(size*.7,10);area.spaces.active.clip_end=max(size*5,1000);area.spaces.active.shading.color_type='MATERIAL'
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/f'{theme}_AssetLibrary.blend'))
    (folder/'asset-index.json').write_text(json.dumps(index_records,indent=2,ensure_ascii=False),encoding='utf-8')
    report=[r for r in report if r['theme']!=theme];report.append(theme_report)
    (OUT/'export-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ALL MAP ASSETS EXPORTED',json.dumps(report),flush=True)
