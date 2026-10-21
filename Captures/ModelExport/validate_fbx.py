import bpy,json,os
BASE=os.path.dirname(os.path.abspath(__file__))
with open(os.path.join(BASE,'geometry.json'),encoding='utf-8-sig') as f: models=json.load(f)['models']
report=[]
for model in models:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=os.path.join(BASE,'FBX',model['name']+'.fbx'),use_custom_normals=True)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    expected=sum(len(p['triangles'])//3 for p in model['parts'])
    actual=sum(len(o.data.polygons) for o in meshes)
    assert len(meshes)==len(model['parts']),(model['name'],'part count')
    assert expected==actual,(model['name'],'triangle count',expected,actual)
    assert all(len(o.data.materials)==0 for o in meshes),(model['name'],'unexpected materials')
    report.append({'model':model['name'],'parts':len(meshes),'triangles':actual,'materials':0})
with open(os.path.join(BASE,'validation.json'),'w') as f:json.dump(report,f,indent=2)
print('VALIDATED',len(report),'FBX files; geometry counts match, zero material slots')
