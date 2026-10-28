using UnityEngine;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  void GroundRomanHorizonObject(GameObject obj,Transform foundationParent,bool foundation){
   var bounds=BoundsIn(obj,transform);float low=float.PositiveInfinity,high=float.NegativeInfinity;
   for(int x=0;x<=4;x++)for(int z=0;z<=6;z++){float px=Mathf.Lerp(bounds.min.x,bounds.max.x,x/4f),pz=Mathf.Lerp(bounds.min.z,bounds.max.z,z/6f);float y=RomanLandscapeSurface.Evaluate(seed,px,pz);low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
   if(float.IsInfinity(low))return;float target=foundation?high-.035f:RomanLandscapeSurface.Evaluate(seed,bounds.center.x,bounds.center.z)-.025f;var p=obj.transform.position;p+=transform.up*(target-bounds.min.y);obj.transform.position=p;
   if(foundation&&high-low>.07f){float bottom=low-.12f,top=target+.02f;Block(foundationParent,"Terrain Embedded Foundation - "+obj.name,new Vector3(bounds.center.x,(bottom+top)*.5f,bounds.center.z),new Vector3(bounds.size.x+.04f,top-bottom,bounds.size.z+.04f),theme.pathMaterial);}
  }
  void GroundRomanRuralPieces(GameObject scenery,Transform parent){
   foreach(Transform t in scenery.transform){bool building=t.name.Contains("Farm")||t.name.Contains("Barn")||t.name.Contains("Chapel")||t.name.Contains("House");bool plant=t.name.Contains("Olive Tree");if(building||plant)GroundRomanHorizonObject(t.gameObject,parent,building);}
  }
 }
}
