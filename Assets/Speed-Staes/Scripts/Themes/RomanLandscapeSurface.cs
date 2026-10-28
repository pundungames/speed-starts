using UnityEngine;
namespace SpeedStars.Themes {
 [ExecuteAlways,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
 public sealed class RomanLandscapeSurface:MonoBehaviour {
  public float start,end;public int seed;public Material material;Mesh owned;
  public static float Evaluate(int seed,float x,float z){float edge=Mathf.Sin(Mathf.InverseLerp(-43,-132,x)*Mathf.PI);float h=-.18f+edge*(.55f+1.3f*Mathf.PerlinNoise(seed*.001f+x*.017f,z*.018f+31));return Mathf.Lerp(-.30f,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.3f,8.5f,Mathf.Abs(x-RomanRiverSurface.Center(seed,z)))));}
  float Height(float x,float z){return Evaluate(seed,x,z);}
  void OnEnable(){Rebuild();}
  public void Rebuild(){
   if(!material||end<=start)return;if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
   const int rows=90,steps=48;var v=new Vector3[(steps+1)*rows];var normals=new Vector3[v.Length];var triangles=new int[steps*(rows-1)*6];int n=0;
   for(int i=0;i<=steps;i++)for(int r=0;r<rows;r++){float x=Mathf.Lerp(-43,-132,(float)r/(rows-1)),z=Mathf.Lerp(start,end,(float)i/steps);int k=i*rows+r;v[k]=new Vector3(x,Height(x,z),z);float dx=(Height(x+.1f,z)-Height(x-.1f,z))/.2f,dz=(Height(x,z+.1f)-Height(x,z-.1f))/.2f;normals[k]=new Vector3(-dx,1,-dz).normalized;
    if(i<steps&&r<rows-1){triangles[n++]=k;triangles[n++]=k+1;triangles[n++]=k+rows;triangles[n++]=k+1;triangles[n++]=k+rows+1;triangles[n++]=k+rows;}}
   owned=new Mesh{name="Continuous Mediterranean Rolling Terrain",hideFlags=HideFlags.DontSave};owned.vertices=v;owned.normals=normals;owned.triangles=triangles;owned.RecalculateBounds();GetComponent<MeshFilter>().sharedMesh=owned;var renderer=GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
  void OnDestroy(){if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}}
 }
 public sealed partial class ThemedLoopWay {
  void BuildRomanGroundDetail(Transform root,float a,float b,int sectionSeed){
   var ground=Group("Roman Terrain and Travertine Detail",root);
   var landscape=new GameObject("Continuous Olive Hills");landscape.transform.SetParent(ground,false);var surface=landscape.AddComponent<RomanLandscapeSurface>();surface.start=a;surface.end=b;surface.seed=seed;surface.material=theme.groundMaterial;surface.Rebuild();
   // Global coordinates keep paving joints continuous across streamed section boundaries.
   var joint=theme.mountainMaterials!=null&&theme.mountainMaterials.Length>1?theme.mountainMaterials[1]:theme.pathMaterial;
   for(int i=Mathf.CeilToInt(a/2.4f);i*2.4f<b;i++)Block(ground,"Paving Cross Joint",new Vector3(-21,.057f,i*2.4f),new Vector3(18,.012f,.035f),joint);
   foreach(float x in new[]{-15.2f,-19.6f,-24f})Block(ground,"Paving Long Joint",new Vector3(x,.057f,(a+b)*.5f),new Vector3(.035f,.012f,b-a),joint);
   foreach(float x in new[]{-12.1f,-27.2f})Block(ground,"Travertine Edge Band",new Vector3(x,.065f,(a+b)*.5f),new Vector3(.28f,.05f,b-a),theme.pathMaterial);
  }
 }
}
