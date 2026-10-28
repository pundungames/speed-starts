#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static void CourtyardVilla(Transform t){
   Box(t,"Villa Rear House",new Vector3(0,2.5f,2),new Vector3(12,5,3.8f),cream);
   foreach(float side in new[]{-1f,1f}){Box(t,"Terracotta Side Wing",new Vector3(side*5,1.6f,-1),new Vector3(2.5f,3.2f,6),sandLight);Box(t,"Wing Tiled Roof",new Vector3(side*5,3.35f,-1),new Vector3(3,.35f,6.5f),red);}
   for(int i=0;i<4;i++)RomanWindow(t,-4.5f+i*3,3.2f,.06f,1.1f);
   Box(t,"Villa Long Red Roof",new Vector3(0,5.15f,2),new Vector3(12.7f,.4f,4.5f),red);
   foreach(float x in new[]{-3f,0,3f})Column(t,new Vector3(x,0,-1.5f),2.7f);
   Box(t,"Garden Loggia Beam",new Vector3(0,2.8f,-1.5f),new Vector3(8,.3f,1.0f),sandLight);
   Cylinder(t,"Courtyard Circular Pool Rim",Vector3.up*.15f,1.4f,.3f,stone,12);Cylinder(t,"Courtyard Blue Pool",Vector3.up*.46f,1.1f,.03f,water,12);
   Tree(t,new Vector3(-3.5f,0,-2.6f),2.6f);Tree(t,new Vector3(3.5f,0,-2.6f),2.9f);
  }
  static void RoundShrine(Transform t){
   Cylinder(t,"Circular Shrine Steps",Vector3.zero,4.9f,.35f,stone,20);Cylinder(t,"Circular Shrine Platform",Vector3.up*.35f,4.5f,.35f,cream,20);
   Cylinder(t,"Shrine Inner Drum",Vector3.up*.7f,2.5f,4.5f,sandLight,16);
   for(int i=0;i<10;i++){float a=i*Mathf.PI*2/10;Column(t,new Vector3(Mathf.Sin(a)*3.5f,.7f,Mathf.Cos(a)*3.5f),4.8f);}
   Cylinder(t,"Round Entablature",Vector3.up*5.5f,4.6f,.45f,cream,20);Lathe(t,"Low Copper Cone Roof",Vector3.up*5.95f,new[]{0f,1.6f,1.9f},new[]{4.9f,1.2f,.15f},teal,20);
   Box(t,"Shrine Dark Door",new Vector3(0,2.0f,-2.54f),new Vector3(1.4f,2.6f,.06f),woodDark);
  }
  static void CeramicWorkshop(Transform t){
   Box(t,"Plaster Workshop",new Vector3(-2,2.0f,0),new Vector3(7,4,5),sandLight);Pyramid(t,"Four Slope Tile Roof",new Vector3(-2,4,0),7.8f,1.8f,red);
   Box(t,"Workshop Blue Shutter",new Vector3(-3.3f,2.3f,-2.54f),new Vector3(1.7f,1.6f,.07f),teal);Box(t,"Workshop Dark Entry",new Vector3(-.6f,1.4f,-2.54f),new Vector3(1.5f,2.8f,.07f),woodDark);
   Lathe(t,"Round Pottery Kiln",new Vector3(3.4f,0,.6f),new[]{0f,2.4f,3.5f,3.8f},new[]{1.9f,1.9f,.9f,.8f},red,12);Cylinder(t,"Kiln Tall Chimney",new Vector3(3.4f,3.6f,.6f),.6f,2.6f,sand,10);Box(t,"Kiln Fire Mouth",new Vector3(3.4f,.7f,-1.32f),new Vector3(.8f,1.3f,.06f),woodDark);
   for(int i=0;i<3;i++){var r=new GameObject("Potter Display Jars");r.transform.SetParent(t,false);r.transform.localPosition=new Vector3(-4+i*1.9f,0,-3.5f);Urn(r.transform,i%2==0?red:teal,.85f);}
   Box(t,"Drying Rack Shelf",new Vector3(-2,1.2f,-3.7f),new Vector3(5,.15f,.7f),wood);
  }
  static void BelfryCourt(Transform t){
   Box(t,"Ivory Belfry Shaft",new Vector3(0,4.2f,0),new Vector3(3.6f,8.4f,3.4f),cream);
   for(int level=0;level<3;level++){Box(t,"Belfry Moulding",new Vector3(0,1.5f+level*2.3f,0),new Vector3(3.9f,.22f,3.7f),sandLight);Box(t,"Tall Bell Window",new Vector3(0,2.4f+level*2.3f,-1.74f),new Vector3(.8f,1.35f,.06f),woodDark);}
   foreach(float x in new[]{-1.4f,1.4f})foreach(float z in new[]{-1.2f,1.2f})Column(t,new Vector3(x,8.4f,z),2.3f);
   Cylinder(t,"Bronze Hanging Bell",new Vector3(0,8.8f,0),.65f,1.3f,gold,10);Pyramid(t,"Steep Teal Bell Roof",new Vector3(0,10.9f,0),4.6f,2.5f,teal);
   Box(t,"Belfry Courtyard Wall",new Vector3(-3.8f,1,1.1f),new Vector3(4,2,1),sandLight);Tree(t,new Vector3(3.3f,0,1),4,true);
  }
  static void RomanHorizon(LoopThemeDefinition d){
   var haze=Paint("Horizon Warm Beige Haze",new Color(.84f,.79f,.68f),.85f);
   var edge=Paint("Horizon Faded Limestone",new Color(.90f,.85f,.75f),.85f);
   var shade=Paint("Horizon Faded Clay Roof",new Color(.77f,.68f,.58f),.85f);
   var silhouettes=new List<GameObject>();
   silhouettes.Add(Model("Horizon Stepped Hill Town",t=>{for(int i=0;i<6;i++){float h=3.0f+(i%3)*1.4f;Box(t,"Terraced Town Mass",new Vector3((i-2.5f)*2.9f,h*.5f,i%2),new Vector3(3.0f,h,3),i%2==0?haze:edge);}Box(t,"Hilltop Hall Mass",new Vector3(-1.5f,6.1f,1.5f),new Vector3(4,2.6f,3.4f),haze);}));
   silhouettes.Add(Model("Horizon Long Roof Warehouses",t=>{for(int i=0;i<3;i++){float x=(i-1)*5;Box(t,"Warehouse Silhouette",new Vector3(x,1.7f,0),new Vector3(4.9f,3.4f,4),haze);Pyramid(t,"Hipped Roof Silhouette",new Vector3(x,3.4f,0),5.2f,1.3f,shade);}}));
   silhouettes.Add(Model("Horizon Citadel Ridge",t=>{Box(t,"Citadel Wall Mass",new Vector3(0,2.0f,0),new Vector3(16,4,2.6f),haze);foreach(float x in new[]{-6.3f,1,6.3f}){float h=x==1?7.0f:5.3f;Box(t,"Distant Squat Tower",new Vector3(x,h*.5f,.3f),new Vector3(2.7f,h,3.0f),edge);Box(t,"Tower Flat Crown",new Vector3(x,h+.15f,.3f),new Vector3(3.1f,.3f,3.4f),shade);}}));
   silhouettes.Add(Model("Horizon Abandoned Roofless Hall",t=>{Box(t,"Ancient Back Wall Mass",new Vector3(0,2.6f,1),new Vector3(13,5.2f,1),haze);for(int i=0;i<4;i++)Box(t,"Uneven Remaining Pier",new Vector3(-5+i*3.4f,2.6f+(i%2)*.7f,-.5f),new Vector3(.8f,5.2f+(i%2)*1.4f,1.2f),edge);Box(t,"Broken High Wall",new Vector3(-3.8f,5.8f,1),new Vector3(5,1.2f,1),haze);}));
   silhouettes.Add(Model("Horizon River Quarter Roofline",t=>{for(int i=0;i<4;i++){float h=2.7f+(i%2)*1.5f;Box(t,"Broad Low Urban Block",new Vector3((i-1.5f)*3.3f,h*.5f,0),new Vector3(3.2f,h,4),i%2==0?haze:edge);Box(t,"Simple Roof Parapet",new Vector3((i-1.5f)*3.3f,h+.12f,0),new Vector3(3.5f,.24f,4.2f),shade);}Box(t,"Distant Utility Turret",new Vector3(2,5.5f,1),new Vector3(1.4f,2.6f,1.6f),haze);}));
   silhouettes.Add(Model("Horizon Twin Slender Spires",t=>{Box(t,"Remote Hall Block",new Vector3(0,1.9f,0),new Vector3(11,3.8f,4),haze);foreach(float x in new[]{-3.8f,3.8f}){Box(t,"Slender Roof Turret",new Vector3(x,4.6f,.8f),new Vector3(1.3f,3.2f,1.4f),edge);Pyramid(t,"Slender Spire",new Vector3(x,6.2f,.8f),1.7f,2.1f,shade);}}));
   silhouettes.Add(Model("Horizon Garden Wall Pavilions",t=>{Box(t,"Distant Enclosure Wall",new Vector3(0,1.2f,0),new Vector3(17,2.4f,1),haze);foreach(float x in new[]{-6f,0,6f}){Box(t,"Quiet Pavilion Block",new Vector3(x,2.2f,1.2f),new Vector3(3.4f,4.4f,3),edge);Pyramid(t,"Quiet Pavilion Roof",new Vector3(x,4.4f,1.2f),4,1.4f,shade);}}));
   silhouettes.Add(Model("Horizon Ancient Terraced Ruins",t=>{for(int i=0;i<4;i++){float h=1.8f+i*.9f;Box(t,"Worn Terrace Mass",new Vector3((i-1.5f)*3.4f,h*.5f,0),new Vector3(3.8f,h,4.2f),i%2==0?haze:edge);}Cylinder(t,"Low Ruined Round Drum",new Vector3(-4,1.8f,1),2.4f,2.7f,shade,10);}));
   AddDetailedRomanHorizon(silhouettes,haze,edge,shade);d.background=silhouettes.ToArray();AddRomanCountryside(d);
  }
  static void AddMoreRomanVariety(LoopThemeDefinition d){
   var buildings=new[]{Model("Courtyard Roman Villa",CourtyardVilla),Model("Circular Copper Roof Shrine",RoundShrine),Model("Ceramic Kiln Workshop",CeramicWorkshop),Model("Ivory Belfry Courtyard",BelfryCourt)};
   d.landmarks=d.landmarks.Concat(buildings).GroupBy(p=>p.name).Select(g=>g.First()).ToArray();RomanHorizon(d);
  }
  public static string RefineRomanDepth(){themeFolder=Base+"/RomeByzantium";Palette();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");AddMoreRomanVariety(d);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return "Four foreground models and eight independent silhouette models created";}
 }
}
#endif


