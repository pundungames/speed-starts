using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.Physical.EditorTools
{
    /// <summary>
    /// One-click setup of the spring-based runner: default pose assets and a PhysicalTest scene
    /// copied from TestScene with the old procedural components swapped for the new ones.
    /// The copy is opened additively, saved and closed, so whatever scene is open stays untouched.
    /// </summary>
    public static class PhysicalSetup
    {
        const string Root = "Assets/Speed-Staes";
        const string PoseFolder = Root + "/Physical/Poses";
        const string SourceScene = Root + "/Scenes/Test/TestScene.unity";
        const string TargetScene = Root + "/Scenes/Test/PhysicalTest.unity";
        const string FinishMaterial = Root + "/Materials/Track_Finish.mat";

        [MenuItem("Tools/Speed Stars/Create Default Poses")]
        public static void CreateDefaultPoses() => LoadOrCreatePoses();

        static RunnerPose[] LoadOrCreatePoses()
        {
            Directory.CreateDirectory(PoseFolder);
            var poses = new[]
            {
                Pose("Ready", RunnerPoseDefaults.Ready),
                Pose("Run", RunnerPoseDefaults.Run),
                Pose("Stumble", RunnerPoseDefaults.Stumble),
            };
            AssetDatabase.SaveAssets();
            return poses;
        }

        static RunnerPose Pose(string name, PoseData data)
        {
            string path = $"{PoseFolder}/{name}.asset";
            var pose = AssetDatabase.LoadAssetAtPath<RunnerPose>(path);
            if (pose) return pose;
            pose = ScriptableObject.CreateInstance<RunnerPose>();
            pose.pose = data;
            AssetDatabase.CreateAsset(pose, path);
            return pose;
        }

        [MenuItem("Tools/Speed Stars/Set Up PhysicalTest Scene")]
        public static string SetUpScene()
        {
            var poses = LoadOrCreatePoses();
            if (!File.Exists(TargetScene))
            {
                if (!AssetDatabase.CopyAsset(SourceScene, TargetScene)) return "Could not copy " + SourceScene;
            }

            Scene scene = SceneManager.GetSceneByPath(TargetScene);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Additive);

            string result = Configure(scene, poses);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            return result;
        }

        static string Configure(Scene scene, RunnerPose[] poses)
        {
            GameObject runner = null;
            Camera cam = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (!runner && go.GetComponentInChildren<SpeedStars.Runner.RunnerInput>()) runner = go.GetComponentInChildren<SpeedStars.Runner.RunnerInput>().gameObject;
                if (!cam) cam = go.GetComponentInChildren<Camera>();
            }
            if (!runner) return "No RunnerInput in " + scene.path;

            // The old procedural system goes (RunnerInput stays).
            Remove<SpeedStars.Runner.RunnerGaitDriver>(runner);
            Remove<SpeedStars.Runner.RunnerDebugHUD>(runner);
            Remove<SpeedStars.Runner.RunnerStepController>(runner);
            Remove<SpeedStars.Runner.RunnerMovement>(runner);

            var input = runner.GetComponent<SpeedStars.Runner.RunnerInput>();
            var body = Ensure<RunnerBody>(runner);
            body.input = input;

            var driver = Ensure<RunnerPoseDriver>(runner);
            var t = runner.transform;
            driver.body = body;
            driver.hipTarget = Find(t, "Hip target");
            driver.spineRootTarget = Find(t, "Spine Root Target");
            driver.spineTipTarget = Find(t, "Spine Tip Target");
            driver.leftLeg.target = Find(t, "Left Leg Target");
            driver.leftLeg.hint = Find(t, "Left Leg Hint");
            driver.rightLeg.target = Find(t, "Right Leg Target");
            driver.rightLeg.hint = Find(t, "Right Leg Hint");
            var steps = Find(t, "Step Points");
            if (steps)
            {
                driver.leftLeg.front = Find(Find(steps, "Left"), "Front");
                driver.leftLeg.back = Find(Find(steps, "Left"), "Back");
                driver.leftLeg.rest = EnsureRest(t, Find(steps, "Left"), "B-toe.L");
                driver.rightLeg.front = Find(Find(steps, "Right"), "Front");
                driver.rightLeg.back = Find(Find(steps, "Right"), "Back");
                driver.rightLeg.rest = EnsureRest(t, Find(steps, "Right"), "B-toe.R");
            }
            driver.leftArm.ikTarget = Find(t, "Left Hand Target");
            driver.rightArm.ikTarget = Find(t, "Right Hand Target");
            driver.ready = poses[0];
            driver.run = poses[1];
            driver.stumble = poses[2];

            // Race flow and finish line.
            RaceController race = null;
            foreach (var go in scene.GetRootGameObjects())
                if (!race) race = go.GetComponentInChildren<RaceController>();
            if (!race)
            {
                var raceGo = new GameObject("Race");
                SceneManager.MoveGameObjectToScene(raceGo, scene);
                race = raceGo.AddComponent<RaceController>();
            }
            race.body = body;
            if (!race.finishLine)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "FinishLine";
                SceneManager.MoveGameObjectToScene(line, scene);
                Object.DestroyImmediate(line.GetComponent<Collider>());
                line.transform.localScale = new Vector3(12f, 0.01f, 0.3f);
                line.transform.position = new Vector3(0f, 0.005f, t.position.z + race.raceDistance);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(FinishMaterial);
                if (mat) line.GetComponent<Renderer>().sharedMaterial = mat;
                race.finishLine = line.transform;
            }

            var replay = Ensure<RunnerReplay>(runner);
            replay.input = input;
            replay.race = race;

            var hud = Ensure<RunnerBodyHUD>(runner);
            hud.body = body;
            hud.race = race;
            hud.replay = replay;

            if (cam)
            {
                var follow = cam.GetComponent<SpeedStars.Runner.SideFollowCamera>();
                if (follow) follow.target = t;
            }
            return $"Set up {scene.path}: runner '{runner.name}', {poses.Length} poses, finish at z={t.position.z + race.raceDistance:F1}";
        }

        /// <summary>
        /// The Rest point under a step-point group, made if missing: above the toe's standing place by
        /// RestCrouch (that is how deep the runner squats waiting) and RestAhead in front of it (the hips sit
        /// back behind the feet). Move it in the scene to change the stance.
        /// </summary>
        static Transform EnsureRest(Transform runner, Transform group, string toeName)
        {
            if (!group) return null;
            var rest = Find(group, "Rest");
            if (rest) return rest;
            var toe = Find(runner, toeName);
            if (!toe) return null;
            const float RestCrouch = 0.25f, RestAhead = 0.08f;
            rest = new GameObject("Rest").transform;
            rest.SetParent(group, false);
            rest.position = toe.position + runner.up * RestCrouch + runner.forward * RestAhead;
            return rest;
        }

        static void Remove<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c) Object.DestroyImmediate(c);
        }

        static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c ? c : go.AddComponent<T>();
        }

        static Transform Find(Transform root, string name)
        {
            if (!root) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
