"""Re-import every delivered FBX into a fresh Blender scene."""
import bpy,json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1]/'Deliverables/Maps'
results=[]
for index_file in sorted(ROOT.glob('*/asset-index.json')):
    for entry in json.loads(index_file.read_text(encoding='utf8')):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        path=index_file.parent/entry['fbx']
        bpy.ops.import_scene.fbx(filepath=str(path))
        meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
        triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
        quads=sum(len(p.vertices)==4 for o in meshes for p in o.data.polygons)
        missing_uv=[o.name for o in meshes if o.data.polygons and not o.data.uv_layers]
        no_material=[o.name for o in meshes if not o.data.materials]
        passed=len(meshes)==entry['meshes'] and triangles==entry['triangles_equivalent'] and not missing_uv and not no_material
        results.append({'file':str(path.relative_to(ROOT)),'meshes':len(meshes),'expected_meshes':entry['meshes'],'triangles_equivalent':triangles,'expected_triangles_equivalent':entry['triangles_equivalent'],'quads':quads,'missing_uv':missing_uv,'missing_materials':no_material,'status':'passed' if passed else 'failed'})
        print('VERIFIED' if passed else 'MISMATCH',path.name,triangles,entry['triangles_equivalent'],flush=True)
    (ROOT/'validation-report.json').write_text(json.dumps(results,indent=2),encoding='utf8')
print('VERIFIED_ALL_FBX',len(results),flush=True)
assert all(r['status']=='passed' for r in results),'FBX round-trip mismatches; see validation-report.json'
