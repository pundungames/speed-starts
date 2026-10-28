#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static bool IsRomanFigurative(string name){return name.Contains("Statue")||name.Contains("Bust")||name.Contains("Horse")||name.Contains("Emperor")||name.Contains("Lion Wall")||name.Contains("Fishmonger")||name=="Art Philosopher Sculpture Court";}
  static void RomanOrbit(Transform t,Vector3 p,float scale=1){
   var root=new GameObject("Anchored Abstract Bronze Orbits");root.transform.SetParent(t,false);root.transform.localPosition=p;root.transform.localScale=Vector3.one*scale;var r=root.transform;
   Box(r,"Orbit Ground Foundation",new Vector3(0,.12f,0),new Vector3(2.7f,.24f,2.7f),rLime);Box(r,"Orbit Pedestal Crown",new Vector3(0,.35f,0),new Vector3(2.3f,.22f,2.3f),rIvory);
   Lathe(r,"Tapered Orbit Support",Vector3.up*.45f,new[]{0f,.3f,1.2f,1.8f},new[]{.75f,.65f,.30f,.28f},rLime,12);
   for(int i=0;i<3;i++){var ring=new GameObject("Intersecting Bronze Ring "+i);ring.transform.SetParent(r,false);ring.transform.localPosition=Vector3.up*2.65f;ring.transform.localRotation=Quaternion.Euler(i==2?28:0,i*60,0);Arc(ring.transform,"Continuous Cast Orbit",Vector3.zero,1.26f-i*.09f,.12f,.16f,rBronze,40,0,360);}
   for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var tile=Box(r,"Orbit Plinth Mosaic",new Vector3(Mathf.Sin(a)*.9f,.475f,Mathf.Cos(a)*.9f),new Vector3(.18f,.025f,.18f),i%2==0?rBronze:rWine);tile.transform.localRotation=Quaternion.Euler(0,45,0);}
  }
  static void RomanMosaicStele(Transform t,Vector3 p,float scale=1){
   var root=new GameObject("Carved Mosaic Stone Stele");root.transform.SetParent(t,false);root.transform.localPosition=p;root.transform.localScale=Vector3.one*scale;var r=root.transform;
   Box(r,"Stele Ground Base",new Vector3(0,.14f,0),new Vector3(1.65f,.28f,1.1f),rLime);Box(r,"Stele Foot Moulding",new Vector3(0,.38f,0),new Vector3(1.35f,.2f,.85f),rIvory);
   Box(r,"Solid Travertine Stele",new Vector3(0,1.9f,0),new Vector3(1.05f,2.85f,.6f),rLime);Box(r,"Stele Top Cornice",new Vector3(0,3.4f,0),new Vector3(1.4f,.24f,.85f),rIvory);
   foreach(float side in new[]{-1f,1f})for(int row=0;row<7;row++)for(int col=0;col<3;col++){var tile=Box(r,"Inset Geometric Tessera",new Vector3((col-1)*.27f,.8f+row*.34f,side*.315f),new Vector3(.15f,.15f,.025f),(row+col)%3==0?rWine:rBronze);tile.transform.localRotation=Quaternion.Euler(0,0,45);}
   foreach(float x in new[]{-.48f,.48f})Box(r,"Stele Framing Pilaster",new Vector3(x,1.92f,-.33f),new Vector3(.1f,2.8f,.1f),rIvory);
  }
  static void RomanAbstractFountain(Transform t){
   Box(t,"Fountain Ground Footing",new Vector3(0,.18f,0),new Vector3(3,.36f,2.0f),rLime);Box(t,"Carved Fountain Back",new Vector3(0,1.6f,.4f),new Vector3(2.8f,2.8f,.55f),rLime);RArch(t,new Vector3(0,.65f,.08f),1.5f,1.95f);
   Box(t,"Fountain Upper Cornice",new Vector3(0,3.08f,.4f),new Vector3(3.15f,.25f,.9f),rIvory);
   Box(t,"Basin Floor",new Vector3(0,.45f,-.35f),new Vector3(2.6f,.25f,1.5f),rIvory);Box(t,"Basin Front Lip",new Vector3(0,.8f,-1),new Vector3(2.6f,.6f,.2f),rIvory);foreach(float x in new[]{-1.2f,1.2f})Box(t,"Basin Side Wall",new Vector3(x,.8f,-.35f),new Vector3(.2f,.6f,1.5f),rIvory);Box(t,"Still Basin Water",new Vector3(0,.9f,-.35f),new Vector3(2.2f,.04f,1.05f),rWater);
   var plaque=Box(t,"Diamond Bronze Spout Plaque",new Vector3(0,1.7f,-.03f),new Vector3(.5f,.5f,.12f),rBronze);plaque.transform.localRotation=Quaternion.Euler(0,0,45);Beam(t,"Attached Bronze Water Spout",new Vector3(0,1.65f,-.02f),new Vector3(0,1.65f,-.36f),.10f,rBronze);Beam(t,"Falling Water Thread",new Vector3(0,1.65f,-.36f),new Vector3(0,.93f,-.36f),.045f,rWater);
  }
  static void RomanSpiralMarker(Transform t){
   Box(t,"Spiral Ground Plinth",new Vector3(0,.15f,0),new Vector3(1.5f,.3f,1.5f),rLime);Cylinder(t,"Spiral Central Stone Core",Vector3.up*.3f,.22f,2.4f,rIvory,12);
   var points=new List<Vector3>();for(int i=0;i<=64;i++){float a=i*Mathf.PI*4/64;points.Add(new Vector3(Mathf.Sin(a)*.42f,.4f+i*2.1f/64,Mathf.Cos(a)*.42f));}Tube(t,"Anchored Bronze Helix",points,.075f,rBronze,8,.075f);foreach(float y in new[]{.4f,2.5f}){Beam(t,"Helix Support Brace",new Vector3(0,y,0),new Vector3(0,y,.42f),.075f,rBronze);}Cylinder(t,"Spiral Moulded Crown",Vector3.up*2.7f,.4f,.18f,rIvory,12);
  }
  public static string BuildRomanAbstractArt(){
   themeFolder=Base+"/RomeByzantium";Palette();RomanArtPalette();egyptAudit.Clear();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");
   var orbit=RomanQualityModel("Art Bronze Orbit Court",t=>{Box(t,"Orbit Court Terrace",new Vector3(0,.08f,0),new Vector3(11,.16f,7),rPave);RomanOrbit(t,Vector3.up*.16f);foreach(float side in new[]{-1f,1f}){RPlanter(t,new Vector3(side*4,0,1));Box(t,"Court Stone Seat",new Vector3(side*3.6f,.5f,-1.8f),new Vector3(2,.22f,.8f),rIvory);foreach(float x in new[]{-.6f,.6f})Box(t,"Grounded Seat Foot",new Vector3(side*3.6f+x,.2f,-1.8f),new Vector3(.3f,.4f,.7f),rLime);}});
   var garden=RomanQualityModel("Art Mosaic Stele Garden",t=>{Box(t,"Stele Garden Paving",new Vector3(0,.08f,0),new Vector3(10,.16f,6),rPave);RomanMosaicStele(t,new Vector3(0,.16f,.8f),1.2f);foreach(float side in new[]{-1f,1f}){RPlanter(t,new Vector3(side*3.8f,0,-1.2f));RColumn(t,new Vector3(side*3.8f,.16f,1.8f),2.3f,.18f);}for(int i=0;i<9;i++){var tile=Box(t,"Inlaid Path Diamond",new Vector3(-4+i,.17f,-2.3f),new Vector3(.55f,.025f,.55f),i%2==0?rBronze:rWine);tile.transform.localRotation=Quaternion.Euler(0,45,0);}});
   var pavilion=RomanQualityModel("Art Ring Fountain Pavilion",t=>{Box(t,"Water Pavilion Paving",new Vector3(0,.08f,0),new Vector3(11,.16f,7),rPave);for(int i=0;i<4;i++)RColumn(t,new Vector3((i-1.5f)*2.7f,.16f,1.8f),3.9f,.22f);Box(t,"Pavilion Stone Lintel",new Vector3(0,4.12f,1.8f),new Vector3(10,.45f,1.8f),rIvory);var feature=new GameObject("Pavilion Geometric Fountain");feature.transform.SetParent(t,false);feature.transform.localPosition=new Vector3(0,.16f,0);RomanAbstractFountain(feature.transform);foreach(float side in new[]{-1f,1f})RPlanter(t,new Vector3(side*4,0,-1.8f));});
   var market=RomanQualityModel("Art Ceramic Tile Market",t=>{RHouse(t,9,3.8f,3.8f,rLime,1,true);RCanopy(t,new Vector3(0,0,-2.4f),7,3.2f,rBronze);Box(t,"Ceramic Display Counter",new Vector3(0,1,-3),new Vector3(6,.2f,1.5f),rIvory);foreach(float x in new[]{-2.5f,2.5f})Box(t,"Grounded Counter Pier",new Vector3(x,.5f,-3),new Vector3(.45f,1,1.3f),rLime);for(int i=0;i<7;i++){var tile=Box(t,"Glazed Mosaic Panel",new Vector3(-2.3f+i*.72f,1.16f,-3),new Vector3(.55f,.08f,.65f),i%2==0?rBronze:rWine);tile.transform.localRotation=Quaternion.Euler(0,i%2*25,0);}});var homes=d.landmarks.Where(p=>p&&!IsRomanFigurative(p.name)).ToList();foreach(var p in new[]{orbit,garden,pavilion,market})if(!homes.Any(a=>a.name==p.name))homes.Add(p);d.landmarks=homes.ToArray();
   var props=d.props.Where(p=>p&&!IsRomanFigurative(p.name)).ToList();foreach(var p in new[]{RomanQualityModel("Art Bronze Concentric Monument",t=>RomanOrbit(t,Vector3.zero,.8f)),RomanQualityModel("Art Carved Mosaic Stele",t=>RomanMosaicStele(t,Vector3.zero)),RomanQualityModel("Art Geometric Wall Fountain",RomanAbstractFountain),RomanQualityModel("Art Spiral Marble Marker",RomanSpiralMarker)})if(!props.Any(a=>a.name==p.name))props.Add(p);d.props=props.ToArray();
   RomanQualityModel("Art Garden Peristyle",t=>{Box(t,"Peristyle Court Paving",new Vector3(0,.08f,0),new Vector3(12,.16f,7),rPave);foreach(float x in new[]{-5f,5f})for(int i=0;i<4;i++)RColumn(t,new Vector3(x,.16f,-2.5f+i*1.8f),3.7f,.18f);foreach(float x in new[]{-5f,5f})Box(t,"Peristyle Colonnade Roof",new Vector3(x,4,0),new Vector3(1.8f,.36f,7),rRoof);Box(t,"Recessed Garden Pool",new Vector3(0,.2f,0),new Vector3(5.5f,.1f,3),rWater);for(int i=-1;i<=1;i++)RPlanter(t,new Vector3(i*2.8f,.1f,-2.8f));RomanMosaicStele(t,new Vector3(0,.16f,2.5f),.7f);});
   RemoveRomanFigureScenery(d);ApplyRomanPixelPaint();EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();System.IO.File.WriteAllText("Docs/RomanAbstractArtAudit.md",string.Join("\n",egyptAudit.ToArray()));BuildRomanTrades();return d.landmarks.Length+" figure-free structures; "+d.props.Length+" figure-free props";
  }
  static void RemoveRomanFigureScenery(LoopThemeDefinition d){
   if(d.ruralScenes==null)return;var clean=new List<GameObject>();
   foreach(var source in d.ruralScenes){if(!source)continue;bool figures=source.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Grazing Sheep"||t.name=="Shepherd With Crook");if(!figures){clean.Add(source);continue;}
    var root=UnityEngine.Object.Instantiate(source);if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    foreach(var child in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Grazing Sheep"||t.name=="Shepherd With Crook").ToArray())if(child)UnityEngine.Object.DestroyImmediate(child.gameObject);
    root.name=source.name.Contains("Flock")?"Countryside Olive Rest Garden":source.name.Contains("Pasture")?"Countryside Open Mountain Meadow":"Countryside Quiet Farmstead";
    var path=themeFolder+"/Models/"+root.name+".prefab";clean.Add(PrefabUtility.SaveAsPrefabAsset(root,path));UnityEngine.Object.DestroyImmediate(root);
   }d.ruralScenes=clean.GroupBy(p=>p.name).Select(g=>g.First()).ToArray();EditorUtility.SetDirty(d);
  }
 }
}
#endif
