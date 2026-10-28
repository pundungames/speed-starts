#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  public static void BuildRomanMountainPalette(){
   themeFolder=Base+"/RomeByzantium";
   var definition=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");
   definition.mountainMaterials=new[]{Paint("Near Mountain Sun",new Color(.69f,.72f,.56f),1),Paint("Near Mountain Shade",new Color(.63f,.67f,.51f),1),Paint("Far Mountain Sun",new Color(.80f,.79f,.70f),1),Paint("Far Mountain Shade",new Color(.75f,.75f,.66f),1)};
   EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
  }
 }
}
#endif
