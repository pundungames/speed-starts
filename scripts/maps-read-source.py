"""Read Unity text assets without opening the Editor. Run with Blender Python."""
import sys,pathlib,re,json,struct,copy,functools,shutil
import bpy
from mathutils import Matrix,Vector,Quaternion
ROOT=pathlib.Path(__file__).resolve().parents[1];OUT=ROOT/'Deliverables/Maps/_source'
sys.path.insert(0,str(ROOT/'Deliverables/Maps/_tools'))
import yaml
OUT.mkdir(parents=True,exist_ok=True);(OUT/'Textures').mkdir(exist_ok=True)
GUIDS={}
for p in (ROOT/'Assets').rglob('*.meta'):
    m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf8',errors='replace'),re.M)
    if m:GUIDS[m[1]]=pathlib.Path(str(p)[:-5])
warnings=[]
fbx_cache={}

@functools.lru_cache(maxsize=4096)
def docs(path):
    text=pathlib.Path(path).read_text(encoding='utf8',errors='replace')
    text=re.sub(r'^%.*\n','',text,flags=re.M)
    text=re.sub(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?',lambda m:f'---\n__type: {m[1]}\n__id: {m[2]}',text,flags=re.M)
    text=re.sub(r'^(\s*(?:m_IndexBuffer|_typelessdata|m_BoneNameHashes):)\s*(\w*)\s*$',r'\1 "\2"',text,flags=re.M)
    return {d['__id']:d for d in yaml.load_all(text,Loader=yaml.CSafeLoader) if d and '__id' in d}

def body(d):return next(v for k,v in d.items() if not k.startswith('__'))
def ref(d):return d.get('fileID',0) if isinstance(d,dict) else 0
def vec(d,keys='xyz'):return tuple(float(d.get(k,0)) for k in keys)
def matrix(t):return Matrix.Translation(vec(t['m_LocalPosition'])) @ Quaternion((t['m_LocalRotation']['w'],*vec(t['m_LocalRotation']))).to_matrix().to_4x4() @ Matrix.Diagonal((*vec(t['m_LocalScale']),1))

@functools.lru_cache(maxsize=2048)
def material(guid):
    p=GUIDS.get(guid);c=dict(r=1,g=1,b=1,a=1)
    s=dict(name='Default',source='',shader='',texture='',color=c,metallic=0,roughness=.8)
    if not p:return s
    d=body(next(iter(docs(str(p)).values())));props=d.get('m_SavedProperties',{})
    colors={k:v for item in props.get('m_Colors',[]) for k,v in item.items()};floats={k:v for item in props.get('m_Floats',[]) for k,v in item.items()}
    s.update(name=d['m_Name'],source=p.relative_to(ROOT).as_posix(),color=colors.get('_Color',colors.get('_BaseColor',c)),metallic=floats.get('_Metallic',0),roughness=1-floats.get('_Glossiness',.2))
    shader=GUIDS.get(d.get('m_Shader',{}).get('guid'))
    if shader:
        match=re.search(r'Shader\s+"([^"]+)"',shader.read_text(encoding='utf8',errors='replace'));s['shader']=match[1] if match else str(shader.name)
    textures={k:v for item in props.get('m_TexEnvs',[]) for k,v in item.items()}
    tex=textures.get('_MainTex',{}).get('m_Texture',{}).get('guid');source=GUIDS.get(tex)
    if source and source.exists():
        s['texture']='Textures/'+tex+source.suffix;target=OUT/s['texture']
        if not target.exists():shutil.copyfile(source,target)
    return s

@functools.lru_cache(maxsize=2048)
def meshdata(guid,fileid,meshname=''):
    p=GUIDS.get(guid)
    if p and p.suffix=='.asset':
        ds=docs(str(p));d=body(ds.get(fileid,next(iter(ds.values()))));vd=d.get('m_VertexData')
        if not vd:raise ValueError('No vertex data '+str(p))
        channels=vd['m_Channels'];data=bytes.fromhex(vd['_typelessdata']);count=vd['m_VertexCount'];strides={};offsets={}
        formats={0:('f',4,1),1:('e',2,1),2:('B',1,255),3:('b',1,127),4:('H',2,65535),5:('h',2,32767),6:('B',1,1),7:('b',1,1),8:('H',2,1),9:('h',2,1),10:('I',4,1),11:('i',4,1)}
        for ch in channels:
            dim=ch['dimension']&15
            if dim:strides[ch['stream']]=max(strides.get(ch['stream'],0),ch['offset']+formats[ch['format']][1]*dim)
        base=0
        for stream,stride in sorted(strides.items()):offsets[stream]=base;base=(base+stride*count+15)//16*16
        def channel(index,keys):
            if index>=len(channels) or not channels[index]['dimension']:return []
            ch=channels[index];fmt,size,div=formats[ch['format']];dim=ch['dimension']&15
            return [dict(zip(keys,[x/div for x in struct.unpack_from('<'+fmt*dim,data,offsets[ch['stream']]+i*strides[ch['stream']]+ch['offset'])])) for i in range(count)]
        fmt='H' if d.get('m_IndexFormat',0)==0 else 'I';ib=bytes.fromhex(d['m_IndexBuffer']);step=struct.calcsize(fmt);triangles=[];mats=[]
        for subindex,sub in enumerate(d['m_SubMeshes']):
            assert sub['topology']==0,str(p)
            indices=struct.unpack_from('<'+fmt*sub['indexCount'],ib,sub['firstByte']);triangles.extend(i+sub.get('baseVertex',0) for i in indices);mats.extend([subindex]*(len(indices)//3))
        return dict(vertices=channel(0,'xyz'),normals=channel(1,'xyz'),colors=channel(3,'rgba'),uv=channel(4,'xy'),triangles=triangles,materials=mats)
    if p and p.suffix.lower()=='.fbx':
        if str(p) not in fbx_cache:
            before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(p));items={}
            for obj in set(bpy.data.objects)-before:
                if obj.type!='MESH':continue
                me=obj.data;me.calc_loop_triangles();vs=[];ns=[];uv=[];ts=[];mats=[]
                for tri in me.loop_triangles:
                    for li in reversed(tri.loops):
                        loop=me.loops[li];v=me.vertices[loop.vertex_index].co;n=me.corner_normals[li].vector
                        vs.append(dict(x=-v.x,y=v.y,z=v.z));ns.append(dict(x=-n.x,y=n.y,z=n.z));uv.append(dict(zip('xy',me.uv_layers.active.data[li].uv)) if me.uv_layers else dict(x=0,y=0));ts.append(len(ts))
                    mats.append(tri.material_index)
                items[obj.name]=dict(vertices=vs,normals=ns,uv=uv,colors=[],triangles=ts,materials=mats)
            for obj in set(bpy.data.objects)-before:bpy.data.objects.remove(obj,do_unlink=True)
            fbx_cache[str(p)]=items
        items=fbx_cache[str(p)]
        if meshname in items:return items[meshname]
        if len(items)==1:return next(iter(items.values()))
        raise ValueError('Unresolved FBX submesh '+str(p)+' '+meshname)
    if guid=='0000000000000000e000000000000000':
        if fileid==10202:bpy.ops.mesh.primitive_cube_add(size=1)
        elif fileid==10206:bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=.5,depth=2)
        elif fileid==10207:bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,radius=.5)
        elif fileid==10209:bpy.ops.mesh.primitive_plane_add(size=10)
        elif fileid==10210:bpy.ops.mesh.primitive_plane_add(size=1)
        else:raise ValueError('Unsupported builtin '+str(fileid))
        obj=bpy.context.object;me=obj.data;me.calc_loop_triangles();vs=[];ns=[];uv=[];ts=[]
        # Blender Z-up primitive to Unity Y-up; cube geometry remains exact.
        for tri in me.loop_triangles:
            for li in reversed(tri.loops):
                loop=me.loops[li];v=me.vertices[loop.vertex_index].co;n=tri.normal
                if fileid==10210:co=(v.x,v.y,0);no=(n.x,n.y,n.z)
                else:co=(v.x,v.z,v.y);no=(n.x,n.z,n.y)
                vs.append(dict(zip('xyz',co)));ns.append(dict(zip('xyz',no)));uv.append(dict(zip('xy',me.uv_layers.active.data[li].uv)));ts.append(len(ts))
        bpy.data.objects.remove(obj,do_unlink=True)
        if fileid not in (10202,10209,10210):warnings.append('Rebuilt Unity builtin primitive '+str(fileid))
        return dict(vertices=vs,normals=ns,uv=uv,colors=[],triangles=ts,materials=[0]*(len(ts)//3))
    raise ValueError('Unsupported mesh '+str(p)+' '+str(fileid))

def expand(ds):
    """Expand prefab instances into the same document table, honoring overrides."""
    ds=copy.deepcopy(ds);nextid=max(ds,default=0)+100
    for instanceid,doc in list(ds.items()):
        if doc['__type']!=1001:continue
        ins=body(doc);guid=ins.get('m_SourcePrefab',{}).get('guid');path=GUIDS.get(guid)
        if path and path.name=='Yol.fbx':
            # Native Unity FBX subobject IDs, retained in the saved scene overrides.
            child=copy.deepcopy(docs(str(ROOT/'Assets/Speed-Staes/Prefabs/LoopWayLibrary/Road_0.prefab')))
            ts={i:body(d) for i,d in child.items() if d['__type']==4};rootid=next(i for i,t in ts.items() if not ref(t['m_Father']));rootgo=ref(ts[rootid]['m_GameObject'])
            rename={rootid:-8679921383154817045,rootgo:919132149155446097}
            gos={i:body(d).get('m_Name') for i,d in child.items() if d['__type']==1}
            for i,d in child.items():
                if d['__type']==23:rename[i]={'Plane.001':882129737928793925,'Plane.002':7339356831882890532}[gos[ref(body(d)['m_GameObject'])]]
            def native_ids(v):
                if isinstance(v,dict):
                    if 'fileID' in v and not v.get('guid'):v['fileID']=rename.get(v['fileID'],v['fileID'])
                    else:
                        for x in v.values():native_ids(x)
                elif isinstance(v,list):
                    for x in v:native_ids(x)
            native_ids(child);child={rename.get(i,i):d for i,d in child.items()}
            for i,d in child.items():d['__id']=i
        elif not path or path.suffix!='.prefab':
            warnings.append('Unexpanded prefab '+str(path));continue
        else:child=copy.deepcopy(docs(str(path)))
        for modification in ins.get('m_Modification',{}).get('m_Modifications',[]):
            target=child.get(ref(modification.get('target',{})))
            if not target:continue
            value=body(target);prop=modification['propertyPath'];replacement=modification.get('value');objref=modification.get('objectReference',{})
            try:
                segments=re.sub(r'\.Array\.data\[(\d+)\]',r'.\1',prop).split('.')
                for seg in segments[:-1]:value=value[int(seg)] if isinstance(value,list) else value[seg]
                last=int(segments[-1]) if isinstance(value,list) else segments[-1]
                if ref(objref) or objref.get('guid'):value[last]=objref
                else:
                    old=value[last]
                    transform_float=segments[0] in ('m_LocalPosition','m_LocalRotation','m_LocalScale','m_LocalEulerAnglesHint')
                    value[last]=float(replacement) if transform_float or isinstance(old,float) else (int(replacement) if isinstance(old,int) else replacement)
            except (KeyError,IndexError,ValueError,TypeError):pass
        child=expand(child);mapping={i:nextid+n for n,i in enumerate(child)};nextid+=len(child)+100
        # Stripped local documents identify inherited objects referenced by scene children.
        for sceneid,scene_doc in ds.items():
            b=body(scene_doc)
            if ref(b.get('m_PrefabInstance',{}))==instanceid:
                sourceid=ref(b.get('m_CorrespondingSourceObject',{}))
                if sourceid in mapping:mapping[sourceid]=sceneid
        parent=ins.get('m_Modification',{}).get('m_TransformParent',{'fileID':0})
        def rewrite(value):
            if isinstance(value,dict):
                if 'fileID' in value and not value.get('guid') and value['fileID'] in mapping:value['fileID']=mapping[value['fileID']]
                else:
                    for v in value.values():rewrite(v)
            elif isinstance(value,list):
                for v in value:rewrite(v)
        for oldid,d in child.items():
            b=body(d);isroot=d['__type']==4 and not ref(b.get('m_Father',{}));rewrite(b)
            if isroot:b['m_Father']=parent
            d['__id']=mapping[oldid];ds[mapping[oldid]]=d
    return ds

def extract(ds,rootid):
    transforms={i:body(d) for i,d in ds.items() if d['__type']==4 and 'm_LocalPosition' in body(d)}
    go_to_transform={ref(t['m_GameObject']):i for i,t in transforms.items()}
    renderers={ref(body(d)['m_GameObject']):body(d) for d in ds.values() if d['__type']==23 and 'm_GameObject' in body(d)}
    names={i:body(d).get('m_Name','Object') for i,d in ds.items() if d['__type']==1}
    @functools.lru_cache(None)
    def lineage(i):
        if i==rootid:return Matrix.Identity(4),''
        t=transforms.get(i)
        if not t:return None,None
        par=ref(t.get('m_Father',{}))
        if not par:return (Matrix.Identity(4),'') if rootid is None else (None,None)
        mat,name=lineage(par)
        return (mat @ matrix(t),name+'/'+names.get(ref(t['m_GameObject']),'Object')) if mat is not None else (None,None)
    parts=[]
    for doc in ds.values():
        if doc['__type']!=33:continue
        f=body(doc);go=ref(f['m_GameObject']);tid=go_to_transform.get(go);r=renderers.get(go)
        if not tid or not r:continue
        mat,name=lineage(tid)
        if mat is None:continue
        meshref=f.get('m_Mesh',{})
        if not ref(meshref):continue
        try:raw=meshdata(meshref.get('guid'),ref(meshref),names.get(go,''))
        except Exception as error:warnings.append(str(error));continue
        normal=mat.to_3x3().inverted_safe().transposed()
        part={**raw,'name':name.strip('/') or names.get(go,'Mesh'),'surfaces':[material(m.get('guid')) for m in r.get('m_Materials',[])]}
        part['vertices']=[dict(zip('xyz',mat @ Vector(vec(v)))) for v in raw['vertices']]
        part['normals']=[dict(zip('xyz',(normal @ Vector(vec(v))).normalized())) for v in raw['normals']]
        if mat.determinant()<0:
            tr=raw['triangles'];part['triangles']=[tr[i+j] for i in range(0,len(tr),3) for j in (0,2,1)]
        parts.append(part)
    return parts

for path in sorted((ROOT/'Assets/Speed-Staes/Prefabs').rglob('*.prefab')):
    if '--city-only' in sys.argv:continue
    relative=path.relative_to(ROOT/'Assets/Speed-Staes/Prefabs')
    if relative.parts[0] in ('Character','Hurdles'):continue
    guid=re.search(r'^guid: (\w+)',path.with_suffix(path.suffix+'.meta').read_text(),re.M)[1]
    fullmap={'LoopWay2.prefab':'Egypt','LoopWay3.prefab':'RomeByzantium','LoopWay4.prefab':'PirateSea','LoopWay5.prefab':'FloatingSky'}.get(path.name) if relative.parts[0]=='Themes' else None
    if '--repair' in sys.argv and (OUT/(guid+'.json')).exists() and not fullmap:continue
    ds=expand(docs(str(path)));transforms={i:body(d) for i,d in ds.items() if d['__type']==4 and 'm_Father' in body(d)}
    roots=[i for i,t in transforms.items() if not ref(t['m_Father'])]
    if not roots:continue
    parts=extract(ds,roots[0])
    if not parts:continue
    name=body(ds[ref(transforms[roots[0]]['m_GameObject'])])['m_Name'];theme=relative.parts[1] if relative.parts[0]=='Themes' else 'City'
    if fullmap:theme=fullmap+'_Map';name=theme
    guid=re.search(r'^guid: (\w+)',path.with_suffix(path.suffix+'.meta').read_text(),re.M)[1]
    (OUT/((theme if fullmap else guid)+'.json')).write_text(json.dumps(dict(name=name,theme=theme,source=path.relative_to(ROOT).as_posix(),parts=parts)),encoding='utf8')
    if fullmap and (OUT/(guid+'.json')).exists():(OUT/(guid+'.json')).unlink()
    print('READ_SOURCE',theme,name,flush=True)

scene=ROOT/'Assets/Speed-Staes/Scenes/Test/ThemeRunPlayground.unity'
ds=expand(docs(str(scene)))
for i,d in ds.items():
    if d['__type']!=1:continue
    name=body(d).get('m_Name','')
    if name.startswith('Generated Theme - '):
        theme=name.split(' - ',1)[1];tid=next((j for j,t in ds.items() if t['__type']==4 and ref(body(t).get('m_GameObject',{}))==i),None)
        if (OUT/(theme+'_Map.json')).exists():continue
        parts=extract(ds,tid)
        if parts:(OUT/(theme+'_Map.json')).write_text(json.dumps(dict(name=theme+'_Map',theme=theme+'_Map',source=scene.relative_to(ROOT).as_posix(),parts=parts)),encoding='utf8');print('READ_MAP',theme,len(parts),flush=True)
loop_guid=re.search(r'^guid: (\w+)',(ROOT/'Assets/Speed-Staes/Scripts/Runner/LoopWayGenerator.cs.meta').read_text(),re.M)[1]
for d in ds.values():
    if d['__type']==114 and body(d).get('m_Script',{}).get('guid')==loop_guid:
        go=ref(body(d)['m_GameObject']);tid=next((j for j,t in ds.items() if t['__type']==4 and ref(body(t).get('m_GameObject',{}))==go),None);parts=extract(ds,tid)
        (OUT/'City_Map.json').write_text(json.dumps(dict(name='City_Map',theme='City_Map',source=scene.relative_to(ROOT).as_posix(),parts=parts)),encoding='utf8');print('READ_MAP City',len(parts),flush=True)
(OUT.parent/'source-read-warnings.json').write_text(json.dumps(sorted(set(warnings)),indent=2),encoding='utf8')
print('SOURCE_READ_COMPLETE',len(list(OUT.glob('*.json'))),len(set(warnings)),flush=True)
