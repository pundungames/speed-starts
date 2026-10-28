#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  public static string BuildRomanStreetPlantExpansion(){
   themeFolder=Base+"/RomeByzantium";Palette();RomanArtPalette();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");
   var names=new[]{"Art Rosemary Stone Bed","Art Low Mediterranean Shrubs","Art Lavender Stone Border"};
   var plants=new List<GameObject>(d.vegetation.Where(p=>p&&!names.Contains(p.name)));
   plants.Add(RomanArtModel(names[0],t=>{Box(t,"Low Travertine Planter",new Vector3(0,.18f,0),new Vector3(3.6f,.36f,1.1f),rLime);for(int i=0;i<5;i++){float x=-1.4f+i*.7f;Ball(t,"Irregular Rosemary Crown",new Vector3(x,.74f+(i%2)*.1f,0),new Vector3(.95f,.95f+(i%2)*.12f,.95f),i%2==0?rOlive:rPine);}}));
   plants.Add(RomanArtModel(names[1],t=>{for(int i=0;i<4;i++){float x=-1.1f+i*.73f,z=Mathf.Sin(i*2)*.25f;Ball(t,"Rounded Mediterranean Shrub",new Vector3(x,.57f+(i%2)*.16f,z),new Vector3(1.3f,1.14f+(i%2)*.2f,1.1f),i%2==0?rOlive:rPine);}}));
   var lavender=Paint("Art Muted Lavender Flowers",new Color(.56f,.44f,.59f),.38f);
   plants.Add(RomanArtModel(names[2],t=>{Box(t,"Lavender Stone Border",new Vector3(0,.14f,0),new Vector3(3.2f,.28f,1.0f),rLime);for(int i=0;i<7;i++){float x=-1.25f+i*.42f;Ball(t,"Lavender Leaves",new Vector3(x,.48f,0),new Vector3(.65f,.65f,.75f),rOlive);for(int j=0;j<3;j++){Vector3 p=new Vector3(x+(j-1)*.18f,.91f+(i%3)*.07f,(j%2)*.18f-.1f);Beam(t,"Lavender Stem",p-Vector3.up*.35f,p,.035f,rOlive);Ball(t,"Lavender Flower Tip",p,new Vector3(.11f,.24f,.11f),lavender);}}}));
   d.vegetation=plants.ToArray();EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return "Three independent Mediterranean shrub models added";
  }
 }
}
#endif
