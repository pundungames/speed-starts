using UnityEditor;
using UnityEngine;
using SpeedStars.Runner;

namespace SpeedStars.Editor
{
    [CustomEditor(typeof(PastelBuildingRow))]
    public sealed class PastelBuildingRowEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Building Count: toplam bina. Depth Layers / Layer Spacing: ön-arka katmanlar. Variation Seed: 8 mimari tip ve renk çeşitleri. Facade Angle serbesttir. Size Variation boyut aralığını, Dark Building Chance koyu bina oranını belirler.",MessageType.Info);
            DrawDefaultInspector();
            if(GUILayout.Button("Rebuild Buildings")) ((PastelBuildingRow)target).Rebuild();
        }
    }
}
