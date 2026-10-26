using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpeedStars.Themes;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    public static partial class ThemeKitBuilder
    {
        const string Base="Assets/Speed-Staes/Prefabs/Themes";
        static string themeFolder,modelName;static int meshNumber;
        static Dictionary<string,Material> colors=new Dictionary<string,Material>();
        static Material sand,sandLight,stone,stoneDark,gold,teal,wood,woodDark,cream,red,green,leaf,dark,blue,purple,water;
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
        static Material Paint(string name,Color color,float glow=.22f)
        {string path=themeFolder+"/Materials/"+name+".mat";Folder(themeFolder+"/Materials");var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Speed Stars/Pastel Street Prop"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_Color",color);mat.SetFloat("_Glow",glow);mat.SetFloat("_SceneLighting",0);EditorUtility.SetDirty(mat);colors[name]=mat;return mat;}
        static void Palette()
        {
            colors.Clear();sand=Paint("Warm Sand",new Color(.81f,.64f,.39f));sandLight=Paint("Pale Sandstone",new Color(.91f,.78f,.55f));stone=Paint("Warm Marble",new Color(.79f,.81f,.74f));stoneDark=Paint("Marble Shade",new Color(.51f,.61f,.62f));gold=Paint("Painted Gold",new Color(.92f,.68f,.23f));teal=Paint("Patinated Teal",new Color(.22f,.49f,.51f));wood=Paint("Warm Wood",new Color(.49f,.30f,.18f));woodDark=Paint("Dark Wood",new Color(.30f,.21f,.16f));cream=Paint("Sail Ivory",new Color(.94f,.91f,.77f));red=Paint("Terracotta Red",new Color(.72f,.28f,.23f));green=Paint("Meadow Green",new Color(.39f,.60f,.22f));leaf=Paint("Leaf Highlights",new Color(.58f,.73f,.28f));dark=Paint("Charcoal",new Color(.15f,.22f,.27f));blue=Paint("Painted Blue",new Color(.29f,.49f,.73f));purple=Paint("Octopus Purple",new Color(.55f,.31f,.53f));water=Paint("Lagoon Water",new Color(.17f,.62f,.77f),.9f);
        }
        static GameObject Model(string name,Action<Transform> build)
        {
            modelName=name;meshNumber=0;var obj=new GameObject(name);build(obj.transform);if(themeFolder.EndsWith("/RomeByzantium")&&name.StartsWith("Horizon "))DressRomanHorizon(obj.transform);
            Folder(themeFolder+"/Models");var prefab=PrefabUtility.SaveAsPrefabAsset(obj,themeFolder+"/Models/"+name+".prefab");UnityEngine.Object.DestroyImmediate(obj);return prefab;
        }
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 scale,Material mat)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Speed-Staes/Prefabs/StreetProps/Signs/SignBoxSection.asset");var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{mat,mat,mat};r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return g;}
        static GameObject MeshObject(Transform parent,string name,List<Vector3> vertices,List<int> triangles,Material mat,bool flat=true)
        {
            if(flat){var vv=new List<Vector3>();var tt=new List<int>();for(int i=0;i<triangles.Count;i++){tt.Add(vv.Count);vv.Add(vertices[triangles[i]]);}vertices=vv;triangles=tt;}
            var mesh=new Mesh();mesh.name=name;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();Folder(themeFolder+"/Meshes");string path=themeFolder+"/Meshes/"+modelName+"_"+(meshNumber++).ToString("000")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return g;
        }
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
        {var g=Box(parent,name,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static GameObject Lathe(Transform parent,string name,Vector3 pos,float[] heights,float[] radii,Material mat,int sides=12)
        {
            var v=new List<Vector3>();var t=new List<int>();for(int y=0;y<heights.Length;y++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*radii[y],heights[y],Mathf.Sin(a)*radii[y]));}
            for(int y=0;y<heights.Length-1;y++)for(int i=0;i<sides;i++){int a=y*sides+i,b=y*sides+(i+1)%sides,c=b+sides,d=a+sides;t.AddRange(new[]{a,d,b,b,d,c});}
            int center=v.Count;v.Add(Vector3.up*heights[heights.Length-1]);for(int i=0;i<sides;i++)t.AddRange(new[]{center,(heights.Length-1)*sides+(i+1)%sides,(heights.Length-1)*sides+i});
            var g=MeshObject(parent,name,v,t,mat);g.transform.localPosition=pos;return g;
        }
        static GameObject Cylinder(Transform parent,string name,Vector3 pos,float radius,float height,Material mat,int sides=12)
        {return Lathe(parent,name,pos,new[]{0f,height},new[]{radius,radius},mat,sides);}
        static GameObject Ball(Transform parent,string name,Vector3 p,Vector3 scale,Material mat)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g;}
        static GameObject Pyramid(Transform parent,string name,Vector3 p,float width,float height,Material mat)
        {float w=width*.5f;var g=MeshObject(parent,name,new List<Vector3>{new Vector3(-w,0,-w),new Vector3(w,0,-w),new Vector3(w,0,w),new Vector3(-w,0,w),new Vector3(0,height,0)},new List<int>{0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3},mat);g.transform.localPosition=p;return g;}
        static void Arc(Transform parent,string name,Vector3 p,float radius,float thickness,float depth,Material mat,int steps=16,float start=0,float end=180)
        {
            var v=new List<Vector3>();var tr=new List<int>();for(int i=0;i<=steps;i++){float a=Mathf.Lerp(start,end,(float)i/steps)*Mathf.Deg2Rad;foreach(float z in new[]{-depth*.5f,depth*.5f})foreach(float r in new[]{radius,radius+thickness})v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z));}
            for(int i=0;i<steps;i++){int k=i*4,n=k+4;tr.AddRange(new[]{k,n,k+1,k+1,n,n+1,k+2,k+3,n+2,k+3,n+3,n+2,k,k+2,n,k+2,n+2,n,k+1,n+1,k+3,k+3,n+1,n+3});}
            var g=MeshObject(parent,name,v,tr,mat);g.transform.localPosition=p;
        }
        static void Tube(Transform parent,string name,List<Vector3> points,float radius,Material mat,int sides=8,float endRadius=.06f)
        {
            var v=new List<Vector3>();var tr=new List<int>();for(int j=0;j<points.Count;j++){Vector3 tangent=j==points.Count-1?points[j]-points[j-1]:points[j+1]-points[j];Quaternion rot=Quaternion.FromToRotation(Vector3.up,tangent);float r=Mathf.Lerp(radius,endRadius,(float)j/(points.Count-1));for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(points[j]+rot*new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}}
            for(int j=0;j<points.Count-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;tr.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}MeshObject(parent,name,v,tr,mat);
        }
        static void Palm(Transform parent,Vector3 p,float height=6)
        {
            var root=new GameObject("Curved Date Palm");root.transform.SetParent(parent,false);root.transform.localPosition=p;var t=root.transform;
            var points=new List<Vector3>();for(int i=0;i<7;i++)points.Add(new Vector3(Mathf.Sin(i*.18f)*.7f,i*height/6,0));Tube(t,"Segmented Palm Trunk",points,.23f,wood,8,.14f);Vector3 crown=points[6];
            for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 side=Vector3.Cross(Vector3.up,dir)*.40f;Vector3 middle=crown+dir*1.4f+Vector3.up*.5f,end=crown+dir*2.8f-Vector3.up*.9f;MeshObject(t,"Broad Palm Frond",new List<Vector3>{crown,middle+side,end,middle-side},new List<int>{0,1,2,0,2,3,2,1,0,3,2,0},i%2==0?green:leaf);}
            for(int i=0;i<3;i++)Ball(t,"Date Cluster",crown+new Vector3((i-1)*.2f,-.15f,0),Vector3.one*.28f,woodDark);
        }
        static void Tree(Transform parent,Vector3 p,float height=4,bool narrow=false)
        {Cylinder(parent,"Tree Trunk",p,.18f,height*.65f,wood,8);if(narrow)Lathe(parent,"Cypress Crown",p+Vector3.up*.9f,new[]{0f,height*.50f,height},new[]{.8f,.65f,.04f},green,9);else{Ball(parent,"Rounded Olive Crown",p+Vector3.up*height*.70f,new Vector3(height*.7f,height*.65f,height*.65f),green);Ball(parent,"Sunlit Crown",p+new Vector3(-.4f,height*.86f,-.1f),new Vector3(height*.5f,height*.45f,height*.45f),leaf);}}
        static void Flag(Transform parent,Vector3 p,Material cloth,float height=4)
        {Cylinder(parent,"Banner Mast",p,.055f,height,gold,8);MeshObject(parent,"Wind Caught Banner",new List<Vector3>{p+Vector3.up*height,p+new Vector3(1.6f,height-.15f,0),p+new Vector3(1.3f,height-1.05f,.15f),p+Vector3.up*(height-.9f)},new List<int>{0,1,2,0,2,3,2,1,0,3,2,0},cloth);}
        static GameObject Background(GameObject prefab,string name)
        {
            var g=UnityEngine.Object.Instantiate(prefab);g.name=name;var replacements=new Dictionary<Material,Material>();foreach(var r in g.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){if(!mats[i])continue;if(!replacements.ContainsKey(mats[i])){var color=mats[i].HasProperty("_Color")?mats[i].GetColor("_Color"):Color.white;var distant=Paint("Distant "+mats[i].name,Color.Lerp(color,themeFolder.EndsWith("/Egypt")?new Color(.92f,.77f,.54f):new Color(.56f,.75f,.84f),themeFolder.EndsWith("/Egypt")?.28f:.48f),.65f);if(mats[i].shader.name=="Speed Stars/Painted Pyramid Stone")distant.shader=mats[i].shader;replacements[mats[i]]=distant;}mats[i]=replacements[mats[i]];}r.sharedMaterials=mats;}var p=PrefabUtility.SaveAsPrefabAsset(g,themeFolder+"/Models/"+name+".prefab");UnityEngine.Object.DestroyImmediate(g);return p;
        }
        public static string BuildAll()
        {
            Folder(Base);var definitions=new List<LoopThemeDefinition>();
            foreach(EnvironmentConcept concept in Enum.GetValues(typeof(EnvironmentConcept)))
            {
                themeFolder=Base+"/"+concept;Folder(themeFolder);Palette();string path=themeFolder+"/Definition.asset";var definition=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(path);if(!definition){definition=ScriptableObject.CreateInstance<LoopThemeDefinition>();AssetDatabase.CreateAsset(definition,path);}definition.concept=concept;
                definition.road=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Speed-Staes/Prefabs/LoopWayLibrary/Road_0.prefab");
                definition.groundMaterial=concept==EnvironmentConcept.Egypt?sand:green;definition.pathMaterial=concept==EnvironmentConcept.Egypt?sandLight:stone;definition.waterMaterial=water;definition.waterAcrossRoad=concept==EnvironmentConcept.PirateSea;definition.noGround=concept==EnvironmentConcept.PirateSea||concept==EnvironmentConcept.FloatingSky;
                var roadRenderer=definition.road.GetComponentsInChildren<Renderer>()[0];var roadMaterials=roadRenderer.sharedMaterials;
                definition.asphaltMaterial=CloneRoad(roadMaterials.First(m=>m.GetFloat("_Asphalt")>.5f),"Themed Asphalt",concept==EnvironmentConcept.Egypt?new Color(.43f,.36f,.29f):concept==EnvironmentConcept.RomeByzantium?new Color(.38f,.38f,.41f):concept==EnvironmentConcept.PirateSea?new Color(.23f,.34f,.43f):new Color(.43f,.40f,.56f));
                definition.paletteMaterial=CloneRoad(roadMaterials.First(m=>m.GetFloat("_Asphalt")<.5f),"Themed Road Palette",concept==EnvironmentConcept.Egypt?new Color(1,.93f,.80f):concept==EnvironmentConcept.FloatingSky?new Color(.96f,.94f,1):new Color(.94f,1,1));
                definition.clouds=Enumerable.Range(1,3).Select(i=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Speed-Staes/Prefabs/Clouds/Cloud_0"+i+".prefab")).Where(p=>p).ToArray();
                if(concept==EnvironmentConcept.Egypt)Egypt(definition);else if(concept==EnvironmentConcept.RomeByzantium)Rome(definition);else if(concept==EnvironmentConcept.PirateSea)Sea(definition);else Sky(definition);
                EditorUtility.SetDirty(definition);definitions.Add(definition);
            }
            AssetDatabase.SaveAssets();return Showcase(definitions.ToArray());
        }
        static Material CloneRoad(Material source,string name,Color color)
        {string path=themeFolder+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}else EditorUtility.CopySerialized(source,mat);mat.name=name;mat.SetColor("_Color",color);EditorUtility.SetDirty(mat);return mat;}
        static string Showcase(LoopThemeDefinition[] definitions)
        {
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                for(int i=0;i<definitions.Length;i++)
                {
                    var obj=new GameObject("LoopWay"+(i+2));SceneManager.MoveGameObjectToScene(obj,preview);
                    var gen=obj.AddComponent<ThemedLoopWay>();gen.theme=definitions[i];gen.loopCount=3;gen.seed=42+i*53;gen.Generate();
                    // Deferred runtime destroys must not be serialized into editor-built prefabs.
                    foreach(var collider in obj.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var t in obj.GetComponentsInChildren<Transform>(true).Where(t=>t&&!t.gameObject.activeSelf).ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(obj,Base+"/LoopWay"+(i+2)+".prefab");
                    Capture(obj,preview,i+2,false);Capture(obj,preview,i+2,true);
                    UnityEngine.Object.DestroyImmediate(obj);
                }
                AssetDatabase.SaveAssets();return Base+"/LoopWay2.prefab through LoopWay5.prefab";
            }
            finally {EditorSceneManager.ClosePreviewScene(preview);}
        }
        static void Capture(GameObject obj,Scene preview,int number,bool detail)
        {
            var cameraObject=new GameObject("Isolated Theme Preview Camera");SceneManager.MoveGameObjectToScene(cameraObject,preview);var camera=cameraObject.AddComponent<Camera>();camera.scene=preview;camera.clearFlags=CameraClearFlags.Skybox;camera.backgroundColor=new Color(.18f,.57f,.94f);camera.fieldOfView=detail?44:52;camera.farClipPlane=210;camera.nearClipPlane=.1f;camera.enabled=false;
            var skybox=cameraObject.AddComponent<Skybox>();string skyPath=Base+"/ThemePreviewSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(!sky){sky=new Material(Shader.Find("Speed Stars/Road Daylight Sky"));AssetDatabase.CreateAsset(sky,skyPath);}skybox.material=sky;
            cameraObject.transform.position=detail?new Vector3(7,6,12):new Vector3(87,105,70);cameraObject.transform.LookAt(detail?new Vector3(-25,4,12):new Vector3(-30,1,68));
            var texture=new RenderTexture(1440,810,24);camera.targetTexture=texture;camera.Render();var previous=RenderTexture.active;RenderTexture.active=texture;var image=new Texture2D(1440,810,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,810),0,0);image.Apply();string folder=System.IO.Path.GetFullPath("Captures/Themes");System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"LoopWay"+number+(detail?"-detail":"-overview")+".png"),image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}



