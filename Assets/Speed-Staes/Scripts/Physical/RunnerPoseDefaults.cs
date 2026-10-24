using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Starting values for the key poses, read off the reference videos. Used when a pose asset is
    /// missing and to fill new assets (Tools → Speed Stars → Create Default Poses). Holds (one foot =
    /// plane, both = dive) have no poses: RunnerPoseDriver builds them from the feet.
    /// </summary>
    public static class RunnerPoseDefaults
    {
        /// <summary>
        /// Waiting at the line: knees forward and a little out, trunk nearly upright, fists up at the chin,
        /// eyes ahead. How deep the runner squats is not here: it comes from the feet's Rest points (the body
        /// sinks until the feet standing on them reach the ground).
        /// </summary>
        public static PoseData Ready => new PoseData
        {
            pelvis = Vector2.zero,
            pelvisPitch = 18f,
            trunkPitch = 6f,
            width = 0.05f,
            left = new FootPose(),
            right = new FootPose(),
            leftArm = new ArmPose { swing = 40f, elbow = 130f },
            rightArm = new ArmPose { swing = 40f, elbow = 130f },
            armSwing = 0.4f,
        };

        /// <summary>Sprinting: tall, slight forward tilt, full arm swing; the head in line with the trunk.</summary>
        public static PoseData Run => new PoseData
        {
            pelvis = new Vector2(0.04f, -0.06f),
            pelvisPitch = 6f,
            trunkPitch = 6f,
            headDown = 1f,
            leftArm = new ArmPose { swing = 0f, elbow = 90f },
            rightArm = new ArmPose { swing = 0f, elbow = 90f },
            armSwing = 1f,
        };

        /// <summary>Mashing: low, pitched forward, short scrambling steps, arms flailing.</summary>
        public static PoseData Stumble => new PoseData
        {
            pelvis = new Vector2(0.05f, -0.2f),
            pelvisPitch = 15f,
            trunkPitch = 15f,
            headDown = 0.6f,
            leftArm = new ArmPose { swing = 20f, elbow = 80f },
            rightArm = new ArmPose { swing = 20f, elbow = 80f },
            armSwing = 1.3f,
        };
    }
}
