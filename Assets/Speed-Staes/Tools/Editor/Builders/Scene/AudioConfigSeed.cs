using System;
using SpeedStars.Core.Audio;
using UnityEditor;
using UnityEngine;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First creation of Data/Config/AudioConfig.asset (T-052). Every sound id gets the WAV that carries its name
    /// (Audio/Sfx/&lt;SfxId&gt;.wav: the real sound pack since T-120); volumes, repeat time and haptic numbers are the field
    /// defaults of AudioConfig, so nothing is written over them. No music clip: the music slot stays empty (silent) until
    /// real music exists. An existing asset is the user's and is never touched unless overwrite is asked for.
    /// <see cref="ApplySoundPack"/> is the one exception: it brings an existing asset up to the shipped sound list.
    /// </summary>
    internal static class AudioConfigSeed
    {
        private const string Source = nameof(AudioConfigSeed);

        public const string SfxFolder = "Assets/Speed-Staes/Audio/Sfx";

        public const string ConfigPath = "Assets/Speed-Staes/Data/Config/AudioConfig.asset";

        /// <summary>
        /// Batchmode entry (-executeMethod): every SfxId of the existing AudioConfig.asset gets its cue with the clip
        /// Audio/Sfx/&lt;SfxId&gt;.wav. A cue that exists keeps its volume (a new one gets 1); music, repeat time and haptic
        /// fields are not touched, and a second run changes nothing. A missing clip is an error (the run fails), never a silent cue.
        /// </summary>
        public static void ApplySoundPack()
        {
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            if (config == null) throw new InvalidOperationException($"{Source}: '{ConfigPath}' does not exist.");

            var so = new SerializedObject(config);
            SerializedProperty cues = Prop(so, "cues");
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                string clipPath = $"{SfxFolder}/{id}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null) throw new InvalidOperationException($"{Source}: clip '{clipPath}' is not imported; {id} would be silent.");

                SerializedProperty cue = FindCue(cues, id);
                if (cue == null)
                {
                    cues.arraySize++;
                    cue = cues.GetArrayElementAtIndex(cues.arraySize - 1);
                    Rel(cue, "id").enumValueIndex = (int)id; // SfxId has no explicit values: the index equals the value
                    Rel(cue, "volume").floatValue = 1f;
                }
                Rel(cue, "clip").objectReferenceValue = clip;
            }
            Apply(so);
            AssetDatabase.SaveAssets();
            Log(Source, $"Sound pack applied to {ConfigPath}.");
        }

        private static SerializedProperty FindCue(SerializedProperty cues, SfxId id)
        {
            for (int i = 0; i < cues.arraySize; i++)
            {
                SerializedProperty cue = cues.GetArrayElementAtIndex(i);
                if (Rel(cue, "id").enumValueIndex == (int)id) return cue;
            }
            return null;
        }

        public static AudioConfig Seed(string path, bool overwrite)
        {
            var config = CreateOrReset<AudioConfig>(path, overwrite);
            if (config == null) { Skipped(Source, path); return AssetDatabase.LoadAssetAtPath<AudioConfig>(path); }

            var so = new SerializedObject(config);
            SerializedProperty cues = Prop(so, "cues");
            Array ids = Enum.GetValues(typeof(SfxId));
            cues.arraySize = ids.Length;
            int index = 0;
            foreach (SfxId id in ids)
            {
                string clipPath = $"{SfxFolder}/{id}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null) Warn(Source, $"'{clipPath}' is not imported (run scripts/sfx-uret.py, then refresh); {id} stays silent.");

                SerializedProperty cue = cues.GetArrayElementAtIndex(index++);
                Rel(cue, "id").enumValueIndex = (int)id; // SfxId has no explicit values: the index equals the value
                Rel(cue, "clip").objectReferenceValue = clip;
                Rel(cue, "volume").floatValue = 1f;
            }
            Apply(so);
            Log(Source, $"Seeded {path} ({ids.Length} sounds, no music clip).");
            return config;
        }
    }
}
