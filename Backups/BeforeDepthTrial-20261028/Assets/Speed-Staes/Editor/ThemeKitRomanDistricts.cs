#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor
{
 public static partial class ThemeKitBuilder
 {
  static void RomanWindow(Transform t,float x,float y,float z,float width=.8f){Box(t,"Recessed Blue Window",new Vector3(x,y,z),new Vector3(width,1.15f,.06f),blue);Box(t,"Stone Window Sill",new Vector3(x,y-.65f,z-.06f),new Vector3(width+.2f,.15f,.18f),cream);}
  static void Insula(Transform t){
   Box(t,"Ochre Apartment Block",new Vector3(-1,4,0),new Vector3(8,8,5),sandLight);Box(t,"Lower Side Wing",new Vector3(4,2.6f,.7f),new Vector3(3,5.2f,4),red);
   for(int floor=0;floor<3;floor++){Box(t,"Floor Cornice",new Vector3(-1,1.0f+floor*2.65f,0),new Vector3(8.4f,.2f,5.3f),cream);for(int col=0;col<3;col++)RomanWindow(t,-3.7f+col*2.7f,2+floor*2.65f,-2.54f);}
   Box(t,"Flat Parapet Roof",new Vector3(-1,8.15f,0),new Vector3(8.5f,.35f,5.6f),red);
   Box(t,"Roof Stair House",new Vector3(-3,8.8f,1),new Vector3(2.4f,1.4f,2.2f),sandLight);
   for(int i=0;i<2;i++){Box(t,"Shop Door",new Vector3(3.1f+i*1.5f,1.2f,-1.34f),new Vector3(1,2.4f,.08f),woodDark);}
   Box(t,"Shop Teal Awning",new Vector3(4,2.8f,-1.8f),new Vector3(3.4f,.2f,1.9f),teal);
   Box(t,"Balcony Floor",new Vector3(-1,4.1f,-3),new Vector3(4,.18f,1.2f),stone);for(int i=0;i<5;i++)Beam(t,"Balcony Iron Rail",new Vector3(-2.7f+i*.85f,4.2f,-3.6f),new Vector3(-2.7f+i*.85f,5,-3.6f),.07f,woodDark);Beam(t,"Balcony Top Rail",new Vector3(-2.9f,5,-3.6f),new Vector3(.9f,5,-3.6f),.09f,woodDark);
  }
  static void Watchtower(Transform t){
   Box(t,"Fortified Brick Tower",new Vector3(0,4.8f,0),new Vector3(4.5f,9.6f,4),red);
   for(int y=0;y<4;y++)Box(t,"Limestone Tower Band",new Vector3(0,1+y*2.3f,0),new Vector3(4.65f,.25f,4.15f),cream);
   Box(t,"Overhanging Tower Crown",new Vector3(0,9.7f,0),new Vector3(5.4f,.6f,4.8f),stone);
   foreach(float x in new[]{-2f,0,2f})foreach(float z in new[]{-1.9f,1.9f})Box(t,"Tower Battlement",new Vector3(x,10.4f,z),new Vector3(.9f,1,.9f),cream);
   for(int row=0;row<3;row++)Box(t,"Narrow Archer Window",new Vector3(0,2.5f+row*2.5f,-2.04f),new Vector3(.5f,1.2f,.06f),woodDark);
   foreach(float side in new[]{-1f,1f}){Box(t,"Low Ruined City Wall",new Vector3(side*4,1.7f,.6f),new Vector3(3.5f,3.4f,1.2f),sand);for(int j=0;j<3;j++)Box(t,"Wall Crenellation",new Vector3(side*(2.8f+j),3.7f,.6f),new Vector3(.55f,.7f,1.3f),cream);}
  }
  static void Amphitheatre(Transform t){
   var ring=new GameObject("Oval Arena");ring.transform.SetParent(t,false);ring.transform.localScale=new Vector3(1,1,.67f);
   for(int level=0;level<2;level++)for(int i=0;i<12;i++){
    float a=i*30*Mathf.Deg2Rad;var bay=new GameObject("Arena Arch Bay");bay.transform.SetParent(ring.transform,false);bay.transform.localPosition=new Vector3(Mathf.Sin(a)*6,level*3.1f,Mathf.Cos(a)*6);bay.transform.localRotation=Quaternion.Euler(0,i*30,0);
    foreach(float x in new[]{-1.55f,1.55f})Box(bay.transform,"Arena Pier",new Vector3(x,1.0f,0),new Vector3(.4f,2,.55f),sandLight);
    Arc(bay.transform,"Arena Vault",new Vector3(0,1.6f,0),1.25f,.36f,.6f,cream,10);Box(bay.transform,"Arena Cornice",new Vector3(0,3.0f,0),new Vector3(3.5f,.25f,.8f),sand);
   }
   Lathe(ring.transform,"Arena Upper Rim",new Vector3(0,6.2f,0),new[]{0f,.4f},new[]{6.4f,6.4f},sandLight,24);
   Cylinder(ring.transform,"Arena Sand Floor",Vector3.zero,5.7f,.12f,sand,24);
  }
  static void Baths(Transform t){
   Box(t,"Roman Baths Hall",new Vector3(0,2.2f,0),new Vector3(12,4.4f,6),sandLight);
   for(int roof=0;roof<2;roof++){
    var v=new List<Vector3>();var tr=new List<int>();for(int i=0;i<=12;i++){float a=i*Mathf.PI/12;foreach(float x in new[]{-6.4f,6.4f})v.Add(new Vector3(x,4.4f+Mathf.Sin(a)*1.9f,(roof==0?-1.6f:1.6f)+Mathf.Cos(a)*1.65f));}
    for(int i=0;i<12;i++){int k=i*2;tr.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3,k+1,k+2,k,k+3,k+2,k+1});}MeshObject(t,"Terracotta Barrel Vault",v,tr,red);
   }
   for(int i=0;i<4;i++){RomanWindow(t,-4.5f+i*3,2.4f,-3.04f,1.4f);Arc(t,"Bath Window Arch",new Vector3(-4.5f+i*3,2.9f,-3.12f),.72f,.18f,.15f,cream,10);}
   foreach(float side in new[]{-1f,1f})Box(t,"Bath End Buttress",new Vector3(side*5.8f,2.4f,-3.3f),new Vector3(.65f,4.8f,.8f),stone);
  }
  static void MarketCourt(Transform t){
   Box(t,"Market Back Wall",new Vector3(0,2.2f,2),new Vector3(13,4.4f,1),sandLight);
   for(int stall=0;stall<3;stall++){float x=(stall-1)*4.2f;Box(t,"Market Arch Recess",new Vector3(x,1.5f,1.45f),new Vector3(2.6f,3,.08f),woodDark);foreach(float side in new[]{-1f,1f})Cylinder(t,"Awning Timber",new Vector3(x+side*1.6f,0,-1.8f),.09f,3.3f,wood,8);for(int stripe=0;stripe<5;stripe++){var canopy=Box(t,"Striped Market Canopy",new Vector3(x+(stripe-2)*.65f,3.5f,-.4f),new Vector3(.66f,.12f,3.7f),stripe%2==0?cream:(stall%2==0?red:teal));canopy.transform.localRotation=Quaternion.Euler(8,0,0);}Box(t,"Stone Market Counter",new Vector3(x,.9f,-1),new Vector3(3,1.8f,.9f),sand);for(int jar=0;jar<2;jar++){var r=new GameObject("Merchant Amphora");r.transform.SetParent(t,false);r.transform.localPosition=new Vector3(x+(jar==0?-.7f:.7f),1.8f,-1);Urn(r.transform,jar==0?teal:red,.45f);}}
  }
  static void FountainCourt(Transform t){
   Box(t,"Garden Court Base",new Vector3(0,.12f,0),new Vector3(12,.24f,7),stone);
   Cylinder(t,"Octagonal Fountain Basin",Vector3.up*.25f,2.4f,.65f,cream,8);Cylinder(t,"Blue Fountain Pool",Vector3.up*.92f,2.0f,.04f,water,12);Cylinder(t,"Fountain Pedestal",Vector3.up*.9f,.28f,1.5f,stone,10);Lathe(t,"Upper Fountain Bowl",Vector3.up*2.4f,new[]{0f,.45f},new[]{.4f,1.25f},cream,12);Cylinder(t,"Upper Blue Pool",Vector3.up*2.87f,1.05f,.04f,water,12);Cylinder(t,"Fountain Finial",Vector3.up*2.9f,.16f,.8f,gold,8);
   foreach(float side in new[]{-1f,1f}){Tree(t,new Vector3(side*4,0,1.2f),3.8f);Box(t,"Garden Bench",new Vector3(side*4,.6f,-1.8f),new Vector3(2.2f,.25f,.8f),cream);foreach(float x in new[]{-.6f,.6f})Box(t,"Bench Stone Foot",new Vector3(side*4+x,.25f,-1.8f),new Vector3(.3f,.5f,.6f),stone);}
  }
  static void AddRomanDistricts(LoopThemeDefinition d){
   var models=new[]{Model("Roman Insula Shops",Insula),Model("Byzantine Brick Watchtower",Watchtower),Model("Oval Roman Amphitheatre",Amphitheatre),Model("Terracotta Vault Baths",Baths),Model("Striped Roman Market",MarketCourt),Model("Tiered Fountain Garden",FountainCourt)};
   d.landmarks=d.landmarks.Concat(models).GroupBy(p=>p.name).Select(g=>g.First()).ToArray();
   d.background=d.background.Concat(models.Select(p=>Background(p,"Distant "+p.name))).GroupBy(p=>p.name).Select(g=>g.First()).ToArray();
   var cart=Model("Roman Produce Cart",t=>{Box(t,"Cart Timber Bed",new Vector3(0,.8f,0),new Vector3(2.4f,.2f,1.4f),wood);foreach(float side in new[]{-1f,1f}){var wheel=Cylinder(t,"Cart Wheel",new Vector3(side*1.3f,.55f,0),.55f,.18f,woodDark,12);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}for(int i=0;i<4;i++)Ball(t,"Terracotta Produce Basket",new Vector3((i-1.5f)*.5f,1.1f,0),new Vector3(.5f,.45f,.6f),i%2==0?red:gold);Beam(t,"Cart Pull Handle",new Vector3(0,.8f,.7f),new Vector3(0,.6f,2.3f),.12f,wood);});
   var well=Model("Stone Well With Winch",t=>{Cylinder(t,"Well Curb",Vector3.zero,1.0f,1.1f,stone,10);Cylinder(t,"Well Dark Opening",Vector3.up*1.12f,.72f,.02f,woodDark,10);foreach(float side in new[]{-1f,1f})Beam(t,"Well Roof Post",new Vector3(side*.9f,0,0),new Vector3(side*.9f,2.6f,0),.14f,wood);Beam(t,"Well Crossbar",new Vector3(-1.2f,2.5f,0),new Vector3(1.2f,2.5f,0),.16f,wood);Cylinder(t,"Lowered Bucket",Vector3.up*1.6f,.22f,.4f,wood,10);});
   var sundial=Model("Marble Sundial",t=>{Cylinder(t,"Sundial Pedestal",Vector3.zero,.32f,1.2f,stone,8);Cylinder(t,"Sundial Face",Vector3.up*1.2f,.75f,.14f,cream,12);MeshObject(t,"Sundial Gnomon",new List<Vector3>{new Vector3(0,1.35f,-.4f),new Vector3(0,2.0f,.25f),new Vector3(0,1.35f,.4f)},new List<int>{0,1,2,2,1,0},gold);});
   var planter=Model("Terracotta Garden Planter",t=>{Lathe(t,"Wide Planting Urn",Vector3.zero,new[]{0f,.15f,.8f,1.0f},new[]{.55f,.7f,.8f,.9f},red,10);for(int i=0;i<5;i++)Ball(t,"Planter Foliage",new Vector3(Mathf.Sin(i*2)*.5f,1.2f+(i%2)*.3f,Mathf.Cos(i*2)*.3f),new Vector3(.8f,.65f,.7f),i%2==0?green:leaf);});
   var ruins=Model("Excavated Column Fragments",t=>{Cylinder(t,"Broken Standing Column",Vector3.zero,.4f,1.5f,cream,10);var fallen=Cylinder(t,"Fallen Column Drum",new Vector3(1,.4f,0),.4f,1.4f,stone,10);fallen.transform.localRotation=Quaternion.Euler(0,0,90);Box(t,"Weathered Column Capital",new Vector3(-.8f,.2f,-.5f),new Vector3(.8f,.4f,.8f),sandLight);});
   d.props=d.props.Concat(new[]{cart,well,sundial,planter,ruins}).GroupBy(p=>p.name).Select(g=>g.First()).ToArray();AddMoreRomanVariety(d);
  }
  public static string ExpandRomanDistricts(){themeFolder=Base+"/RomeByzantium";Palette();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");AddRomanDistricts(d);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return "Six Roman landmarks and five street props added";}
 }
}
#endif

