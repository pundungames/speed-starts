#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static void DressRomanHorizon(Transform root){
   var recess=Paint("Horizon Warm Recesses",new Color(.67f,.61f,.51f),.85f);
   var trim=Paint("Horizon Faded Limestone Trim",new Color(.90f,.85f,.75f),.85f);
   var roof=Paint("Horizon Faded Terracotta",new Color(.79f,.69f,.59f),.85f);
   var masses=root.Cast<Transform>().ToArray();int variant=0;
   foreach(var mass in masses){
    string n=mass.name;
    if(!(n.Contains("Mass")||n.Contains("Silhouette")||n.Contains("Block")||n.Contains("Tower")||n.Contains("Turret")))continue;
    if(n.Contains("Roof")||n.Contains("Crown")||n.Contains("Parapet")||n.Contains("Cornice"))continue;
    Vector3 size=mass.localScale,p=mass.localPosition;
    if(size.x<1.2f||size.y<1.8f||size.z<.8f)continue;
    float bottom=p.y-size.y*.5f,top=p.y+size.y*.5f,z=p.z-size.z*.5f-.035f;
    int cols=Mathf.Clamp(Mathf.FloorToInt(size.x/2.1f),1,5),rows=size.y>4.5f?2:1;
    for(int row=0;row<rows;row++)for(int col=0;col<cols;col++){
     float x=p.x-size.x*.5f+(col+.5f)*size.x/cols;
     float y=bottom+(rows==1?.65f:(row+.55f)/(rows+.25f))*size.y;float w=Mathf.Min(1.05f,size.x/cols*.42f);
     Box(root,"Distant Grouped Window Recess",new Vector3(x,y,z),new Vector3(w,.85f,.055f),recess);
     Box(root,"Distant Window Sill",new Vector3(x,y-.49f,z-.03f),new Vector3(w+.22f,.13f,.12f),trim);
     if((variant+row)%2==0&&size.y>3.2f)Arc(root,"Distant Window Arch",new Vector3(x,y+.4f,z-.04f),w*.5f,.12f,.06f,trim,7);
    }
    Box(root,"Distant Facade Cornice",new Vector3(p.x,top-.13f,z-.04f),new Vector3(size.x+.12f,.20f,.16f),trim);
    if(rows==2)Box(root,"Distant Floor Belt",new Vector3(p.x,bottom+size.y*.49f,z-.03f),new Vector3(size.x+.08f,.17f,.13f),trim);
    if(size.x>3.5f){foreach(float side in new[]{-1f,1f})Box(root,"Distant Shallow Pilaster",new Vector3(p.x+side*(size.x*.5f-.15f),p.y,z-.02f),new Vector3(.23f,size.y,.12f),trim);}
    if(variant%3==1&&size.z>2)Box(root,"Distant Small Chimney",new Vector3(p.x+size.x*.25f,top+.45f,p.z+.3f),new Vector3(.4f,.9f,.5f),roof);
    variant++;
   }
  }
  static void AddDetailedRomanHorizon(System.Collections.Generic.List<GameObject> models,Material facade,Material light,Material roof){
   models.Add(Model("Horizon Hillside Monastery",t=>{
    Box(t,"Monastery Lower Block",new Vector3(-2,2,0),new Vector3(11,4,4),facade);Box(t,"Monastery Upper Block",new Vector3(1,5.0f,1),new Vector3(7,2,3),light);
    Pyramid(t,"Monastery Sloped Roof",new Vector3(1,6.0f,1),7.5f,1.7f,roof);
    Box(t,"Monastery End Tower",new Vector3(-6,3.8f,.5f),new Vector3(2.1f,7.6f,3),light);Pyramid(t,"Monastery Tower Roof",new Vector3(-6,7.6f,.5f),2.6f,1.1f,roof);
    for(int i=0;i<4;i++){Box(t,"Monastery Gallery Pier",new Vector3(-5+i*2.1f,1.25f,-2.4f),new Vector3(.35f,2.5f,.6f),light);Arc(t,"Monastery Gallery Vault",new Vector3(-4.0f+i*2.1f,1.8f,-2.4f),.8f,.2f,.5f,light,8);}
   }));
   models.Add(Model("Horizon Terraced Imperial Palace",t=>{
    for(int i=0;i<3;i++)Box(t,"Palace Broad Terrace",new Vector3(0,.2f+i*.25f,-1.2f-i*.6f),new Vector3(16-i, .4f,5.5f-i*.6f),light);
    Box(t,"Palace Central Block",new Vector3(0,3.9f,.8f),new Vector3(8,6.2f,4),facade);
    foreach(float side in new[]{-1f,1f})Box(t,"Palace Side Block",new Vector3(side*5.5f,2.5f,1),new Vector3(3,3.4f,4),light);
    Box(t,"Palace Roof Parapet",new Vector3(0,7.2f,.8f),new Vector3(8.6f,.4f,4.6f),roof);for(int i=0;i<5;i++)Box(t,"Palace Roof Baluster",new Vector3(-3+i*1.5f,7.6f,-1.45f),new Vector3(.25f,.55f,.25f),light);
   }));
   models.Add(Model("Horizon Round Tower City Gate",t=>{
    foreach(float x in new[]{-4.4f,4.4f}){Cylinder(t,"Gate Round Tower",new Vector3(x,0,.7f),1.6f,6,facade,12);Lathe(t,"Gate Roof Cone",new Vector3(x,6,.7f),new[]{0f,1.6f},new[]{1.9f,.05f},roof,12);for(int row=0;row<2;row++)Box(t,"Gate Tower Recess",new Vector3(x,2+row*2,-.94f),new Vector3(.55f,.9f,.05f),Paint("Horizon Warm Recesses",new Color(.67f,.61f,.51f),.85f));}
    foreach(float side in new[]{-1f,1f})Box(t,"Gate Thick Pier",new Vector3(side*2.6f,1.8f,0),new Vector3(1.0f,3.6f,2),light);
    Arc(t,"Remote Gate Round Vault",new Vector3(0,2.5f,0),2.05f,.55f,1.8f,light,12);Box(t,"Gate Upper Block",new Vector3(0,5.5f,0),new Vector3(6.0f,1.5f,2.4f),facade);
   }));
   models.Add(Model("Horizon Gabled Merchant Quarter",t=>{
    for(int i=0;i<4;i++){float x=(i-1.5f)*3.3f,h=3.5f+(i%3)*.9f;Box(t,"Merchant Quarter Block",new Vector3(x,h*.5f,i%2),new Vector3(3.2f,h,4),i%2==0?facade:light);Pyramid(t,"Merchant Broad Gable Roof",new Vector3(x,h,i%2),3.6f,1.8f,roof);}
    foreach(float x in new[]{-3.3f,3.3f})Box(t,"Faded Ground Awning",new Vector3(x,1.5f,-2.2f),new Vector3(2.7f,.16f,1.0f),roof);
   }));
  }
  public static string WarmAndDetailRomanHorizon(){themeFolder=Base+"/RomeByzantium";Palette();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");RomanHorizon(d);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return "Twelve warm beige Roman backdrops with restrained architecture details";}
 }
}
#endif

