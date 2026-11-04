using System.Linq;
using SpeedStars.UI.Race;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tools.Builders
{
    /// <summary>Uses the clean panel regions of the v2 atlas and keeps existing clean runtime icon layers.</summary>
    public static class UiHudV2Refresh
    {
        const string Atlas="Assets/Speed-Staes/UI/Art/RaceHudReference/hud_v2_atlas.png";
        [MenuItem("Tools/Speed Stars/Apply HUD preview v2")]
        public static void Apply()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;
            importer.spritesheet=new[]{
                Slice("v2_exit",32,30,560,156,40),
                Slice("v2_restart",612,30,555,156,40),
                Slice("v2_currency",1190,30,553,156,40),
                Slice("v2_performance",32,222,1074,168,42),
                Slice("v2_position",1154,225,590,164,0),
                Slice("v2_energy",32,421,840,111,36),
                Slice("v2_cadence",903,421,840,111,36)
            };
            importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().ToDictionary(s=>s.name);
            const string path="Assets/Speed-Staes/UI/Screens/RaceHud.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                SetPanel(root,"ReferenceExit",sprites["v2_exit"]);
                SetPanel(root,"ReferenceRestart",sprites["v2_restart"]);
                SetPanel(root,"ReferenceCurrency",sprites["v2_currency"]);
                SetPanel(root,"Telemetry",sprites["v2_performance"]);
                SetPanel(root,"ReferencePlace",sprites["v2_position"],false);
                SetPanel(root,"ReferenceEnergy",sprites["v2_energy"]);
                SetPanel(root,"ReferenceCadence",sprites["v2_cadence"]);
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Speed-Staes/UI/Fonts/Kanit/Kanit-BlackItalic SDF.asset");
                foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if(!t.gameObject.activeInHierarchy && t.transform.parent.name.StartsWith("Legacy"))continue;
                    if(font)t.font=font;
                    t.fontStyle=FontStyles.Normal;t.enableAutoSizing=true;t.fontSizeMin=18;t.fontSizeMax=t.fontSize;
                }
                // Keep the emote's square geometry, icon and caption from the approved version.
                PrefabUtility.SaveAsPrefabAsset(root,path);
                AssetDatabase.SaveAssets();
                Debug.Log("HUD v2 applied: separate clean panels, icons, live labels and bars.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static SpriteMetaData Slice(string name,int x,int top,int width,int height,int border)
        {
            return new SpriteMetaData{name=name,rect=new Rect(x,887-top-height,width,height),pivot=new Vector2(.5f,.5f),alignment=9,
                border=new Vector4(border,border,border,border)};
        }
        static void SetPanel(GameObject root,string name,Sprite sprite,bool sliced=true)
        {
            var panel=root.transform.Find(name);
            if(!panel)throw new System.InvalidOperationException("HUD panel missing: "+name);
            var image=panel.Find("Background").GetComponent<Image>();
            image.sprite=sprite;image.type=sliced?Image.Type.Sliced:Image.Type.Simple;
            image.pixelsPerUnitMultiplier=3.5f;
        }
    }
}
