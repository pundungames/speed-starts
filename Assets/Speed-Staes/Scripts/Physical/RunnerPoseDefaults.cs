using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Starting values for the key poses, read off the reference videos. Used when a pose asset is
    /// missing and to fill new assets (Tools → Speed Stars → Create Default Poses). Lunge and dive are
    /// upright shapes: the physical lean (40° / 75°) tips them over the planted feet.
    /// </summary>
    public static class RunnerPoseDefaults
    {
        /// <summary>
        /// Waiting at the line (the runner's home pose: no input = exactly this): a deep squat, sitting
        /// back — pelvis low and behind the feet, knees forward and a little out, feet side by side under
        /// the knees, trunk nearly upright, fists up at the chin, eyes ahead.
        /// </summary>
        public static PoseData Ready => new PoseData
        {
            pelvis = new Vector2(-0.08f, -0.27f),
            pelvisPitch = 18f,
            trunkPitch = 6f,
            width = 0.05f,
            left = new FootPose(),
            right = new FootPose(),
            leftArm = new ArmPose { swing = 40f, elbow = 130f },
            rightArm = new ArmPose { swing = 40f, elbow = 130f },
            armSwing = 0.4f,
        };

        /// <summary>Sprinting: tall, slight forward tilt, full arm swing.</summary>
        public static PoseData Run => new PoseData
        {
            pelvis = new Vector2(0.04f, -0.06f),
            pelvisPitch = 6f,
            trunkPitch = 6f,
            leftArm = new ArmPose { swing = 0f, elbow = 90f },
            rightArm = new ArmPose { swing = 0f, elbow = 90f },
            armSwing = 1f,
        };

        /// <summary>Left foot held back: right foot planted ahead, deep support knee.</summary>
        public static PoseData LungeLeft => new PoseData
        {
            pelvis = new Vector2(0f, -0.28f),
            pelvisPitch = 0f,
            trunkPitch = 5f,
            headDown = 0.8f,
            left = new FootPose { ankle = new Vector2(-0.5f, 0.05f), pitch = 70f, withBody = 1f },
            right = new FootPose { ankle = new Vector2(0.3f, 0f), knee = new Vector2(0.1f, 0f) },
            leftArm = new ArmPose { swing = 40f, elbow = 100f },
            rightArm = new ArmPose { swing = -30f, elbow = 60f },
            armSwing = 0f,
        };

        public static PoseData LungeRight => LungeLeft.Mirrored();

        /// <summary>Both held: a straight plank from the toes; the 75° lean lays it down.</summary>
        public static PoseData Dive => new PoseData
        {
            pelvis = new Vector2(-0.02f, -0.02f),
            pelvisPitch = 0f,
            trunkPitch = 5f,
            headDown = 1f,
            left = new FootPose { ankle = new Vector2(-0.03f, 0f), pitch = 50f, withBody = 1f },
            right = new FootPose { ankle = new Vector2(-0.03f, 0f), pitch = 50f, withBody = 1f },
            leftArm = new ArmPose { swing = -20f, elbow = 70f },
            rightArm = new ArmPose { swing = -20f, elbow = 70f },
            armSwing = 0f,
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
