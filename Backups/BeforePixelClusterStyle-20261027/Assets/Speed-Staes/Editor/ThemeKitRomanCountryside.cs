#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static Material ruralGrass,ruralLight,ruralHill,ruralMountain,ruralStone,ruralRoof,ruralDark,ruralWool,ruralField;
  static void RuralPalette(){
   ruralGrass=Paint("Countryside Muted Meadow",new Color(.50f,.65f,.32f),.65f);ruralLight=Paint("Countryside Sunlit Grass",new Color(.70f,.75f,.49f),.7f);ruralHill=Paint("Countryside Distant Olive Hills",new Color(.66f,.70f,.53f),.75f);ruralMountain=Paint("Countryside Hazy Mountain",new Color(.73f,.72f,.61f),.8f);ruralStone=Paint("Countryside Pale Farm Plaster",new Color(.88f,.81f,.65f),.7f);ruralRoof=Paint("Countryside Faded Tile",new Color(.72f,.48f,.34f),.7f);ruralDark=Paint("Countryside Warm Recess",new Color(.45f,.39f,.29f),.65f);ruralWool=Paint("Countryside Sheep Wool",new Color(.95f,.91f,.76f),.65f);ruralField=Paint("Countryside Harvest Gold",new Color(.75f,.66f,.40f),.7f);
  }
  static void Ridge(Transform t,string name,Vector3 position,float width,float depth,float height,Material material,int variant){
   var v=new List<Vector3>();var tr=new List<int>();const int count=9;
   for(int i=0;i<count;i++){float u=(float)i/(count-1),x=(u-.5f)*width;float h=(.4f+.6f*Mathf.Abs(Mathf.Sin(i*1.43f+variant)))*height*Mathf.Sin(u*Mathf.PI);v.Add(new Vector3(x,-.2f,-depth*.5f));v.Add(new Vector3(x,h,(i%2==0?-.1f:.18f)*depth));v.Add(new Vector3(x,-.2f,depth*.5f));}
   for(int i=0;i<count-1;i++){int k=i*3;tr.AddRange(new[]{k,k+1,k+3,k+1,k+4,k+3,k+1,k+2,k+4,k+2,k+5,k+4});}
   var g=MeshObject(t,name,v,tr,material);g.transform.localPosition=position;
  }
  static void MeadowBase(Transform t,int variant,bool mountain=false){
   var ground=new List<Vector3>{new Vector3(0,-.025f,0)};var triangles=new List<int>();
   for(int i=0;i<12;i++){float a=i*Mathf.PI/6;ground.Add(new Vector3(Mathf.Sin(a)*(18.5f+(i%3)*.6f),-.025f,Mathf.Cos(a)*8));}
   for(int i=0;i<12;i++)triangles.AddRange(new[]{0,i+1,(i+1)%12+1});MeshObject(t,"Soft Meadow Ground",ground,triangles,ruralGrass);
   Ridge(t,"Rolling Meadow Ridge",new Vector3(0,0,5),43,15,3.0f,ruralHill,variant);
   if(mountain)Ridge(t,"Distant Mountain Peaks",new Vector3(0,0,13),49,17,8.2f,ruralMountain,variant+3);
   else Ridge(t,"Far Low Hills",new Vector3(3,0,12),45,12,4.3f,ruralMountain,variant+2);
  }
  static void RuralFence(Transform t,float z,float width){
   for(float x=-width*.5f;x<=width*.5f;x+=2.0f)Box(t,"Farm Fence Post",new Vector3(x,.55f,z),new Vector3(.13f,1.1f,.13f),ruralDark);
   foreach(float y in new[]{.35f,.8f})Beam(t,"Farm Fence Rail",new Vector3(-width*.5f,y,z),new Vector3(width*.5f,y,z),.08f,ruralDark);
  }
  static void Sheep(Transform parent,Vector3 p,float yaw,float scale=1){
   var g=new GameObject("Grazing Sheep");g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;var t=g.transform;
   Ball(t,"Broad Wool Body",new Vector3(0,.75f,0),new Vector3(1.45f,.85f,.75f),ruralWool);
   Ball(t,"Sheep Head",new Vector3(.75f,.86f,-.03f),new Vector3(.48f,.48f,.37f),ruralDark);
   foreach(float x in new[]{-.48f,.45f})foreach(float z in new[]{-.24f,.24f})Beam(t,"Short Sheep Leg",new Vector3(x,0,z),new Vector3(x,.55f,z),.105f,ruralDark);
   foreach(float z in new[]{-.26f,.21f})Box(t,"Sheep Ear",new Vector3(.68f,1.0f,z),new Vector3(.3f,.12f,.18f),ruralWool);
  }
  static void Shepherd(Transform parent,Vector3 p){
   var g=new GameObject("Shepherd With Crook");g.transform.SetParent(parent,false);g.transform.localPosition=p;var t=g.transform;
   var cloth=Paint("Countryside Shepherd Cloak",new Color(.58f,.37f,.27f),.65f);
   Lathe(t,"Shepherd Long Cloak",new Vector3(0,.25f,0),new[]{0f,1.05f,1.35f},new[]{.34f,.25f,.18f},cloth,8);
   Ball(t,"Shepherd Head",new Vector3(0,1.76f,0),Vector3.one*.32f,ruralStone);Cylinder(t,"Shepherd Straw Hat",new Vector3(0,1.93f,0),.32f,.08f,ruralField,12);
   Beam(t,"Shepherd Arm",new Vector3(.22f,1.45f,0),new Vector3(.5f,1.0f,-.06f),.13f,cloth);
   Beam(t,"Crook Staff",new Vector3(.62f,0,-.06f),new Vector3(.62f,2.05f,-.06f),.065f,ruralDark);Arc(t,"Crook Curved Tip",new Vector3(.47f,2.02f,-.06f),.16f,.05f,.06f,ruralDark,8,0,200);
  }
  static void RuralHouse(Transform t,Vector3 p,bool barn=false){
   var r=new GameObject(barn?"Rustic Barn":"Farm Cottage");r.transform.SetParent(t,false);r.transform.localPosition=p;var h=r.transform;
   Box(h,"Warm Farm Walls",new Vector3(0,1.8f,0),new Vector3(6,3.6f,4.3f),barn?ruralField:ruralStone);Pyramid(h,"Farm Tiled Roof",new Vector3(0,3.6f,0),6.8f,1.5f,ruralRoof);
   Box(h,"Farm Dark Door",new Vector3(0,1.2f,-2.18f),new Vector3(barn?2.4f:1.2f,2.4f,.06f),ruralDark);
   foreach(float x in new[]{-2f,2f}){Box(h,"Farm Window",new Vector3(x,2.3f,-2.18f),new Vector3(.8f,.9f,.06f),ruralDark);Box(h,"Farm Window Sill",new Vector3(x,1.79f,-2.24f),new Vector3(1,.14f,.15f),ruralStone);}
   Box(h,"Farm Chimney",new Vector3(1.7f,4.9f,.4f),new Vector3(.6f,1.5f,.7f),ruralStone);
  }
  static void CountryOlive(Transform t,Vector3 p,float height=3.5f){var r=new GameObject("Country Olive Tree");r.transform.SetParent(t,false);r.transform.localPosition=p;Tree(r.transform,Vector3.zero,height);foreach(var renderer in r.GetComponentsInChildren<Renderer>()){var a=renderer.sharedMaterials;for(int i=0;i<a.Length;i++)if(a[i]==green||a[i]==leaf)a[i]=ruralHill;renderer.sharedMaterials=a;}}
  static void AddRomanCountryside(LoopThemeDefinition d){
   RuralPalette();
   var meadow=Model("Countryside Open Olive Meadow",t=>{MeadowBase(t,1);CountryOlive(t,new Vector3(-10,0,0),4);CountryOlive(t,new Vector3(9,0,2),3.4f);for(int i=0;i<4;i++)Ball(t,"Meadow Flat Stone",new Vector3(-3+i*1.7f,.16f,-2),new Vector3(1.2f,.35f,.8f),ruralStone);});
   var mountains=Model("Countryside Open Mountain Meadow",t=>{MeadowBase(t,6,true);CountryOlive(t,new Vector3(-12,0,1),3.8f);});
   var flock=Model("Countryside Olive Rest Garden",t=>{MeadowBase(t,3);CountryOlive(t,new Vector3(-12,0,2),4.2f);CountryOlive(t,new Vector3(12,0,2),3.5f);});
   var farm=Model("Countryside Quiet Farmstead",t=>{MeadowBase(t,8);RuralHouse(t,new Vector3(-6,0,1));RuralHouse(t,new Vector3(4,0,2),true);RuralFence(t,-4,25);for(int row=0;row<4;row++)Box(t,"Harvest Field Furrow",new Vector3(10,.04f,-1+row*.75f),new Vector3(8,.08f,.35f),row%2==0?ruralField:ruralLight);CountryOlive(t,new Vector3(-13,0,2),3.8f);});
   var chapel=Model("Countryside Village Chapel",t=>{MeadowBase(t,5);Box(t,"Chapel Nave",new Vector3(0,2.2f,0),new Vector3(7,4.4f,4.6f),ruralStone);Pyramid(t,"Chapel Red Roof",new Vector3(0,4.4f,0),7.6f,2,ruralRoof);Box(t,"Chapel Bell Tower",new Vector3(-3,3.7f,.2f),new Vector3(2.0f,7.4f,2.5f),ruralStone);Pyramid(t,"Chapel Bell Roof",new Vector3(-3,7.4f,.2f),2.6f,1.1f,ruralRoof);Box(t,"Chapel Door",new Vector3(0,1.25f,-2.34f),new Vector3(1.6f,2.5f,.06f),ruralDark);Arc(t,"Chapel Door Arch",new Vector3(0,2.3f,-2.4f),.82f,.18f,.12f,ruralStone,10);Box(t,"Bell Tower Opening",new Vector3(-3,6.1f,-1.1f),new Vector3(.7f,1.0f,.06f),ruralDark);Beam(t,"Chapel Cross",new Vector3(-3,8.4f,.2f),new Vector3(-3,9.4f,.2f),.1f,ruralField);Beam(t,"Chapel Cross Arms",new Vector3(-3.4f,9.1f,.2f),new Vector3(-2.6f,9.1f,.2f),.1f,ruralField);CountryOlive(t,new Vector3(-11,0,0),3.8f);CountryOlive(t,new Vector3(10,0,1),4.2f);RuralHouse(t,new Vector3(11,0,5));});
   var vineyard=Model("Countryside Terraced Vineyard",t=>{MeadowBase(t,11);RuralHouse(t,new Vector3(-9,0,3));for(int row=0;row<4;row++){float z=-3+row*1.5f;for(int col=0;col<5;col++){float x=-1+col*2.5f;Beam(t,"Vineyard Support",new Vector3(x,0,z),new Vector3(x,1.4f,z),.07f,ruralDark);Ball(t,"Vine Row Canopy",new Vector3(x,1.2f,z),new Vector3(2.2f,.75f,.85f),row%2==0?ruralHill:ruralLight);}}RuralFence(t,-5,28);CountryOlive(t,new Vector3(14,0,5),4);});
   d.ruralScenes=new[]{meadow,mountains,flock,farm,chapel,vineyard};
  }
  public static string BuildRomanCountryside(){themeFolder=Base+"/RomeByzantium";Palette();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");AddRomanCountryside(d);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return "Six Roman countryside scenery prefabs created";}
 }
}
#endif


