using UnityEngine;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  void RaiseDesertStreetProp(GameObject prop,Transform parent,Bounds bounds){
   // Keep the actual prop above the rail's projected band, on visible load-bearing stone.
   bool access=prop.name.Contains("Well")||prop.name.Contains("Cart")||prop.name.Contains("Seat")||prop.name.Contains("Rack");
   float elevation=access?Mathf.Clamp(2.5f-bounds.max.y,.4f,.85f):Mathf.Clamp(2.65f-bounds.max.y,.55f,1.3f);
   var baseBounds=bounds;var position=prop.transform.localPosition;position.y+=elevation;prop.transform.localPosition=position;
   var platform=Group("Grounded Sandstone Platform - "+prop.name,parent);
   float ground=.05f,top=baseBounds.min.y+elevation;float height=top-ground;
   float depth=Mathf.Max(.85f,baseBounds.size.x+.25f),width=Mathf.Max(.85f,baseBounds.size.z+.25f);
   var center=new Vector3(baseBounds.center.x,0,baseBounds.center.z);
   Block(platform,"Buried Foundation Foot",center+Vector3.up*(ground+.09f),new Vector3(depth+.16f,.22f,width+.16f),theme.groundMaterial);
   Block(platform,"Solid Sandstone Support",center+Vector3.up*(ground+height*.5f),new Vector3(depth,height,width),theme.pathMaterial);
   Block(platform,"Stone Display Cap",center+Vector3.up*(top-.06f),new Vector3(depth+.12f,.12f,width+.12f),theme.pathMaterial);
   // Recessed courses are physical shallow blocks, not floating decoration or screen-space lines.
   int courses=Mathf.Max(1,Mathf.RoundToInt(height/.32f));for(int row=0;row<courses;row++){
    float y=ground+(row+.5f)*height/courses;
    Block(platform,"Front Masonry Course",new Vector3(center.x+depth*.5f+.012f,y,center.z),new Vector3(.025f,height/courses-.035f,width-.08f),theme.pathMaterial);
   }
   if(access){int steps=Mathf.Clamp(Mathf.CeilToInt(elevation/.24f),2,4);float stairWidth=Mathf.Min(1.1f,width*.65f);
    // Stairs run along the sidewalk instead of towards the running lane.
    for(int i=0;i<steps;i++){float h=(i+1)*elevation/steps;float z=center.z-width*.5f-(steps-i-.5f)*.28f;Block(platform,"Grounded Access Step",new Vector3(center.x,ground+h*.5f,z),new Vector3(Mathf.Min(depth,stairWidth),h,.30f),theme.pathMaterial);}
   }
  }
 }
}
