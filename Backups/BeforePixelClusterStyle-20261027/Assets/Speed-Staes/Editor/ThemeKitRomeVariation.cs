#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  public static string ExpandRomanBackdrop(){
   themeFolder=Base+"/RomeByzantium";Palette();
   var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");
   var gate=Background(AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Triumphal Arch.prefab"),"Distant Triumphal Gate");
   var courtyard=Background(AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Statue and Mosaic Plaza.prefab"),"Distant Mosaic Courtyard");
   d.background=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Distant Domed City.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Distant Aqueduct.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Distant Forum.prefab"),gate,courtyard};
   EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return "Five Roman backdrop silhouettes";
  }
 }
}
#endif
