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
            const float width=18,height=10.7f;
            string materialPath=themeFolder+"/Materials/Pyramid Continuous Stone.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("Speed Stars/Painted Pyramid Stone"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetColor("_Color",new Color(.84f,.69f,.46f));EditorUtility.SetDirty(material);
            Pyramid(t,"Continuous Four Face Stone Pyramid",Vector3.zero,width,height,material);
            Pyramid(t,"Pale Limestone Pyramidion",new Vector3(0,9.58f,0),1.91f,1.12f,sandLight);
            Box(t,"Buried Foundation Course",new Vector3(0,.12f,0),new Vector3(18.5f,.24f,18.5f),sand);
            // A compact dark passage gives the base a clear landmark at running speed.
            MeshObject(t,"Recessed Pyramid Passage",new List<Vector3>{new Vector3(-.68f,.20f,-8.85f),new Vector3(-.68f,1.7f,-7.65f),new Vector3(.68f,1.7f,-7.65f),new Vector3(.68f,.20f,-8.85f)},new List<int>{0,1,2,0,2,3},woodDark);
            foreach(float side in new[]{-1f,1f})Beam(t,"Passage Stone Jamb",new Vector3(side*.87f,.2f,-8.92f),new Vector3(side*.87f,1.78f,-7.65f),.30f,sandLight);
            Box(t,"Passage Lintel",new Vector3(0,1.82f,-7.65f),new Vector3(2.2f,.36f,.65f),sandLight);
            for(int i=0;i<3;i++)Box(t,"Worn Approach Slab",new Vector3(0,.04f,-9.1f-i*.85f),new Vector3(2.4f,.08f,.70f),sandLight);
            foreach(float side in new[]{-1f,1f})for(int i=0;i<2;i++){
                var b=Box(t,"Fallen Casing Stone",new Vector3(side*(6.8f+i*.9f),.25f,-7.8f-i*.8f),new Vector3(1.0f,.5f,.75f),i==0?sandLight:sand);
                b.transform.localRotation=Quaternion.Euler(0,side*(13+i*19),0);
            }
        }
        public static string PolishEgyptPyramids()
        {
            themeFolder=Base+"/Egypt";Palette();
            var definition=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");
            var pyramid=Model("Desert Pyramid",PolishedPyramid);
            Background(pyramid,"Distant Great Pyramid");
            definition.landmarks=new[]{pyramid,AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Nile Pylon Temple.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Sphinx Courtyard.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Palm Oasis Pavilion.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(themeFolder+"/Models/Desert Merchant Stall.prefab")};
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



