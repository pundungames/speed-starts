using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    [CustomEditor(typeof(LoopWayGenerator))]
    public sealed class LoopWayGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var g=(LoopWayGenerator)target;
            EditorGUILayout.HelpBox("Loop Count başlangıç dahil toplam bölüm sayısıdır. İlk hazırladığın bölüm korunur. Seed yalnızca Generated Loops altındaki devam bölümlerini değiştirir. İlk devam bölümü bina, ikincisi hastane, üçüncüsü durak, dördüncüsü bina, beşincisi park; sonrası seed ile çeşitlenir. Ön binalar farklı cephe yönleri, değişken aralıklar ve geri çekilmelerle gruplar halinde yerleşir. Her bölüm: 3–5 görünür çalı grubu, 6–8 sokak eşyası; şehir seyrek/orta/yoğun bölgelerle başlangıç sayısı ±6. Bulut sayısı ve şekli bağımsızdır; iki ardışık bölüm bulutsuz kalmaz. Lambalar eşit aralıklıdır.",MessageType.Info);
            serializedObject.Update();EditorGUI.BeginChangeCheck();DrawPropertiesExcluding(serializedObject,"m_Script");bool changed=EditorGUI.EndChangeCheck();serializedObject.ApplyModifiedProperties();
            if(changed&&g.autoPreview&&g.IsConfigured&&!Application.isPlaying)g.Generate();
            EditorGUILayout.LabelField("Bölüm uzunluğu",g.segmentLength.ToString("F3")+" m");
            EditorGUILayout.LabelField("Şehir bina aralığı",Mathf.Max(5,g.cityBaseline-6)+" – "+(g.cityBaseline+6));
            using(new EditorGUI.DisabledScope(!g.IsConfigured))
            {
                if(GUILayout.Button("Loopları Üret / Seed'i Uygula"))g.Generate();
                if(GUILayout.Button("Üretilen Loopları Temizle")){g.ClearGenerated();EditorSceneManager.MarkSceneDirty(g.gameObject.scene);}
            }
            EditorGUILayout.Space();
            if(GUILayout.Button("Başlangıç Düzeninden Şablonları Güncelle"))LoopWaySetup.Capture(g);
            EditorGUILayout.HelpBox("Başlangıçtaki objeleri düzenledikten sonra şablonları güncelle. Runtime Continuation açıkken Runner ilerledikçe arkada kalan üretilmiş bölümler yeni seed varyasyonlarıyla öne alınır. Başlangıç bölümü silinmez. Kopyaladığın bulutun şeklini Cloud Variation bileşeninden değiştir.",MessageType.None);
        }
    }
    public static class LoopWaySetup
    {
        const string Library="Assets/Speed-Staes/Prefabs/LoopWayLibrary";
        static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
        static Transform Child(LoopWayGenerator g,string name)
        {var t=g.transform.Find(name);if(!t)throw new InvalidOperationException("Başlangıç objesi bulunamadı: "+name);return t;}
        static LoopWayAsset CaptureAsset(LoopWayGenerator g,Transform source,string name)
        {
            var asset=new LoopWayAsset {position=g.transform.InverseTransformPoint(source.position),rotation=Quaternion.Inverse(g.transform.rotation)*source.rotation,scale=new Vector3(source.lossyScale.x/g.transform.lossyScale.x,source.lossyScale.y/g.transform.lossyScale.y,source.lossyScale.z/g.transform.lossyScale.z)};
            var copy=UnityEngine.Object.Instantiate(source.gameObject);copy.name=name;
            foreach(var row in copy.GetComponentsInChildren<PastelBuildingRow>())row.automaticUpdate=false;
            foreach(var plant in copy.GetComponentsInChildren<PastelPlantGenerator>())plant.automaticUpdate=false;
            copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);copy.transform.localScale=Vector3.one;
            asset.prefab=PrefabUtility.SaveAsPrefabAsset(copy,Library+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(copy);return asset;
        }
        public static void Capture(LoopWayGenerator g)
        {
            Folder(Library);Undo.RecordObject(g,"Capture LoopWay source layout");
            var roadNames=new[]{"Yol","Yol (1)","Yol (2)"};
            g.roads=roadNames.Select((n,i)=>CaptureAsset(g,Child(g,n),"Road_"+i)).ToArray();
            var bounds=roadNames.Select(n=>LoopWayGenerator.LocalBounds(Child(g,n).gameObject,g.transform)).ToArray();
            g.startZ=bounds.Min(b=>b.min.z);g.endZ=bounds.Max(b=>b.max.z);g.segmentLength=g.endZ-g.startZ;
            var row=Child(g,"Building Row - Count and Seed");g.cityBaseline=row.GetComponent<PastelBuildingRow>().buildingCount;g.city=CaptureAsset(g,row,"BackgroundCity");
            var cloudRoot=Child(g,"Clouds - Move Freely");
            foreach(Transform cloud in cloudRoot){var variation=cloud.GetComponent<CloudVariation>();if(!variation){variation=Undo.AddComponent<CloudVariation>(cloud.gameObject);variation.ReadMaterial();}}
            g.clouds=cloudRoot.Cast<Transform>().Select((t,i)=>CaptureAsset(g,t,"Cloud_"+i)).ToArray();
            var sidewalkRoots=g.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Sidewalk Under Hedges")).ToArray();
            var slab=sidewalkRoots[0].Cast<Transform>().First(t=>t.name.StartsWith("Paving Slab"));
            var curb=sidewalkRoots[0].Cast<Transform>().First(t=>t.name.StartsWith("Curb"));
            g.sidewalk=CaptureAsset(g,slab,"PavingSlab");g.curb=CaptureAsset(g,curb,"Curb");g.sidewalkEnd=sidewalkRoots.Max(t=>LoopWayGenerator.LocalBounds(t.gameObject,g.transform).max.z);
            var bridgeRoot=Child(g,"Background Bridge Canal and Riverside Trees");var modules=bridgeRoot.Find("Bridge - Repeating Rail Modules");
            var completeModule=modules.Cast<Transform>().OrderByDescending(t=>t.childCount).First();
            g.bridge=CaptureAsset(g,completeModule,"BridgeModule");var parts=modules.GetComponentsInChildren<Renderer>().GroupBy(r=>r.name).ToArray();
            g.bridgePartNames=parts.Select(p=>p.Key).ToArray();
            g.bridgePartEnds=parts.Select(p=>p.Max(r=>LoopWayGenerator.LocalBounds(r.gameObject,g.transform).max.z)).ToArray();
            g.bridgeEnd=parts.Where(p=>p.Key.Contains("Deck")||p.Key.Contains("Rail")).Min(p=>p.Max(r=>LoopWayGenerator.LocalBounds(r.gameObject,g.transform).max.z));
            var water=bridgeRoot.Find("Blue Canal Surface");g.water=CaptureAsset(g,water,"CanalWater");g.waterEnd=LoopWayGenerator.LocalBounds(water.gameObject,g.transform).max.z;
            var bank=bridgeRoot.Find("Near Riverside Bank");g.bank=CaptureAsset(g,bank,"CanalBank");g.bankEnd=LoopWayGenerator.LocalBounds(bank.gameObject,g.transform).max.z;
            var riverPlants=bridgeRoot.Find("Riverside Foliage - Independent Seeds");
            var nearPlant=riverPlants.Cast<Transform>().First(t=>t.position.x> -35);g.riversideHedge=CaptureAsset(g,nearPlant,"RiversideHedge");g.riversideEnd=LoopWayGenerator.LocalBounds(riverPlants.gameObject,g.transform).max.z;
            var lamps=g.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Street Lamp - Reference")).OrderBy(t=>t.position.z).ToArray();
            g.lamp=CaptureAsset(g,lamps[lamps.Length-1],"StreetLamp");g.lampAnchor=g.lamp.position.z;
            g.hedge=CaptureAsset(g,Child(g,"Pastel Plants - Independent Generators").GetChild(0),"ForegroundHedge");
            var buildingNames=new[]{"Right Street Building - Small Horizontal Windows","Right Street Building - Tall Blue Glazing"};
            var original=buildingNames.Select((n,i)=>CaptureAsset(g,Child(g,n),"ForegroundBuilding_"+i)).ToList();
            for(int i=0;i<3;i++)original.Add(CreateBuilding(g,i));g.buildings=original.ToArray();
            var furniture=Child(g,"Street Furniture - Beyond Camera");var props=furniture.Cast<Transform>().Select((t,i)=>CaptureAsset(g,t,"StreetProp_"+i)).ToList();
            props.Add(CaptureAsset(g,Child(g,"Vending Machine - Pastel Drinks"),"StreetProp_Vending"));
            for(int i=0;i<2;i++)props.Add(CreateExtraProp(g,i));props.AddRange(StreetPropCatalogBuilder.Build(g));g.props=props.ToArray();
            var movement=UnityEngine.Object.FindObjectOfType<RunnerMovement>();if(movement)g.runner=movement.transform;
            if(g.busStop==null||!g.busStop.prefab||g.park==null||!g.park.prefab||g.hospital==null||!g.hospital.prefab)LoopWayLandmarkBuilder.Setup(g);
            g.configured=true;EditorUtility.SetDirty(g);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
        }
        static Material Material(string name,Color color)
        {
            string folder="Assets/Speed-Staes/Materials/LoopWay";Folder(folder);string path=folder+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Speed Stars/Pastel Street Prop"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_Color",color);m.SetFloat("_Glow",1);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material front,Material side,Material top)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Speed-Staes/Prefabs/StreetProps/Signs/SignBoxSection.asset");
            var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{front,side,top};r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return g;
        }
        static LoopWayAsset CreateBuilding(LoopWayGenerator owner,int type)
        {
            string[] names={"MintCornerStore","TerracottaBalconyApartments","CreamWorkshop"};
            var root=new GameObject(names[type]);var t=root.transform;
            Color[] colors={new Color(.68f,.76f,.65f),new Color(.72f,.48f,.37f),new Color(.85f,.79f,.65f)};
            var wall=Material(names[type]+" Front",colors[type]);var side=Material(names[type]+" Side",colors[type]*.79f);var trim=Material("Pale Cream Trim",new Color(.85f,.84f,.74f));
            var glass=Material("Store Blue Glass",new Color(.24f,.43f,.55f));var glassShade=Material("Window Recess",new Color(.17f,.29f,.37f));
            var metal=Material("Muted Dark Frame",new Color(.20f,.26f,.29f));var green=Material("Muted Awning Green",new Color(.26f,.43f,.34f));
            float w=type==0?7:type==1?6.4f:8,h=type==0?7.6f:type==1?10.2f:5.5f,d=4.6f;
            Box(t,"Plaster Block",new Vector3(0,h*.5f,0),new Vector3(w,h,d),wall,side,trim);
            Box(t,"Roof Cornice",new Vector3(0,h+.1f,0),new Vector3(w+.28f,.22f,d+.22f),trim,side,trim);
            int floors=type==0?3:type==1?4:2;float step=(h-2.5f)/floors;
            for(int floor=0;floor<floors;floor++)for(int col=0;col<3;col++)
            {
                float x=(col-1)*w*.29f,y=2.8f+(floor+.5f)*step;
                Box(t,"Recessed Window",new Vector3(x,y,-d*.5f-.025f),new Vector3(w*.23f,step*.70f,.08f),glassShade,metal,metal);
                Box(t,"Blue Window Pane",new Vector3(x,y-.03f,-d*.5f-.075f),new Vector3(w*.20f,step*.59f,.025f),glass,glassShade,glass);
                Box(t,"Window Sill",new Vector3(x,y-step*.36f,-d*.5f-.10f),new Vector3(w*.25f,.09f,.22f),trim,side,trim);
                if(type==1&&col!=1)
                {
                    Box(t,"Balcony Slab",new Vector3(x,y-step*.39f,-d*.5f-.40f),new Vector3(w*.27f,.13f,.80f),trim,side,trim);
                    Box(t,"Balcony Front Rail",new Vector3(x,y-.18f,-d*.5f-.79f),new Vector3(w*.27f,.08f,.07f),metal,metal,trim);
                    for(int bar=0;bar<5;bar++)Box(t,"Balcony Baluster",new Vector3(x-w*.11f+bar*w*.055f,y-.40f,-d*.5f-.79f),new Vector3(.045f,.43f,.045f),metal,metal,trim);
                }
            }
            for(int floor=0;floor<floors;floor++)for(int col=0;col<2;col++)
                Box(t,"Side Window",new Vector3(w*.5f+.025f,2.8f+(floor+.5f)*step,(col-.5f)*2.0f),new Vector3(.07f,step*.60f,1.25f),glassShade,glass,trim);
            for(int col=0;col<3;col++)
            {
                float x=(col-1)*w*.29f;
                Box(t,"Ground Store Glass",new Vector3(x,1.22f,-d*.5f-.06f),new Vector3(w*.25f,2.30f,.12f),glass,glassShade,glass);
                Box(t,"Door Mullion",new Vector3(x,1.22f,-d*.5f-.14f),new Vector3(.08f,2.30f,.07f),metal,metal,trim);
            }
            if(type!=1)
            {
                var awning=Box(t,type==0?"Green Shop Awning":"Navy Workshop Awning",new Vector3(0,2.6f,-d*.5f-.40f),new Vector3(w*.93f,.25f,.95f),type==0?green:metal,metal,type==0?green:metal);awning.transform.localRotation=Quaternion.Euler(-8,0,0);
                if(type==0)for(int stripe=0;stripe<7;stripe++)Box(t,"Awning Cream Stripe",new Vector3(-w*.40f+stripe*w*.133f,2.74f,-d*.5f-.4f),new Vector3(.20f,.025f,.91f),trim,trim,trim);
            }
            if(type==2)
            {
                Box(t,"Stepped Roof Room",new Vector3(w*.23f,h+.72f,.3f),new Vector3(w*.40f,1.15f,d*.64f),wall,side,trim);
                Box(t,"Blank Shop Fascia",new Vector3(0,3.0f,-d*.5f-.14f),new Vector3(w*.65f,.45f,.16f),trim,side,trim);
            }
            root.transform.position=new Vector3(-22.5f,0,10);root.transform.rotation=Quaternion.Euler(0,252,0);
            var asset=CaptureAsset(owner,root.transform,names[type]);UnityEngine.Object.DestroyImmediate(root);return asset;
        }
        static LoopWayAsset CreateExtraProp(LoopWayGenerator owner,int type)
        {
            string name=type==0?"ParkingMeter":"StreetUtilityCabinet";var root=new GameObject(name);var t=root.transform;
            var metal=Material("Prop Dark Steel",new Color(.20f,.27f,.29f));var lit=Material("Prop Lit Steel",new Color(.34f,.42f,.43f));var cream=Material("Utility Pale Gray",new Color(.64f,.68f,.65f));
            if(type==0)
            {
                Box(t,"Square Base",new Vector3(0,.05f,0),new Vector3(.3f,.10f,.3f),metal,lit,lit);
                Box(t,"Post",new Vector3(0,.68f,0),new Vector3(.10f,1.25f,.10f),metal,lit,lit);
                Box(t,"Meter Head",new Vector3(0,1.42f,0),new Vector3(.38f,.48f,.27f),cream,lit,cream);
                Box(t,"Display",new Vector3(0,1.51f,-.142f),new Vector3(.26f,.16f,.02f),metal,metal,metal);
                Box(t,"Coin Slot",new Vector3(0,1.30f,-.145f),new Vector3(.10f,.025f,.02f),metal,metal,metal);
            }
            else
            {
                Box(t,"Cabinet",new Vector3(0,.66f,0),new Vector3(.9f,1.25f,.50f),cream,lit,cream);
                Box(t,"Overhanging Top",new Vector3(0,1.32f,0),new Vector3(.98f,.10f,.58f),cream,lit,cream);
                Box(t,"Door Seam",new Vector3(.20f,.67f,-.257f),new Vector3(.012f,1.08f,.01f),metal,metal,metal);
                Box(t,"Handle",new Vector3(.10f,.75f,-.28f),new Vector3(.035f,.15f,.045f),metal,metal,lit);
                for(int j=0;j<4;j++)Box(t,"Vent",new Vector3(-.15f,.30f+j*.065f,-.26f),new Vector3(.35f,.018f,.01f),metal,metal,metal);
            }
            root.transform.position=new Vector3(-13.5f,.12f,10);root.transform.rotation=Quaternion.Euler(0,270,0);
            var asset=CaptureAsset(owner,t,name);UnityEngine.Object.DestroyImmediate(root);return asset;
        }
    }
}








