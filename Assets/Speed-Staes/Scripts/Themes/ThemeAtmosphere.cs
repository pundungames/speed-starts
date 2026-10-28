using UnityEngine;
namespace SpeedStars.Themes {
 [ExecuteAlways]
 public sealed class ThemeAtmosphere:MonoBehaviour {
  public Color haze=new Color(.8f,.8f,.72f);public float strength=.7f,startDistance=28,endDistance=120;
  void OnEnable(){Apply();}
  public void Apply(){foreach(var r in GetComponentsInChildren<Renderer>(true)){for(int i=0;i<r.sharedMaterials.Length;i++){var m=r.sharedMaterials[i];if(!m)continue;if(m.shader.name!="Speed Stars/Desert Pixel Paint"&&m.shader.name!="Speed Stars/Pastel Building")continue;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);block.SetVector("_AerialSettings",new Vector4(strength,startDistance,endDistance,0));block.SetColor("_AerialColor",haze);r.SetPropertyBlock(block,i);}}}
  public static void Set(GameObject root,Color color,float amount){var a=root.GetComponent<ThemeAtmosphere>();if(!a)a=root.AddComponent<ThemeAtmosphere>();a.haze=color;a.strength=amount;a.Apply();}
 }
}
