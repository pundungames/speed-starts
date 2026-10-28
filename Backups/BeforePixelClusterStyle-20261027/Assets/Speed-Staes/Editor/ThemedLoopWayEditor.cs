using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor
{
    [CustomEditor(typeof(ThemedLoopWay))]
    public sealed class ThemedLoopWayEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var g=(ThemedLoopWay)target;
            EditorGUILayout.HelpBox("Orijinal yol meshleri korunur; yalnızca yol materyal renkleri temaya göre değişir. Seed, çevre modellerini, boşlukları, bitkileri ve propları çeşitlendirir. Loop Count bu temanın toplam bölüm sayısıdır.",MessageType.Info);
            serializedObject.Update();EditorGUI.BeginChangeCheck();DrawPropertiesExcluding(serializedObject,"m_Script");bool changed=EditorGUI.EndChangeCheck();serializedObject.ApplyModifiedProperties();if(changed&&g.autoPreview&&!Application.isPlaying)g.Generate();
            if(GUILayout.Button("Temayı Yeniden Üret"))g.Generate();
            if(GUILayout.Button("Oyun Kamerasında Bu Temayı Göster"))
            {var camera=Camera.main;if(camera){camera.transform.position=g.transform.TransformPoint(new Vector3(4.18f,3.0f,4));camera.transform.rotation=g.transform.rotation*Quaternion.Euler(4.6f,265.68f,0);camera.farClipPlane=145;}}
            if(GUILayout.Button("Scene View'da Temaya Odaklan"))
            {if(SceneView.lastActiveSceneView){var local=ThemedLoopWay.BoundsIn(g.gameObject,g.transform);SceneView.lastActiveSceneView.Frame(new Bounds(g.transform.TransformPoint(local.center),Vector3.Scale(local.size,g.transform.lossyScale)),false);}}
        }
    }
    [InitializeOnLoad]
    public static class ThemedLoopUndo
    {
        static bool queued;
        static ThemedLoopUndo(){Undo.undoRedoPerformed+=Queue;}
        static void Queue(){if(Application.isPlaying||queued)return;queued=true;EditorApplication.update+=Rebuild;}
        static void Rebuild(){EditorApplication.update-=Rebuild;queued=false;if(Application.isPlaying)return;foreach(var g in Object.FindObjectsOfType<ThemedLoopWay>())if(g.autoPreview&&g.theme&&g.gameObject.scene.IsValid())g.Generate();SceneView.RepaintAll();}
    }
}

