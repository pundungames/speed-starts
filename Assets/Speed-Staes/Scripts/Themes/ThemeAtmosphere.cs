using UnityEngine;
namespace SpeedStars.Themes {
 [ExecuteAlways]
 public sealed class ThemeAtmosphere:MonoBehaviour {
  /// <summary>
  /// The shown theme's pull on the haze every generator authored per object (see <see cref="ThemeWeather"/>). Zero fields
  /// mean "keep what the generator set", so an unset blend changes nothing and the authored depth order is preserved.
  /// </summary>
  public struct AirBlend {public Color tint;public float tintBlend,strengthBoost,startScale,endScale;}
  static AirBlend air;static object airOwner;
  /// <summary>Sets the air of the theme being shown. Call <see cref="Apply"/> on the world afterwards.</summary>
  public static void SetAir(object owner,AirBlend blend){air=blend;airOwner=owner;}
  /// <summary>Clears the air, but only for the owner that set it: themes switching in any order cannot wipe each other.</summary>
  public static void ClearAir(object owner){if(airOwner!=owner)return;air=default;airOwner=null;}
  public Color haze=new Color(.8f,.8f,.72f);public float strength=.7f,startDistance=28,endDistance=120;
  void OnEnable(){Apply();}
  public void Apply(){
   Color color=air.tintBlend>0?Color.Lerp(haze,air.tint,air.tintBlend):haze;
   float amount=Mathf.Clamp01(strength+air.strengthBoost);
   float start=startDistance*(air.startScale<=0?1:air.startScale),end=endDistance*(air.endScale<=0?1:air.endScale);
   foreach(var r in GetComponentsInChildren<Renderer>(true)){for(int i=0;i<r.sharedMaterials.Length;i++){var m=r.sharedMaterials[i];if(!m)continue;if(m.shader.name!="Speed Stars/Desert Pixel Paint"&&m.shader.name!="Speed Stars/Pastel Building")continue;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);block.SetVector("_AerialSettings",new Vector4(amount,start,end,0));block.SetColor("_AerialColor",color);r.SetPropertyBlock(block,i);}}}
  public static void Set(GameObject root,Color color,float amount){var a=root.GetComponent<ThemeAtmosphere>();if(!a)a=root.AddComponent<ThemeAtmosphere>();a.haze=color;a.strength=amount;a.Apply();}
 }
}
