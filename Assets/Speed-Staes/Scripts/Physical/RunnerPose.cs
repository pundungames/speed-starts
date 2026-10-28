using UnityEngine;

namespace SpeedStars.Physical
{
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
        [Tooltip("Pelvis offset from its rest place (x = forward, m). y is not used: the height comes from the leg angles.")]
        public Vector2 pelvis;
        [Tooltip("Pelvis tilt (deg, + = forward): the hips' base angle; the lean comes on top (RunnerPoseDriver → " +
                 "pelvisPitchRange). The legs do not turn with this base, only with the lean.")]
        public float pelvisPitch;

        [Header("Head")]
        [Tooltip("0 = the head looks ahead, 1 = it follows the trunk down.")]
        [Range(0f, 1f)] public float headDown;

        [Header("Feet")]
        [Tooltip("Feet (and knees) further apart sideways than at rest, each side (m). The legs themselves come " +
                 "from RunnerPoseDriver → Leg angles.")]
        public float width;

        [Header("Arms (rest only: all the way back is RunnerPoseDriver → armBack / elbowBack)")]
        [Tooltip("Left arm in this pose.")]
        public ArmPose leftArm;
        [Tooltip("Right arm in this pose.")]
        public ArmPose rightArm;

        public static PoseData Lerp(PoseData a, PoseData b, float t) => new PoseData
        {
            pelvis = Vector2.LerpUnclamped(a.pelvis, b.pelvis, t),
            pelvisPitch = Mathf.LerpUnclamped(a.pelvisPitch, b.pelvisPitch, t),
            headDown = Mathf.LerpUnclamped(a.headDown, b.headDown, t),
            width = Mathf.LerpUnclamped(a.width, b.width, t),
            leftArm = ArmPose.Lerp(a.leftArm, b.leftArm, t),
            rightArm = ArmPose.Lerp(a.rightArm, b.rightArm, t),
        };

        /// <summary>Left and right arms swapped.</summary>
        public PoseData Mirrored()
        {
            var m = this;
            m.leftArm = rightArm;
            m.rightArm = leftArm;
            return m;
        }
    }

    /// <summary>A key pose asset. Edit it in play mode with RunnerPoseDriver's preview: changes stay.</summary>
    [CreateAssetMenu(menuName = "Speed Stars/Runner Pose", fileName = "RunnerPose")]
    public sealed class RunnerPose : ScriptableObject
    {
        [Tooltip("The pose (a new asset starts from the Ready default).")]
        public PoseData pose = RunnerPoseDefaults.Ready;

        [ContextMenu("Mirror (swap left and right)")]
        void Mirror() => pose = pose.Mirrored();
    }
}
