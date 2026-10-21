using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
namespace SpeedStars.Runner
{
    [Serializable]
    public sealed class LoopWayAsset
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation=Quaternion.identity;
        public Vector3 scale=Vector3.one;
    }
    // Only objects below this marker belong to the generator. Authored scene objects are never cleared.


    [DisallowMultipleComponent]
    public sealed class LoopWayGenerator : MonoBehaviour
    {
        [Header("Preview: includes the original start section")]
        [Range(1,30)] public int loopCount=4;
        public int seed=42;
        public bool autoPreview=true;
        [Header("Runtime continuation")]
        public bool streamAtRuntime=true;
        public Transform runner;
        [Min(5)] public float retireDistance=25;
        [Header("Street rhythm")]
        [Min(2)] public float lampSpacing=11.81f;
        public Vector2 cloudCountRange=new Vector2(0,2);
        [SerializeField,HideInInspector] public float segmentLength=60.43017f;
        [SerializeField,HideInInspector] public float startZ,endZ,sidewalkEnd,bridgeEnd,waterEnd,bankEnd,riversideEnd,lampAnchor;
        [SerializeField,HideInInspector] public int cityBaseline=14;
        [SerializeField,HideInInspector] public LoopWayAsset[] roads,clouds,buildings,props;
        [SerializeField,HideInInspector] public LoopWayAsset sidewalk,curb,bridge,water,bank,city,lamp,hedge,riversideHedge;
        [SerializeField,HideInInspector] public LoopWayAsset busStop,park,hospital;
        [SerializeField,HideInInspector] public string[] bridgePartNames;
        [SerializeField,HideInInspector] public float[] bridgePartEnds;
        [SerializeField,HideInInspector] public bool configured;
        [SerializeField,HideInInspector] Transform generatedRoot;
        readonly List<LoopWayChunk> live=new List<LoopWayChunk>();
        int nextIndex;
        public bool IsConfigured {get{return configured&&roads!=null&&roads.Length>0&&city!=null&&city.prefab;}}
        void Start()
        {
            if(!Application.isPlaying||!streamAtRuntime||!IsConfigured)return;
            if(!runner){var movement=FindObjectOfType<RunnerMovement>();if(movement)runner=movement.transform;}
            if(loopCount<2)loopCount=2;
            Generate();
        }
        void Update()
        {
            if(!Application.isPlaying||!streamAtRuntime||!runner||live.Count==0)return;
            float z=transform.InverseTransformPoint(runner.position).z;
            // A teleport can retire several sections. Keep work bounded in one frame.
            for(int n=0;n<8&&live.Count>0&&z>live[0].end+retireDistance;n++)
            {
                var old=live[0];live.RemoveAt(0);old.gameObject.SetActive(false);Destroy(old.gameObject);
                live.Add(BuildChunk(nextIndex++));
            }
        }
        public void Generate()
        {
            if(!IsConfigured)return;
            ClearGenerated();
            var root=new GameObject("Generated Loops - Seed "+seed);root.transform.SetParent(transform,false);
            root.AddComponent<LoopWayGenerated>();generatedRoot=root.transform;
            live.Clear();
            for(int i=1;i<Mathf.Clamp(loopCount,1,30);i++)live.Add(BuildChunk(i));
            nextIndex=Mathf.Max(1,loopCount);
#if UNITY_EDITOR
            if(!Application.isPlaying){EditorUtility.SetDirty(this);EditorSceneManager.MarkSceneDirty(gameObject.scene);}
#endif
        }
        public void ClearGenerated()
        {
            // Ownership checks also make duplicating LoopWay safe.
            var owned=new List<GameObject>();
            foreach(Transform child in transform)if(child.GetComponent<LoopWayGenerated>())owned.Add(child.gameObject);
            foreach(var item in owned){item.SetActive(false);if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
            generatedRoot=null;live.Clear();
        }
        static int ChunkSeed(int baseSeed,int index)
        {unchecked{return (baseSeed*73856093)^(index*19349663)^83492791;}}
        float Range(System.Random r,float min,float max){return Mathf.Lerp(min,max,(float)r.NextDouble());}
        GameObject Spawn(LoopWayAsset asset,Transform parent,float offset=0)
        {
            if(asset==null||!asset.prefab)return null;
            var g=Instantiate(asset.prefab,parent,false);g.name=asset.prefab.name;
            g.transform.localPosition=asset.position+Vector3.forward*offset;
            g.transform.localRotation=asset.rotation;g.transform.localScale=asset.scale;return g;
        }
        Transform Group(string name,Transform parent)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        void SetPosition(GameObject g,float x,float y,float z)
        {g.transform.localPosition=new Vector3(x,y,z);}
        LoopWayChunk BuildChunk(int index)
        {
            int chunkSeed=ChunkSeed(seed,index);var r=new System.Random(chunkSeed);
            var root=Group("Loop "+index.ToString("000")+" - Seed "+chunkSeed,generatedRoot);
            var info=root.gameObject.AddComponent<LoopWayChunk>();info.index=index;info.seed=chunkSeed;
            float a=startZ+segmentLength*index,b=a+segmentLength;info.start=a;info.end=b;
            var infrastructure=Group("Continuous Road Sidewalk Bridge Canal",root);
            foreach(var road in roads)Spawn(road,infrastructure,segmentLength*index);
            TileSlabs(sidewalk,infrastructure,index==1?Mathf.Min(a,sidewalkEnd):a,b,1.5f);
            TileSlabs(curb,infrastructure,index==1?Mathf.Min(a,sidewalkEnd):a,b,1.5f);
            Stretch(water,infrastructure,Mathf.Max(a,waterEnd),b);
            Stretch(bank,infrastructure,Mathf.Max(a,bankEnd),b);
            BuildBridge(infrastructure,Mathf.Max(a,bridgeEnd),b);
            var lighting=Group("Evenly Spaced Street Lamps",root);
            int first=Mathf.CeilToInt((a-lampAnchor)/lampSpacing);
            for(int k=first;lampAnchor+k*lampSpacing<b-.001f;k++)
            {var g=Spawn(lamp,lighting);var p=g.transform.localPosition;p.z=lampAnchor+k*lampSpacing;g.transform.localPosition=p;}
            var skyline=Group("Background City",root);var cityObject=Spawn(city,skyline,segmentLength*index);
            var row=cityObject.GetComponent<PastelBuildingRow>();row.automaticUpdate=false;row.variationSeed=r.Next();
            var districtRandom=new System.Random(ChunkSeed(seed,index)^17095733);
            int districtType=districtRandom.Next(3);
            int countDelta=districtType==0?districtRandom.Next(-6,-2):districtType==1?districtRandom.Next(-2,3):districtRandom.Next(3,7);
            row.buildingCount=Mathf.Clamp(cityBaseline+countDelta,5,100);
            row.positionJitter=Range(districtRandom,1.5f,4.0f);
            row.layerStagger=Range(districtRandom,1.5f,6);
            row.gap=Range(districtRandom,.7f,2.8f);
            row.heightRange=city.prefab.GetComponent<PastelBuildingRow>().heightRange*Range(districtRandom,.80f,1.22f);
            row.Rebuild();info.cityCount=row.buildingCount;
            // Fit the district to its section, retaining facade rotations and depth layers.
            Bounds district=LocalBounds(cityObject,transform);
            float coverage=Range(districtRandom,.78f,.97f);
            float factor=segmentLength*coverage/Mathf.Max(1,district.size.z);
            var scale=cityObject.transform.localScale;scale.z*=factor;cityObject.transform.localScale=scale;
            district=LocalBounds(cityObject,transform);var cp=cityObject.transform.localPosition;cp.z+=(a+b)*.5f-district.center.z;cityObject.transform.localPosition=cp;
            var sky=Group("Independent Cloud Variations",root);
            int cmin=Mathf.Clamp(Mathf.RoundToInt(cloudCountRange.x),0,2),cmax=Mathf.Clamp(Mathf.RoundToInt(cloudCountRange.y),cmin,2);
            int cloudCount=CloudCount(index,cmin,cmax);info.cloudCount=cloudCount;
            for(int j=0;j<cloudCount;j++)
            {
                var g=Spawn(clouds[r.Next(clouds.Length)],sky);var p=g.transform.localPosition;
                p.z=a+segmentLength*(j+.5f)/Mathf.Max(1,cloudCount)+Range(r,-2,2);p.y+=Range(r,-2,5);g.transform.localPosition=p;
                g.transform.localScale*=Range(r,.70f,1.12f);
                var variation=g.GetComponent<CloudVariation>();if(!variation){variation=g.AddComponent<CloudVariation>();variation.ReadMaterial();}
                variation.shapeVariant=Range(r,0,10000)+index*.371f;variation.Apply();
            }
            var frontage=Group("Foreground Building Clusters",root);
            int frontageKind=FrontageKind(index);
            info.frontageKind=frontageKind==0?"Buildings":frontageKind==1?"Bus Stop":frontageKind==2?"Pocket Park":"Hospital";
            frontage.name="Foreground - "+info.frontageKind;
            if(frontageKind==0||(frontageKind==1&&(busStop==null||!busStop.prefab))||(frontageKind==2&&(park==null||!park.prefab))||(frontageKind==3&&(hospital==null||!hospital.prefab)))
                BuildFrontage(frontage,a,b,index,info);
            else BuildLandmark(frontage,frontageKind==1?busStop:frontageKind==2?park:hospital,a,b,index,frontageKind);
            // Street props use spaced curb-side bays. Deep building frontage stays separate.
            var vegetation=Group("Visible Foreground Hedge Groups",root);
            int hedgeCount=r.Next(3,6);info.hedgeCount=hedgeCount;
            var bays=new List<float>();
            for(int slot=0;slot<16;slot++)bays.Add(a+(slot+.5f)*segmentLength/16+Range(r,-.45f,.45f));
            Shuffle(bays,r);
            for(int j=0;j<hedgeCount;j++)
            {
                var g=Spawn(hedge,vegetation);var p=g.transform.localPosition;p.z=bays[j];
                p.x=lamp.position.x-Range(r,1.1f,2.1f);p.y=sidewalk.position.y+.15f;g.transform.localPosition=p;
                var plant=g.GetComponent<PastelPlantGenerator>();plant.automaticUpdate=false;plant.variationSeed=r.Next();
                plant.width=Range(r,4.5f,6.5f);plant.height=Range(r,1.7f,2.6f);plant.Rebuild();
            }
            var furnishings=Group("Street Props - Varied Street Activity",root);
            int propCount=r.Next(6,9);info.propCount=propCount;
            var propOrder=new List<int>();for(int p=0;p<props.Length;p++)propOrder.Add(p);
            for(int p=propOrder.Count-1;p>0;p--){int q=r.Next(p+1);int value=propOrder[p];propOrder[p]=propOrder[q];propOrder[q]=value;}
            for(int j=0;j<propCount;j++)
            {
                int choice=propOrder[j%propOrder.Count];
                var g=Spawn(props[choice],furnishings);var p=g.transform.localPosition;p.z=bays[hedgeCount+j]+Range(r,-.5f,.5f);g.transform.localPosition=p;
                g.transform.localRotation=props[choice].rotation*Quaternion.Euler(0,Range(r,-9,9),0);
                // Keep furniture in front of the building line, including wide benches and containers.
                var furnitureBounds=LocalBounds(g,transform);
                foreach(Transform building in frontage)
                {
                    var buildingBounds=LocalBounds(building.gameObject,transform);
                    if(furnitureBounds.Intersects(buildingBounds))
                    {
                        p=g.transform.localPosition;p.x+=buildingBounds.max.x+.65f-furnitureBounds.min.x;
                        g.transform.localPosition=p;furnitureBounds=LocalBounds(g,transform);
                    }
                }
            }
            var riverPlants=Group("Riverside Foliage",root);
            for(int j=0;j<6;j++)
            {
                var g=Spawn(riversideHedge,riverPlants);var p=g.transform.localPosition;p.z=a+(j+.5f)*segmentLength/6;p.x+=(j%3==0?-18:0);if(p.z<riversideEnd){g.SetActive(false);if(Application.isPlaying)Destroy(g);else DestroyImmediate(g);continue;}g.transform.localPosition=p;
                var plant=g.GetComponent<PastelPlantGenerator>();plant.automaticUpdate=false;plant.variationSeed=r.Next();plant.width*=Range(r,.8f,1.25f);plant.Rebuild();
            }
            return info;
        }
        public int FrontageKind(int index)
        {
            // Two urban sections, a low bus-stop section, then introduce the park before random continuation.
            if(index==1)return 0;if(index==2)return 3;if(index==3)return 1;if(index==4)return 0;if(index==5)return 2;
            int previous=2;
            for(int i=6;i<=index;i++)
            {
                int roll=new System.Random(ChunkSeed(seed,i)^18275301).Next(100);
                int next=roll<40?0:roll<60?1:roll<82?2:3;
                if(next==previous&&next!=0)next=0;
                previous=next;
            }
            return previous;
        }
        void BuildLandmark(Transform parent,LoopWayAsset asset,float a,float b,int index,int kind)
        {
            var random=new System.Random(ChunkSeed(seed,index)^661427);
            var landmark=Spawn(asset,parent);var bounds=LocalBounds(landmark,transform);
            var p=landmark.transform.localPosition;
            p.z+=(a+b)*.5f+Range(random,-5,5)-bounds.center.z;
            // Keep the public-space entrance aligned to the sidewalk and leave the curb-side furniture strip clear.
            p.x+=sidewalk.position.x+sidewalk.scale.x*.5f-7.0f-bounds.max.x;
            landmark.transform.localPosition=p;
        }
        void BuildFrontage(Transform parent,float a,float b,int index,LoopWayChunk info)
        {
            var random=new System.Random(ChunkSeed(seed,index)^1274126177);
            var order=new List<int>();for(int i=0;i<buildings.Length;i++)order.Add(i);
            for(int i=order.Count-1;i>0;i--){int j=random.Next(i+1);int t=order[i];order[i]=order[j];order[j]=t;}
            float cursor=a+Range(random,1.5f,6);
            int placed=0,clusterLeft=random.Next(2,4),headingOffset=random.Next(3);
            while(cursor<b-4)
            {
                int type=order[placed%order.Count];var building=Spawn(buildings[type],parent);
                // Front-facing facades and opposite corner views, rather than the same right-facing angle.
                int heading=(placed+headingOffset)%3;
                float yaw=(heading==0?246:heading==1?270:294)+Range(random,-3,3);
                building.transform.localRotation=Quaternion.Euler(0,yaw,0);
                building.transform.localScale*=Range(random,.84f,1.08f);
                var bounds=LocalBounds(building,transform);
                if(cursor+bounds.size.z>b-1.5f)
                {building.SetActive(false);if(Application.isPlaying)Destroy(building);else DestroyImmediate(building);break;}
                var p=building.transform.localPosition;
                p.z+=cursor-bounds.min.z;
                // Align the visible facade to irregular setbacks, keeping a clear pedestrian strip.
                float facadeX=sidewalk.position.x+sidewalk.scale.x*.5f-7.3f-Range(random,0,2.6f);
                p.x+=facadeX-bounds.max.x;building.transform.localPosition=p;
                bounds=LocalBounds(building,transform);placed++;clusterLeft--;
                float gap=clusterLeft>0?Range(random,.55f,2.8f):Range(random,6.5f,13.5f);
                if(clusterLeft<=0)clusterLeft=random.Next(2,4);
                cursor=bounds.max.z+gap;
            }
            info.buildingType=-1;
        }
        int CloudCount(int index,int min,int max)
        {
            max=Mathf.Max(1,max);int current=new System.Random(ChunkSeed(seed,index)^915488749).Next(min,max+1);
            if(current==0&&index>1&&new System.Random(ChunkSeed(seed,index-1)^915488749).Next(min,max+1)==0)current=1;
            return current;
        }
        static void Shuffle(List<float> a,System.Random r)
        {for(int i=a.Count-1;i>0;i--){int j=r.Next(i+1);float t=a[i];a[i]=a[j];a[j]=t;}}
        void TileSlabs(LoopWayAsset asset,Transform parent,float a,float b,float step)
        {
            for(float z=a;z<b-.001f;z+=step)
            {float length=Mathf.Min(step,b-z);var g=Spawn(asset,parent);var p=g.transform.localPosition;p.z=z+length*.5f;g.transform.localPosition=p;var s=g.transform.localScale;s.z=length-.018f;g.transform.localScale=s;}
        }
        void Stretch(LoopWayAsset asset,Transform parent,float a,float b)
        {
            if(b<=a+.001f)return;var g=Spawn(asset,parent);var p=g.transform.localPosition;p.z=(a+b)*.5f;g.transform.localPosition=p;var s=g.transform.localScale;s.z=b-a;g.transform.localScale=s;
        }
        void BuildBridge(Transform parent,float a,float b)
        {
            if(b<=a+.001f)return;
            // Clip every module to the chunk boundary; no overlapping decks or doubled posts.
            const float length=20.14339f;
            int first=Mathf.FloorToInt((a-bridge.position.z+length*.5f)/length);
            for(int i=first;bridge.position.z+i*length-length*.5f<b;i++)
            {
                var g=Spawn(bridge,parent,i*length);g.name="Bridge Rail Module";
                var children=new List<Transform>();foreach(Transform t in g.transform)children.Add(t);
                foreach(var t in children)
                {
                    float center=g.transform.localPosition.z+t.localPosition.z;
                    float continuityEnd=a;
                    if(bridgePartNames!=null)for(int part=0;part<bridgePartNames.Length;part++)if(bridgePartNames[part]==t.name)continuityEnd=Mathf.Max(continuityEnd,bridgePartEnds[part]);
                    float half=t.localScale.z*.5f,lo=Mathf.Max(center-half,continuityEnd),hi=Mathf.Min(center+half,b);
                    if(hi<=lo+.001f){t.gameObject.SetActive(false);if(Application.isPlaying)Destroy(t.gameObject);else DestroyImmediate(t.gameObject);continue;}
                    var p=t.localPosition;p.z=(lo+hi)*.5f-g.transform.localPosition.z;t.localPosition=p;
                    var s=t.localScale;s.z=hi-lo;t.localScale=s;
                }
            }
        }
        public static Bounds LocalBounds(GameObject g,Transform relative)
        {
            bool found=false;Bounds result=new Bounds();
            foreach(var renderer in g.GetComponentsInChildren<Renderer>())
            {var b=renderer.bounds;for(int i=0;i<8;i++){var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));p=relative.InverseTransformPoint(p);if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);}}
            return result;
        }
    }
}






