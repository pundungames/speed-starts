using UnityEditor;
using UnityEngine;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    [CustomEditor(typeof(PastelPlantGenerator)),CanEditMultipleObjects]
    public sealed class PastelPlantGeneratorEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Her kopyanın seed, şekil, boyut ve renk ayarları bağımsızdır. Shape ile ağaç veya çalı seç; Variation Seed ile silueti değiştir.",MessageType.Info);
            DrawDefaultInspector();
            if(GUILayout.Button("Rebuild Plant"))foreach(var item in targets)((PastelPlantGenerator)item).Rebuild();
            if(GUILayout.Button("Next Variation"))foreach(var item in targets){var plant=(PastelPlantGenerator)item;Undo.RecordObject(plant,"Change Plant Seed");plant.variationSeed++;plant.Rebuild();}
        }
    }
}
