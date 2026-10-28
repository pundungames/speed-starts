#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static void RStoneRing(Transform t,string name,float y,float radius,float thickness,float height,Material material){
   const int count=40;var vertices=new List<Vector3>();var triangles=new List<int>();
   for(int i=0;i<=count;i++){float a=i*Mathf.PI*2/count;foreach(float h in new[]{y,y+height})foreach(float r in new[]{radius-thickness,radius})vertices.Add(new Vector3(Mathf.Sin(a)*r,h,Mathf.Cos(a)*r));}
   for(int i=0;i<count;i++){int k=i*4,n=k+4;triangles.AddRange(new[]{k+1,n+1,k+3,k+3,n+1,n+3,k,k+2,n,k+2,n+2,n,k+2,k+3,n+2,n+2,k+3,n+3});}
   MeshObject(t,name,vertices,triangles,material);
  }
  static void UpgradeRomanArena(){
   RomanArtModel("Oval Roman Amphitheatre",t=>{
    var oval=new GameObject("Hollow Elliptical Arena");oval.transform.SetParent(t,false);oval.transform.localScale=new Vector3(1,1,.68f);var ring=oval.transform;
    Cylinder(ring,"Sandy Arena Floor",Vector3.zero,6,.05f,rOchre,40);
    for(int level=0;level<2;level++)for(int i=0;i<16;i++){
     float a=i*22.5f*Mathf.Deg2Rad;var bay=new GameObject("Carved Arena Arch Bay");bay.transform.SetParent(ring,false);bay.transform.localPosition=new Vector3(Mathf.Sin(a)*7,level*2.8f,Mathf.Cos(a)*7);bay.transform.localRotation=Quaternion.Euler(0,i*22.5f,0);var b=bay.transform;
     foreach(float x in new[]{-1.24f,1.24f}){Box(b,"Arena Ashlar Pier",new Vector3(x,.95f,0),new Vector3(.4f,1.9f,.75f),rLime);RColumn(b,new Vector3(x,0,.48f),2.5f,.12f);}
     Arc(b,"Deep Arena Vault",new Vector3(0,1.35f,0),1.08f,.28f,.76f,rIvory,12);
     Box(b,"Arena Spandrel",new Vector3(0,2.61f,0),new Vector3(2.75f,.3f,.75f),rLime);
     Box(b,"Arena Projecting Cornice",new Vector3(0,2.82f,.06f),new Vector3(2.9f,.18f,.95f),rIvory);
    }
    RStoneRing(ring,"Continuous Attic Masonry",5.65f,7.25f,.45f,.65f,rLime);
    RStoneRing(ring,"Upper Attic Cornice",6.3f,7.45f,.75f,.18f,rIvory);
    RStoneRing(ring,"Inner Seating First Tier",.1f,6.3f,.6f,.45f,rLime);
    RStoneRing(ring,"Inner Seating Second Tier",.55f,6.7f,.45f,.45f,rIvory);
    for(int i=0;i<16;i++){float a=i*Mathf.PI/8;Box(ring,"Attic Bronze Shield",new Vector3(Mathf.Sin(a)*7.27f,5.95f,Mathf.Cos(a)*7.27f),new Vector3(.35f,.3f,.14f),rBronze).transform.localRotation=Quaternion.Euler(0,i*22.5f,0);}
   });
  }
 }
}
#endif
