#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static class ThemeFoliageStyle {
  public static string Apply(){int count=0;foreach(var theme in new[]{"Egypt","RomeByzantium","PirateSea","FloatingSky"}){var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>("Assets/Speed-Staes/Prefabs/Themes/"+theme+"/Definition.asset");var roots=d.vegetation.Concat(d.landmarks).Concat(d.background).Where(x=>x);var mats=roots.SelectMany(x=>x.GetComponentsInChildren<Renderer>(true)).SelectMany(x=>x.sharedMaterials).Where(x=>x&&x.shader.name=="Speed Stars/Desert Pixel Paint").Distinct();foreach(var m in mats){string n=m.name.ToLowerInvariant();bool foliage=n.Contains("dry sage")||n.Contains("foliage")||n.Contains("leaf")||n.Contains("green")||n.Contains("pine")||n.Contains("lavender")||n.Contains("rosemary")||n.Contains("scrub")||n.Contains("grass")||n.Contains("olive")||n.Contains("meadow");if(!foliage||n.Contains("horizon")||n.Contains("hazy")||n.Contains("distant")||n.Contains("ground")||n.Contains("mountain"))continue;m.SetFloat("_PaintProfile",3);m.SetFloat("_PixelSize",.14f);m.SetFloat("_ClusterStrength",.7f);m.SetFloat("_PixelSnap",1);m.SetFloat("_Grain",0);m.SetFloat("_Glow",0);m.SetColor("_PaintShadow",new Color(.32f,.55f,.25f));m.SetColor("_PaintLight",new Color(1.35f,1.28f,.72f));EditorUtility.SetDirty(m);count++;}}AssetDatabase.SaveAssets();return count+" foliage materials receive clustered five-tone pixel paint across four themes";}
 }
}
#endif
