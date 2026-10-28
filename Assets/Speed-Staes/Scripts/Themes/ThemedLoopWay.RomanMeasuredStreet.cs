using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpeedStars.Themes {
 // Shares the original city's measured facade alignment, building clusters and curbside bays.
 // Legacy RomanComposition slots never participate in this layout.
 public sealed partial class ThemedLoopWay {
  sealed class RomanStreetPlacement {
   public GameObject asset;public Vector3 position,scale;public float yaw;public Bounds bounds;public bool focal;
  }
  sealed class RomanMeshFootprint {public Bounds bounds;public Matrix4x4 toRoot;}
  sealed class RomanStreetSectionPlan {
   public bool civic,rural;public string title;public GameObject landscape;
   public readonly List<RomanStreetPlacement> buildings=new List<RomanStreetPlacement>();
   public readonly List<Vector3> treeGaps=new List<Vector3>();
  }
  readonly List<RomanStreetSectionPlan> measuredRomanSections=new List<RomanStreetSectionPlan>();
  readonly Dictionary<GameObject,List<RomanMeshFootprint>> romanNativeBounds=new Dictionary<GameObject,List<RomanMeshFootprint>>();
  readonly Dictionary<string,int> romanModelUse=new Dictionary<string,int>();
  readonly Queue<string> romanRecentModels=new Queue<string>();
  string romanLastRoadModel;int romanCivicCount;bool romanPreviousLivestock;
  void ResetRomanStreetLayout(){measuredRomanSections.Clear();romanNativeBounds.Clear();romanModelUse.Clear();romanRecentModels.Clear();romanLastRoadModel=null;romanCivicCount=0;romanPreviousLivestock=false;}
  static bool RomanMajorMonument(string name){return name=="Oval Roman Amphitheatre"||name=="Marble Forum Temple"||name=="Turquoise Dome Basilica"||name=="Aqueduct Arcade"||name=="Triumphal Arch"||name=="Art Senate Curia"||name=="Art Broken Sanctuary"||name=="Art Imperial Gatehouse"||name=="Art Round Brick Mausoleum"||name=="Art Roman Library"||name=="Art Octagonal Baptistery";}
  static bool RomanResidential(string name){return name.Contains("Insula")||name.Contains("Villa")||name.Contains("Workshop")||name.Contains("Tabernae")||name.Contains("Baker")||name.Contains("Loggia")||name.Contains("Estate")||name=="Striped Roman Market";}
  GameObject PickMeasuredRomanAsset(List<GameObject> pool,System.Random random,RomanStreetSectionPlan section){
   var candidates=new List<GameObject>();int best=int.MaxValue;
   for(int pass=0;pass<3&&candidates.Count==0;pass++)foreach(var asset in pool){
    bool already=false;foreach(var placed in section.buildings)if(placed.asset==asset)already=true;
    if(already||asset.name==romanLastRoadModel||(pass==0&&romanRecentModels.Contains(asset.name))||(pass<2&&romanRecentModels.Count>0&&asset.name==LastRomanRecent()))continue;
    int used;romanModelUse.TryGetValue(asset.name,out used);if(used<best){best=used;candidates.Clear();}if(used==best)candidates.Add(asset);
   }
   if(candidates.Count==0)foreach(var asset in pool){bool already=false;foreach(var p in section.buildings)if(p.asset==asset)already=true;if(!already&&asset.name!=romanLastRoadModel)candidates.Add(asset);}
   return candidates.Count>0?candidates[random.Next(candidates.Count)]:null;
  }
  string LastRomanRecent(){string value=null;foreach(var name in romanRecentModels)value=name;return value;}
  void RecordRomanModel(GameObject asset){int count;romanModelUse.TryGetValue(asset.name,out count);romanModelUse[asset.name]=count+1;romanRecentModels.Enqueue(asset.name);while(romanRecentModels.Count>6)romanRecentModels.Dequeue();}
  Bounds MeasuredRomanBounds(GameObject asset,Vector3 scale,float yaw){
   List<RomanMeshFootprint> parts;if(!romanNativeBounds.TryGetValue(asset,out parts)){parts=new List<RomanMeshFootprint>();foreach(var f in asset.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)parts.Add(new RomanMeshFootprint{bounds=f.sharedMesh.bounds,toRoot=asset.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});romanNativeBounds[asset]=parts;}
   var rotation=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,yaw,0),Vector3.Scale(asset.transform.localScale,scale));var result=new Bounds();bool found=false;
   foreach(var part in parts){var matrix=rotation*part.toRoot;for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(part.bounds.center+Vector3.Scale(part.bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);}}return result;
  }
  RomanStreetPlacement MeasureRomanBuilding(GameObject asset,System.Random random,bool focal,int heading){
   var native=MeasuredRomanBounds(asset,Vector3.one,270);float factor;
   if(focal){float lo=.65f,hi=.92f;if(asset.name.Contains("Amphitheatre")){lo=.8f;hi=1.04f;}else if(asset.name.Contains("Basilica")){lo=.44f;hi=.6f;}else if(asset.name.Contains("Triumphal")){lo=.42f;hi=.55f;}else if(asset.name.Contains("Sanctuary")){lo=.82f;hi=1.06f;}factor=R(random,lo,hi);}
   else factor=R(random,6.0f,8.1f)/Mathf.Max(1,native.size.z);
   var scale=Vector3.one*factor;
   if(RomanResidential(asset.name)&&random.NextDouble()<.5)scale.x=-scale.x;
   float yaw=focal?270+R(random,-3,3):(heading%3==0?246:heading%3==1?270:294)+R(random,-3,3);
   var bounds=MeasuredRomanBounds(asset,scale,yaw);
   return new RomanStreetPlacement{asset=asset,scale=scale,yaw=yaw,bounds=bounds,focal=focal};
  }
  static void PositionMeasuredRoman(RomanStreetPlacement item,float front,float z,bool center){
   var p=new Vector3(front-item.bounds.max.x,.05f-item.bounds.min.y,center?z-item.bounds.center.z:z-item.bounds.min.z);
   item.position=p;var b=item.bounds;b.center+=p;item.bounds=b;
  }
  void PlanRomanStreetRow(RomanStreetSectionPlan section,List<GameObject> homes,List<GameObject> publicSpaces,float a,float b,System.Random random){
   float cursor=a+R(random,1.3f,3.8f);int left=random.Next(2,4),heading=random.Next(3);
   int depthIndex=Mathf.Max(0,Mathf.FloorToInt((a-StartZ)/SectionLength));int budget=DepthBuildingBudget(depthIndex);while(cursor<b-4&&section.buildings.Count<budget){
    var pool=random.NextDouble()<.26?publicSpaces:homes;var trades=homes.FindAll(p=>p.name=="Art Blacksmith Forge Workshop"||p.name=="Art Carpenter Timber Workshop"||p.name=="Art Textile Dyer Workshop"||p.name=="Art Stonemason Open Workshop"||p.name=="Art Raised Granary Storehouse"||p.name=="Art Olive Oil Merchant Loggia");if(trades.Count>0&&random.NextDouble()<.38)pool=trades;if(section.buildings.Count==0&&a<StartZ+4&&trades.Exists(p=>p.name=="Art Blacksmith Forge Workshop"))pool=trades.FindAll(p=>p.name=="Art Blacksmith Forge Workshop");var asset=PickMeasuredRomanAsset(pool,random,section);if(!asset)asset=PickMeasuredRomanAsset(homes,random,section);if(!asset)break;
    var item=MeasureRomanBuilding(asset,random,false,heading++);DepthScalePlacement(item,DepthPhrasePhase(depthIndex)==4?.88f:.75f);
    if(cursor+item.bounds.size.z>b-1.2f)break;
    // The original city aligns the facade edge, not each prefab's pivot.
    PositionMeasuredRoman(item,DepthFront(depthIndex,section.buildings.Count,R(random,-25,-23),R(random,-37,-32)),cursor,false);section.buildings.Add(item);RecordRomanModel(asset);
    left--;float gap=left>0?R(random,7,12):R(random,19,30);
    if(left==0){if(gap>7)section.treeGaps.Add(new Vector3(R(random,-29,-25),0,item.bounds.max.z+gap*.5f));left=random.Next(2,4);}
    cursor=item.bounds.max.z+gap;
   }
  }
  void EnsureMeasuredRomanSection(int index){
   while(measuredRomanSections.Count<=index){
    int number=measuredRomanSections.Count;var random=new System.Random(Hash(seed,number+20831));float a=StartZ+number*SectionLength,b=a+SectionLength;
    var homes=new List<GameObject>();var publicSpaces=new List<GameObject>();var monuments=new List<GameObject>();
    foreach(var model in theme.landmarks){if(!model)continue;if(RomanMajorMonument(model.name))monuments.Add(model);else if(RomanResidential(model.name))homes.Add(model);else publicSpaces.Add(model);}
    var plan=new RomanStreetSectionPlan();plan.civic=number==1||(number>1&&random.NextDouble()<.44);
    if(number>1&&measuredRomanSections[number-1].civic&&measuredRomanSections[number-2].civic)plan.civic=false;
    if(plan.civic&&monuments.Count>0){
     var candidates=monuments;
     if(romanCivicCount==0){candidates=monuments.FindAll(p=>p.name=="Oval Roman Amphitheatre"||p.name=="Marble Forum Temple"||p.name=="Turquoise Dome Basilica"||p.name=="Art Imperial Gatehouse");}
     else if(romanCivicCount==1&&!romanModelUse.ContainsKey("Oval Roman Amphitheatre"))candidates=monuments.FindAll(p=>p.name=="Oval Roman Amphitheatre");
     var asset=PickMeasuredRomanAsset(candidates,random,plan);if(asset){
      var focus=MeasureRomanBuilding(asset,random,true,1);float facade=R(random,-38,-32);DepthScalePlacement(focus,.86f);
      // Civic display space stays near the street. Fit only a focal monument to that forecourt,
      // rather than moving every tall structure to the same remote line or flattening house heights.
      float availableHeight=1.5f+(1.37f-facade)*.132f;if(focus.bounds.size.y>availableHeight){focus.scale*=availableHeight/focus.bounds.size.y;focus.bounds=MeasuredRomanBounds(asset,focus.scale,focus.yaw);}
      float center=a+SectionLength*R(random,.43f,.58f);PositionMeasuredRoman(focus,facade,center,true);plan.buildings.Add(focus);RecordRomanModel(asset);romanCivicCount++;
      float margin=R(random,3.5f,5.3f);
      PlanRomanStreetRow(plan,homes,publicSpaces,a,focus.bounds.min.z-margin,random);
      PlanRomanStreetRow(plan,homes,publicSpaces,focus.bounds.max.z+margin,b,random);
      plan.treeGaps.Add(new Vector3(focus.bounds.min.x-2,0,focus.bounds.min.z-3));plan.treeGaps.Add(new Vector3(focus.bounds.min.x-2,0,focus.bounds.max.z+3));
      plan.title="Civic forecourt - "+asset.name;
     }
    }
    if(plan.buildings.Count==0){plan.civic=false;PlanRomanStreetRow(plan,homes,publicSpaces,a,b,random);plan.title="Measured Roman street clusters";}
    plan.buildings.Sort((x,y)=>x.bounds.center.z.CompareTo(y.bounds.center.z));
    if(plan.buildings.Count>0)romanLastRoadModel=plan.buildings[plan.buildings.Count-1].asset.name;
    plan.rural=!plan.civic&&number>1&&random.NextDouble()<.24&&(number==0||!measuredRomanSections[number-1].rural);
    if(plan.rural&&theme.ruralScenes!=null&&theme.ruralScenes.Length>0){
     var pool=new List<GameObject>();foreach(var landscape in theme.ruralScenes)if(landscape&&(!romanPreviousLivestock||!RomanLivestock(landscape.name)))pool.Add(landscape);
     plan.landscape=PickMeasuredRomanAsset(pool,random,plan);if(plan.landscape){RecordRomanModel(plan.landscape);romanPreviousLivestock=RomanLivestock(plan.landscape.name);}
    }
    measuredRomanSections.Add(plan);
   }
  }
  GameObject SpawnMeasuredRoman(RomanStreetPlacement placement,Transform parent){
   var obj=Item(placement.asset,parent,placement.position,placement.yaw);obj.transform.localScale=Vector3.Scale(placement.asset.transform.localScale,placement.scale);return obj;
  }
  void BuildRomanStreetLayout(Transform root,ThemeChunk chunk,float a,float b,int index,int sectionSeed){
   EnsureMeasuredRomanSection(index);var plan=measuredRomanSections[index];var random=new System.Random(Hash(sectionSeed,9031));
   BuildRomanMountains(root,a,b);BuildRomanGroundDetail(root,a,b,sectionSeed);BuildRomanRiver(root,a,b);
   var frontage=Group("Measured Roman Frontage",root);var names=new List<string>();
   foreach(var placement in plan.buildings){SpawnMeasuredRoman(placement,frontage);names.Add(placement.asset.name);if(placement.focal)BuildMeasuredRomanForecourt(root,placement,random);}
   BuildMeasuredRomanBackdrop(root,plan,a,b,index,sectionSeed);
   BuildMeasuredRomanFurniture(root,frontage,a,b,sectionSeed);
   var trees=Group("Trees in Street Openings",root);var treePool=new List<GameObject>();foreach(var plant in theme.vegetation)if(plant&&(plant.name.Contains("Pine")||plant.name.Contains("Cypress")||plant.name.Contains("Grove")))treePool.Add(plant);
   foreach(var gap in plan.treeGaps)if(gap.z>a+2&&gap.z<b-2&&treePool.Count>0)Item(treePool[random.Next(treePool.Count)],trees,gap,270+R(random,-18,18),R(random,.65f,.94f));
   var lighting=Group("Continuous Roman Street Lamps",root);float spacing=Mathf.Max(2,romanLampSpacing)*1.75f;
   for(int i=Mathf.CeilToInt(a/spacing);i*spacing<b-.001f;i++)Item(theme.lamps[0],lighting,new Vector3(-16,0,i*spacing),270,.51f);
   BuildMeasuredRomanClouds(root,a,b,sectionSeed);chunk.landmarkSummary=plan.title+": "+string.Join(", ",names.ToArray());
  }
  GameObject RomanNamedAsset(GameObject[] pool,string name){foreach(var p in pool)if(p&&p.name==name)return p;return null;}
  void BuildMeasuredRomanForecourt(Transform root,RomanStreetPlacement focus,System.Random random){
   var court=Group("Integrated Civic Forecourt",root);float back=focus.bounds.min.x-1,front=-14,span=focus.bounds.size.z+8;
   Block(court,"Forecourt Paving",new Vector3((back+front)*.5f,.021f,focus.bounds.center.z),new Vector3(front-back,.05f,span),theme.pathMaterial);
   string left="Art Carved Mosaic Stele",right="Carved Marble Bench",name=focus.asset.name;
   if(name.Contains("Amphitheatre")||name.Contains("Gatehouse")||name.Contains("Triumphal")){left="Roman Red Standard";right="Roman Red Standard";}
   else if(name.Contains("Curia")||name.Contains("Temple")){left="Art Bronze Concentric Monument";right="Art Carved Mosaic Stele";}
   else if(name.Contains("Sanctuary")||name.Contains("Aqueduct")){left="Excavated Column Fragments";right="Art Travertine Milestone";}
   foreach(int side in new[]{-1,1}){var asset=RomanNamedAsset(theme.props,side<0?left:right);if(!asset)continue;float z=side<0?focus.bounds.min.z-2:focus.bounds.max.z+2;Item(asset,court,new Vector3(focus.bounds.max.x+1.5f,.05f,z),270+R(random,-5,5),R(random,.78f,.95f));}
   var mosaic=RomanNamedAsset(theme.props,"Art Mosaic Ground Medallion");if(mosaic)Item(mosaic,court,new Vector3(focus.bounds.max.x+4,.02f,focus.bounds.center.z),270,R(random,.9f,1.2f));
  }
  void BuildMeasuredRomanBackdrop(Transform root,RomanStreetSectionPlan plan,float a,float b,int index,int sectionSeed){
   var group=Group("Layered Variable Roman Skyline",root);var random=new System.Random(Hash(sectionSeed,12017));int count=plan.rural?1:random.Next(1,4);
   var order=new List<int>();for(int i=0;i<theme.background.Length;i++)order.Add(i);for(int i=order.Count-1;i>0;i--){int k=random.Next(i+1),t=order[i];order[i]=order[k];order[k]=t;}
   for(int i=0;i<count&&order.Count>0;i++){
    float z=a+(i+.5f)*(b-a)/count+R(random,-4,4),x=plan.rural?R(random,-103,-94):(i%3==0?R(random,-105,-95):R(random,-77,-67));float y=Mathf.Max(0,RomanLandscapeSurface.Evaluate(seed,x,z));
    var asset=theme.background[order[i%order.Count]];float scale=R(random,.8f,1.18f),yaw=270+R(random,-7,7);var bounds=MeasuredRomanBounds(asset,Vector3.one*scale,yaw);float height=R(random,4.5f,6.8f);if(bounds.size.y>height)scale*=height/bounds.size.y;
    var distant=Item(asset,group,new Vector3(x,y,z),yaw,scale);GroundRomanHorizonObject(distant,group,true);ApplyRomanDepthPaint(distant,x < -90?.37f:.14f);
   }
   if(plan.landscape){var landscape=Item(plan.landscape,group,new Vector3(R(random,-79,-74),0,(a+b)*.5f+R(random,-5,5)),270,R(random,.65f,.85f));var p=landscape.transform.localPosition;p.y=Mathf.Max(0,RomanLandscapeSurface.Evaluate(seed,p.x,p.z));landscape.transform.localPosition=p;NaturalizeRomanCountryside(landscape,Hash(sectionSeed,531));GroundRomanRuralPieces(landscape,group);ApplyRomanDepthPaint(landscape,.13f);}
  }
  void BuildMeasuredRomanFurniture(Transform root,Transform frontage,float a,float b,int sectionSeed){
   var random=new System.Random(Hash(sectionSeed,8753));var plants=new List<GameObject>();foreach(var p in theme.vegetation)if(p&&(p.name.Contains("Rosemary")||p.name.Contains("Mediterranean Shrubs")||p.name.Contains("Lavender")))plants.Add(p);
   var props=new List<GameObject>();foreach(var p in theme.props)if(p&&!p.name.Contains("Ivy Garden Wall")&&!p.name.Contains("Colonnade")&&!p.name.Contains("Ground")&&!p.name.Contains("Horse")&&!p.name.Contains("Emperor"))props.Add(p);
   var bays=new List<float>();for(int i=0;i<16;i++)bays.Add(a+(i+.5f)*(b-a)/16+R(random,-.2f,.2f));for(int i=bays.Count-1;i>0;i--){int k=random.Next(i+1);float t=bays[i];bays[i]=bays[k];bays[k]=t;}
   var vegetation=Group("Curbside Roman Planting Bays",root);var furniture=Group("Curbside Roman Furniture Bays",root);int phrase=DepthPhrasePhase(root.GetComponent<ThemeChunk>().index);int hedgeCount=phrase==4?2:1,propCount=phrase==4?3:random.Next(1,3);
   // Walk a seed-shuffled deck across section boundaries, keeping every prop in circulation.
   var order=new List<GameObject>(props);var deckRandom=new System.Random(Hash(seed,82413));for(int i=order.Count-1;i>0;i--){int k=deckRandom.Next(i+1);var t=order[i];order[i]=order[k];order[k]=t;}
   int propOffset=root.GetComponent<ThemeChunk>().index*5;
   for(int i=0;i<hedgeCount+propCount;i++){
    bool plant=i<hedgeCount;var pool=plant?plants:order;if(pool.Count==0)continue;var asset=plant?pool[random.Next(pool.Count)]:pool[(propOffset+i-hedgeCount)%pool.Count];
    float yaw=270+R(random,-9,9);var scale=Vector3.one*R(random,.8f,1);var bounds=MeasuredRomanBounds(asset,scale,yaw);float maxWidth=plant?3.1f:3f;if(bounds.size.z>maxWidth)scale*=maxWidth/bounds.size.z;
    bounds=MeasuredRomanBounds(asset,scale,yaw);if(!plant&&bounds.size.y>2.5f)scale*=2.5f/bounds.size.y;if(plant)scale=Vector3.Scale(scale,new Vector3(phrase==4?1.12f:.88f,phrase==4?R(random,1.9f,2.2f):R(random,1.15f,1.45f),phrase==4?1.12f:.88f));bounds=MeasuredRomanBounds(asset,scale,yaw);
    float front=plant?R(random,-19,-16.5f):R(random,-18,-15.5f);var position=new Vector3(front-bounds.max.x,.08f-bounds.min.y,bays[i]-bounds.center.z);var obj=Item(asset,plant?vegetation:furniture,position,yaw);obj.transform.localScale=Vector3.Scale(asset.transform.localScale,scale);
    var actual=BoundsIn(obj,transform);foreach(Transform building in frontage){var house=BoundsIn(building.gameObject,transform);if(actual.Intersects(house)){var p=obj.transform.localPosition;p.x+=house.max.x+.65f-actual.min.x;obj.transform.localPosition=p;actual=BoundsIn(obj,transform);}}
    if(!plant)RaiseRomanStreetProp(obj,furniture,actual);
   }
  }
  void BuildMeasuredRomanClouds(Transform root,float a,float b,int sectionSeed){
   if(theme.clouds==null||theme.clouds.Length==0)return;var group=Group("Roman Volume Clouds Above Mountain Ridge",root);var random=new System.Random(Hash(sectionSeed,6817));int count=random.Next(1,3);
   for(int i=0;i<count;i++){float z=a+(i+R(random,.25f,.75f))*(b-a)/count;var cloud=Item(theme.clouds[random.Next(theme.clouds.Length)],group,new Vector3(R(random,-87,-83),R(random,8.4f,8.9f),z),270,R(random,.50f,.60f));var variation=cloud.GetComponent<SpeedStars.Runner.CloudVariation>();if(!variation){variation=cloud.AddComponent<SpeedStars.Runner.CloudVariation>();variation.ReadMaterial();}variation.shapeVariant=R(random,0,1000);variation.Apply();}
  }
 }
}
