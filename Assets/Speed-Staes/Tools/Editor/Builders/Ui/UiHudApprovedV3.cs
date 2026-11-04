using System;
using System.IO;
using System.Linq;
using SpeedStars.UI.Race;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tools.Builders
{
    /// <summary>Patches only the seven approved HUD widgets; keeps race actions and optional emotes.</summary>
    public static class UiHudApprovedV3
    {
        const string Art = "Assets/Speed-Staes/UI/Art/RaceHudApprovedV3/";
        const string Hud = "Assets/Speed-Staes/UI/Screens/RaceHud.prefab";
        const float Scale = 2400f / 1853f;
        static TMP_FontAsset Font;

        [MenuItem("Tools/Speed Stars/Apply approved HUD v3")]
        public static void Apply()
        {
            foreach (var file in Directory.GetFiles(Art, "*.png"))
            {
                string path = file.Replace('\\', '/');
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                // Fill artwork shares the panel canvas. Import only its actual fill rectangle.
                if (path.EndsWith("energy_bar_fill.png") || path.EndsWith("cadence_bar_fill.png"))
                {
                    bool energy = path.EndsWith("energy_bar_fill.png");
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritesheet = new[] { new SpriteMetaData {
                        name = Path.GetFileNameWithoutExtension(path), alignment = 9,
                        pivot = new Vector2(.5f,.5f),
                        rect = new Rect(93, (energy ? 88 : 89) - (energy ? 52 : 47) - 17, 281, 17)
                    }};
                }
                importer.SaveAndReimport();
            }
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Speed-Staes/UI/Fonts/Kanit/Kanit-BlackItalic SDF.asset");
            if (!Font) throw new InvalidOperationException("HUD font is missing.");
            var root = PrefabUtility.LoadPrefabContents(Hud);
            try
            {
                var view = new SerializedObject(root.GetComponent<RaceHudView>());
                Panel(root,"ReferenceExit",24,12,296,91,false,false,"exit_background");
                Panel(root,"ReferenceRestart",24,102,296,88,false,false,"restart_background");
                Panel(root,"ReferenceCurrency",1548,0,267,84,true,false,"currency_background");
                Panel(root,"Telemetry",1402,94,413,166,true,false,"performance_background");
                Panel(root,"ReferenceEnergy",1402,259,413,88,true,false,"energy_background");
                Panel(root,"ReferenceCadence",1402,346,413,89,true,false,"cadence_background");
                Panel(root,"ReferencePlace",19,733,209,105,false,true,"position_background");
                var exit = root.transform.Find("ReferenceExit");
                var restart = root.transform.Find("ReferenceRestart");
                var currency = root.transform.Find("ReferenceCurrency");
                var perf = root.transform.Find("Telemetry");
                var energy = root.transform.Find("ReferenceEnergy");
                var cadence = root.transform.Find("ReferenceCadence");
                var place = root.transform.Find("ReferencePlace");
                Icon(exit,"Icon","exit_icon"); Icon(restart,"Icon","restart_icon");
                Icon(currency,"Icon","diamond_icon");
                Icon(perf,"TimeIcon","stopwatch_icon"); Icon(perf,"SpeedIcon","speedometer_icon");
                Icon(energy,"Icon","energy_icon"); Icon(cadence,"Icon","cadence_icon");
                var dark = new Color32(30,38,47,255);
                Text(exit,"Label",118,6,170,76,44,dark,TextAlignmentOptions.Center);
                Text(restart,"Label",116,3,158,77,39,dark,TextAlignmentOptions.Center);
                Text(currency,"Value",108,1,143,75,48,dark,TextAlignmentOptions.Center);
                Text(perf,"Time",104,1,165,78,64,Color.white,TextAlignmentOptions.MidlineRight);
                Text(perf,"Seconds",273,9,42,66,50,Color.white,TextAlignmentOptions.MidlineLeft);
                Text(perf,"Speed",105,75,293,83,62,new Color32(244,173,177,255),TextAlignmentOptions.MidlineRight);
                Text(energy,"Label",93,3,188,44,29,Color.white,TextAlignmentOptions.MidlineLeft);
                Text(energy,"Value",291,2,84,44,33,Color.white,TextAlignmentOptions.MidlineRight);
                Text(cadence,"Label",93,0,188,43,29,Color.white,TextAlignmentOptions.MidlineLeft);
                Text(cadence,"Value",291,0,84,43,33,Color.white,TextAlignmentOptions.MidlineRight);
                Text(place,"Label",24,0,159,98,76,Color.white,TextAlignmentOptions.Center);
                Icon(energy,"Empty","energy_bar_empty"); Icon(cadence,"Empty","cadence_bar_empty");
                Fill(energy,"energy_bar_fill",52); Fill(cadence,"cadence_bar_fill",47);
                view.FindProperty("approvedHudV3").boolValue = true;
                view.FindProperty("referenceHud").boolValue = true;
                view.ApplyModifiedPropertiesWithoutUndo();
                HandEditGuard.SavePatchedPrefab(root,Hud);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            HandEditGuard.ConvertPending();
            AssetDatabase.SaveAssets();
            Debug.Log("Approved HUD v3 applied: seven widgets, cropped live bars and wallet gems.");
        }

        static void Rect(RectTransform r,Vector2 anchor,Vector2 pivot,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=anchor; r.pivot=pivot;
            r.anchoredPosition=new Vector2(x,y)*Scale; r.sizeDelta=new Vector2(w,h)*Scale;
            r.localScale=Vector3.one;
        }
        static void Panel(GameObject root,string name,float x,float y,float w,float h,bool right,bool bottom,string file)
        {
            var panel=root.transform.Find(name) as RectTransform;
            if (!panel) throw new InvalidOperationException("Missing HUD widget: "+name);
            var anchor=new Vector2(right?1:0,bottom?0:1);
            Rect(panel,anchor,anchor,right?x+w-1853:x,bottom?849-y-h:-y,w,h);
            panel.gameObject.SetActive(true);
            Icon(panel,"Background",file);
        }
        static Image Icon(Transform panel,string name,string file)
        {
            var image=panel.Find(name).GetComponent<Image>();
            var r=image.rectTransform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one;
            r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;
            image.sprite=AssetDatabase.LoadAllAssetsAtPath(Art+file+".png").OfType<Sprite>().First();
            image.type=Image.Type.Simple; image.color=Color.white; image.preserveAspect=true;
            image.material=null;
            return image;
        }
        static void Fill(Transform panel,string file,float top)
        {
            var image=Icon(panel,"Fill",file);
            Rect(image.rectTransform,new Vector2(0,1),new Vector2(0,1),93,-top,281,17);
            image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;
            image.fillOrigin=0;image.fillAmount=1;image.raycastTarget=false;
        }
        static void Text(Transform panel,string name,float x,float y,float w,float h,float size,Color color,TextAlignmentOptions alignment)
        {
            var text=panel.Find(name).GetComponent<TMP_Text>();
            Rect(text.rectTransform,new Vector2(0,1),new Vector2(0,1),x,-y,w,h);
            text.font=Font;text.fontSharedMaterial=Font.material;text.fontStyle=FontStyles.Normal;
            text.color=color;text.fontSize=size*Scale;text.enableAutoSizing=true;
            text.fontSizeMin=size*Scale*.65f;text.fontSizeMax=size*Scale;
            text.enableWordWrapping=false;text.alignment=alignment;text.margin=Vector4.zero;
        }
    }
}
