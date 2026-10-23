using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    public static class LoopWayLandmarkBuilder
    {
        const string Folder="Assets/Speed-Staes/Prefabs/LoopWayLandmarks";
        static Material steel,steelSide,steelTop,cream,wood,woodTop,teal,glass,glassLight,grass,path,orange,red,water,stone;
        static void EnsureFolder(string p){if(AssetDatabase.IsValidFolder(p))return;int k=p.LastIndexOf('/');EnsureFolder(p.Substring(0,k));AssetDatabase.CreateFolder(p.Substring(0,k),p.Substring(k+1));}
        static Material Mat(string name,Color color)
        {
            string folder="Assets/Speed-Staes/Materials/LoopWayLandmarks";EnsureFolder(folder);
            string p=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
            if(!m){m=new Material(Shader.Find("Speed Stars/Pastel Street Prop"));AssetDatabase.CreateAsset(m,p);}
            m.SetColor("_Color",color);m.SetFloat("_Glow",1);EditorUtility.SetDirty(m);return m;
        }
        static void Palette()
        {
            steel=Mat("Deep Painted Steel",new Color(.17f,.24f,.27f));steelSide=Mat("Steel Side",new Color(.22f,.30f,.33f));steelTop=Mat("Steel Top",new Color(.31f,.40f,.42f));
            cream=Mat("Warm Pale Trim",new Color(.83f,.85f,.79f));wood=Mat("Warm Bench Wood",new Color(.50f,.34f,.20f));woodTop=Mat("Wood Lit Top",new Color(.65f,.46f,.28f));
            teal=Mat("Shelter Teal",new Color(.23f,.43f,.46f));glass=Mat("Pale Blue Glass",new Color(.39f,.65f,.73f));glassLight=Mat("Glass Painted Reflection",new Color(.61f,.79f,.82f));
            grass=Mat("Park Grass",new Color(.39f,.57f,.22f));path=Mat("Park Sand Path",new Color(.72f,.68f,.54f));orange=Mat("Slide Orange",new Color(.90f,.43f,.20f));red=Mat("Playground Red",new Color(.73f,.24f,.21f));
            water=Mat("Fountain Blue Water",new Color(.30f,.69f,.80f));stone=Mat("Fountain Pale Stone",new Color(.67f,.73f,.70f));
        }
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m,Material side=null,Material top=null)
        {
            var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;
            o.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Speed-Staes/Prefabs/StreetProps/Signs/SignBoxSection.asset");
            var renderer=o.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{m,side?side:m,top?top:m};renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return o;
        }
        static GameObject Cylinder(Transform parent,string name,Vector3 p,float radius,float height,Material m)
        {
            // Eight-sided stone/metal forms keep the same broad painted facets as the street props.
            var v=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;int k=v.Count;
                Vector3 p1=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),p2=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                v.Add(p1+Vector3.up*height*.5f);v.Add(p2+Vector3.up*height*.5f);v.Add(p2-Vector3.up*height*.5f);v.Add(p1-Vector3.up*height*.5f);
                tris.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});k=v.Count;v.Add(Vector3.up*height*.5f);v.Add(p2+Vector3.up*height*.5f);v.Add(p1+Vector3.up*height*.5f);tris.AddRange(new[]{k,k+1,k+2});
            }
            var mesh=new Mesh();mesh.name=name;mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string meshPath=Folder+"/"+name.Replace(" ","")+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,meshPath);
            var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=p;o.AddComponent<MeshFilter>().sharedMesh=mesh;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return o;
        }
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material m)
        {var g=Box(parent,name,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),m,steelSide,steelTop);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void Bench(Transform parent,float x,float z,float angle=0)
        {
            var g=new GameObject("Wood Slat Park Bench");g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(x,.12f,z);g.transform.localRotation=Quaternion.Euler(0,angle,0);
            for(int i=0;i<3;i++)Box(g.transform,"Seat Slat",new Vector3(0,.50f,-.22f+i*.22f),new Vector3(2.7f,.10f,.19f),wood,wood,woodTop);
            for(int i=0;i<2;i++)Box(g.transform,"Backrest Slat",new Vector3(0,.87f+i*.21f,.33f),new Vector3(2.7f,.18f,.10f),wood,wood,woodTop);
            foreach(float side in new[]{-1f,1f}){Box(g.transform,"Bench Leg",new Vector3(side,.26f,0),new Vector3(.14f,.52f,.65f),steel,steelSide,steelTop);Box(g.transform,"Back Support",new Vector3(side,.73f,.35f),new Vector3(.12f,.80f,.12f),steel,steelSide,steelTop);}
        }
        static void PixelText(Transform parent,string word,Vector3 p,float unit,Material color)
        {
            var glyph=new Dictionary<char,string>{{'B',"110/101/110/101/110"},{'U',"101/101/101/101/111"},{'S',"111/100/111/001/111"}};
            for(int n=0;n<word.Length;n++){var rows=glyph[word[n]].Split('/');for(int y=0;y<5;y++)for(int x=0;x<3;x++)if(rows[y][x]=='1')Box(parent,"BUS Letter Pixel",p+new Vector3((n*4+x-5.5f)*unit,(2-y)*unit,0),new Vector3(unit,unit,.025f),color);}
        }
        static GameObject BuildStop()
        {
            var g=new GameObject("Bus Stop - Painted Glass Shelter");var t=g.transform;
            Box(t,"Raised Shelter Pad",new Vector3(0,.09f,0),new Vector3(14,.18f,4.5f),stone,stone,cream);
            Box(t,"Broad Teal Canopy",new Vector3(0,3.30f,0),new Vector3(12.5f,.24f,4.2f),teal,steelSide,cream);
            Box(t,"Canopy Front Fascia",new Vector3(0,3.16f,-2.07f),new Vector3(12.5f,.30f,.10f),teal,steelSide,steelTop);
            foreach(float x in new[]{-5.8f,-1.9f,1.9f,5.8f})
            {Box(t,"Shelter Back Column",new Vector3(x,1.67f,1.72f),new Vector3(.13f,3.1f,.13f),steel,steelSide,steelTop);Box(t,"Glass Lower Rail",new Vector3(x>0?3.85f:-3.85f,.70f,1.73f),new Vector3(3.7f,.08f,.10f),steel,steelSide,steelTop);}
            foreach(float x in new[]{-3.85f,0,3.85f})
            {
                Box(t,"Tinted Back Glass",new Vector3(x,1.91f,1.75f),new Vector3(3.65f,2.48f,.045f),glass);
                var reflection=Box(t,"Broad Glass Reflection",new Vector3(x-.70f,1.95f,1.71f),new Vector3(.25f,2.1f,.015f),glassLight);reflection.transform.localRotation=Quaternion.Euler(0,0,-16);
            }
            foreach(float x in new[]{-5.85f,5.85f}){Box(t,"Front Column",new Vector3(x,1.67f,-1.7f),new Vector3(.13f,3.1f,.13f),steel,steelSide,steelTop);Box(t,"Side Glass",new Vector3(x,1.85f,0),new Vector3(.045f,2.3f,3.2f),glass);}
            Bench(t,-2.8f,.8f);Bench(t,1.0f,.8f);
            Box(t,"Timetable Frame",new Vector3(4.4f,1.85f,1.65f),new Vector3(1.1f,1.55f,.10f),steel,steelSide,steelTop);
            Box(t,"Timetable Paper",new Vector3(4.4f,1.85f,1.58f),new Vector3(.92f,1.36f,.025f),cream);
            for(int i=0;i<5;i++)Box(t,"Timetable Route",new Vector3(4.36f,2.28f-i*.21f,1.56f),new Vector3(.65f,.045f,.015f),i%2==0?teal:wood);
            Box(t,"BUS Header",new Vector3(-3.1f,3.37f,-2.15f),new Vector3(2.7f,.68f,.12f),teal,steel,cream);PixelText(t,"BUS",new Vector3(-3.1f,3.38f,-2.23f),.095f,cream);
            Box(t,"Bus Stop Sign Post",new Vector3(-7.1f,1.9f,-1.7f),new Vector3(.12f,3.8f,.12f),steel,steelSide,steelTop);
            Box(t,"Bus Icon Blue Plate",new Vector3(-7.1f,3.6f,-1.76f),new Vector3(.85f,.95f,.10f),teal,steelSide,cream);
            Box(t,"Bus Icon Body",new Vector3(-7.1f,3.62f,-1.83f),new Vector3(.53f,.55f,.02f),cream);
            Box(t,"Bus Icon Window",new Vector3(-7.1f,3.72f,-1.85f),new Vector3(.39f,.23f,.02f),glass);
            foreach(float x in new[]{-7.27f,-6.93f})Box(t,"Bus Icon Wheel",new Vector3(x,3.30f,-1.85f),new Vector3(.10f,.10f,.02f),steel);
            return g;
        }
        static void FenceRun(Transform parent,Vector3 a,Vector3 b,bool gate=false)
        {
            var delta=b-a;float length=delta.magnitude;int count=Mathf.CeilToInt(length/.75f);
            for(int i=0;i<=count;i++){float f=(float)i/count;if(gate&&f>.43f&&f<.57f)continue;Vector3 p=Vector3.Lerp(a,b,f);Box(parent,"Iron Fence Picket",p+Vector3.up*.66f,new Vector3(.055f,1.25f,.055f),steel,steelSide,steelTop);Box(parent,"Picket Top",p+Vector3.up*1.32f,new Vector3(.085f,.09f,.085f),steel,steelSide,steelTop);}
            if(gate){FenceRunRails(parent,a,Vector3.Lerp(a,b,.43f));FenceRunRails(parent,Vector3.Lerp(a,b,.57f),b);}else FenceRunRails(parent,a,b);
        }
        static void FenceRunRails(Transform parent,Vector3 a,Vector3 b)
        {Beam(parent,"Iron Fence Upper Rail",a+Vector3.up*1.17f,b+Vector3.up*1.17f,.075f,steel);Beam(parent,"Iron Fence Lower Rail",a+Vector3.up*.30f,b+Vector3.up*.30f,.065f,steel);}
        static void Fountain(Transform parent)
        {
            var g=new GameObject("Octagonal Fountain Pool");g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(2.5f,.13f,0);
            Cylinder(g.transform,"Fountain Foundation",new Vector3(0,.13f,0),2.0f,.26f,stone);
            Cylinder(g.transform,"Still Blue Fountain Water",new Vector3(0,.31f,0),1.74f,.035f,water);
            for(int i=0;i<8;i++){float angle=i*45;var rim=Box(g.transform,"Octagonal Basin Rim",new Vector3(Mathf.Cos(angle*Mathf.Deg2Rad)*1.86f,.40f,Mathf.Sin(angle*Mathf.Deg2Rad)*1.86f),new Vector3(1.56f,.35f,.22f),stone,stone,cream);rim.transform.localRotation=Quaternion.Euler(0,90-angle,0);}
            Cylinder(g.transform,"Fountain Pedestal",new Vector3(0,.79f,0),.27f,1.0f,stone);
            Cylinder(g.transform,"Upper Fountain Bowl",new Vector3(0,1.29f,0),.72f,.18f,cream);
            Cylinder(g.transform,"Upper Bowl Water",new Vector3(0,1.39f,0),.61f,.025f,water);
            Beam(g.transform,"Central Water Jet",new Vector3(0,1.40f,0),new Vector3(0,2.45f,0),.08f,glassLight);
            for(int arc=0;arc<6;arc++)
            {float angle=arc*Mathf.PI/3;Vector3 previous=new Vector3(0,1.65f,0);for(int sample=1;sample<=7;sample++){float u=sample/7f;Vector3 point=new Vector3(Mathf.Cos(angle)*u*1.45f,1.65f+1.4f*u-2.65f*u*u,Mathf.Sin(angle)*u*1.45f);Beam(g.transform,"Curved Water Stream",previous,point,.035f,glassLight);previous=point;}}
        }
        static void Slide(Transform parent)
        {
            var root=new GameObject("Slide Playground Tower");root.transform.SetParent(parent,false);root.transform.localPosition=new Vector3(-7,.13f,.4f);var t=root.transform;
            foreach(float x in new[]{-.75f,.75f})foreach(float z in new[]{-.65f,.65f})Box(t,"Play Tower Leg",new Vector3(x,1.30f,z),new Vector3(.12f,2.6f,.12f),teal,steelSide,cream);
            Box(t,"Tower Platform",new Vector3(0,2.12f,0),new Vector3(1.75f,.20f,1.65f),wood,wood,woodTop);
            Box(t,"Tower Canopy",new Vector3(0,3.12f,0),new Vector3(2.1f,.20f,2.0f),red,orange,orange);
            foreach(float x in new[]{-.75f,.75f})Box(t,"Safety Side Panel",new Vector3(x,2.57f,0),new Vector3(.10f,.65f,1.45f),teal,steelSide,cream);
            for(int i=0;i<7;i++)Box(t,"Ladder Rung",new Vector3(0,.30f+i*.28f,1.08f-i*.05f),new Vector3(1.2f,.075f,.12f),steel,steelSide,steelTop);
            foreach(float x in new[]{-.66f,.66f})Beam(t,"Ladder Side",new Vector3(x,.10f,1.3f),new Vector3(x,2.12f,.7f),.09f,teal);
            Vector3 a=new Vector3(0,2.12f,-.70f),b=new Vector3(0,.30f,-3.2f);var ramp=Box(t,"Broad Orange Slide",(a+b)*.5f,new Vector3(1.25f,.09f,(b-a).magnitude),orange,red,orange);ramp.transform.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
            foreach(float x in new[]{-.66f,.66f})Beam(t,"Slide Raised Edge",a+new Vector3(x,.12f,0),b+new Vector3(x,.12f,0),.11f,red);
            Box(t,"Slide Landing",new Vector3(0,.23f,-3.42f),new Vector3(1.35f,.11f,.65f),orange,red,orange);
        }
        static void Tree(Transform parent,float x,float z,float height,int seed)
        {
            Box(parent,"Park Tree Trunk",new Vector3(x,1.15f,z),new Vector3(.30f,2.3f,.30f),wood,wood,woodTop);
            var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name="Painted Rounded Tree Crown";o.transform.SetParent(parent,false);o.transform.localPosition=new Vector3(x,height*.71f,z);o.transform.localScale=new Vector3(3.1f,height*.62f,2.7f);UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            o.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Speed-Staes/Materials/Foliage/Pastel_Leaves.mat");
            var surface=o.AddComponent<PastelFoliageSurface>();surface.color=new Color(.36f,.60f,.12f);surface.patchData=new Vector4(3,seed,.12f,0);surface.canopyShape=new Vector4(.13f,3.4f,3.1f,height*.62f);surface.Apply();
        }
        static GameObject BuildPark()
        {
            var g=new GameObject("Pocket Park - Slide Fountain Iron Fence");var t=g.transform;
            Box(t,"Raised Park Plinth",new Vector3(0,.07f,0),new Vector3(27,.14f,8.8f),stone,stone,cream);
            Box(t,"Grass Lawn",new Vector3(0,.155f,0),new Vector3(26.5f,.06f,8.3f),grass);
            Box(t,"Front Park Promenade",new Vector3(0,.20f,-2.7f),new Vector3(25.9f,.06f,1.15f),path);
            Box(t,"Entrance Path",new Vector3(0,.20f,-.5f),new Vector3(2.6f,.06f,7.4f),path);
            FenceRun(t,new Vector3(-13,.14f,-4.15f),new Vector3(13,.14f,-4.15f),true);FenceRun(t,new Vector3(-13,.14f,4.15f),new Vector3(13,.14f,4.15f));
            FenceRun(t,new Vector3(-13,.14f,-4.15f),new Vector3(-13,.14f,4.15f));FenceRun(t,new Vector3(13,.14f,-4.15f),new Vector3(13,.14f,4.15f));
            Fountain(t);Slide(t);Bench(t,8.5f,2.4f);Bench(t,-1.7f,2.5f);
            Tree(t,-11,2.0f,4.5f,13);Tree(t,11,2.5f,5.1f,44);
            // A small swing frame adds a different silhouette beside the low fountain.
            foreach(float x in new[]{7f,10.5f}){Beam(t,"Swing A Frame",new Vector3(x,.2f,-1.5f),new Vector3(x,2.6f,0),.10f,teal);Beam(t,"Swing A Frame",new Vector3(x,.2f,1.5f),new Vector3(x,2.6f,0),.10f,teal);}
            Beam(t,"Swing Top Bar",new Vector3(6.8f,2.6f,0),new Vector3(10.7f,2.6f,0),.12f,teal);
            foreach(float x in new[]{8f,9.3f}){Beam(t,"Swing Chain",new Vector3(x,2.5f,0),new Vector3(x,.80f,0),.025f,steel);}
            Box(t,"Swing Seat",new Vector3(8.65f,.76f,0),new Vector3(1.5f,.10f,.42f),wood,wood,woodTop);
            return g;
        }
        static GameObject BuildHospital()
        {
            var g=new GameObject("Neighborhood Hospital - Red Cross and Low Wings");var t=g.transform;
            var wall=Mat("Hospital Cool Ivory",new Color(.81f,.85f,.82f));var side=Mat("Hospital Ivory Side",new Color(.62f,.70f,.70f));var navy=Mat("Hospital Deep Blue",new Color(.24f,.43f,.54f));
            Box(t,"Hospital Forecourt",new Vector3(0,.07f,0),new Vector3(25,.14f,8.2f),stone,stone,cream);
            Box(t,"Central Hospital Block",new Vector3(0,4,1),new Vector3(8,8,4.4f),wall,side,cream);
            foreach(float x in new[]{-7.4f,7.4f})
            {
                Box(t,"Low Hospital Wing",new Vector3(x,2.65f,1),new Vector3(6.8f,5.3f,4.4f),wall,side,cream);
                Box(t,"Wing Cornice",new Vector3(x,5.35f,1),new Vector3(7.05f,.20f,4.65f),cream,side,cream);
                for(int floor=0;floor<2;floor++)
                {
                    float y=1.75f+floor*1.72f;
                    Box(t,"Wing Window Band",new Vector3(x,y,-1.24f),new Vector3(5.7f,.96f,.08f),glass,navy,glassLight);
                    Box(t,"Window Top Shade",new Vector3(x,y+.37f,-1.30f),new Vector3(5.7f,.14f,.04f),navy);
                    for(int col=0;col<4;col++)Box(t,"Window Mullion",new Vector3(x-2.15f+col*1.44f,y,-1.31f),new Vector3(.10f,.96f,.055f),cream);
                }
            }
            for(int floor=0;floor<2;floor++)for(int col=0;col<3;col++)
            {
                float x=(col-1)*2.25f,y=4.75f+floor*1.65f;
                Box(t,"Central Blue Window",new Vector3(x,y,-1.26f),new Vector3(1.75f,1.14f,.08f),glass,navy,glassLight);
                Box(t,"Central Window Shade",new Vector3(x,y+.45f,-1.31f),new Vector3(1.75f,.14f,.03f),navy);
                Box(t,"Central Sill",new Vector3(x,y-.6f,-1.33f),new Vector3(1.94f,.10f,.22f),cream,side,cream);
            }
            Box(t,"Entrance Recess",new Vector3(0,1.45f,-1.27f),new Vector3(4.6f,2.65f,.10f),navy);
            foreach(float x in new[]{-1.13f,1.13f})
            {Box(t,"Sliding Glass Door",new Vector3(x,1.45f,-1.34f),new Vector3(2.10f,2.4f,.04f),glass);Box(t,"Door Handle",new Vector3(x*.2f,1.3f,-1.4f),new Vector3(.05f,.55f,.035f),cream);}
            Box(t,"Wide Entrance Canopy",new Vector3(0,3.10f,-2.1f),new Vector3(9.1f,.25f,2.3f),teal,navy,cream);
            foreach(float x in new[]{-4.15f,4.15f})Box(t,"Entrance Column",new Vector3(x,1.55f,-2.85f),new Vector3(.14f,2.95f,.14f),steel,steelSide,steelTop);
            Box(t,"Hospital Sign Fascia",new Vector3(0,3.56f,-1.39f),new Vector3(7.65f,.75f,.14f),navy,teal,cream);
            string[] letters={"101/101/111/101/101","111/101/101/101/111","111/100/111/001/111","110/101/110/100/100","111/010/010/010/111","111/010/010/010/010","010/101/111/101/101","100/100/100/100/111"};
            for(int n=0;n<letters.Length;n++){var rows=letters[n].Split('/');for(int y=0;y<5;y++)for(int x=0;x<3;x++)if(rows[y][x]=='1')Box(t,"Hospital Letter Pixel",new Vector3((n*4+x-15.5f)*.115f,3.58f+(2-y)*.115f,-1.48f),new Vector3(.115f,.115f,.025f),cream);}
            Box(t,"Central Roof Cornice",new Vector3(0,8.05f,1),new Vector3(8.35f,.23f,4.7f),cream,side,cream);
            Box(t,"Roof Health Sign",new Vector3(0,8.80f,.15f),new Vector3(2.6f,1.65f,.28f),wall,side,cream);
            Box(t,"Red Cross Upright",new Vector3(0,8.83f,-.015f),new Vector3(.38f,1.25f,.07f),red);
            Box(t,"Red Cross Arms",new Vector3(0,8.83f,-.025f),new Vector3(1.32f,.38f,.07f),red);
            foreach(float sign in new[]{-1f,1f})for(int floor=0;floor<2;floor++)for(int col=0;col<2;col++)Box(t,"Hospital Side Window",new Vector3(sign*10.85f,1.75f+floor*1.72f,col*1.65f),new Vector3(.06f,1.02f,1.1f),glass,glass,cream);
            Box(t,"Entrance Path",new Vector3(0,.16f,-3.1f),new Vector3(5.4f,.05f,1.9f),path);
            var van=new GameObject("Parked Ambulance");van.transform.SetParent(t,false);van.transform.localPosition=new Vector3(-8.4f,.16f,-2.7f);var v=van.transform;
            Box(v,"Ambulance Body",new Vector3(-.25f,.94f,0),new Vector3(3.4f,1.25f,1.5f),wall,side,cream);
            Box(v,"Ambulance Cab",new Vector3(1.25f,.82f,0),new Vector3(1.25f,1.02f,1.5f),wall,side,cream);
            Box(v,"Cab Windshield",new Vector3(1.89f,1.03f,0),new Vector3(.04f,.56f,1.26f),glass);
            foreach(float z in new[]{-.77f,.77f})
            {
                Box(v,"Ambulance Stripe",new Vector3(-.25f,.71f,z),new Vector3(3.6f,.20f,.025f),red);
                Box(v,"Side Cross Upright",new Vector3(-.9f,1.18f,z*1.01f),new Vector3(.13f,.48f,.025f),red);
                Box(v,"Side Cross Arms",new Vector3(-.9f,1.18f,z*1.02f),new Vector3(.48f,.13f,.025f),red);
                Box(v,"Cab Side Glass",new Vector3(1.25f,1.12f,z),new Vector3(.78f,.43f,.025f),glass);
            }
            foreach(float x in new[]{-1.25f,1.20f})foreach(float z in new[]{-.78f,.78f}){var wheel=Cylinder(v,"Ambulance Wheel",new Vector3(x,.34f,z),.32f,.16f,steel);wheel.transform.localRotation=Quaternion.Euler(90,0,0);}
            Box(v,"Ambulance Beacon",new Vector3(.8f,1.57f,0),new Vector3(.70f,.16f,.35f),red,red,orange);Bench(t,8,-2.5f);
            return g;
        }
        static LoopWayAsset Save(LoopWayGenerator owner,GameObject model,string file)
        {
            var prefab=PrefabUtility.SaveAsPrefabAsset(model,Folder+"/"+file+".prefab");UnityEngine.Object.DestroyImmediate(model);
            return new LoopWayAsset{prefab=prefab,position=new Vector3(owner.sidewalk.position.x-1,0,0),rotation=Quaternion.Euler(0,270,0),scale=Vector3.one};
        }
        public static void Setup(LoopWayGenerator owner)
        {
            EnsureFolder(Folder);Palette();owner.busStop=Save(owner,BuildStop(),"BusStop");owner.park=Save(owner,BuildPark(),"PocketPark");owner.hospital=Save(owner,BuildHospital(),"Hospital");EditorUtility.SetDirty(owner);AssetDatabase.SaveAssets();
        }
    }
}
