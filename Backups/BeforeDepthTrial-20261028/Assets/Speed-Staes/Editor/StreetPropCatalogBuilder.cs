using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    public static class StreetPropCatalogBuilder
    {
        const string Folder="Assets/Speed-Staes/Prefabs/StreetProps/ExtraStreetProps";
        static Material dark,lit,cream,red,orange,wood,woodTop,teal,green,soil,yellow;
        static void Ensure(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');Ensure(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
        static Material Mat(string name,Color c)
        {string folder="Assets/Speed-Staes/Materials/ExtraStreetProps";Ensure(folder);string path=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Speed Stars/Pastel Street Prop"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_Color",c);m.SetFloat("_Glow",1);EditorUtility.SetDirty(m);return m;}
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,Material top=null)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Speed-Staes/Prefabs/StreetProps/Signs/SignBoxSection.asset");var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{material,material,top?top:material};r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return g;}
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float thickness,Material material)
        {var g=Box(parent,name,(a+b)*.5f,new Vector3(thickness,(a-b).magnitude,thickness),material,lit);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void Ring(Transform parent,Vector3 center,float radius)
        {
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;Beam(parent,"Faceted Bicycle Tire",center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),center+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0),.075f,dark);}
            for(int i=0;i<4;i++){float a=i*Mathf.PI/2;Beam(parent,"Wheel Spoke",center,center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),.025f,lit);}
        }
        static GameObject BicycleRack()
        {
            var g=new GameObject("Bicycle Rack and Parked Bicycle");var t=g.transform;
            foreach(float x in new[]{-1.2f,0,1.2f}){Beam(t,"Rack Left Upright",new Vector3(x-.32f,.05f,.35f),new Vector3(x-.32f,.95f,.35f),.075f,lit);Beam(t,"Rack Top",new Vector3(x-.32f,.95f,.35f),new Vector3(x+.32f,.95f,.35f),.075f,lit);Beam(t,"Rack Right Upright",new Vector3(x+.32f,.95f,.35f),new Vector3(x+.32f,.05f,.35f),.075f,lit);}
            Vector3 rear=new Vector3(-.68f,.46f,-.26f),front=new Vector3(.78f,.46f,-.26f),pedal=new Vector3(0,.47f,-.26f),seat=new Vector3(-.2f,1.02f,-.26f),handle=new Vector3(.52f,1.03f,-.26f);
            Ring(t,rear,.40f);Ring(t,front,.40f);
            foreach(var pair in new[]{new[]{rear,seat},new[]{seat,pedal},new[]{pedal,rear},new[]{seat,handle},new[]{handle,pedal},new[]{handle,front}})Beam(t,"Painted Teal Bike Frame",pair[0],pair[1],.055f,teal);
            Beam(t,"Handlebar Stem",handle,handle+new Vector3(.03f,.20f,0),.045f,dark);Beam(t,"Handlebar",handle+new Vector3(.03f,.20f,-.18f),handle+new Vector3(.03f,.20f,.18f),.045f,dark);
            Box(t,"Bike Saddle",seat+Vector3.up*.09f,new Vector3(.34f,.08f,.19f),dark);
            Box(t,"Bike Pedal",pedal+new Vector3(.03f,-.05f,-.12f),new Vector3(.20f,.05f,.08f),dark);
            return g;
        }
        static GameObject Mailbox()
        {
            var g=new GameObject("Red Street Mailbox");var t=g.transform;
            Box(t,"Dark Plinth",new Vector3(0,.08f,0),new Vector3(.70f,.16f,.60f),dark,lit);
            Box(t,"Mailbox Body",new Vector3(0,.78f,0),new Vector3(.68f,1.36f,.56f),red,orange);
            Box(t,"Overhanging Hood",new Vector3(0,1.50f,-.04f),new Vector3(.80f,.16f,.69f),red,orange);
            Box(t,"Black Letter Slot",new Vector3(0,1.21f,-.291f),new Vector3(.49f,.095f,.025f),dark);
            Box(t,"White Postal Label",new Vector3(0,.96f,-.294f),new Vector3(.42f,.20f,.025f),cream);
            Box(t,"Lower Door Recess",new Vector3(0,.47f,-.29f),new Vector3(.50f,.48f,.02f),dark);
            Box(t,"Collection Door",new Vector3(0,.47f,-.31f),new Vector3(.46f,.44f,.02f),red);
            Box(t,"Door Lock",new Vector3(.15f,.53f,-.33f),new Vector3(.05f,.07f,.025f),lit);return g;
        }
        static GameObject NewsStand()
        {
            var g=new GameObject("Newspaper and Magazine Stand");var t=g.transform;
            Box(t,"Wood Cabinet",new Vector3(0,.48f,0),new Vector3(1.80f,.92f,.70f),wood,woodTop);
            Box(t,"Counter Lip",new Vector3(0,.97f,-.04f),new Vector3(1.95f,.11f,.85f),wood,woodTop);
            foreach(float x in new[]{-.83f,.83f})Box(t,"Canopy Post",new Vector3(x,1.35f,.27f),new Vector3(.07f,.8f,.07f),dark,lit);
            var canopy=Box(t,"Small Teal Canopy",new Vector3(0,1.85f,0),new Vector3(2.12f,.12f,1.07f),teal,cream);canopy.transform.localRotation=Quaternion.Euler(-7,0,0);
            for(int i=0;i<4;i++)
            {
                float x=-.66f+i*.44f;var cover=Box(t,"Magazine Cover",new Vector3(x,1.15f,-.16f),new Vector3(.35f,.40f,.055f),i%3==0?red:i%3==1?teal:cream);cover.transform.localRotation=Quaternion.Euler(-18,0,0);
                Box(t,"Magazine Headline",new Vector3(x,1.25f,-.215f),new Vector3(.26f,.055f,.015f),cream);
                Box(t,"Stack of Papers",new Vector3(x,1.04f,.12f),new Vector3(.35f,.09f,.24f),cream);
            }
            Box(t,"Front Paper Display",new Vector3(0,.57f,-.375f),new Vector3(1.5f,.55f,.04f),cream);
            for(int i=0;i<4;i++)Box(t,"Printed Newspaper Line",new Vector3(0,.74f-i*.11f,-.402f),new Vector3(1.20f,.028f,.015f),dark);return g;
        }
        static GameObject Bollards()
        {
            var g=new GameObject("Three Painted Road Bollards");var t=g.transform;
            foreach(float x in new[]{-.80f,0,.80f})
            {Box(t,"Bollard Base",new Vector3(x,.04f,0),new Vector3(.27f,.08f,.27f),dark,lit);Box(t,"Bollard Post",new Vector3(x,.49f,0),new Vector3(.16f,.90f,.16f),dark,lit);Box(t,"Reflective Yellow Band",new Vector3(x,.72f,0),new Vector3(.17f,.14f,.17f),yellow);Box(t,"Bollard Cap",new Vector3(x,.96f,0),new Vector3(.19f,.07f,.19f),lit);}
            return g;
        }
        static GameObject Planter()
        {
            var g=new GameObject("Terracotta Flower Planter");var t=g.transform;
            Box(t,"Terracotta Planter",new Vector3(0,.31f,0),new Vector3(1.85f,.62f,.74f),wood,orange);
            Box(t,"Planter Upper Rim",new Vector3(0,.65f,0),new Vector3(2.02f,.14f,.87f),orange,woodTop);
            Box(t,"Dark Soil",new Vector3(0,.726f,0),new Vector3(1.72f,.025f,.60f),soil);
            for(int i=0;i<6;i++)
            {
                float x=-.73f+i*.29f,y=.98f+(i%2)*.18f,z=(i%2==0?-.16f:.16f);
                Beam(t,"Flower Stem",new Vector3(x,.73f,z),new Vector3(x,y,z),.035f,green);
                var leaf=Box(t,"Broad Green Leaf",new Vector3(x+.08f,y-.16f,z),new Vector3(.23f,.09f,.13f),green);leaf.transform.localRotation=Quaternion.Euler(0,0,25);
                var color=i%2==0?red:yellow;Box(t,"Flower Petals",new Vector3(x,y,z),new Vector3(.22f,.10f,.22f),color);Box(t,"Flower Center",new Vector3(x,y+.065f,z),new Vector3(.075f,.055f,.075f),cream);
            }
            return g;
        }
        static GameObject Cone(Transform parent,string name,Vector3 pos)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=pos;
            Box(root.transform,"Cone Rubber Base",new Vector3(0,.045f,0),new Vector3(.56f,.09f,.56f),dark);
            // Four broad painted faces, with a white band intersecting the taper.
            var mesh=new Mesh();mesh.name="SquareTaperTrafficCone";var v=new List<Vector3>();var triangles=new List<int>();
            for(int side=0;side<4;side++){float a=(side*90+45)*Mathf.Deg2Rad,b=((side+1)*90+45)*Mathf.Deg2Rad;int k=v.Count;v.Add(new Vector3(Mathf.Cos(a)*.31f,.09f,Mathf.Sin(a)*.31f));v.Add(new Vector3(Mathf.Cos(b)*.31f,.09f,Mathf.Sin(b)*.31f));v.Add(new Vector3(Mathf.Cos(b)*.05f,.81f,Mathf.Sin(b)*.05f));v.Add(new Vector3(Mathf.Cos(a)*.05f,.81f,Mathf.Sin(a)*.05f));triangles.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});}
            mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Folder+"/TrafficConeMesh.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var body=new GameObject("Orange Tapered Cone");body.transform.SetParent(root.transform,false);body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterial=orange;
            Box(root.transform,"White Reflective Band",new Vector3(0,.48f,0),new Vector3(.26f,.13f,.26f),cream);return root;
        }
        static GameObject Cones()
        {var g=new GameObject("Traffic Cone Pair");Cone(g.transform,"Traffic Cone A",new Vector3(-.48f,0,-.12f));Cone(g.transform,"Traffic Cone B",new Vector3(.48f,0,.17f));return g;}
        static GameObject Barrier()
        {
            var g=new GameObject("Street Maintenance Barrier");var t=g.transform;
            foreach(float x in new[]{-.91f,.91f}){Box(t,"Wide Barrier Foot",new Vector3(x,.055f,0),new Vector3(.20f,.11f,.85f),dark,lit);Box(t,"Barrier Upright",new Vector3(x,.61f,0),new Vector3(.09f,1.15f,.09f),dark,lit);}
            Box(t,"Cream Barrier Board",new Vector3(0,.87f,0),new Vector3(2.12f,.38f,.12f),cream);
            for(int i=0;i<6;i++){var stripe=Box(t,"Red Diagonal Stripe",new Vector3(-.85f+i*.34f,.87f,-.071f),new Vector3(.15f,.35f,.025f),red);stripe.transform.localRotation=Quaternion.Euler(0,0,-23);}
            Box(t,"Barrier Lower Rail",new Vector3(0,.35f,0),new Vector3(1.9f,.07f,.07f),lit);return g;
        }
        public static LoopWayAsset[] Build(LoopWayGenerator owner)
        {
            Ensure(Folder);dark=Mat("Charcoal Painted Metal",new Color(.18f,.24f,.27f));lit=Mat("Metal Lit Surface",new Color(.32f,.40f,.42f));cream=Mat("Warm White",new Color(.85f,.87f,.80f));red=Mat("Muted Postal Red",new Color(.76f,.24f,.23f));orange=Mat("Painted Orange",new Color(.86f,.43f,.23f));wood=Mat("Dark Wood",new Color(.49f,.34f,.24f));woodTop=Mat("Wood Top",new Color(.64f,.47f,.29f));teal=Mat("Deep Teal",new Color(.22f,.43f,.46f));green=Mat("Leaf Green",new Color(.34f,.55f,.18f));soil=Mat("Planter Soil",new Color(.25f,.26f,.16f));yellow=Mat("Reflective Ochre",new Color(.94f,.70f,.23f));
            var models=new[]{BicycleRack(),Mailbox(),NewsStand(),Bollards(),Planter(),Cones(),Barrier()};var result=new List<LoopWayAsset>();
            for(int i=0;i<models.Length;i++){var model=models[i];string file=model.name.Replace(" ","");var prefab=PrefabUtility.SaveAsPrefabAsset(model,Folder+"/"+file+".prefab");UnityEngine.Object.DestroyImmediate(model);result.Add(new LoopWayAsset{prefab=prefab,position=new Vector3(owner.lamp.position.x-1.9f,.12f,0),rotation=Quaternion.Euler(0,270,0),scale=Vector3.one});}
            AssetDatabase.SaveAssets();return result.ToArray();
        }
        public static void AddToCatalog(LoopWayGenerator owner)
        {var extra=Build(owner);var current=owner.props==null?new List<LoopWayAsset>():owner.props.Where(p=>p.prefab&&!AssetDatabase.GetAssetPath(p.prefab).StartsWith(Folder)).ToList();current.AddRange(extra);owner.props=current.ToArray();EditorUtility.SetDirty(owner);}
    }
}
