using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpeedStars.Runner;
using SpeedStars.Themes;

public static class FrontSidewalkPrefabBaker
{
    [MenuItem("Speed Stars/Themes/Update Front Sidewalks On All Maps")]
    public static void BakeAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before updating prefabs.");
        if(PrefabStageUtility.GetCurrentPrefabStage()!=null)
            throw new InvalidOperationException("Save and close Prefab Mode before updating the map assets.");
        var paths=new[]{
            "Assets/Kevin Iglesias/Human Character Dummy/Models/LoopWay.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay2.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay3.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay4.prefab",
            "Assets/Speed-Staes/Prefabs/Themes/LoopWay5.prefab"
        };
        foreach(var path in paths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var basic=root.GetComponent<LoopWayGenerator>();
                var themed=root.GetComponent<ThemedLoopWay>();
                if(basic)basic.Generate();else themed.Generate();
                var backdrop=root.transform.Find(FixedStartBackdropBuilder.RootName);
                if(backdrop)foreach(Transform section in backdrop)
                {
                    // Preserve all baked scenery, replacing only its front paving.
                    foreach(var old in section.GetComponentsInChildren<Transform>(true)
                        .Where(t=>t.name==FrontSidewalkGeometry.GroupName).ToArray())
                        UnityEngine.Object.DestroyImmediate(old.gameObject);
                    if(basic)
                        FrontSidewalkGeometry.Build(section,basic.FrontRoadEdge(),basic.startZ,basic.startZ+basic.segmentLength,basic.sidewalk);
                    else
                    {
                        var roads=section.Find("Unchanged Road Geometry");
                        if(!roads)throw new InvalidOperationException("Missing fixed-section roads: "+path);
                        float edge=LoopWayGenerator.LocalBounds(roads.gameObject,section).max.x;
                        FrontSidewalkGeometry.Build(section,edge,ThemedLoopWay.StartZ,ThemedLoopWay.StartZ+ThemedLoopWay.SectionLength,themed.theme.pathMaterial);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
