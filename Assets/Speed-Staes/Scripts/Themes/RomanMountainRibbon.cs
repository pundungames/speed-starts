using UnityEngine;
namespace SpeedStars.Themes {
 [ExecuteAlways,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
 public sealed class RomanMountainRibbon:MonoBehaviour {
  public int seed,layer;public float start,end;public Material lightMaterial,shadeMaterial;
  Mesh owned;
  void OnEnable(){Rebuild();}
  public void Rebuild(){
   if(end<=start||!lightMaterial)return;
   if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
   const int segments=20;var vertices=new Vector3[(segments+1)*3];var front=new int[segments*6];var back=new int[segments*6];
   float phase=(seed&65535)*.0071f+layer*41.3f;
   for(int i=0;i<=segments;i++){
    float z=Mathf.Lerp(start,end,(float)i/segments);
    float broad=Mathf.PerlinNoise(phase,z*.009f+17),detail=Mathf.PerlinNoise(phase+37,z*.026f+61);
    float peaks=1-Mathf.Abs(Mathf.Repeat(z/(layer==0?39f:57f)+phase*.1f,2)-1);
    float h=(layer==0?3f:5f)+broad*3+peaks*(layer==0?9f:12f)+detail;
    float x=layer==0?-107:-134;
    vertices[i*3]=new Vector3(x+14,-4f,z);vertices[i*3+1]=new Vector3(x-2+broad*4,h,z);vertices[i*3+2]=new Vector3(x-17,-4f,z);
    if(i<segments){int k=i*3,j=i*6;front[j]=k;front[j+1]=k+1;front[j+2]=k+3;front[j+3]=k+1;front[j+4]=k+4;front[j+5]=k+3;back[j]=k+1;back[j+1]=k+2;back[j+2]=k+4;back[j+3]=k+2;back[j+4]=k+5;back[j+5]=k+4;}
   }
   owned=new Mesh{name="Continuous Roman Mountain Profile",hideFlags=HideFlags.DontSave};owned.vertices=vertices;owned.subMeshCount=2;owned.SetTriangles(front,0);owned.SetTriangles(back,1);owned.RecalculateNormals();owned.RecalculateBounds();
   GetComponent<MeshFilter>().sharedMesh=owned;var renderer=GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{lightMaterial,shadeMaterial?shadeMaterial:lightMaterial};renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
  void OnDestroy(){if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}}
 }
 public sealed partial class ThemedLoopWay {
  static bool RomanLivestock(string name){return name.Contains("Shepherd")||name.Contains("Mountain Pasture")||name.Contains("Farmstead");}
  void BuildRomanMountains(Transform section,float a,float b){
   if(theme.mountainMaterials==null||theme.mountainMaterials.Length<4)return;
   var mountains=Group("Continuous Distant Mountain Range",section);
   for(int i=1;i>=0;i--){var go=new GameObject(i==0?"Near Olive Mountains":"Far Hazy Mountains");go.transform.SetParent(mountains,false);var ridge=go.AddComponent<RomanMountainRibbon>();ridge.seed=seed;ridge.layer=i;ridge.start=a;ridge.end=b;ridge.lightMaterial=theme.mountainMaterials[i*2];ridge.shadeMaterial=theme.mountainMaterials[i*2+1];ridge.Rebuild();}
  }
 }
}
