import bpy,json,pathlib
root=pathlib.Path(__file__).resolve().parents[1]/'Deliverables/Maps'
results=[]
for path in sorted(root.glob('*/*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    expected=sum(item['meshes'] for item in json.loads((path.parent/'asset-index.json').read_text(encoding='utf8')))
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    missing_materials=[obj.name for obj in meshes if not obj.data.materials]
    unpacked=[img.name for img in bpy.data.images if img.source=='FILE' and not img.packed_file]
    assert len(meshes)==expected,(path,len(meshes),expected)
    assert not missing_materials,(path,missing_materials)
    assert not unpacked,(path,unpacked)
    results.append(dict(file=str(path.relative_to(root)),meshes=len(meshes),materials=len(bpy.data.materials),packed_images=len([img for img in bpy.data.images if img.packed_file]),status='passed'))
    print('VERIFIED_BLEND',path.name,flush=True)
(root/'blend-validation-report.json').write_text(json.dumps(results,indent=2),encoding='utf8')
print('VERIFIED_ALL_BLEND',len(results),flush=True)
