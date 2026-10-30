using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpeedStars.Physical.EditorTools
{
    /// <summary>
    /// Moves the Mixamo animations onto the runner's B-* bones through Unity's human pose and saves them as
    /// RunnerEmoteData. The avatars only live during the bake, so no import setting or prefab changes.
    /// </summary>
    public static class RunnerEmoteBaker
    {
        const string SourceFolder = "Assets/Speed-Staes/Animations";
        const string OutputFolder = "Assets/Speed-Staes/Animations/Baked";
        const string Character = "Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_F.fbx";
        const float FrameRate = 30f;

        static readonly string[] Fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };
        static readonly string[] Parts = { "Proximal", "Intermediate", "Distal" };

        [MenuItem("Tools/Speed Stars/Bake Emotes")]
        public static void BakeAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder(SourceFolder, "Baked");
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { SourceFolder }))
                Bake(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.SaveAssets();
        }

        static void Bake(string path)
        {
            AnimationClip clip = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip c && !c.name.StartsWith("__preview")) clip = c;
            if (!clip) return;

            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var source = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
                var target = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Character), scene);
                ToBindPose(source);

                var sourceAvatar = BuildAvatar(source, MixamoBones());
                var targetAvatar = BuildAvatar(target, RunnerBones());
                var bones = UpperBones(target.transform);

                int frameCount = Mathf.CeilToInt(clip.length * FrameRate) + 1;
                var data = ScriptableObject.CreateInstance<RunnerEmoteData>();
                data.frameRate = FrameRate;
                data.bones = bones.ConvertAll(b => b.name).ToArray();
                data.frames = new Quaternion[frameCount * bones.Count];

                var sourceHandler = new HumanPoseHandler(sourceAvatar, source.transform);
                var targetHandler = new HumanPoseHandler(targetAvatar, target.transform);
                var pose = new HumanPose();
                for (int f = 0; f < frameCount; f++)
                {
                    clip.SampleAnimation(source, Mathf.Min(f / FrameRate, clip.length));
                    sourceHandler.GetHumanPose(ref pose);
                    targetHandler.SetHumanPose(ref pose);
                    for (int b = 0; b < bones.Count; b++)
                        data.frames[f * bones.Count + b] = bones[b].localRotation;
                }
                sourceHandler.Dispose();
                targetHandler.Dispose();
                Object.DestroyImmediate(sourceAvatar);
                Object.DestroyImmediate(targetAvatar);

                string name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(data, $"{OutputFolder}/{name}.asset");
                Debug.Log($"[RunnerEmoteBaker] {name}: {frameCount} frames, {data.Length:0.00} s, {bones.Count} bones");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>A Mixamo file opens on its first frame; the skin remembers the T-pose the avatar needs.</summary>
        static void ToBindPose(GameObject root)
        {
            var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var binds = skin.sharedMesh.bindposes;
            Matrix4x4 mesh = skin.transform.localToWorldMatrix;
            var byDepth = new List<int>();
            for (int i = 0; i < skin.bones.Length; i++) byDepth.Add(i);
            byDepth.Sort((a, b) => Depth(skin.bones[a]).CompareTo(Depth(skin.bones[b])));
            foreach (int i in byDepth)
            {
                Matrix4x4 world = mesh * binds[i].inverse;
                skin.bones[i].SetPositionAndRotation(world.GetColumn(3), world.rotation);
            }
        }

        static int Depth(Transform t)
        {
            int depth = 0;
            for (; t.parent; t = t.parent) depth++;
            return depth;
        }

        static Avatar BuildAvatar(GameObject root, Dictionary<string, string> map)
        {
            var human = new List<HumanBone>();
            var skeleton = new List<SkeletonBone>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                skeleton.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
                if (map.TryGetValue(t.name, out string humanName))
                {
                    var bone = new HumanBone { boneName = t.name, humanName = humanName };
                    bone.limit.useDefaultValues = true;
                    human.Add(bone);
                }
            }
            var description = new HumanDescription
            {
                human = human.ToArray(),
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };
            var avatar = AvatarBuilder.BuildHumanAvatar(root, description);
            if (!avatar.isValid || !avatar.isHuman) throw new System.InvalidOperationException($"Avatar failed for {root.name}");
            return avatar;
        }

        static Dictionary<string, string> MixamoBones()
        {
            var map = new Dictionary<string, string>
            {
                { "mixamorig:Hips", "Hips" }, { "mixamorig:Spine", "Spine" }, { "mixamorig:Spine1", "Chest" },
                { "mixamorig:Spine2", "UpperChest" }, { "mixamorig:Neck", "Neck" }, { "mixamorig:Head", "Head" }
            };
            foreach (string side in new[] { "Left", "Right" })
            {
                string m = "mixamorig:" + side;
                map[m + "UpLeg"] = side + "UpperLeg";
                map[m + "Leg"] = side + "LowerLeg";
                map[m + "Foot"] = side + "Foot";
                map[m + "ToeBase"] = side + "Toes";
                map[m + "Shoulder"] = side + "Shoulder";
                map[m + "Arm"] = side + "UpperArm";
                map[m + "ForeArm"] = side + "LowerArm";
                map[m + "Hand"] = side + "Hand";
                foreach (string finger in Fingers)
                    for (int i = 0; i < 3; i++)
                        map[$"{m}Hand{(finger == "Little" ? "Pinky" : finger)}{i + 1}"] = $"{side} {finger} {Parts[i]}";
            }
            return map;
        }

        static Dictionary<string, string> RunnerBones()
        {
            var map = new Dictionary<string, string>
            {
                { "B-hips", "Hips" }, { "B-spine", "Spine" }, { "B-chest", "Chest" }, { "B-neck", "Neck" }, { "B-head", "Head" }
            };
            foreach (var (s, side) in new[] { ("L", "Left"), ("R", "Right") })
            {
                map[$"B-thigh.{s}"] = side + "UpperLeg";
                map[$"B-shin.{s}"] = side + "LowerLeg";
                map[$"B-foot.{s}"] = side + "Foot";
                map[$"B-toe.{s}"] = side + "Toes";
                map[$"B-shoulder.{s}"] = side + "Shoulder";
                map[$"B-upperArm.{s}"] = side + "UpperArm";
                map[$"B-forearm.{s}"] = side + "LowerArm";
                map[$"B-hand.{s}"] = side + "Hand";
                string[] names = { "thumb", "indexFinger", "middleFinger", "ringFinger", "pinky" };
                for (int f = 0; f < names.Length; f++)
                    for (int i = 0; i < 3; i++)
                        map[$"B-{names[f]}0{i + 1}.{s}"] = $"{side} {Fingers[f]} {Parts[i]}";
            }
            return map;
        }

        /// <summary>From the waist up: everything under B-hips but the legs, the jaw and the helper bones.</summary>
        static List<Transform> UpperBones(Transform root)
        {
            var bones = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("B-") || t.name == "B-spineProxy" || t.name == "B-jaw" || t.name.StartsWith("B-handProp")) continue;
                for (var p = t; p; p = p.parent)
                    if (p.name == "B-spine") { bones.Add(t); break; }
            }
            return bones;
        }
    }
}
