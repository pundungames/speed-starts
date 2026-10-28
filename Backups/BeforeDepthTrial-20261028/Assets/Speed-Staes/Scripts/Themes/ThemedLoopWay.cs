using System;
using System.Collections.Generic;
using UnityEngine;
using SpeedStars.Physical;
using SpeedStars.Runner;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
namespace SpeedStars.Themes
{
    [DisallowMultipleComponent]
    public sealed partial class ThemedLoopWay:MonoBehaviour
    {
        public LoopThemeDefinition theme;
        [Range(1,20)]public int loopCount=3;
        public int seed=42;
        public bool autoPreview=true;
        public bool streamAtRuntime=true;
        public Transform runner;
        [Min(5)]public float retireDistance=25;
        [Min(2)]public float romanLampSpacing=11.81f;
        public const float RoadLength=20.14339f;
        public const float SectionLength=RoadLength*3;
        public const float FirstRoadCenter=-9.95f;
        public const float StartZ=FirstRoadCenter-RoadLength*.5f;
        readonly List<ThemeChunk> live=new List<ThemeChunk>();
        Transform generated;int next;
        void Start(){if(Application.isPlaying&&streamAtRuntime){if(!runner){var r=FindObjectOfType<RunnerBody>();if(r)runner=r.transform;}if(loopCount<2)loopCount=2;if(live.Count==0)Generate();}}
        void Update(){if(!Application.isPlaying||!streamAtRuntime||!runner||live.Count==0)return;float z=transform.InverseTransformPoint(runner.position).z;for(int n=0;n<8&&live.Count>0&&z>live[0].end+retireDistance;n++){var old=live[0];live.RemoveAt(0);old.gameObject.SetActive(false);Destroy(old.gameObject);live.Add(Build(next++));}}
        public void Generate()
        {
            if(theme&&theme.concept==EnvironmentConcept.Egypt&&Application.isPlaying&&streamAtRuntime){loopCount=Mathf.Max(4,loopCount);retireDistance=Mathf.Max(70,retireDistance);}
            if(theme&&theme.concept==EnvironmentConcept.PirateSea&&Application.isPlaying&&streamAtRuntime){loopCount=Mathf.Max(4,loopCount);retireDistance=Mathf.Max(90,retireDistance);}
            if(!theme||!theme.road)return;Clear();ResetRomanStreetLayout();ResetEgyptDesertLayout();ResetMarineLayout();ResetSkyLayout();generated=Group("Generated Theme - "+theme.concept,transform);generated.gameObject.AddComponent<ThemeGeneratedRoot>();
            for(int i=0;i<Mathf.Clamp(loopCount,1,20);i++)live.Add(Build(i));next=loopCount;
#if UNITY_EDITOR
            if(!Application.isPlaying){EditorUtility.SetDirty(this);if(gameObject.scene.IsValid())EditorSceneManager.MarkSceneDirty(gameObject.scene);}
#endif
        }
        public void Clear(){var owned=new List<GameObject>();foreach(Transform child in transform)if(child.GetComponent<ThemeGeneratedRoot>())owned.Add(child.gameObject);foreach(var g in owned){g.SetActive(false);if(Application.isPlaying)Destroy(g);else DestroyImmediate(g);}live.Clear();generated=null;}
        static int Hash(int seed,int index){unchecked{return (seed*73856093)^(index*19349663)^973719;}}
        static float R(System.Random random,float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
        static Transform Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        GameObject Item(GameObject prefab,Transform parent,Vector3 p,float yaw=270,float scale=1)
        {var g=Instantiate(prefab,parent,false);g.name=prefab.name;g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=prefab.transform.localScale*scale;return g;}
        GameObject Block(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;var collider=g.GetComponent<Collider>();if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);var renderer=g.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g;}
        ThemeChunk Build(int index)
        {
            int sectionSeed=Hash(seed,index);var random=new System.Random(sectionSeed);var root=Group("Section "+index.ToString("00")+" - "+theme.concept,generated);var chunk=root.gameObject.AddComponent<ThemeChunk>();chunk.index=index;chunk.seed=sectionSeed;float a=StartZ+index*SectionLength,b=a+SectionLength;chunk.start=a;chunk.end=b;
            var infrastructure=Group("Unchanged Road Geometry",root);
            for(int t=0;t<3;t++)
            {
                var road=Item(theme.road,infrastructure,new Vector3(-5.63f,0,FirstRoadCenter+index*SectionLength+t*RoadLength),0);
                road.name="Road - Original Mesh "+t;
                foreach(var renderer in road.GetComponentsInChildren<Renderer>())
                {var materials=renderer.sharedMaterials;for(int m=0;m<materials.Length;m++)materials[m]=materials[m]&&materials[m].HasProperty("_Asphalt")&&materials[m].GetFloat("_Asphalt")>.5f?theme.asphaltMaterial:theme.paletteMaterial;renderer.sharedMaterials=materials;}
            }
            var terrain=Group("Continuous Theme Setting",root);
            bool romanSetting=theme.concept==EnvironmentConcept.RomeByzantium;
            bool wideGround=romanSetting||theme.concept==EnvironmentConcept.Egypt;
            if(!theme.noGround)Block(terrain,"Continuous Ground",new Vector3(wideGround?-78:-43,-.55f,(a+b)*.5f),new Vector3(wideGround?132:64,.7f,SectionLength),theme.groundMaterial);
            if(theme.waterAcrossRoad)Block(terrain,"Open Sea Under Road",new Vector3(-55,-.42f,(a+b)*.5f),new Vector3(154,.10f,SectionLength),theme.waterMaterial);

            if(!theme.noGround)Block(terrain,"Pedestrian Terrace",new Vector3(-19.5f,-.05f,(a+b)*.5f),new Vector3(16,.20f,SectionLength),theme.pathMaterial);
            else if(theme.concept==EnvironmentConcept.PirateSea){Block(terrain,"Timber Harbor Boardwalk",new Vector3(-17,-.08f,(a+b)*.5f),new Vector3(11,.20f,SectionLength),theme.pathMaterial);for(float z=a+3;z<b;z+=11.81f)foreach(float x in new[]{-12.5f,-21.5f})Block(terrain,"Boardwalk Support Pile",new Vector3(x,-1,z),new Vector3(.28f,2,.28f),theme.pathMaterial);}

            if(theme.concept==EnvironmentConcept.RomeByzantium){BuildRomanStreetLayout(root,chunk,a,b,index,sectionSeed);return chunk;}
            if(theme.concept==EnvironmentConcept.Egypt){BuildEgyptDesertLayout(root,chunk,a,b,index,sectionSeed);return chunk;}
            if(theme.concept==EnvironmentConcept.PirateSea){BuildMarineLayout(root,chunk,a,b,index,sectionSeed);return chunk;}if(theme.concept==EnvironmentConcept.FloatingSky){BuildSkyLayout(root,chunk,a,b,index,sectionSeed);return chunk;}
            var foreground=Group("Irregular Landmark Plots",root);var names=new List<string>();var naturalSelection=new RomanStreetSectionPlan();
            // Curated introduction, followed by seeded selections and varying silhouette gaps.
            float cursor=a+R(random,3,6);int placed=0;
            while(cursor<b-5)
            {
                var candidate=PickMeasuredRomanAsset(new List<GameObject>(theme.landmarks),random,naturalSelection);if(!candidate)break;int choice=Array.IndexOf(theme.landmarks,candidate);
                if(theme.concept==EnvironmentConcept.RomeByzantium){
                    var candidates=new List<int>();
                    for(int c=0;c<theme.landmarks.Length;c++){
                        string n=theme.landmarks[c].name;
                        if(n.Contains("Aqueduct")||names.Contains(n)||RomanLandmarkNearby(n,cursor)||(n.Contains("Plaza")&&index%3!=1))continue;
                        candidates.Add(c);
                    }
                    if(candidates.Count==0)break;choice=candidates[random.Next(candidates.Count)];
                }
                float yaw=270+R(random,-15,15),scale=R(random,.82f,1.04f);
                if(theme.concept==EnvironmentConcept.FloatingSky)scale*=.44f;else if(theme.concept==EnvironmentConcept.PirateSea){var native=MeasuredRomanBounds(theme.landmarks[choice],Vector3.one,270);scale=Mathf.Min(scale,6.3f/Mathf.Max(1,native.size.y));scale=Mathf.Min(scale,12f/Mathf.Max(1,native.size.z));}
                else if(theme.concept==EnvironmentConcept.Egypt)scale*=.46f;
                var g=Item(theme.landmarks[choice],foreground,Vector3.zero,yaw,scale);Bounds bounds=BoundsIn(g,transform);
                if(theme.concept==EnvironmentConcept.RomeByzantium){g.transform.localScale*=Mathf.Min(.62f,4.5f/Mathf.Max(1,bounds.size.y));bounds=BoundsIn(g,transform);}
                float width=Mathf.Max(5,bounds.size.z);
                if(cursor+width>b-2){Remove(g);break;}
                var p=g.transform.localPosition;p.z+=cursor-bounds.min.z;p.x+=(theme.concept==EnvironmentConcept.FloatingSky?-30f:theme.concept==EnvironmentConcept.Egypt?-23f:theme.concept==EnvironmentConcept.RomeByzantium?-19f:-23f)-R(random,0,3)-bounds.max.x;p.y=theme.concept==EnvironmentConcept.FloatingSky?R(random,.9f,1.25f):0;g.transform.localPosition=p;

                cursor+=width+(theme.concept==EnvironmentConcept.RomeByzantium?R(random,3,7):R(random,4,10));placed++;names.Add(g.name);naturalSelection.buildings.Add(new RomanStreetPlacement{asset=theme.landmarks[choice]});RecordRomanModel(theme.landmarks[choice]);
            }
            chunk.landmarkSummary=string.Join(", ",names.ToArray());
            bool countryside=theme.concept==EnvironmentConcept.RomeByzantium&&theme.ruralScenes!=null&&theme.ruralScenes.Length>0&&RomanLandscapeChoice(index)<theme.ruralScenes.Length;
            var backdrop=Group("Distant Theme Silhouettes",root);
            var backgroundOrder=new List<int>();for(int i=0;i<theme.background.Length;i++){if(theme.concept==EnvironmentConcept.RomeByzantium&&((theme.background[i].name.Contains("Aqueduct")&&index%3!=0)||(theme.background[i].name.Contains("Courtyard")&&index%3!=2)))continue;if(theme.concept==EnvironmentConcept.RomeByzantium&&names.Exists(n=>RomanFamily(n)==RomanFamily(theme.background[i].name)))continue;backgroundOrder.Add(i);}for(int i=backgroundOrder.Count-1;i>0;i--){int j=random.Next(i+1),temp=backgroundOrder[i];backgroundOrder[i]=backgroundOrder[j];backgroundOrder[j]=temp;}
            int backgroundCount=theme.concept==EnvironmentConcept.RomeByzantium?random.Next(4,7):random.Next(3,6);if(theme.concept==EnvironmentConcept.RomeByzantium)backgroundCount=Mathf.Min(backgroundCount,backgroundOrder.Count);for(int i=0;i<backgroundCount;i++)
            {
                float z=a+(i+.5f)*SectionLength/backgroundCount+R(random,-3,3),x=R(random,-76,-53);
                float y=theme.backgroundHeight+(theme.concept==EnvironmentConcept.FloatingSky?R(random,-1.5f,-.5f):0);
                float yaw=270+R(random,-20,20),scale=R(random,.85f,1.35f)*(theme.concept==EnvironmentConcept.FloatingSky?.45f:theme.concept==EnvironmentConcept.Egypt?.55f:1);
                if(theme.concept==EnvironmentConcept.PirateSea){var native=MeasuredRomanBounds(theme.background[backgroundOrder[i%backgroundOrder.Count]],Vector3.one,yaw);scale=Mathf.Min(scale,6.2f/Mathf.Max(1,native.size.y));}if(!countryside){var distant=Item(theme.background[backgroundOrder[i%backgroundOrder.Count]],backdrop,new Vector3(x,y,z),yaw,scale);if(theme.concept==EnvironmentConcept.RomeByzantium)distant.transform.localScale*=Mathf.Min(.75f,8.2f/Mathf.Max(1,BoundsIn(distant,transform).size.y));}
            }
            if(countryside)BuildRomanRuralScene(root,a,b,index,sectionSeed);
            if(theme.concept!=EnvironmentConcept.FloatingSky){
            var greens=Group("Organic Plant and Accent Groups",root);
            int greenCount=random.Next(4,7);for(int i=0;i<greenCount;i++)
            {var p=new Vector3(R(random,-15.8f,-13.8f),0,a+(i+.5f)*SectionLength/greenCount+R(random,-1.2f,1.2f));Item(theme.vegetation[random.Next(theme.vegetation.Length)],greens,p,270+R(random,-8,8),R(random,.7f,1.0f)*(theme.concept==EnvironmentConcept.Egypt?.62f:theme.concept==EnvironmentConcept.RomeByzantium?.55f:1));}
            var furnishings=Group("Theme Props and Lantern Rhythm",root);
            float spacing=11.81f;int start=Mathf.CeilToInt((a+3)/spacing);for(int lamp=start;lamp*spacing<b;lamp++)Item(theme.lamps[0],furnishings,new Vector3(-12.4f,0,lamp*spacing),270,theme.concept==EnvironmentConcept.Egypt?.78f:theme.concept==EnvironmentConcept.RomeByzantium?.7f:1);
            int count=theme.concept==EnvironmentConcept.RomeByzantium?random.Next(10,14):random.Next(6,9);var order=new List<int>();for(int i=0;i<theme.props.Length;i++){if(theme.concept==EnvironmentConcept.RomeByzantium&&theme.props[i].name.Contains("Statue"))continue;order.Add(i);}for(int i=order.Count-1;i>0;i--){int j=random.Next(i+1),temp=order[i];order[i]=order[j];order[j]=temp;}
            for(int i=0;i<count;i++){var prop=theme.props[order[i%order.Count]];if(theme.concept==EnvironmentConcept.RomeByzantium&&prop.name.Contains("Statue"))prop=theme.props[(i+index)%3];Item(prop,furnishings,new Vector3(R(random,-15.0f,-13.0f),0,a+(i+.5f)*SectionLength/count+R(random,-1,1)),270+R(random,-12,12),R(random,.85f,1.1f)*(theme.concept==EnvironmentConcept.Egypt&&prop.name.Contains("Obelisk")?.45f:theme.concept==EnvironmentConcept.RomeByzantium?.65f:1));}
            }
            if(theme.concept!=EnvironmentConcept.RomeByzantium&&theme.accent!=null&&theme.accent.Length>0)
            {var accents=Group("Signature Story Moments",root);Item(theme.accent[new System.Random(Hash(seed,index+814)).Next(theme.accent.Length)],accents,new Vector3(theme.concept==EnvironmentConcept.PirateSea?-42:-35,theme.concept==EnvironmentConcept.FloatingSky?.5f:0,a+SectionLength*.60f),270+R(random,-12,12),theme.concept==EnvironmentConcept.FloatingSky?.38f:theme.concept==EnvironmentConcept.PirateSea?.48f:theme.concept==EnvironmentConcept.Egypt?.8f:theme.concept==EnvironmentConcept.RomeByzantium?.55f:1);}
            if(theme.clouds!=null&&theme.clouds.Length>0)
            {
                var clouds=Group("Independent Background Clouds",root);int cloudCount=theme.concept==EnvironmentConcept.RomeByzantium?random.Next(1,3):(index%2==0)?random.Next(1,3):random.Next(0,3);
                for(int i=0;i<cloudCount;i++){var c=Item(theme.clouds[random.Next(theme.clouds.Length)],clouds,new Vector3(-95,theme.concept==EnvironmentConcept.RomeByzantium?R(random,4,7):R(random,16,25),a+(i+.5f)*SectionLength/cloudCount),270,R(random,.65f,.95f));var variation=c.GetComponent<CloudVariation>();if(!variation){variation=c.AddComponent<CloudVariation>();variation.ReadMaterial();}variation.shapeVariant=R(random,0,1000);variation.Apply();}
            }
            if(theme.concept==EnvironmentConcept.FloatingSky)BuildSkyComposition(root,a,b,sectionSeed);
            return chunk;
        }
        int RomanLandscapeChoice(int index){
            int count=theme.ruralScenes.Length+2;
            var order=new List<int>();for(int i=0;i<count;i++)order.Add(i);
            var random=new System.Random(Hash(seed,index/count+2071));
            for(int i=count-1;i>0;i--){int j=random.Next(i+1),temp=order[i];order[i]=order[j];order[j]=temp;}
            return order[index%count];
        }
        void BuildRomanRuralScene(Transform section,float start,float end,int index,int sectionSeed){
            var random=new System.Random(Hash(sectionSeed,3091));int choice=RomanLandscapeChoice(index);
            var landscape=Group("Roman Countryside View",section);
            var prefab=theme.ruralScenes[choice];
            var scene=Item(prefab,landscape,new Vector3(R(random,-52,-47),0,(start+end)*.5f+R(random,-3,3)),270+R(random,-3,3),R(random,.88f,.98f));
            scene.name=prefab.name;
            if(prefab.name.Contains("Chapel"))scene.transform.localScale*=Mathf.Min(1,7.1f/Mathf.Max(1,BoundsIn(scene,transform).size.y));
        }
        bool RomanLandmarkNearby(string name,float z){
            if(!generated)return false;
            foreach(Transform section in generated){
                if(!section.gameObject.activeSelf)continue;
                var plots=section.Find("Irregular Landmark Plots");if(!plots)continue;
                foreach(Transform item in plots)if(item.name==name&&Mathf.Abs(transform.InverseTransformPoint(item.position).z-z)<26)return true;
            }
            return false;
        }
        static string RomanFamily(string name){
            if(name.Contains("Basilica")||name.Contains("Domed"))return "Basilica";
            if(name.Contains("Forum")||name.Contains("Temple"))return "Temple";
            if(name.Contains("Triumphal"))return "Gate";
            if(name.Contains("Mosaic Courtyard")||name.Contains("Mosaic Plaza"))return "Plaza";
            if(name.Contains("Aqueduct"))return "Aqueduct";
            return name.Replace("Distant ","");
        }
        GameObject EgyptianAsset(GameObject[] assets,string name)
        {foreach(var asset in assets)if(asset.name.Contains(name))return asset;return null;}
        void BuildEgyptLayout(Transform section,ThemeChunk chunk,float start,float end,int index,int sectionSeed)
        {
            var random=new System.Random(Hash(sectionSeed,441));
            var monuments=Group("Spaced Desert Landmarks",section);var details=Group("Small Desert Detail Clusters",section);
            var names=new List<string>();
            // A temple is a rare event, with no duplicate temple in the backdrop.
            string[] first={"Pyramid","Sphinx","Oasis","Pyramid","Merchant"};
            string primary=index%5==1?"Pylon":first[(index%5+5)%5];
            int count=index%3==2?1:2;
            for(int i=0;i<count;i++){
                string kind=i==0?primary:(primary=="Pyramid"?"Sphinx":"Merchant");
                if(i>0&&kind==primary)kind="Oasis";
                var prefab=EgyptianAsset(theme.landmarks,kind);if(!prefab)continue;
                float z=start+(i==0?R(random,12,19):R(random,43,49));
                var g=Item(prefab,monuments,new Vector3(R(random,-33,-29),0,z),270+R(random,-12,12),R(random,.39f,.46f));names.Add(g.name);
                // One small context group per section, offset from the main silhouette.
                if(i==0){
                    var prop=EgyptianAsset(theme.props,kind=="Pylon"?"Stele":"Amphora");
                    if(prop)Item(prop,details,new Vector3(-18,0,z+R(random,6,9)),270+R(random,-18,18),R(random,.65f,.85f));
                    if(kind!="Oasis"){
                        var palm=EgyptianAsset(theme.vegetation,"Palm");
                        if(palm)Item(palm,details,new Vector3(-23,0,z-R(random,6,9)),270,.52f);
                    }
                }
            }
            chunk.landmarkSummary=string.Join(", ",names.ToArray());
            var horizon=Group("Open Sand and Dune Horizon",section);
            var dune=EgyptianAsset(theme.background,"Dunes");
            if(dune)for(int i=0;i<2;i++)Item(dune,horizon,new Vector3(R(random,-78,-65),-.5f,start+(i==0?R(random,7,18):R(random,39,54))),270+R(random,-20,20),R(random,.8f,1.1f));
            // Some sections are open desert; pyramids never alternate with identical pylons.
            if(index%3!=2){var pyramid=EgyptianAsset(theme.background,"Pyramid");if(pyramid)Item(pyramid,horizon,new Vector3(R(random,-68,-59),0,start+R(random,24,38)),270+R(random,-23,23),R(random,.55f,.70f));}
            var lamps=Group("Sparse Desert Braziers",section);
            const float spacing=23.62f;for(int lamp=Mathf.CeilToInt((start+3)/spacing);lamp*spacing<end;lamp++)Item(theme.lamps[0],lamps,new Vector3(-12.4f,0,lamp*spacing),270,.64f);
        }
        void BuildSkyComposition(Transform section,float start,float end,int sectionSeed)
        {
            var random=new System.Random(Hash(sectionSeed,912));
            var islands=new List<Transform>();var rainbows=new List<Transform>();
            foreach(Transform group in section)foreach(Transform item in group){
                if(item.Find("Tapered Floating Rock"))islands.Add(item);
                if(item.name.Contains("Rainbow Between Clouds"))rainbows.Add(item);
            }
            var atmosphere=Group("Cloud Cradles Around Floating Islands",section);
            foreach(var island in islands){
                var rock=island.Find("Tapered Floating Rock");var bounds=BoundsIn(rock.gameObject,transform);
                float width=bounds.size.z,height=bounds.size.y,surface=island.localPosition.y;
                // Put opaque shader-cloud lobes in front of the rock taper, below the grassy rim.
                for(int i=0;i<3;i++){
                    float cloudHeight=Mathf.Max(3.4f,height*1.6f);
                    var pos=new Vector3(bounds.max.x+.8f+i*.12f,surface-cloudHeight*.42f+.65f,bounds.center.z+(i-1)*.18f*width);
                    SkyVolume(atmosphere,"Island Volume Cloud Cradle",pos,new Vector3(width*1.65f,cloudHeight,1),random);
                }
                // Treasure, flowers and stars belong to island gardens, not roadside furniture.
                if(island.parent.name=="Irregular Landmark Plots"){
                    var details=Group("Island Garden Details",island);
                    foreach(var prefab in theme.props){
                        if(prefab.name.Contains("Cloud")||prefab.name.Contains("Gull")||random.NextDouble()<.55)continue;
                        var detail=Item(prefab,details,Vector3.zero,0,R(random,.65f,.9f));
                        detail.transform.localPosition=prefab.name.Contains("Chest")?new Vector3(3,.35f,-2):prefab.name.Contains("Star")?new Vector3(-3,.35f,-2):new Vector3(2,.35f,2.5f);
                        foreach(var part in detail.GetComponentsInChildren<Transform>())if(part.name=="Star Plinth"||part.name=="Star Stem")part.gameObject.SetActive(false);
                    }
                    foreach(var prefab in theme.vegetation)if(prefab.name.Contains("Flower"))Item(prefab,details,new Vector3(-2,.35f,-3),0,.8f);
                }
            }
            foreach(var rainbow in rainbows){
                foreach(Transform part in rainbow)if(part.name=="Rainbow Cloud")part.gameObject.SetActive(false);
                foreach(float side in new[]{-1f,1f}){
                    var world=rainbow.TransformPoint(new Vector3(side*8.8f,3,0));var local=transform.InverseTransformPoint(world);local.x+=1;local.y-=.5f;
                    SkyVolume(atmosphere,"Rainbow Volume Cloud Foot",local,new Vector3(5,2.6f,1),random);
                }
            }
            // A small flock follows the islands rather than the sidewalk rhythm.
            foreach(var prefab in theme.props)if(prefab.name.Contains("Gull"))for(int i=0;i<3;i++)Item(prefab,atmosphere,new Vector3(-44-i*2,2.4f+i*.25f,start+(end-start)*.72f+i*1.2f),270,.45f);
            BuildSkyCloudDeck(section,start,end,sectionSeed);
        }
        GameObject SkyVolume(Transform parent,string name,Vector3 position,Vector3 size,System.Random random)
        {
            if(theme.clouds==null||theme.clouds.Length==0)return null;
            var cloud=Item(theme.clouds[random.Next(theme.clouds.Length)],parent,position,270);cloud.name=name;cloud.transform.localScale=size;
            var variation=cloud.GetComponent<CloudVariation>();if(!variation){variation=cloud.AddComponent<CloudVariation>();variation.ReadMaterial();}
            variation.shapeVariant=R(random,0,1000);variation.sunlitColor=new Color(.97f,1,1,1);variation.shadowColor=new Color(.70f,.86f,.94f,1);variation.Apply();return cloud;
        }
        void BuildSkyCloudDeck(Transform section,float start,float end,int sectionSeed)
        {
            if(theme.clouds==null||theme.clouds.Length==0)return;
            var random=new System.Random(Hash(sectionSeed,731));
            var deck=Group("Volume Cloud Sea Beneath Islands",section);
            // Overlapping existing shader clouds form a low horizon, including across chunk seams.
            // A separate random stream preserves the seeded island and street layouts.
            for(int row=0;row<2;row++){
                int count=5;float stride=(end-start)/count;
                for(int i=0;i<count;i++){
                    float z=start+(i+.5f)*stride+R(random,-1.2f,1.2f);
                    float x=row==0?R(random,-25,-23):R(random,-59,-55);
                    float y=row==0?R(random,-3.1f,-2.8f):R(random,-5.5f,-4.5f);
                    var cloud=Item(theme.clouds[random.Next(theme.clouds.Length)],deck,new Vector3(x,y,z),270);
                    cloud.name=row==0?"Near Volume Cloud Bank":"Far Volume Cloud Bank";
                    cloud.transform.localScale=new Vector3(R(random,31,39),row==0?R(random,9,11):R(random,14,17),1);
                    var variation=cloud.GetComponent<CloudVariation>();
                    if(!variation){variation=cloud.AddComponent<CloudVariation>();variation.ReadMaterial();}
                    variation.shapeVariant=R(random,0,1000);
                    variation.sunlitColor=new Color(.96f,1,1,1);
                    variation.shadowColor=new Color(.62f,.83f,.93f,1);
                    variation.Apply();
                }
            }
        }
        void Remove(GameObject g){g.SetActive(false);if(Application.isPlaying)Destroy(g);else DestroyImmediate(g);}
        public static Bounds BoundsIn(GameObject g,Transform relative)
        {bool found=false;Bounds result=new Bounds();foreach(var r in g.GetComponentsInChildren<Renderer>()){Bounds b=r.bounds;for(int i=0;i<8;i++){Vector3 p=relative.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);}}return result;}
    }
}

































