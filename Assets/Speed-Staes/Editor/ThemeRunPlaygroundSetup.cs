#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SpeedStars.Runner;
using SpeedStars.Physical;
using SpeedStars.Themes;
public static class ThemeRunPlaygroundSetup
{
    public const string ScenePath="Assets/Speed-Staes/Scenes/Test/ThemeRunPlayground.unity";
    [MenuItem("Tools/Speed Stars/Open Theme Run Playground")]
    public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
    public static string Create()
    {
        if(Application.isPlaying)return "Stop Play first";
        var source=SceneManager.GetSceneByPath("Assets/Speed-Staes/Scenes/Test/PhysicalTest.unity");
        if(!source.isLoaded)source=EditorSceneManager.OpenScene("Assets/Speed-Staes/Scenes/Test/PhysicalTest.unity",OpenSceneMode.Additive);
        RunnerBody original=null;Light originalLight=null;
        foreach(var root in source.GetRootGameObjects()){if(root.GetComponent<RunnerBody>())original=root.GetComponent<RunnerBody>();if(root.GetComponent<Light>())originalLight=root.GetComponent<Light>();}
        if(!original)return "Physical runner missing";
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var runner=Object.Instantiate(original.gameObject);runner.name="Theme Runner";runner.transform.position=new Vector3(-5.63f,0,-.43f);runner.transform.rotation=Quaternion.identity;
        foreach(var name in new[]{"Glove","Helmet","Leggings","Shoes","Short","Tshirt"}){var t=runner.transform.Find(name);if(t)t.gameObject.SetActive(false);}
        foreach(var replay in runner.GetComponentsInChildren<RunnerReplay>(true))replay.enabled=false;
        foreach(var hud in runner.GetComponentsInChildren<RunnerBodyHUD>(true))hud.enabled=false;
        var body=runner.GetComponent<RunnerBody>();body.enabled=true;body.acceptInput=true;
        var input=runner.GetComponent<RunnerInput>();input.enabled=true;body.input=input;
        var pose=runner.GetComponent<RunnerPoseDriver>();pose.enabled=true;pose.preview=false;pose.body=body;
        foreach(var skin in runner.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
        var camObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(SideFollowCamera));camObject.tag="MainCamera";
        var camera=camObject.GetComponent<Camera>();camera.fieldOfView=44;camera.nearClipPlane=.1f;camera.farClipPlane=160;camera.clearFlags=CameraClearFlags.Skybox;
        var follow=camObject.GetComponent<SideFollowCamera>();follow.target=runner.transform;follow.distance=7;follow.height=1.75f;follow.leadAhead=.8f;follow.lookHeight=.95f;follow.followSharpness=12;
        Light light;if(originalLight)light=Object.Instantiate(originalLight);else light=new GameObject("Directional Light").AddComponent<Light>();light.name="Directional Light";light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-35,0);light.intensity=1.1f;light.color=Color.white;RenderSettings.sun=light;
        RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>("Assets/Speed-Staes/Prefabs/Themes/ThemePreviewSky.mat");RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.65f,.77f,.9f);RenderSettings.ambientEquatorColor=new Color(.42f,.5f,.6f);RenderSettings.ambientGroundColor=new Color(.26f,.29f,.33f);RenderSettings.fog=false;
        var control=new GameObject("Theme Run Playground");var controller=control.AddComponent<ThemeRunPlayground>();controller.demoRunning=true;controller.body=body;controller.input=input;controller.followCamera=follow;
        var race=control.AddComponent<RaceController>();race.body=body;race.raceDistance=1000000;controller.race=race;
        var wardrobe=control.AddComponent<AccessoryPhotoStudio>();wardrobe.character=runner.transform;wardrobe.photoCamera=camera;wardrobe.handleInput=false;wardrobe.showControls=false;wardrobe.selectedSet=1;wardrobe.SelectSet(1);controller.wardrobe=wardrobe;
        controller.themes=new ThemedLoopWay[4];
        for(int i=0;i<4;i++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Speed-Staes/Prefabs/Themes/LoopWay"+(i+2)+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.position=Vector3.zero;var theme=go.GetComponent<ThemedLoopWay>();theme.runner=runner.transform;theme.streamAtRuntime=true;theme.loopCount=3;controller.themes[i]=theme;go.SetActive(i==0);}
        var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Runner Contact Shadow";Object.DestroyImmediate(shadow.GetComponent<Collider>());shadow.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Speed-Staes/Materials/RunnerToon/ContactShadow.mat");shadow.AddComponent<RunnerContactShadow>().target=runner.transform;
        var standardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Character Dummy/Models/LoopWay.prefab");
        var standard=(GameObject)PrefabUtility.InstantiatePrefab(standardPrefab,scene);controller.standardTheme=standard.GetComponent<LoopWayGenerator>();controller.standardOrigin=standard.transform.position;controller.standardTheme.runner=runner.transform;controller.standardTheme.loopCount=3;controller.standardTheme.streamAtRuntime=true;controller.SelectTheme(0);
        controller.SnapCamera();EditorSceneManager.SaveScene(scene,ScenePath);
        for(int i=SceneManager.sceneCount-1;i>=0;i--){var other=SceneManager.GetSceneAt(i);if(other!=scene)EditorSceneManager.CloseScene(other,true);}
        Selection.activeGameObject=control;return ScenePath;
    }
}
#endif



