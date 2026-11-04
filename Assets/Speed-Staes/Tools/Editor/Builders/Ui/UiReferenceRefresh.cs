using System;
using System.IO;
using SpeedStars.UI.Race;
using SpeedStars.UI.Settings;
using SpeedStars.UI.Home;
using SpeedStars.UI.Costume;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tools.Builders
{
    /// <summary>Applies the supplied reference art to existing prefabs, preserving gameplay and settings bindings.</summary>
    public static class UiReferenceRefresh
    {
        const string Art = "Assets/Speed-Staes/UI/Art/RaceHudReference/";
        const string HudPath = "Assets/Speed-Staes/UI/Screens/RaceHud.prefab";
        const string SettingsPath = "Assets/Speed-Staes/UI/Popups/SettingsPopup.prefab";
        const float S = 2.4f;
        static readonly Color Navy = new Color32(3, 12, 43, 255);

        [MenuItem("Tools/Speed Stars/Apply supplied HUD and settings reference")]
        public static void Apply()
        {
            ImportArt();
            PatchHud();
            PatchSettings();
            PatchHome();
            HandEditGuard.ConvertPending();
            AssetDatabase.SaveAssets();
            Debug.Log("Applied reference HUD and settings feedback.");
        }

        static void ImportArt()
        {
            foreach (string path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
        }

        static RectTransform Node(Transform parent, string name)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.layer=5;
            g.transform.SetParent(parent,false); return (RectTransform)g.transform;
        }
        static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        { r.anchorMin=r.anchorMax=anchor; r.pivot=pivot; r.anchoredPosition=position; r.sizeDelta=size; r.localScale=Vector3.one; }
        static void Stretch(RectTransform r)
        { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        static Image Layer(Transform parent, string name, string file)
        {
            var r=Node(parent,name); Stretch(r); var image=r.gameObject.AddComponent<Image>();
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+file+".png"); image.raycastTarget=false; return image;
        }
        static TMP_Text Label(Transform parent,string name,string value,float size, Color color, Vector2 pos,Vector2 dimensions,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var r=Node(parent,name); Place(r,new Vector2(0,1),new Vector2(0,1),pos*S,dimensions*S);
            var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Speed-Staes/UI/Fonts/AmericanCaptain/AmericanCaptain SDF.asset");
            t.text=value; t.fontSize=size*S; t.fontStyle=FontStyles.Italic; t.color=color;
            t.alignment=align; t.raycastTarget=false; t.enableWordWrapping=false; t.margin=Vector4.zero; return t;
        }
        static void Bind(SerializedObject so,string field,UnityEngine.Object value) => so.FindProperty(field).objectReferenceValue=value;
        static RectTransform Panel(Transform root,string name,string file,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
        { var r=Node(root,name); Place(r,anchor,pivot,pos*S,size*S); Layer(r,"Background",file); return r; }
        static Image Bar(Transform p,string name,string file)
        { var im=Layer(p,name,file); im.type=Image.Type.Filled; im.fillMethod=Image.FillMethod.Horizontal; im.fillOrigin=0; return im; }

        static void PatchHud()
        {
            var root=PrefabUtility.LoadPrefabContents(HudPath);
            try
            {
                var view=root.GetComponent<RaceHudView>(); var so=new SerializedObject(view);
                Transform emote=so.FindProperty("emoteButton").objectReferenceValue is Button b ? b.transform : null;
                bool patched=so.FindProperty("referenceHud").boolValue;
                for(int i=root.transform.childCount-1;i>=0;i--)
                {
                    Transform c=root.transform.GetChild(i);
                    if(c.name.StartsWith("Reference",StringComparison.Ordinal) || (patched && c.name=="Telemetry")) UnityEngine.Object.DestroyImmediate(c.gameObject);
                    else if(!patched && c.name=="Telemetry")c.name="LegacyTelemetry";
                }
                // Keep optional features and old serialized objects available, while only the reference groups render.
                foreach(Transform c in root.transform) if(c!=emote)c.gameObject.SetActive(false);
                // Each direct group is independently edge anchored and fitted by the existing SafeAreaFitter.
                var tl=new Vector2(0,1);var tr=Vector2.one;
                var exit=Panel(root.transform,"ReferenceExit","exit_background",tl,tl,new Vector2(17,-10),new Vector2(144,42));
                Layer(exit,"Icon","exit_icon"); Label(exit,"Label","Exit",22,Navy,new Vector2(53,-4),new Vector2(82,34));
                var exitButton=exit.gameObject.AddComponent<Button>(); exitButton.targetGraphic=exit.Find("Background").GetComponent<Image>(); exitButton.targetGraphic.raycastTarget=true;
                var restart=Panel(root.transform,"ReferenceRestart","restart_background",tl,tl,new Vector2(17,-54),new Vector2(144,42));
                Layer(restart,"Icon","restart_icon"); Label(restart,"Label","Restart",22,Navy,new Vector2(49,-4),new Vector2(88,34));
                var restartButton=restart.gameObject.AddComponent<Button>(); restartButton.targetGraphic=restart.Find("Background").GetComponent<Image>(); restartButton.targetGraphic.raycastTarget=true;
                var currency=Panel(root.transform,"ReferenceCurrency","currency_background",tr,tr,new Vector2(-15,-4),new Vector2(128,38));
                Layer(currency,"Icon","currency_icon"); var cash=Label(currency,"Value","0",25,Navy,new Vector2(48,-1),new Vector2(72,34));
                var perf=Panel(root.transform,"Telemetry","performance_background",tr,tr,new Vector2(-15,-52),new Vector2(204,82));
                Layer(perf,"TimeIcon","stopwatch_icon");Layer(perf,"SpeedIcon","speed_icon");
                var time=Label(perf,"Time","0.00",32,Color.white,new Vector2(48,-1),new Vector2(105,39),TextAlignmentOptions.MidlineRight);
                Label(perf,"Seconds","s",31,Color.white,new Vector2(157,-1),new Vector2(25,39));
                var speed=Label(perf,"Speed","0.0 mph",31,new Color32(244,173,177,255),new Vector2(49,-39),new Vector2(145,39),TextAlignmentOptions.MidlineRight);
                var energy=Panel(root.transform,"ReferenceEnergy","energy_background",tr,tr,new Vector2(-16,-137),new Vector2(203,43));
                Layer(energy,"Icon","energy_icon");Layer(energy,"Empty","energy_bar_empty");var energyFill=Bar(energy,"Fill","energy_bar_fill");
                Label(energy,"Label","Energy",16,Color.white,new Vector2(43,-2),new Vector2(95,21),TextAlignmentOptions.MidlineLeft);
                var energyValue=Label(energy,"Value","100",17,Color.white,new Vector2(151,-2),new Vector2(40,21),TextAlignmentOptions.MidlineRight);
                var cadence=Panel(root.transform,"ReferenceCadence","cadence_background",tr,tr,new Vector2(-16,-181),new Vector2(203,43));
                Layer(cadence,"Icon","cadence_icon");Layer(cadence,"Empty","cadence_bar_empty");var cadenceFill=Bar(cadence,"Fill","cadence_bar_fill");
                Label(cadence,"Label","Cadence",16,Color.white,new Vector2(43,-2),new Vector2(95,21),TextAlignmentOptions.MidlineLeft);
                var cadenceValue=Label(cadence,"Value","0.0",17,Color.white,new Vector2(151,-2),new Vector2(40,21),TextAlignmentOptions.MidlineRight);
                var place=Panel(root.transform,"ReferencePlace","position_background",Vector2.zero,Vector2.zero,new Vector2(15,6),new Vector2(98,49));
                var rank=Label(place,"Label","1<size=60%>st</size>",42,Color.white,new Vector2(9,-1),new Vector2(80,44));
                Bind(so,"exitButton",exitButton);Bind(so,"restartButton",restartButton);Bind(so,"cashText",cash);Bind(so,"cashPlusButton",null);
                Bind(so,"timeText",time);Bind(so,"speedText",speed);Bind(so,"energyFill",energyFill);Bind(so,"energyValueText",energyValue);
                Bind(so,"cadenceFill",cadenceFill);Bind(so,"cadenceValueText",cadenceValue);Bind(so,"cadenceTrack",null);Bind(so,"cadenceMarker",null);
                Bind(so,"placeRoot",place.gameObject);Bind(so,"placeText",rank);so.FindProperty("referenceHud").boolValue=true;
                so.ApplyModifiedPropertiesWithoutUndo();
                if(emote)
                {
                    Place((RectTransform)emote,new Vector2(1,0),new Vector2(1,0),new Vector2(-35,25),new Vector2(150,150));
                    var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Speed-Staes/UI/Art/Home/emote_icon.png");
                    FitVisibleIcon(emote,"Icon",icon,new Vector2(75,-51),new Vector2(76,76));
                    var caption=emote.Find("Label").GetComponent<TMP_Text>();caption.text="Emote";caption.fontSize=28;caption.alignment=TextAlignmentOptions.Center;
                    Place(caption.rectTransform,new Vector2(0,0),Vector2.zero,new Vector2(8,10),new Vector2(134,38));
                    var name=emote.Find("Name") as RectTransform;
                    if(name)Place(name,Vector2.one,new Vector2(1,0),new Vector2(0,8),new Vector2(320,44));
                }
                HandEditGuard.SavePatchedPrefab(root,HudPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void PatchSettings()
        {
            var root=PrefabUtility.LoadPrefabContents(SettingsPath);
            try
            {
                var so=new SerializedObject(root.GetComponent<SettingsPopupView>());
                var shader=Shader.Find("SpeedStars/UI/Icon Silhouette");
                const string materialPath="Assets/Speed-Staes/UI/Art/Settings/IconSilhouette.mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,materialPath);}
                ColorIcon(so,"musicLabel",mat,new Color32(146,39,216,255),false);
                ColorIcon(so,"languageLabel",mat,new Color32(36,97,234,255),false);
                ColorIcon(so,"nameLabel",mat,new Color32(255,211,38,255),true);
                var flag=(Image)so.FindProperty("flagValueImage").objectReferenceValue;
                var button=(Button)so.FindProperty("flagButton").objectReferenceValue;
                var arrow=(Image)so.FindProperty("flagArrow").objectReferenceValue;
                // Keep the existing hit target/list, remove only its long framed field artwork.
                button.transition=Selectable.Transition.None;
                if(button.targetGraphic){button.targetGraphic.color=Color.clear;}
                var square=button.transform.Find("SquareFlag") as RectTransform;
                if(!square){square=Node(button.transform,"SquareFlag");square.gameObject.AddComponent<RectMask2D>();}
                Place(square,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-115,0),new Vector2(88,88));
                flag.transform.SetParent(square,false);
                Place(flag.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(168,118));
                flag.preserveAspect=true;
                Place(arrow.rectTransform,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-25,0),new Vector2(56,56));
                var buttonRect=(RectTransform)button.transform;
                FitVisibleIcon(arrow.transform.parent,arrow.name,arrow.sprite,new Vector2(buttonRect.rect.width-44,-buttonRect.rect.height*.5f),new Vector2(38,28));
                var input=(InputField)so.FindProperty("nameInput").objectReferenceValue;
                var pencil=input.transform.Find("Pencil");
                if(pencil && !pencil.GetComponent<CanvasRenderer>())pencil.gameObject.AddComponent<CanvasRenderer>();
                if(!pencil){var r=Node(input.transform,"Pencil");Place(r,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(22,0),new Vector2(40,40));var g=r.gameObject.AddComponent<PencilIconGraphic>();g.color=Navy;g.raycastTarget=false;}
                input.textComponent.rectTransform.offsetMin=new Vector2(76,input.textComponent.rectTransform.offsetMin.y);
                if(input.placeholder is Graphic placeholder) placeholder.rectTransform.offsetMin=new Vector2(76,placeholder.rectTransform.offsetMin.y);
                Transform content=((TMP_Text)so.FindProperty("musicLabel").objectReferenceValue).transform.parent.parent;
                string[] fields={"musicLabel","languageLabel","flagLabel","nameLabel"};
                for(int i=0;i<fields.Length;i++)((TMP_Text)so.FindProperty(fields[i]).objectReferenceValue).transform.parent.SetSiblingIndex(i);
                foreach(var t in root.GetComponentsInChildren<RectTransform>(true))if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                HandEditGuard.SavePatchedPrefab(root,SettingsPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void ColorIcon(SerializedObject so,string labelField,Material material,Color color,bool outline)
        {
            var label=(TMP_Text)so.FindProperty(labelField).objectReferenceValue;
            var icon=label.transform.parent.Find("Icon").GetComponent<Image>();
            string path="Assets/Speed-Staes/UI/Art/Settings/"+labelField+"Tint.mat";
            var tint=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!tint){tint=new Material(material);AssetDatabase.CreateAsset(tint,path);}
            tint.SetTexture("_MainTex",icon.sprite.texture);tint.SetColor("_Color",color);
            tint.SetFloat("_LightCutout",labelField=="languageLabel"?1:0);
            icon.material=tint;icon.color=Color.white;
            if(outline){var o=icon.GetComponent<Outline>()??icon.gameObject.AddComponent<Outline>();o.effectColor=Navy;o.effectDistance=new Vector2(2,-2);}
            PrefabUtility.RecordPrefabInstancePropertyModifications(icon);
        }

        [MenuItem("Tools/Speed Stars/Apply home card proportions")]
        public static void PatchHome()
        {
            const string path="Assets/Speed-Staes/UI/Screens/HomeScreen.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so=new SerializedObject(root.GetComponent<HomeScreenView>());
                var card=(RectTransform)root.transform.Find("CharacterCard");
                // Keep the card's authored position and all outer menu controls.
                card.sizeDelta=new Vector2(620,740);
                var cardBackground=card.Find("Background").GetComponent<Image>();
                cardBackground.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Speed-Staes/UI/Art/Settings/panel_single_9slice.png");
                cardBackground.type=Image.Type.Sliced;
                var select=card.Find("Content/Select") as RectTransform;
                if(!select)select=card.Find("Select") as RectTransform;
                if(select){select.SetParent(card,false);select.gameObject.SetActive(true);Place(select,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-25),new Vector2(card.sizeDelta.x-60,100));}
                if(select)
                {
                    var original=select.GetComponent<Image>();
                    var plate=select.Find("Plate") as RectTransform;if(!plate)plate=Node(select,"Plate");
                    Place(plate,new Vector2(0,1),new Vector2(0,1),new Vector2(70,0),new Vector2(select.rect.width-140,100));
                    var plateImage=plate.GetComponent<Image>()??plate.gameObject.AddComponent<Image>();
                    plateImage.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Speed-Staes/UI/Art/Settings/field_normal.png");plateImage.type=Image.Type.Sliced;plateImage.raycastTarget=false;
                    original.enabled=false;
                    if(select.Find("Portrait"))select.Find("Portrait").gameObject.SetActive(false);
                    var portrait=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Speed-Staes/UI/Art/Home/character_select_icon.png");
                    FitVisibleIcon(select,"Icon",portrait,new Vector2(135,-50),new Vector2(100,90));
                    var name=(TMP_Text)so.FindProperty("racerNameText").objectReferenceValue;
                    Place(name.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(205,0),new Vector2(select.rect.width-285,100));
                    name.fontSize=42;name.alignment=TextAlignmentOptions.MidlineLeft;
                    select.Find("Plate").SetAsFirstSibling();
                    foreach(string field in new[]{"previousRacerButton","nextRacerButton"})
                    {
                        var arrow=(Button)so.FindProperty(field).objectReferenceValue;arrow.transform.SetParent(select,false);arrow.gameObject.SetActive(true);
                        bool previous=field=="previousRacerButton";
                        Place((RectTransform)arrow.transform,new Vector2(previous?0:1,1),new Vector2(previous?0:1,1),Vector2.zero,new Vector2(70,100));
                    }
                }
                var stats=card.Find("StatsStrip"); if(stats)stats.gameObject.SetActive(false);
                var power=(Image)so.FindProperty("powerFill").objectReferenceValue;
                var stamina=(Image)so.FindProperty("staminaFill").objectReferenceValue;
                var height=(TMP_Text)so.FindProperty("heightText").objectReferenceValue;
                Transform oldGroup=power.transform.parent.parent;
                if(oldGroup!=card)oldGroup.gameObject.SetActive(false);
                power.transform.SetParent(card,false);stamina.transform.SetParent(card,false);height.transform.SetParent(card,false);
                Transform old=card.Find("ReferenceStats");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var group=Node(card,"ReferenceStats");Stretch(group);
                var background=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Speed-Staes/UI/Art/Settings/row_normal.png");
                string[] names={"Power","Stamina","Height"};
                for(int i=0;i<3;i++)
                {
                    var row=Node(group,names[i]);Place(row,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-150-i*126),new Vector2(card.sizeDelta.x-60,114));
                    var frame=row.gameObject.AddComponent<Image>();frame.sprite=background;frame.type=Image.Type.Sliced;frame.raycastTarget=false;
                    var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Speed-Staes/UI/Art/Costume/stats_"+names[i].ToLowerInvariant()+"_icon.png");
                    FitVisibleIcon(row,"Icon",sprite,new Vector2(58,-57),new Vector2(72,85));
                    if(i<2)
                    {
                        var text=Label(row,"Label",names[i],17,Navy,Vector2.zero,Vector2.one,TextAlignmentOptions.MidlineLeft);
                        Place(text.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(122,-5),new Vector2(400,49));
                        Image fill=i==0?power:stamina;
                        var bar=Node(row,"Bar");Place(bar,new Vector2(0,0),new Vector2(0,0),new Vector2(122,17),new Vector2(card.sizeDelta.x-200,35));
                        var empty=bar.gameObject.AddComponent<Image>();empty.color=Navy;empty.raycastTarget=false;
                        fill.transform.SetParent(bar,false);Stretch(fill.rectTransform);fill.rectTransform.offsetMin=new Vector2(6,6);fill.rectTransform.offsetMax=new Vector2(-6,-6);
                        fill.sprite=null;fill.type=Image.Type.Simple;
                        fill.color=i==0?new Color32(255,171,0,255):new Color32(0,218,40,255);
                        fill.gameObject.SetActive(true);
                    }
                    else
                    {
                        height.transform.SetParent(row,false);height.gameObject.SetActive(true);
                        Place(height.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(122,0),new Vector2(card.sizeDelta.x-200,114));
                        height.fontSize=40;height.alignment=TextAlignmentOptions.MidlineLeft;
                    }
                }
                foreach(var field in new[]{"editRacerButton","emoteButton"})
                {
                    var button=(Button)so.FindProperty(field).objectReferenceValue;
                    bool edit=field=="editRacerButton";
                    Place((RectTransform)button.transform,new Vector2(0,0),Vector2.zero,new Vector2(edit?30:card.sizeDelta.x*.5f+7,35),new Vector2(card.sizeDelta.x*.5f-37,156));
                    var icon=button.transform.Find("Icon").GetComponent<Image>();
                    FitVisibleIcon(button.transform,"Icon",icon.sprite,new Vector2(((RectTransform)button.transform).sizeDelta.x*.5f,-48),new Vector2(78,72));
                    var caption=button.transform.Find("Label")?.GetComponent<TMP_Text>();
                    if(!caption)caption=Label(button.transform,"Label",edit?"Edit Racer":"Emote",17,Navy,Vector2.zero,Vector2.one);
                    caption.text=edit?"Edit Racer":"Emote";caption.fontSize=38;caption.color=Navy;caption.fontStyle=FontStyles.Italic;
                    Place(caption.rectTransform,new Vector2(0,0),Vector2.zero,new Vector2(7,8),new Vector2(((RectTransform)button.transform).sizeDelta.x-14,48));
                    caption.alignment=TextAlignmentOptions.Center;
                }
                var player=so.FindProperty("playerNameText").objectReferenceValue as TMP_Text;
                if(player){Place(player.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,4),new Vector2(card.sizeDelta.x-50,28));player.fontSize=22;}
                const string fontPath="Assets/Speed-Staes/UI/Fonts/Kanit/Kanit-BlackItalic SDF.asset";
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                if(!font)
                {
                    font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Speed-Staes/UI/Fonts/Kanit/Kanit-BlackItalic.ttf"));
                    AssetDatabase.CreateAsset(font,fontPath);
                    AssetDatabase.AddObjectToAsset(font.material,font);
                    foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
                }
                foreach(var text in card.GetComponentsInChildren<TMP_Text>(true))
                {text.font=font;text.fontStyle=FontStyles.Normal;text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=text.fontSize;}
                // The newly labelled rows follow the same Race of the Day visibility rule as the old stats.
                var hidden=so.FindProperty("hiddenForRacerOfTheDay"); bool found=false;
                for(int i=0;i<hidden.arraySize;i++)if(hidden.GetArrayElementAtIndex(i).objectReferenceValue==group.gameObject)found=true;
                if(!found){int i=hidden.arraySize;hidden.arraySize++;hidden.GetArrayElementAtIndex(i).objectReferenceValue=group.gameObject;}
                var configs=AssetDatabase.FindAssets("t:CostumeEditorConfig");
                if(configs.Length>0)Bind(so,"statsConfig",AssetDatabase.LoadAssetAtPath<CostumeEditorConfig>(AssetDatabase.GUIDToAssetPath(configs[0])));
                so.ApplyModifiedPropertiesWithoutUndo();
                HandEditGuard.SavePatchedPrefab(root,path);
                AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        // Keep the original padded sprite and pivot; position its visible silhouette inside the desired icon box.
        static void FitVisibleIcon(Transform parent,string name,Sprite sprite,Vector2 center,Vector2 size)
        {
            var existing=parent.Find(name);var r=existing?(RectTransform)existing:Node(parent,name);
            var im=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;
            string path=AssetDatabase.GetAssetPath(sprite);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool readable=importer.isReadable;if(!readable){importer.isReadable=true;importer.SaveAndReimport();}
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
            int minX=texture.width,minY=texture.height,maxX=0,maxY=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>32){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            float scale=Mathf.Min(size.x/(maxX-minX+1),size.y/(maxY-minY+1));
            Vector2 full=new Vector2(texture.width,texture.height)*scale;
            Vector2 offset=new Vector2((minX+maxX+1)*.5f-texture.width*.5f,(minY+maxY+1)*.5f-texture.height*.5f)*scale;
            Place(r,new Vector2(0,1),new Vector2(.5f,.5f),center-offset,full);
            if(!readable){importer.isReadable=false;importer.SaveAndReimport();}
        }
    }
}
