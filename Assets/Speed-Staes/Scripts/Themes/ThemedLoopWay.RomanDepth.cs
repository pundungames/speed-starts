using UnityEngine;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  // Renderer overrides keep all shared source materials and other themes intact.
  void ApplyRomanDepthPaint(GameObject obj,float amount){ThemeAtmosphere.Set(obj,new Color(.83f,.81f,.72f),.85f);foreach(var r in obj.GetComponentsInChildren<Renderer>()){var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++){var m=materials[i];if(!m||!m.HasProperty("_Color"))continue;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);var color=m.GetColor("_Color");float gray=color.r*.30f+color.g*.59f+color.b*.11f;var quiet=Color.Lerp(color,new Color(gray,gray,gray,color.a),amount*.35f);var haze=new Color(.83f,.81f,.70f,color.a);block.SetColor("_Color",Color.Lerp(quiet,haze,amount));if(m.HasProperty("_PaintShadow"))block.SetColor("_PaintShadow",Color.Lerp(m.GetColor("_PaintShadow"),Color.white,amount*.6f));if(m.HasProperty("_ClusterStrength"))block.SetFloat("_ClusterStrength",m.GetFloat("_ClusterStrength")*(1-amount*.6f));r.SetPropertyBlock(block,i);}}}
 }
}
