using UnityEngine;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  void RaiseRomanStreetProp(GameObject item,Transform parent,Bounds footprint){
   bool access=item.name.Contains("Well")||item.name.Contains("Cart")||item.name.Contains("Bench")||item.name.Contains("Stand")||item.name.Contains("Basin");
   float height=access?Mathf.Clamp(2.45f-footprint.max.y,.35f,.8f):Mathf.Clamp(2.6f-footprint.max.y,.45f,1.20f);
   var position=item.transform.localPosition;position.y+=height;item.transform.localPosition=position;
   var root=Group("Grounded Travertine Platform - "+item.name,parent);float baseY=.05f,top=footprint.min.y+height;
   float depth=Mathf.Max(.8f,footprint.size.x+.20f),width=Mathf.Max(.85f,footprint.size.z+.20f);Vector3 center=footprint.center;
   Block(root,"Travertine Foundation",new Vector3(center.x,.15f,center.z),new Vector3(depth+.16f,.24f,width+.16f),theme.pathMaterial);
   Block(root,"Load Bearing Masonry",new Vector3(center.x,(top+baseY)*.5f,center.z),new Vector3(depth,top-baseY,width),theme.pathMaterial);
   Block(root,"Moulded Pedestal Cap",new Vector3(center.x,top-.07f,center.z),new Vector3(depth+.12f,.14f,width+.12f),theme.pathMaterial);
   if(access){int steps=Mathf.Clamp(Mathf.CeilToInt(height/.24f),2,4);for(int i=0;i<steps;i++){float h=(i+1)*height/steps;Block(root,"Grounded Platform Step",new Vector3(center.x,baseY+h*.5f,center.z-width*.5f-(steps-i-.5f)*.26f),new Vector3(Mathf.Min(1,depth),h,.28f),theme.pathMaterial);}}
  }
 }
}
