using System.Linq;
using UnityEditor;
using UnityEngine;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    // Preview meshes are a derived cache. Undo restores the settings; the cache is rebuilt afterwards.
    [InitializeOnLoad]
    public static class LoopWayUndoRecovery
    {
        static bool queued;
        static LoopWayUndoRecovery(){Undo.undoRedoPerformed+=QueueRebuild;}
        static void QueueRebuild()
        {
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||queued)return;
            queued=true;EditorApplication.update+=Rebuild;
        }
        static void Rebuild()
        {
            EditorApplication.update-=Rebuild;
            queued=false;
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)return;
            foreach(var generator in Object.FindObjectsOfType<LoopWayGenerator>())
            {
                if(!generator.gameObject.scene.IsValid()||!generator.gameObject.scene.isLoaded||!generator.IsConfigured||!generator.autoPreview)continue;
                generator.Generate();
            }
            SceneView.RepaintAll();
        }
    }
}

