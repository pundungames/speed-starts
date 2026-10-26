using UnityEngine;
using SpeedStars.Runner;
using SpeedStars.Physical;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace SpeedStars.Themes
{
    [DefaultExecutionOrder(-250)]
    public sealed class ThemeRunPlayground:MonoBehaviour
    {
        public RunnerBody body;
        public RunnerInput input;
        public RaceController race;
        public AccessoryPhotoStudio wardrobe;
        public SideFollowCamera followCamera;
        public ThemedLoopWay[] themes;
        public LoopWayGenerator standardTheme;
        public Vector3 standardOrigin=new Vector3(-5.6f,0,-7.2f);
        [Range(0,4)]public int activeTheme;
        int ThemeCount {get{return (themes==null?0:themes.Length)+(standardTheme?1:0);}}
        public bool showHelp=true;
        public bool demoRunning=true;
        string[] names={"STANDART SEHIR","MISIR","ROMA / BIZANS","DENIZ / KORSAN","GOKYUZU ADALARI"};
        int demoLeg,heldLeg=-1;double nextTap,releaseAt;
        GUIStyle title,label,small;Texture2D pixel;
        void Start(){Application.runInBackground=true;if(wardrobe){wardrobe.handleInput=false;wardrobe.showControls=false;}SelectTheme(activeTheme);if(demoRunning){input.enabled=false;input.Clear();demoLeg=0;nextTap=Time.timeAsDouble+.2;}}
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k!=null){if(k.gKey.wasPressedThisFrame)NextTheme();if(k.cKey.wasPressedThisFrame&&wardrobe)wardrobe.NextSet();if(k.rKey.wasPressedThisFrame)ResetRun();if(k.tKey.wasPressedThisFrame)ToggleDemo();if(k.hKey.wasPressedThisFrame)showHelp=!showHelp;if(k.digit0Key.wasPressedThisFrame&&wardrobe)wardrobe.SelectSet(0);for(int i=0;i<9;i++)if(k[(Key)((int)Key.Digit1+i)].wasPressedThisFrame&&wardrobe)wardrobe.SelectSet(i+1);}
#else
            if(Input.GetKeyDown(KeyCode.G))NextTheme();if(Input.GetKeyDown(KeyCode.C)&&wardrobe)wardrobe.NextSet();if(Input.GetKeyDown(KeyCode.R))ResetRun();if(Input.GetKeyDown(KeyCode.T))ToggleDemo();if(Input.GetKeyDown(KeyCode.H))showHelp=!showHelp;for(int i=0;i<10;i++)if(Input.GetKeyDown(KeyCode.Alpha0+i)&&wardrobe)wardrobe.SelectSet(i);
#endif
            if(!demoRunning||!input)return;
            // Keep scheduled event timestamps when recording slows a frame down.
            // RunnerBody consumes these events before advancing its simulation to now.
            double now=Time.timeAsDouble;
            for(int n=0;n<512;n++){
                if(heldLeg>=0){if(releaseAt>now)break;input.Inject(heldLeg,false,releaseAt);heldLeg=-1;}
                else {if(nextTap>now)break;heldLeg=demoLeg;demoLeg=1-demoLeg;input.Inject(heldLeg,true,nextTap);releaseAt=nextTap+.10;nextTap+=.24;}
            }
        }
        public void NextTheme(){if(ThemeCount>0)SelectTheme((activeTheme+1)%ThemeCount);}
        public void SelectTheme(int index)
        {
            int count=ThemeCount;if(count==0)return;activeTheme=(index%count+count)%count;
            int themedIndex=activeTheme-(standardTheme?1:0);
            if(standardTheme)standardTheme.gameObject.SetActive(activeTheme==0);
            if(themes!=null)for(int i=0;i<themes.Length;i++)if(themes[i])themes[i].gameObject.SetActive(i==themedIndex);
            float z=body?body.transform.position.z:0;
            if(standardTheme&&activeTheme==0){
                standardTheme.runner=body?body.transform:null;
                float offset=Mathf.Floor((z-standardOrigin.z-standardTheme.startZ)/standardTheme.segmentLength)*standardTheme.segmentLength;
                standardTheme.transform.position=standardOrigin+Vector3.forward*offset;
                standardTheme.Generate();return;
            }
            var theme=themes[themedIndex];if(!theme)return;theme.runner=body?body.transform:null;
            float themedOffset=Mathf.Floor((z-ThemedLoopWay.StartZ)/ThemedLoopWay.SectionLength)*ThemedLoopWay.SectionLength;
            theme.transform.position=new Vector3(0,0,themedOffset);theme.Generate();
        }
        public void ToggleDemo(){
            demoRunning=!demoRunning;
            if(input){input.enabled=!demoRunning;input.Clear();for(int leg=0;leg<2;leg++)input.Inject(leg,false,Time.timeAsDouble);}
            heldLeg=-1;demoLeg=0;nextTap=Time.timeAsDouble+.2;
        }
        public void ResetRun(){if(heldLeg>=0&&input)input.Inject(heldLeg,false,Time.timeAsDouble);heldLeg=-1;demoLeg=0;nextTap=Time.timeAsDouble+.2;if(race)race.ResetRace();else if(body)body.ResetRun();SelectTheme(activeTheme);SnapCamera();}
        public void SnapCamera(){if(!followCamera||!followCamera.target)return;var t=followCamera.target;followCamera.transform.position=t.position+t.forward*followCamera.leadAhead+t.right*followCamera.distance+Vector3.up*followCamera.height;followCamera.transform.rotation=Quaternion.LookRotation(-t.right,Vector3.up)*Quaternion.Euler(Mathf.Atan2(followCamera.height-followCamera.lookHeight,followCamera.distance)*Mathf.Rad2Deg,0,0);}
        void Styles(){if(title!=null)return;title=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold};title.normal.textColor=Color.white;label=new GUIStyle(GUI.skin.label){fontSize=14};label.normal.textColor=Color.white;small=new GUIStyle(label){fontSize=12};pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();}
        void Panel(Rect rect){var c=GUI.color;GUI.color=new Color(.06f,.10f,.16f,.90f);GUI.DrawTexture(rect,pixel);GUI.color=c;}
        void OnGUI()
        {
            Styles();float scale=Mathf.Clamp(Screen.height/810f,.75f,1.6f);var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float width=Screen.width/scale,height=Screen.height/scale;
            Panel(new Rect(16,16,305,112));GUI.Label(new Rect(30,24,280,27),names[Mathf.Clamp(activeTheme+(standardTheme?0:1),0,names.Length-1)],title);GUI.Label(new Rect(30,53,280,23),"G: tema degistir   |   "+(activeTheme+1)+" / "+ThemeCount,label);if(GUI.Button(new Rect(30,85,132,28),"Sonraki tema (G)"))NextTheme();if(GUI.Button(new Rect(171,85,132,28),"Yeniden baslat (R)"))ResetRun();
            Panel(new Rect(width-240,16,224,102));float speed=body!=null&&body.Sim!=null?body.Sim.Speed:0,distance=body!=null&&body.Sim!=null?body.Sim.Distance:0,energy=body!=null&&body.Sim!=null?body.Sim.Energy:100;
            GUI.Label(new Rect(width-226,25,210,25),string.Format("{0:0.0} km/sa",speed*3.6f),title);GUI.Label(new Rect(width-226,55,210,22),string.Format("Mesafe: {0:0.0} m",distance),label);GUI.Label(new Rect(width-226,79,210,22),string.Format("Enerji: {0:0}",energy),label);
            if(showHelp){Panel(new Rect(16,height-115,610,98));GUI.Label(new Rect(30,height-108,580,24),demoRunning?"OTOMATIK KOSU ACIK | T: elle kosmaya gec":"SOL / SAG OK: ayaklari sirayla basip birakarak kos",label);GUI.Label(new Rect(30,height-83,580,22),"1-9: kostum   C: sonraki kostum   0: temel set   H: yardim",small);if(GUI.Button(new Rect(30,height-55,180,26),demoRunning?"Demo kosusunu durdur (T)":"Demo kosusu (T)"))ToggleDemo();if(wardrobe)GUI.Label(new Rect(225,height-53,380,24),"Kostum: "+wardrobe.selectedSet+(wardrobe.selectedSet>=8?" / Premium":" / Standart"),label);}
            GUI.matrix=old;
        }
        void OnDestroy(){if(pixel)Destroy(pixel);}
    }
}




