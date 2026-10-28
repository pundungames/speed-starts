#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static class EgyptDesertAtmosphereSetup {
  public static void Configure(GameObject root){
   const string folder="Assets/Speed-Staes/Prefabs/Themes/Egypt/Materials/";
   var sky=AssetDatabase.LoadAssetAtPath<Material>(folder+"Desert Sun Sky.mat");if(!sky){sky=new Material(Shader.Find("Speed Stars/Desert Sun Sky"));AssetDatabase.CreateAsset(sky,folder+"Desert Sun Sky.mat");}
   var dust=AssetDatabase.LoadAssetAtPath<Material>(folder+"Desert Airborne Dust.mat");if(!dust){dust=new Material(Shader.Find("Speed Stars/Desert Dust"));AssetDatabase.CreateAsset(dust,folder+"Desert Airborne Dust.mat");}
   var atmosphere=root.GetComponent<EgyptDesertAtmosphere>();if(!atmosphere)atmosphere=root.AddComponent<EgyptDesertAtmosphere>();atmosphere.skyMaterial=sky;atmosphere.dustMaterial=dust;EditorUtility.SetDirty(atmosphere);EditorUtility.SetDirty(sky);EditorUtility.SetDirty(dust);AssetDatabase.SaveAssets();
  }
 }
}
#endif
