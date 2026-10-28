#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static class MainMenuThemeSceneBuilder {
  public const string ScenePath="Assets/Speed-Staes/Scenes/Anamenu Themaları.unity";
  public static string Build(){
   if(System.IO.File.Exists(ScenePath))return "Scene already exists; preserved: "+ScenePath;
   EditorSceneManager.SaveOpenScenes();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
   var root=new GameObject("Anamenu Themaları");SceneManager.MoveGameObjectToScene(root,scene);var showcase=root.AddComponent<MainMenuThemeShowcase>();
   var cameraObj=new GameObject("Main Menu Camera");cameraObj.transform.SetParent(root.transform,false);var camera=cameraObj.AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=40;camera.nearClipPlane=.1f;camera.farClipPlane=250;camera.clearFlags=CameraClearFlags.Skybox;camera.backgroundColor=new Color(.21f,.60f,.92f);cameraObj.AddComponent<AudioListener>();showcase.previewCamera=camera;
   var skybox=cameraObj.AddComponent<Skybox>();skybox.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Speed-Staes/Prefabs/Themes/ThemePreviewSky.mat");
   var lightObj=new GameObject("Menu Sun");lightObj.transform.SetParent(root.transform,false);var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.color=new Color(1,.97f,.91f);lightObj.transform.rotation=Quaternion.Euler(45,-35,0);
   var paths=new[]{"Assets/Kevin Iglesias/Human Character Dummy/Models/LoopWay.prefab","Assets/Speed-Staes/Prefabs/Themes/LoopWay2.prefab","Assets/Speed-Staes/Prefabs/Themes/LoopWay3.prefab","Assets/Speed-Staes/Prefabs/Themes/LoopWay4.prefab","Assets/Speed-Staes/Prefabs/Themes/LoopWay5.prefab"};
   showcase.themeDisplays=new GameObject[5];for(int i=0;i<paths.Length;i++){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);if(!asset)throw new System.Exception("Missing existing theme prefab: "+paths[i]);var display=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);display.transform.SetParent(root.transform,false);display.name=new[]{"01 - Standart Şehir","02 - Mısır","03 - Roma Bizans","04 - Deniz Korsan","05 - Gökyüzü Adaları"}[i];var basic=display.GetComponent<SpeedStars.Runner.LoopWayGenerator>();if(basic){basic.streamAtRuntime=false;basic.autoPreview=false;basic.enabled=false;}var g=display.GetComponent<ThemedLoopWay>();if(g){g.streamAtRuntime=false;g.autoPreview=false;g.enabled=false;}showcase.themeDisplays[i]=display;}
   showcase.cameraPositions=new[]{new Vector3(36,16,40),new Vector3(38,17,52),new Vector3(38,18,75),new Vector3(44,19,42),new Vector3(32,12,52)};
   showcase.cameraTargets=new[]{new Vector3(-32,3,30),new Vector3(-40,2,67),new Vector3(-44,3,75),new Vector3(-48,2,60),new Vector3(-44,2,65)};
   showcase.SelectTheme(0);EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(SceneManager.GetSceneByPath("Assets/Speed-Staes/Scenes/Test/ThemeRunPlayground.unity"),true);AssetDatabase.SaveAssets();Selection.activeGameObject=root;return "Created "+ScenePath+" using five existing prefab copies";
  }
 }
}
#endif
