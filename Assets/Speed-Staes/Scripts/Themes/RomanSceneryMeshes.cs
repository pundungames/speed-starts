using System.Collections.Generic;
using UnityEngine;
namespace SpeedStars.Themes {
 public sealed class RomanSceneryMeshes:MonoBehaviour {
  readonly List<Mesh> owned=new List<Mesh>();
  public void Own(Mesh mesh){owned.Add(mesh);}
  void OnDestroy(){foreach(var mesh in owned)if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
 }
 public sealed partial class ThemedLoopWay {
  void NaturalizeRomanCountryside(GameObject scenery,int variationSeed){
   var random=new System.Random(variationSeed);var owner=Application.isPlaying?scenery.AddComponent<RomanSceneryMeshes>():null;
   foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>()){
    if(filter.name=="Soft Meadow Ground"){filter.gameObject.SetActive(false);continue;}
    if(!(filter.name.Contains("Ridge")||filter.name.Contains("Peaks")||filter.name.Contains("Low Hills")))continue;
    float stretch=R(random,.85f,1.22f),height=R(random,.8f,1.2f),phase=R(random,-10,10);
    if(!Application.isPlaying){
     filter.transform.localScale=Vector3.Scale(filter.transform.localScale,new Vector3(stretch,height,1));
     filter.transform.localPosition-=Vector3.up*.12f;
     filter.transform.localRotation*=Quaternion.Euler(0,R(random,-8,8),0);continue;
    }
    var mesh=Instantiate(filter.sharedMesh);mesh.name="Unique countryside ridge";var vertices=mesh.vertices;
    for(int i=0;i<vertices.Length;i++){
     var v=vertices[i];float edge=1+Mathf.Sin(v.x*.27f+phase)*.2f;
     v.x*=stretch;v.z*=edge;v.y=v.y<0?-.42f:v.y*height;vertices[i]=v;
    }
    mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();filter.sharedMesh=mesh;owner.Own(mesh);
    filter.transform.localRotation*=Quaternion.Euler(0,R(random,-8,8),0);
   }
   foreach(var t in scenery.GetComponentsInChildren<Transform>()){
    if(t.name=="Grazing Sheep"||t.name=="Country Olive Tree"||t.name=="Farm Cottage"||t.name=="Rustic Barn"){
     t.localPosition+=new Vector3(R(random,-.8f,.8f),0,R(random,-.7f,.7f));t.localRotation*=Quaternion.Euler(0,R(random,-14,14),0);
    }
   }
  }
 }
}

