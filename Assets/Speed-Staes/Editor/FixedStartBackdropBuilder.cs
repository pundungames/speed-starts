using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SpeedStars.Runner;
using SpeedStars.Themes;

// Baked scenery behind the start. It deliberately lives outside the streamed roots.
public static class FixedStartBackdropBuilder
{
    public const string RootName = "Fixed Start Backdrop - 3 Sections";

    [MenuItem("Speed Stars/Themes/Bake Three Fixed Sections Behind Start")]
    public static void BakeAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before baking prefabs.");
        var paths = new[] {
            "Assets/Kevin Iglesias/Human Character Dummy/Models/LoopWay.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay2.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay3.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay4.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay5.prefab"
        };
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Bake(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    static void Bake(GameObject root)
    {
        var previous = root.transform.Find(RootName);
        if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var basic = root.GetComponent<LoopWayGenerator>();
        var themed = root.GetComponent<ThemedLoopWay>();
        float length = basic ? basic.segmentLength : ThemedLoopWay.SectionLength;
        Transform[] sources;
        if (basic)
            sources = root.transform.Cast<Transform>().Where(t => !t.GetComponent<LoopWayGenerated>()).ToArray();
        else
        {
            var first = root.GetComponentsInChildren<ThemeChunk>(true).FirstOrDefault(c => c.index == 0);
            if (!themed || !first) throw new InvalidOperationException("Missing baked start section: " + root.name);
            sources = first.transform.Cast<Transform>().ToArray();
        }
        var backdrop = new GameObject(RootName).transform;
        backdrop.SetParent(root.transform, false);
        for (int section = 1; section <= 3; section++)
        {
            var copy = new GameObject("Fixed Section -" + section).transform;
            copy.SetParent(backdrop, false);
            foreach (var source in sources)
            {
                var item = UnityEngine.Object.Instantiate(source.gameObject, copy, false);
                item.name = source.name;
                // Keep the authored local pose, translating only the section as a whole.
                item.transform.localPosition = source.localPosition;
                item.transform.localRotation = source.localRotation;
                item.transform.localScale = source.localScale;
            }
            copy.localPosition = Vector3.back * (length * section);
        }
    }
}
