using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  sealed class DesertSectionPlan {
   public bool publicSpace;public string title;public readonly List<RomanStreetPlacement> buildings=new List<RomanStreetPlacement>();
  }
  readonly List<DesertSectionPlan> desertPlans=new List<DesertSectionPlan>();
  readonly Dictionary<string,int> desertUsage=new Dictionary<string,int>();readonly Queue<string> desertRecent=new Queue<string>();string desertLast;
  void ResetEgyptDesertLayout(){desertPlans.Clear();desertUsage.Clear();desertRecent.Clear();desertLast=null;}
  static bool EgyptHouse(string name){return name.Contains("House")||name.Contains("Granary");}
  static bool EgyptTrading(string name){return name.Contains("Souk")||name.Contains("Bazaar")||name.Contains("Tent")||name.Contains("Merchant Stall");}
  static bool EgyptPublic(string name){return name.Contains("Palace")||name.Contains("Caravanserai")||name.Contains("Sphinx")||name.Contains("Pylon")||name.Contains("Camel")||name.Contains("Oasis")||name.Contains("Statue");}
  GameObject PickDesertModel(List<GameObject> pool,System.Random random,DesertSectionPlan plan){
   var candidates=new List<GameObject>();int best=int.MaxValue;for(int pass=0;pass<2&&candidates.Count==0;pass++)foreach(var model in pool){bool already=false;foreach(var item in plan.buildings)if(item.asset==model)already=true;if(already||model.name==desertLast||(pass==0&&desertRecent.Contains(model.name)))continue;int uses;desertUsage.TryGetValue(model.name,out uses);if(uses<best){best=uses;candidates.Clear();}if(uses==best)candidates.Add(model);}
   return candidates.Count>0?candidates[random.Next(candidates.Count)]:null;
  }
  void RecordDesert(GameObject model){int count;desertUsage.TryGetValue(model.name,out count);desertUsage[model.name]=count+1;desertRecent.Enqueue(model.name);while(desertRecent.Count>5)desertRecent.Dequeue();}
  RomanStreetPlacement MeasureDesertBuilding(GameObject asset,System.Random random,bool publicSpace,int heading){
   var native=MeasuredRomanBounds(asset,Vector3.one,270);float scale;
   if(publicSpace){scale=R(random,.65f,.9f);if(native.size.y*scale>6.2f)scale=R(random,5.1f,6.2f)/native.size.y;}
   else scale=R(random,EgyptTrading(asset.name)?5.2f:5.8f,EgyptTrading(asset.name)?7.2f:8.5f)/Mathf.Max(1,native.size.z);
   if(!publicSpace&&native.size.y*scale>4.8f)scale=4.8f/Mathf.Max(1,native.size.y);float yaw=publicSpace?270+R(random,-5,5):(heading%3==0?251:heading%3==1?270:289)+R(random,-3,3);var vector=Vector3.one*scale;
   if(EgyptHouse(asset.name)&&random.NextDouble()<.5)vector.x=-vector.x;
   return new RomanStreetPlacement{asset=asset,scale=vector,yaw=yaw,bounds=MeasuredRomanBounds(asset,vector,yaw),focal=publicSpace};
  }
  void DesertBuildingRow(DesertSectionPlan plan,List<GameObject> houses,List<GameObject> markets,float start,float end,System.Random random){
   float cursor=start+R(random,1.5f,3.5f);int cluster=random.Next(2,4),heading=random.Next(3);
   while(cursor<end-4){var pool=random.NextDouble()<.35?markets:houses;var asset=PickDesertModel(pool,random,plan);if(!asset)asset=PickDesertModel(houses,random,plan);if(!asset)break;var item=MeasureDesertBuilding(asset,random,false,heading++);if(cursor+item.bounds.size.z>end-1.2f)break;
    PositionMeasuredRoman(item,R(random,-23,-19.4f),cursor,false);plan.buildings.Add(item);RecordDesert(asset);cluster--;float gap=cluster>0?R(random,.7f,2.4f):R(random,5,9.5f);if(cluster<=0)cluster=random.Next(2,4);cursor=item.bounds.max.z+gap;
   }
  }
  void EnsureDesertPlan(int index){
   while(desertPlans.Count<=index){int number=desertPlans.Count;var random=new System.Random(Hash(seed,number+50211));float start=StartZ+number*SectionLength,end=start+SectionLength;var plan=new DesertSectionPlan();var houses=new List<GameObject>();var markets=new List<GameObject>();var landmarks=new List<GameObject>();foreach(var p in theme.landmarks){if(!p)continue;if(EgyptHouse(p.name))houses.Add(p);else if(EgyptTrading(p.name))markets.Add(p);else if(EgyptPublic(p.name))landmarks.Add(p);}
    if(houses.Count==0){desertPlans.Add(plan);continue;}
    plan.publicSpace=number==1||(number>1&&random.NextDouble()<.47);if(number>1&&desertPlans[number-1].publicSpace&&desertPlans[number-2].publicSpace)plan.publicSpace=false;
    if(plan.publicSpace){var pool=landmarks;if(number==1){var showcase=landmarks.FindAll(p=>p.name.Contains("Caravanserai")||p.name.Contains("Palace")||p.name.Contains("Loaded Camel"));if(showcase.Count>0)pool=showcase;}else{int previousPublic=0;foreach(var earlier in desertPlans)if(earlier.publicSpace)previousPublic++;if(previousPublic==1&&!desertUsage.ContainsKey("Desert Art Camel and Bedouin")&&!desertUsage.ContainsKey("Desert Art Loaded Camel Caravan")){var caravan=landmarks.FindAll(p=>p.name.Contains("Camel"));if(caravan.Count>0)pool=caravan;}}var asset=PickDesertModel(pool,random,plan);
     if(asset){var item=MeasureDesertBuilding(asset,random,true,1);float front=asset.name.Contains("Camel")||asset.name.Contains("Statue")?R(random,-23,-20):R(random,-30,-25);float limit=1.5f+(1.37f-front)*.132f;if(item.bounds.size.y>limit){item.scale*=limit/item.bounds.size.y;item.bounds=MeasuredRomanBounds(asset,item.scale,item.yaw);}PositionMeasuredRoman(item,front,start+SectionLength*R(random,.43f,.58f),true);plan.buildings.Add(item);RecordDesert(asset);float margin=R(random,2.6f,4.0f);DesertBuildingRow(plan,houses,markets,start,item.bounds.min.z-margin,random);DesertBuildingRow(plan,houses,markets,item.bounds.max.z+margin,end,random);plan.title="Desert public space - "+asset.name;}
    }
    if(plan.buildings.Count==0){plan.publicSpace=false;DesertBuildingRow(plan,houses,markets,start,end,random);plan.title="Desert village and souk clusters";}
    plan.buildings.Sort((x,y)=>x.bounds.center.z.CompareTo(y.bounds.center.z));if(plan.buildings.Count>0)desertLast=plan.buildings[plan.buildings.Count-1].asset.name;desertPlans.Add(plan);
   }
  }
  void BuildEgyptDesertLayout(Transform root,ThemeChunk chunk,float start,float end,int index,int sectionSeed){
   EnsureDesertPlan(index);var plan=desertPlans[index];var random=new System.Random(Hash(sectionSeed,4531));var foreground=Group("Measured Desert Village Frontage",root);var names=new List<string>();
   foreach(var placement in plan.buildings){SpawnMeasuredRoman(placement,foreground);names.Add(placement.asset.name);if(placement.focal&&placement.asset.name.Contains("Palace")){var statue=EgyptianAsset(theme.landmarks,"Pharaoh Statue");if(statue)foreach(float side in new[]{-1f,1f})Item(statue,root,new Vector3(placement.bounds.max.x+1,0,placement.bounds.center.z+side*(placement.bounds.size.z*.5f+1.8f)),270,.62f);}}
   foreach(var placement in plan.buildings)if(placement.focal&&placement.asset.name.Contains("Pylon")){var obelisk=EgyptianAsset(theme.props,"Obelisk");if(obelisk)foreach(float side in new[]{-1f,1f})Item(obelisk,root,new Vector3(placement.bounds.max.x+1.2f,.05f,placement.bounds.center.z+side*(placement.bounds.size.z*.5f+1.5f)),270,.45f);}
   BuildDesertHorizon(root,start,end,index,sectionSeed);BuildDesertFurniture(root,foreground,start,end,sectionSeed);
   var lamps=Group("Continuous Desert Braziers",root);for(int i=Mathf.CeilToInt(start/23.62f);i*23.62f<end;i++)Item(theme.lamps[0],lamps,new Vector3(-12.4f,0,i*23.62f),270,.64f);
   // No cloud layer is created in the desert theme.
   chunk.landmarkSummary=plan.title+": "+string.Join(", ",names.ToArray());
  }
  void BuildDesertHorizon(Transform root,float start,float end,int index,int sectionSeed){
   var random=new System.Random(Hash(sectionSeed,12039));var horizon=Group("Layered Desert Pyramid and Royal Horizon",root);var pyramids=new List<GameObject>();var accents=new List<GameObject>();GameObject dune=null;
   foreach(var p in theme.background){if(!p)continue;if(p.name.Contains("Pyramid"))pyramids.Add(p);else if(p.name.Contains("Dune"))dune=p;else accents.Add(p);}
   if(dune)for(int i=0;i<3;i++)Item(dune,horizon,new Vector3(R(random,-94,-73),-.08f,start+(i+.5f)*(end-start)/3+R(random,-5,5)),270+R(random,-15,15),R(random,.85f,1.35f));
   int count=random.Next(1,3);for(int i=0;i<count&&pyramids.Count>0;i++){var prefab=pyramids[(Mathf.Abs(seed%pyramids.Count)+index+i)%pyramids.Count];var item=Item(prefab,horizon,new Vector3(R(random,-77,-56),0,start+(i+.5f)*(end-start)/count+R(random,-6,6)),270+R(random,-16,16),R(random,.48f,.75f));}
   if(accents.Count>0&&random.NextDouble()<.78){var accent=accents[random.Next(accents.Count)];Item(accent,horizon,new Vector3(R(random,-58,-43),0,start+R(random,14,46)),270+R(random,-7,7),R(random,.66f,.9f));}
  }
  void BuildDesertFurniture(Transform root,Transform frontage,float start,float end,int sectionSeed){
   var random=new System.Random(Hash(sectionSeed,18341));var dry=new List<GameObject>();foreach(var p in theme.vegetation)if(p&&(p.name.Contains("Dry Grass")||p.name.Contains("Thorn Scrub")))dry.Add(p);
   var props=new List<GameObject>();foreach(var p in theme.props)if(p&&!p.name.Contains("Obelisk"))props.Add(p);var bays=new List<float>();for(int i=0;i<18;i++)bays.Add(start+(i+.5f)*(end-start)/18+R(random,-.18f,.18f));for(int i=bays.Count-1;i>0;i--){int k=random.Next(i+1);float t=bays[i];bays[i]=bays[k];bays[k]=t;}
   var planting=Group("Dry Desert Roadside Vegetation",root);var details=Group("Desert Roadside Trade and Water",root);int grassCount=random.Next(4,7),propCount=random.Next(6,10);var order=new List<GameObject>(props);for(int i=order.Count-1;i>0;i--){int k=random.Next(i+1);var p=order[i];order[i]=order[k];order[k]=p;}
   for(int i=0;i<grassCount+propCount;i++){bool grass=i<grassCount;var pool=grass?dry:order;if(pool.Count==0)continue;var asset=grass?pool[random.Next(pool.Count)]:pool[(i-grassCount)%pool.Count];float yaw=270+R(random,asset.name.Contains("Cart")?-22:-9,asset.name.Contains("Cart")?22:9);var scale=Vector3.one*R(random,.75f,1);var b=MeasuredRomanBounds(asset,scale,yaw);if(b.size.z>2.8f)scale*=2.8f/b.size.z;b=MeasuredRomanBounds(asset,scale,yaw);if(!grass&&b.size.y>2.55f)scale*=2.55f/b.size.y;if(grass)scale=Vector3.Scale(scale,new Vector3(1.2f,2.25f,1.2f));b=MeasuredRomanBounds(asset,scale,yaw);float front=grass?R(random,-16.5f,-14.3f):R(random,-16,-13.8f);var p=new Vector3(front-b.max.x,.07f-b.min.y,bays[i]-b.center.z);var obj=Item(asset,grass?planting:details,p,yaw);obj.transform.localScale=Vector3.Scale(asset.transform.localScale,scale);var bounds=BoundsIn(obj,transform);foreach(Transform house in frontage){var h=BoundsIn(house.gameObject,transform);if(bounds.Intersects(h)){var pos=obj.transform.localPosition;pos.x+=h.max.x+.5f-bounds.min.x;obj.transform.localPosition=pos;bounds=BoundsIn(obj,transform);}}if(!grass)RaiseDesertStreetProp(obj,details,bounds);}
   var acacia=EgyptianAsset(theme.vegetation,"Acacia");if(acacia&&random.NextDouble()<.7)Item(acacia,planting,new Vector3(-30,0,start+R(random,10,50)),270,R(random,.8f,1.15f));
  }
 }
}
