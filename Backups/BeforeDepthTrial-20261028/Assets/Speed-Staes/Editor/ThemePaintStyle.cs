#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
namespace SpeedStars.Editor {
 public static class ThemePaintStyle {
  public static string Apply(){int count=0;foreach(string theme in new[]{"Egypt","RomeByzantium","PirateSea","FloatingSky"})foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Speed-Staes/Prefabs/Themes/"+theme+"/Materials"})){
   var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));if(!m||m.shader.name!="Speed Stars/Desert Pixel Paint")continue;
   string n=m.name.ToLowerInvariant();bool distant=n.Contains("distant")||n.Contains("horizon")||n.Contains("hazy");bool plant=n.Contains("green")||n.Contains("leaf")||n.Contains("olive")||n.Contains("pine")||n.Contains("scrub")||n.Contains("grass")||n.Contains("meadow");bool wood=n.Contains("wood")||n.Contains("plank")||n.Contains("timber")||n.Contains("soil");bool metal=n.Contains("bronze")||n.Contains("copper")||n.Contains("gold")||n.Contains("iron");bool stone=n.Contains("stone")||n.Contains("marble")||n.Contains("lime")||n.Contains("cliff")||n.Contains("basalt")||n.Contains("brick")||n.Contains("roof")||n.Contains("tile");
   int profile=distant?5:plant?3:wood?2:metal?4:stone?1:0;m.SetFloat("_PaintProfile",profile);m.SetFloat("_PixelSize",plant?.24f:wood?.16f:.22f);m.SetFloat("_Grain",distant?.003f:plant?0:wood?.025f:metal?.003f:stone?.012f:.006f);
   m.SetFloat("_ClusterStrength",distant?.035f:plant?.65f:wood?.18f:metal?.035f:stone?.12f:.07f);m.SetFloat("_PixelSnap",plant?1f:.45f);m.SetFloat("_PixelSize",plant?.3f:.18f);m.SetFloat("_Grain",wood?.012f:stone?.004f:0f);Color shade=distant?new Color(.84f,.86f,.88f):plant?new Color(.43f,.62f,.35f):wood?new Color(.63f,.57f,.50f):metal?new Color(.55f,.62f,.62f):new Color(.70f,.74f,.76f);Color light=plant?new Color(1.22f,1.19f,.85f):wood?new Color(1.10f,1.07f,.98f):new Color(1.06f,1.05f,1.01f);m.SetColor("_PaintShadow",shade);m.SetColor("_PaintLight",light);EditorUtility.SetDirty(m);count++;
  }AssetDatabase.SaveAssets();return count+" themed paint materials updated; geometry and layout unchanged";}
 }
}
#endif
