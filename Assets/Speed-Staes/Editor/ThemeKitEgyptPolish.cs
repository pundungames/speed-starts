#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor
{
    public static partial class ThemeKitBuilder
    {
        static void PolishedPyramid(Transform t)
        {
            DesertPalette();EStonePyramid(t,18,10.7f,0);
        }
        public static string PolishEgyptPyramids()
        {
            themeFolder=Base+"/Egypt";Palette();
            var definition=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");
            var pyramid=Model("Desert Pyramid",PolishedPyramid);
            Background(pyramid,"Distant Great Pyramid");
            var models=new List<GameObject>(definition.landmarks);models.RemoveAll(p=>p&&p.name=="Desert Pyramid");models.Insert(0,pyramid);definition.landmarks=models.ToArray();
            EditorUtility.SetDirty(definition);
            foreach(var prefab in definition.background){
                if(prefab.name=="Distant Temple")Background(AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Nile Pylon Temple.prefab"),prefab.name);
                if(prefab.name=="Distant Dunes")Background(AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Wind Sculpted Dune.prefab"),prefab.name);
            }
            AssetDatabase.SaveAssets();return "Egypt pyramid models and warm atmospheric palette updated";
        }
    }
}
#endif



