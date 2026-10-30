using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// One upper-body animation already moved onto the runner's own B-* bones (RunnerEmoteBaker): local rotations
    /// per frame. No Animator or avatar is needed at runtime, so the rig and the procedural run stay untouched.
    /// </summary>
    public sealed class RunnerEmoteData : ScriptableObject
    {
        [Tooltip("Frames per second the animation was sampled at.")]
        public float frameRate = 30f;
        [Tooltip("Names of the bones the animation drives, from the waist up.")]
        public string[] bones;
        [Tooltip("Local rotations: every bone of the first frame, then of the second, and so on.")]
        public Quaternion[] frames;

        public int FrameCount => bones == null || bones.Length == 0 ? 0 : frames.Length / bones.Length;
        public float Length => Mathf.Max(0, FrameCount - 1) / frameRate;
    }
}
