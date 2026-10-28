using UnityEngine;
namespace SpeedStars.Themes {
 [ExecuteAlways,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
 public sealed class RomanRiverSurface:MonoBehaviour {
  public float start,end;public int seed;public Material water,bank;Mesh owned;
  public static float Center(int seed,float z){return -52+Mathf.Sin(z*.017f+seed*.13f)*1.25f+Mathf.Sin(z*.041f)*.4f;}
  void OnEnable(){Rebuild();}
  public void Rebuild(){
   if(!water||end<=start)return;if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
   const int steps=48,cols=6;var vertices=new Vector3[(steps+1)*cols];var uv=new Vector2[vertices.Length];var strips=new System.Collections.Generic.List<int>[] {new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
   float[] offsets={-5.1f,-4.2f,-3.8f,3.8f,4.2f,5.1f};float[] heights={.07f,.10f,.035f,.035f,.10f,.07f};
   for(int i=0;i<=steps;i++){float z=Mathf.Lerp(start,end,(float)i/steps),c=Center(seed,z);for(int j=0;j<cols;j++){int k=i*cols+j;vertices[k]=new Vector3(c+offsets[j],heights[j],z);uv[k]=new Vector2(offsets[j],z);if(i<steps&&j<cols-1){var tr=strips[j==2?0:1];tr.Add(k);tr.Add(k+cols);tr.Add(k+1);tr.Add(k+1);tr.Add(k+cols);tr.Add(k+cols+1);}}}
   owned=new Mesh{name="Continuous Meandering Roman River",hideFlags=HideFlags.DontSave};owned.vertices=vertices;owned.uv=uv;owned.subMeshCount=2;owned.SetTriangles(strips[0],0);owned.SetTriangles(strips[1],1);owned.RecalculateNormals();owned.RecalculateBounds();GetComponent<MeshFilter>().sharedMesh=owned;var r=GetComponent<MeshRenderer>();r.sharedMaterials=new[]{water,bank};r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  void OnDestroy(){if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}}
 }
 public sealed partial class ThemedLoopWay {
  void BuildRomanRiver(Transform root,float a,float b){var go=Group("Continuous Jade River and Limestone Banks",root);var river=go.gameObject.AddComponent<RomanRiverSurface>();river.start=a;river.end=b;river.seed=seed;river.water=theme.waterMaterial;river.bank=theme.pathMaterial;river.Rebuild();}
 }
}
