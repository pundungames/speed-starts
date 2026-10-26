using System;
using System.Collections.Generic;
using UnityEngine;
using SpeedStars.Runner;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  sealed class RomanDistrictRun {public int template,number,seed;public float start,length;public bool reverse;public Dictionary<int,string> assets=new Dictionary<int,string>();}
  readonly List<RomanDistrictRun> romanRoute=new List<RomanDistrictRun>();
  void EnsureRomanRoute(float until){
   var plan=theme.romanComposition;
   while(romanRoute.Count==0||romanRoute[romanRoute.Count-1].start+romanRoute[romanRoute.Count-1].length<until){
    int number=romanRoute.Count;var routeRandom=new System.Random(Hash(seed,number+6001));int template=number;
    int introCount=plan.districts.Length;
    if(number>=introCount){
     // Deal every district once per cycle, in a new order, rather than endlessly favouring housing.
     var available=new List<int>();int cycleStart=number-number%introCount;
     for(int candidate=0;candidate<introCount;candidate++){bool dealt=false;for(int n=cycleStart;n<number;n++)if(romanRoute[n].template==candidate)dealt=true;if(!dealt)available.Add(candidate);}
     var candidates=available.FindAll(candidate=>candidate!=romanRoute[number-1].template&&candidate!=romanRoute[number-2].template);
     if(candidates.Count==0)candidates=available;template=candidates[routeRandom.Next(candidates.Count)];
    }
    template%=plan.districts.Length;var district=plan.districts[template];int routeSeed=Hash(seed,number+6001);var random=new System.Random(routeSeed);
    float start=number==0?0:romanRoute[number-1].start+romanRoute[number-1].length;
    var run=new RomanDistrictRun{template=template,number=number,seed=routeSeed,start=start,length=Mathf.Max(10,district.length)*(number<introCount?1:R(random,.94f,1.33f))};
    run.reverse=number>=introCount&&random.NextDouble()<.5;
    var used=new HashSet<string>();var recent=new HashSet<string>();bool livestockUsed=false;
    for(int n=Mathf.Max(0,number-2);n<number;n++)foreach(var asset in romanRoute[n].assets.Values)recent.Add(asset);
    string previousBuilding=null;
    bool lastCountrysideAnimals=false;
    for(int n=number-1;n>=0;n--){var olderRun=romanRoute[n];var olderDistrict=plan.districts[olderRun.template];float last=-1;string lastScene=null;for(int p=0;p<olderDistrict.slots.Length;p++){var s=olderDistrict.slots[p];float d=olderRun.reverse?olderDistrict.length-s.distance:s.distance;if(s.layer==RomanAssetLayer.Countryside&&d>last){last=d;lastScene=olderRun.assets[p];}}if(lastScene!=null){lastCountrysideAnimals=RomanLivestock(lastScene);break;}}
    if(number>0){var previousRun=romanRoute[number-1];var previousDistrict=plan.districts[previousRun.template];float last=-1;for(int p=0;p<previousDistrict.slots.Length;p++){var s=previousDistrict.slots[p];float d=previousRun.reverse?previousDistrict.length-s.distance:s.distance;if(s.layer==RomanAssetLayer.Building&&d>last){last=d;previousBuilding=previousRun.assets[p];}}}
    var order=new List<int>();for(int p=0;p<district.slots.Length;p++)order.Add(p);
    order.Sort((i,j)=>(run.reverse?-district.slots[i].distance:district.slots[i].distance).CompareTo(run.reverse?-district.slots[j].distance:district.slots[j].distance));
    foreach(int i in order){
     var slot=district.slots[i];string chosen=slot.asset;
     if(number>=introCount&&(slot.layer==RomanAssetLayer.Building||slot.layer==RomanAssetLayer.Skyline||slot.layer==RomanAssetLayer.Countryside)){
      var options=new List<string>{slot.asset};if(slot.alternatives!=null)foreach(var option in slot.alternatives)if(!options.Contains(option))options.Add(option);
      if(slot.layer==RomanAssetLayer.Building){var different=options.FindAll(n=>n!=previousBuilding);if(different.Count>0)options=different;}
      if(slot.layer==RomanAssetLayer.Countryside){bool recentAnimals=false;foreach(var asset in recent)if(RomanLivestock(asset))recentAnimals=true;if(livestockUsed||recentAnimals||lastCountrysideAnimals){var quiet=options.FindAll(n=>!RomanLivestock(n));if(quiet.Count>0)options=quiet;}}
      var candidates=options.FindAll(n=>!used.Contains(n)&&!recent.Contains(n));
      if(candidates.Count==0)candidates=options.FindAll(n=>!used.Contains(n));if(candidates.Count==0)candidates=options;
      chosen=candidates[random.Next(candidates.Count)];used.Add(chosen);
     }
     run.assets[i]=chosen;if(slot.layer==RomanAssetLayer.Building)previousBuilding=chosen;if(slot.layer==RomanAssetLayer.Countryside){lastCountrysideAnimals=RomanLivestock(chosen);if(lastCountrysideAnimals)livestockUsed=true;}
    }
    romanRoute.Add(run);
   }
  }
  GameObject RomanSlotAsset(RomanCompositionSlot slot,string name){
   GameObject[] pool=slot.layer==RomanAssetLayer.Building?theme.landmarks:slot.layer==RomanAssetLayer.StreetProp?theme.props:slot.layer==RomanAssetLayer.Plant?theme.vegetation:slot.layer==RomanAssetLayer.Skyline?theme.background:theme.ruralScenes;
   if(pool!=null)foreach(var asset in pool)if(asset&&asset.name==name)return asset;
   return null;
  }
  void BuildRomanComposition(Transform section,ThemeChunk chunk,float a,float b,int sectionSeed){
   var plan=theme.romanComposition;EnsureRomanRoute(b-StartZ+1);BuildRomanMountains(section,a,b);
   var foreground=Group("Composed Roman Street",section);var skyline=Group("Coordinated Roman Backdrop",section);var props=Group("Contextual Street Detail Clusters",section);
   var names=new List<string>();var districtNames=new List<string>();
   foreach(var run in romanRoute){
    if(run.start>b-StartZ+15||run.start+run.length<a-StartZ-15)continue;
    var district=plan.districts[run.template];float stretch=run.length/Mathf.Max(10,district.length);var random=new System.Random(run.seed);
    if(run.start<b-StartZ&&run.start+run.length>a-StartZ)districtNames.Add(district.name);
    if(district.courts!=null)foreach(var court in district.courts){
     float center=StartZ+run.start+(run.reverse?district.length-court.distance:court.distance)*stretch,lo=Mathf.Max(a,center-court.length*stretch*.5f),hi=Mathf.Min(b,center+court.length*stretch*.5f);
     if(hi>lo)Block(section,court.name,new Vector3(court.x,.018f,(lo+hi)*.5f),new Vector3(court.width,.045f,hi-lo),theme.pathMaterial);
    }
    var usedBuildings=new HashSet<string>();
    for(int i=0;i<district.slots.Length;i++){
     var slot=district.slots[i];string assetName=run.assets[i];

     if(slot.layer==RomanAssetLayer.Building){
      if(usedBuildings.Contains(assetName)){
       if(!usedBuildings.Contains(slot.asset))assetName=slot.asset;
       else if(slot.alternatives!=null)foreach(var alternative in slot.alternatives)if(!usedBuildings.Contains(alternative)){assetName=alternative;break;}
      }
      usedBuildings.Add(assetName);
     }
     float z=StartZ+run.start+(run.reverse?district.length-slot.distance:slot.distance)*stretch;
     if(slot.focal)z=Mathf.Floor(z/plan.lampSpacing)*plan.lampSpacing+plan.lampSpacing*.5f;
     float x=slot.x,scale=slot.scale,yaw=slot.yaw;
     if(run.number>=plan.districts.Length){x+=R(random,-.8f,.8f);scale*=R(random,.96f,1.04f);yaw+=R(random,-4,4);}
     if(z<a||z>=b)continue;
     var prefab=RomanSlotAsset(slot,assetName);if(!prefab){Debug.LogWarning("Roman composition asset missing: "+assetName,this);continue;}
     var parent=slot.layer==RomanAssetLayer.Skyline||slot.layer==RomanAssetLayer.Countryside?skyline:slot.layer==RomanAssetLayer.Building?foreground:props;
     var obj=Item(prefab,parent,new Vector3(x,0,z),yaw,scale);obj.name=assetName;var label=Group(slot.role+" - "+district.name,section);obj.transform.SetParent(label,true);
     if(slot.layer==RomanAssetLayer.Countryside)NaturalizeRomanCountryside(obj,Hash(run.seed,i));
     if(slot.focal){var bounds=BoundsIn(obj,transform);if(bounds.size.y>6.8f)obj.transform.localScale*=6.8f/bounds.size.y;}
     if(slot.layer==RomanAssetLayer.Building)names.Add(assetName);
    }
   }
   chunk.landmarkSummary=string.Join(" / ",districtNames.ToArray())+": "+string.Join(", ",names.ToArray());
   var lamps=Group("Continuous Roman Street Lamps",section);float spacing=plan.lampSpacing;
   for(int i=Mathf.CeilToInt(a/spacing);i*spacing<b;i++)Item(theme.lamps[0],lamps,new Vector3(-12.4f,0,i*spacing),270,.66f);
   if(theme.clouds!=null&&theme.clouds.Length>0){var clouds=Group("Low Roman Volume Clouds",section);var random=new System.Random(Hash(sectionSeed,6817));int count=random.Next(1,3);
    for(int i=0;i<count;i++){var c=Item(theme.clouds[random.Next(theme.clouds.Length)],clouds,new Vector3(-95,R(random,4,7),a+(i+.5f)*SectionLength/count),270,R(random,.65f,.95f));var v=c.GetComponent<CloudVariation>();if(!v){v=c.AddComponent<CloudVariation>();v.ReadMaterial();}v.shapeVariant=R(random,0,1000);v.Apply();}
   }
  }
 }
}




