using UnityEngine;

namespace SpeedStars.Physical
{
    [System.Serializable]
    public struct FootPose
    {
        [Tooltip("Ankle offset from its standing place (x = forward, y = up, m).")]
        public Vector2 ankle;
        [Tooltip("Foot angle (deg, + = heel up / toes pointing down).")]
        public float pitch;
        [Tooltip("Knee hint offset (x = forward, y = up, m).")]
        public Vector2 knee;
        [Tooltip("1 = this foot rides with the body lean (held back); 0 = planted, the body leans over it.")]
        [Range(0f, 1f)] public float withBody;

        public static FootPose Lerp(FootPose a, FootPose b, float t) => new FootPose
        {
            ankle = Vector2.LerpUnclamped(a.ankle, b.ankle, t),
            pitch = Mathf.LerpUnclamped(a.pitch, b.pitch, t),
            knee = Vector2.LerpUnclamped(a.knee, b.knee, t),
            withBody = Mathf.LerpUnclamped(a.withBody, b.withBody, t),
        };
    }

    [System.Serializable]
    public struct ArmPose
    {
        [Tooltip("Upper arm swing (deg, + = forward).")]
        public float swing;
        [Tooltip("Elbow bend (deg, 0 = straight).")]
        public float elbow;

        public static ArmPose Lerp(ArmPose a, ArmPose b, float t) => new ArmPose
        {
            swing = Mathf.LerpUnclamped(a.swing, b.swing, t),
            elbow = Mathf.LerpUnclamped(a.elbow, b.elbow, t),
        };
    }

    /// <summary>
    /// One key pose of the body, before the physical lean and hop are put on top. Everything is in
    /// runner space (x = forward, y = up) relative to the rig's rest pose, so poses blend linearly.
    /// </summary>
    [System.Serializable]
    public struct PoseData
    {
        [Header("Pelvis")]
        [Tooltip("Pelvis offset from its rest place (x = forward, y = up, m).")]
        public Vector2 pelvis;
        [Tooltip("Pelvis tilt (deg, + = forward).")]
        public float pelvisPitch;

        [Header("Trunk and head")]
        [Tooltip("Spine bend above the pelvis (deg, + = forward).")]
        public float trunkPitch;
        [Tooltip("0 = the head looks ahead, 1 = it follows the trunk down.")]
        [Range(0f, 1f)] public float headDown;

        [Header("Feet")]
        [Tooltip("Feet (and knees) further apart sideways than at rest, each side (m).")]
        public float width;
        public FootPose left;
        public FootPose right;

        [Header("Arms")]
        public ArmPose leftArm;
        public ArmPose rightArm;
        [Tooltip("How much the arms swing with the legs while running (0 = hold this arm pose).")]
        public float armSwing;

        public static PoseData Lerp(PoseData a, PoseData b, float t) => new PoseData
        {
            pelvis = Vector2.LerpUnclamped(a.pelvis, b.pelvis, t),
            pelvisPitch = Mathf.LerpUnclamped(a.pelvisPitch, b.pelvisPitch, t),
            trunkPitch = Mathf.LerpUnclamped(a.trunkPitch, b.trunkPitch, t),
            headDown = Mathf.LerpUnclamped(a.headDown, b.headDown, t),
            width = Mathf.LerpUnclamped(a.width, b.width, t),
            left = FootPose.Lerp(a.left, b.left, t),
            right = FootPose.Lerp(a.right, b.right, t),
            leftArm = ArmPose.Lerp(a.leftArm, b.leftArm, t),
            rightArm = ArmPose.Lerp(a.rightArm, b.rightArm, t),
            armSwing = Mathf.LerpUnclamped(a.armSwing, b.armSwing, t),
        };

        /// <summary>Left and right swapped (a left lunge becomes a right lunge).</summary>
        public PoseData Mirrored()
        {
            var m = this;
            m.left = right;
            m.right = left;
            m.leftArm = rightArm;
            m.rightArm = leftArm;
            return m;
        }
    }

    /// <summary>A key pose asset. Edit it in play mode with RunnerPoseDriver's preview: changes stay.</summary>
    [CreateAssetMenu(menuName = "Speed Stars/Runner Pose", fileName = "RunnerPose")]
    public sealed class RunnerPose : ScriptableObject
    {
        public PoseData pose = RunnerPoseDefaults.Ready;

        [ContextMenu("Mirror (swap left and right)")]
        void Mirror() => pose = pose.Mirrored();
    }
}
