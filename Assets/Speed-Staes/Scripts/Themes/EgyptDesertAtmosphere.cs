using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
namespace SpeedStars.Themes {
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class EgyptDesertAtmosphere:MonoBehaviour {
  public Material skyMaterial,dustMaterial;
  Camera targetCamera;CameraClearFlags savedFlags;Color savedBackground;
  AmbientMode savedAmbientMode;Color savedAmbient,savedSky,savedEquator,savedGround;
  Light sunlight;Color savedLightColor;float savedIntensity;GameObject sky,dust;bool captured;
  void OnEnable(){Apply();}
  void Apply(){
   if(!Application.isPlaying||captured||!skyMaterial||gameObject.scene!=SceneManager.GetActiveScene())return;
   targetCamera=Camera.main;if(!targetCamera)return;
   savedFlags=targetCamera.clearFlags;savedBackground=targetCamera.backgroundColor;
   savedAmbientMode=RenderSettings.ambientMode;savedAmbient=RenderSettings.ambientLight;savedSky=RenderSettings.ambientSkyColor;savedEquator=RenderSettings.ambientEquatorColor;savedGround=RenderSettings.ambientGroundColor;
   foreach(var light in FindObjectsOfType<Light>())if(light.type==LightType.Directional&&light.gameObject.scene==gameObject.scene){sunlight=light;break;}
   if(sunlight){savedLightColor=sunlight.color;savedIntensity=sunlight.intensity;sunlight.color=new Color(1,.82f,.56f);sunlight.intensity=savedIntensity*.96f;}
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.87f,.70f,.46f);RenderSettings.ambientEquatorColor=new Color(.64f,.49f,.31f);RenderSettings.ambientGroundColor=new Color(.36f,.27f,.18f);
   targetCamera.clearFlags=CameraClearFlags.SolidColor;targetCamera.backgroundColor=new Color(.96f,.65f,.30f);captured=true;
   sky=GameObject.CreatePrimitive(PrimitiveType.Quad);sky.name="Desert Sun Backdrop";sky.hideFlags=HideFlags.HideAndDontSave;sky.transform.SetParent(transform,false);var collider=sky.GetComponent<Collider>();if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);var renderer=sky.GetComponent<MeshRenderer>();renderer.sharedMaterial=skyMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   if(Application.isPlaying&&dustMaterial){dust=new GameObject("Subtle Golden Desert Dust");dust.hideFlags=HideFlags.HideAndDontSave;dust.transform.SetParent(transform,false);var particles=dust.AddComponent<ParticleSystem>();var main=particles.main;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(8,13);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.45f);main.startSize=new ParticleSystem.MinMaxCurve(1.0f,3.4f);main.startColor=new Color(1,.72f,.34f,.075f);main.maxParticles=150;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=true;var emission=particles.emission;emission.rateOverTime=12;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(75,3.4f,125);var r=dust.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=dustMaterial;r.renderMode=ParticleSystemRenderMode.Billboard;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;particles.Play();}
   UpdatePositions();
  }
  void LateUpdate(){if(!captured)Apply();UpdatePositions();}
  void UpdatePositions(){if(!targetCamera)return;if(sky){float distance=targetCamera.farClipPlane*.94f,height=2*Mathf.Tan(targetCamera.fieldOfView*Mathf.Deg2Rad*.5f)*distance;sky.transform.SetPositionAndRotation(targetCamera.transform.position+targetCamera.transform.forward*distance,targetCamera.transform.rotation);sky.transform.localScale=new Vector3(height*targetCamera.aspect*1.02f,height*1.02f,1);}if(dust)dust.transform.position=new Vector3(-30,1.5f,targetCamera.transform.position.z+22);}
  void OnDisable(){Restore();}
  void OnDestroy(){Restore();}
  void Restore(){
   if(captured){if(targetCamera){targetCamera.clearFlags=savedFlags;targetCamera.backgroundColor=savedBackground;}if(sunlight){sunlight.color=savedLightColor;sunlight.intensity=savedIntensity;}RenderSettings.ambientMode=savedAmbientMode;RenderSettings.ambientLight=savedAmbient;RenderSettings.ambientSkyColor=savedSky;RenderSettings.ambientEquatorColor=savedEquator;RenderSettings.ambientGroundColor=savedGround;captured=false;}
   if(sky){if(Application.isPlaying)Destroy(sky);else DestroyImmediate(sky);sky=null;}if(dust){if(Application.isPlaying)Destroy(dust);else DestroyImmediate(dust);dust=null;}
  }
 }
}
