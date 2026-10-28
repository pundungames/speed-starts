using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace SpeedStars.Themes {
 [ExecuteAlways]
 public sealed class MainMenuThemeShowcase:MonoBehaviour {
  public GameObject[] themeDisplays;public Camera previewCamera;
  public Vector3[] cameraPositions,cameraTargets;
  [Tooltip("Optional: move the camera to stored theme views when selecting a theme. Leave off for free scene editing.")]
  public bool useThemeCameraPresets=false;
  [Range(0,4)]public int selectedTheme;
  public UnityEvent<int> onThemeSelected=new UnityEvent<int>();
  public bool showSelection=true;
  public Transform characterPlatform;
  void PrepareDisplay(){if(themeDisplays==null||selectedTheme>=themeDisplays.Length||!themeDisplays[selectedTheme])return;foreach(var r in themeDisplays[selectedTheme].GetComponentsInChildren<Renderer>(true))for(int i=0;i<r.sharedMaterials.Length;i++){var m=r.sharedMaterials[i];if(!m||!m.HasProperty("_PixelSnap"))continue;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);block.SetFloat("_PixelSnap",0);r.SetPropertyBlock(block,i);}}
  readonly string[] titles={"Standart Şehir","Mısır","Roma / Bizans","Deniz / Korsan","Gökyüzü Adaları"};
  readonly string[] descriptions={"Şehir ışıkları, köprüler ve sokak hayatı","Kumullar, piramitler ve çöl pazarları","Antik meydanlar, taş sokaklar ve uzak tepeler","Limanlar, yelkenliler ve volkanik adalar","Bulutların üzerinde kaleler ve gökkuşakları"};
  int applied=-1;GUIStyle heading,subtitle,button;Texture2D panel;
  void OnEnable(){Apply();}
  void Update(){if(applied!=selectedTheme)Apply();if(!Application.isPlaying)return;
#if ENABLE_INPUT_SYSTEM
   var k=Keyboard.current;if(k!=null){if(k.rightArrowKey.wasPressedThisFrame||k.gKey.wasPressedThisFrame)SelectTheme((selectedTheme+1)%5);if(k.leftArrowKey.wasPressedThisFrame)SelectTheme((selectedTheme+4)%5);}
#endif
  }
  public void SelectTheme(int index){selectedTheme=Mathf.Clamp(index,0,4);Apply();if(Application.isPlaying)onThemeSelected.Invoke(selectedTheme);}
  public void Apply(){if(themeDisplays==null||themeDisplays.Length==0)return;selectedTheme=Mathf.Clamp(selectedTheme,0,themeDisplays.Length-1);for(int i=0;i<themeDisplays.Length;i++)if(themeDisplays[i])themeDisplays[i].SetActive(i==selectedTheme);if(useThemeCameraPresets&&previewCamera&&cameraPositions!=null&&cameraTargets!=null&&selectedTheme<cameraPositions.Length&&selectedTheme<cameraTargets.Length){previewCamera.transform.position=cameraPositions[selectedTheme];previewCamera.transform.LookAt(cameraTargets[selectedTheme]);}applied=selectedTheme;PrepareDisplay();}
  void Styles(){if(heading!=null)return;heading=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold};heading.normal.textColor=Color.white;subtitle=new GUIStyle(GUI.skin.label){fontSize=15};subtitle.normal.textColor=new Color(.86f,.90f,.95f);button=new GUIStyle(GUI.skin.button){fontSize=15,fontStyle=FontStyle.Bold};panel=new Texture2D(1,1);panel.SetPixel(0,0,new Color(.035f,.065f,.105f,.88f));panel.Apply();}
  void OnGUI(){if(!showSelection)return;Styles();float scale=Mathf.Clamp(Screen.width/1280f,.6f,1.8f);var matrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;GUI.DrawTexture(new Rect(24,24,520,116),panel);GUI.Label(new Rect(44,34,480,24),"ANAMENÜ TEMALARI",subtitle);GUI.Label(new Rect(42,60,490,44),titles[selectedTheme],heading);GUI.Label(new Rect(44,106,480,24),descriptions[selectedTheme],subtitle);float width=Mathf.Min(1130,w-48),x=(w-width)*.5f;GUI.DrawTexture(new Rect(x-12,h-90,width+24,74),panel);for(int i=0;i<5;i++){var old=GUI.backgroundColor;GUI.backgroundColor=i==selectedTheme?new Color(.40f,.70f,.87f):Color.white;if(GUI.Button(new Rect(x+i*width/5,h-76,width/5-8,46),titles[i],button))SelectTheme(i);GUI.backgroundColor=old;}GUI.matrix=matrix;}
  void OnDestroy(){if(panel){if(Application.isPlaying)Destroy(panel);else DestroyImmediate(panel);}}
 }
}
