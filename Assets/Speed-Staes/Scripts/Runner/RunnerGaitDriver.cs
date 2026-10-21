using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Visual only: reads the logical leg cycles and writes Control Rig IK targets.
    /// Never changes speed or timing.
    ///
    /// Each foot is placed by its ball point (toe joint) using two editable step points
    /// (children of the runner, positions in runner space):
    ///   Front – where the reaching foot lands
    ///   Back  – where the driving foot pushes off
    ///   Mid   – between them (midAlong): both feet together under the body, waiting on the ground
    /// One press = one stride: the driving foot pushes Mid → Back on the ground, then swings back to Mid;
    /// the reaching foot swings Mid → Front, lands on the heel and comes back to Mid under the body.
    /// The pelvis runs crouched (runHeight) so the knees stay bent, lowest with the feet together and
    /// highest at the open split mid-stride.
    /// The body only pitches around X; Y and Z rotations stay 0. Lean follows how far back the feet
    /// are; leaning forward the pelvis rolls over the feet midpoint, leaning back it stays over the feet (see Update).
    /// Hold poses (plane / prone, see FinishLeg / UpdateSupport) are kept but have no trigger in the
    /// stride model: holding a key does nothing.
    /// Feet: leg IK places the ankle only; the foot angle is applied after IK relative to the shin and
    /// clamped to the ankle's range (see PoseAnkle).
    /// Arms: hand IK is off; each arm is a pendulum from the shoulder joint (X only) with an elbow
    /// bend and a small shoulder (clavicle) follow, driven crosswise by the opposite foot
    /// (right foot → left arm, left foot → right arm).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RunnerGaitDriver : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Leg
        {
            [Header("Rig")]
            public Transform target;
            public Transform hint;
            public Transform toeBone;
            [Tooltip("Found by name if empty (B-shin.L/R, B-foot.L/R).")]
            public Transform shinBone;
            public Transform footBone;
            [Header("Step points (ball of foot)")]
            public Transform front;
            public Transform back;

            [HideInInspector] public Vector3 restAnkle;
            [HideInInspector] public Vector3 ballOffset;
            [HideInInspector] public Quaternion restRot;
            [HideInInspector] public Quaternion restFootInShin;  // flat standing foot relative to the shin
            [HideInInspector] public Quaternion restFootRunner;  // flat standing foot in runner space
            [HideInInspector] public Vector3 toeInFoot;          // ankle → ball in foot space
            [HideInInspector] public LegState lastState;
            [HideInInspector] public double lastStateTime;
            [HideInInspector] public Vector3 fromBall;
            [HideInInspector] public float fromPitch;
            [HideInInspector] public Vector3 ball;        // final (after hold poses), written to IK
            [HideInInspector] public float pitch;
            [HideInInspector] public Vector3 baseBall;    // from the step cycle only
            [HideInInspector] public float basePitch;
            [HideInInspector] public float holdW;         // smoothed "held back" weight
            [HideInInspector] public float supportW;      // this foot carries the body (other foot held)
            [HideInInspector] public bool supportLocked;
            [HideInInspector] public Vector3 supportWorld;
            [HideInInspector] public Vector3 supportLocal;
            [HideInInspector] public Quaternion restToeInFoot;   // toe relative to the foot at rest
            [HideInInspector] public Vector3 restHingeInThigh;   // knee hinge axis (runner right at rest), thigh space
            [HideInInspector] public Vector3 restHingeInShin;    // same, shin space
            [HideInInspector] public float toeRoll;              // toes rolling over their tips (deg, end of stance)
            [HideInInspector] public float swingAnkle;           // ankle bend relative to the shin in the air (deg)
            [HideInInspector] public float swingW;               // 1 = ankle follows swingAnkle, 0 = foot pitch
            [HideInInspector] public float swingToe;             // toe bend in the air (deg, + = toes up)
            public Vector3 RestBall => restAnkle + ballOffset;
        }

        /// <summary>
        /// Foot motion over one gait cycle, as a function of phase (0..1).
        /// Stance = Step (front → back on the ground), swing = Return (back → front in the air).
        /// Heel-strike running: the foot lands toes up on the heel and rolls down flat (Ready), stays flat
        /// while loaded, then the heel rises late and fast and the toes roll over their tips at toe-off;
        /// in the air the ankle plantarflexes after toe-off (heel kicks up), then pulls the toes up for the
        /// next heel strike.
        /// </summary>
        [System.Serializable]
        public sealed class FootProfile
        {
            [Tooltip("Foot angle at heel strike (deg, − = toes up, heel first).")]
            public float touchdownPitch = -12f;
            [Tooltip("Stance: foot angle to the ground (deg, + = heel up). 0 = push starts (foot flat), 1 = toe-off.")]
            public AnimationCurve stancePitch;
            [Tooltip("Stance: toes roll over their tips, lifting the ball (deg).")]
            public AnimationCurve toeRoll;
            [Tooltip("Swing: ankle bend relative to the shin (deg, + = toes down / plantarflexion).")]
            public AnimationCurve swingAnkle;
            [Tooltip("Swing: toe bend relative to the foot (deg, + = toes up).")]
            public AnimationCurve swingToe;
        }

        static AnimationCurve Curve(params float[] timeValue)
        {
            var keys = new Keyframe[timeValue.Length / 2];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(timeValue[2 * i], timeValue[2 * i + 1]);
            var c = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) c.SmoothTangents(i, 0f);
            return c;
        }

        [System.Serializable]
        public sealed class Arm
        {
            [Header("Bones (found by name if empty)")]
            public Transform upperArm;
            public Transform forearm;
            [Header("IK (turned off at runtime)")]
            public Transform target;
            public Transform hint;

            [HideInInspector] public Transform shoulder;     // upper arm parent (clavicle)
            [HideInInspector] public Quaternion restShoulderLocal;
            [HideInInspector] public Quaternion chestRest;  // shoulder parent in runner space
            [HideInInspector] public Quaternion restUpperLocal;
            [HideInInspector] public Quaternion restForeInParent;
            [HideInInspector] public Quaternion parentRest; // shoulder (upper arm parent) in runner space
            [HideInInspector] public Vector3 restDir;       // runner space: rest (T-pose) arm direction
            [HideInInspector] public float side;
        }

        public RunnerStepController controller;
        public RunnerMovement movement;

        [Header("Rig References")]
        public Transform hipTarget;
        public Transform spineRootTarget;
        public Transform spineTipTarget;
        public Leg leftLeg = new Leg();
        public Leg rightLeg = new Leg();
        public Arm leftArm = new Arm();
        public Arm rightArm = new Arm();

        [Header("Stride")]
        [Tooltip("Mid (feet together under the body) along Front (0) → Back (1).")]
        [Range(0f, 1f)] public float midAlong = 0.5f;
        [Tooltip("Stride phase at which the driving foot leaves the ground and the reaching foot lands.")]
        [Range(0.2f, 0.8f)] public float strideSplit = 0.5f;

        [Header("Foot motion (blended by speed: acceleration → max velocity)")]
        public FootProfile accelProfile = new FootProfile
        {
            touchdownPitch = -8f,
            stancePitch = Curve(0f, 0f, 0.35f, 3f, 0.6f, 14f, 0.8f, 32f, 1f, 52f),
            toeRoll = Curve(0f, 0f, 0.7f, 0f, 1f, 22f),
            swingAnkle = Curve(0f, 24f, 0.15f, 28f, 0.4f, 14f, 0.65f, -4f, 0.85f, -12f, 1f, -10f),
            swingToe = Curve(0f, 30f, 0.3f, 8f, 0.7f, 14f, 1f, 12f),
        };
        public FootProfile maxSpeedProfile = new FootProfile
        {
            touchdownPitch = -12f,
            stancePitch = Curve(0f, 0f, 0.4f, 2f, 0.6f, 9f, 0.8f, 26f, 1f, 48f),
            toeRoll = Curve(0f, 0f, 0.75f, 0f, 1f, 18f),
            swingAnkle = Curve(0f, 22f, 0.12f, 28f, 0.35f, 16f, 0.6f, -2f, 0.85f, -14f, 1f, -12f),
            swingToe = Curve(0f, 28f, 0.25f, 8f, 0.6f, 14f, 1f, 14f),
        };
        [Tooltip("Speed (m/s) at which the acceleration profile is fully used.")]
        public float accelProfileSpeed = 2f;
        [Tooltip("Speed (m/s) at which the max-velocity profile is fully used.")]
        public float maxProfileSpeed = 9f;

        [Header("Toes")]
        [Tooltip("Toe joint (ball) → toe tip (m).")]
        public float toeLength = 0.06f;
        [Tooltip("Max toes-up bend at the ball joint (deg).")]
        public float toeUpMax = 55f;
        [Tooltip("Max toes-down bend at the ball joint (deg).")]
        public float toeDownMax = 15f;
        [Header("Foot loop (skewed ellipse through Front and Back)")]
        [Tooltip("Arc height above / depth below the Front–Back line (m). The return travels the upper arc, " +
                 "the step its mirror below. The lower arc is cut at the ground: that flat part is ground contact.")]
        public float loopHeight = 0.35f;
        [Tooltip("1 = symmetric arc. < 1 peaks earlier (heel kicks up first), > 1 peaks later.")]
        [Range(0.3f, 2f)] public float loopSkew = 0.7f;
        public Vector3 kneeHintOffset = new Vector3(0f, -0.35f, 0.6f);
        [Tooltip("Blend time into the Front pose when the runner stops (s).")]
        public float groundBlendTime = 0.25f;

        [Header("Ankle (relative to the shin)")]
        [Tooltip("Max toes-up bend of the ankle (dorsiflexion, deg).")]
        public float ankleUpMax = 20f;
        [Tooltip("Max toes-down bend of the ankle (plantarflexion, deg).")]
        public float ankleDownMax = 50f;

        [Header("Pelvis")]
        [Tooltip("Running pelvis height as a fraction of the standing height above the ground; < 1 keeps the knees bent " +
                 "so both feet reach the ground at Front and Back.")]
        [Range(0.6f, 1f)] public float runHeight = 0.82f;
        [Tooltip("Pelvis rise at the open split mid-stride over the feet-together height (m).")]
        public float bobAmplitude = 0.03f;
        public float bobSharpness = 14f;
        public float pelvisForward = 0.04f;
        public float pelvisTilt = 6f;

        [Header("Lean (X rotation only, + forward / − back)")]
        [Tooltip("X: how far back the feet are (0 = both at Front, 0.5 = one front one back, 1 = back). " +
                 "A foot held at Back counts as 1. Y: body lean (deg).")]
        public AnimationCurve leanByFootBack = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0f),
            new Keyframe(1f, 85f));
        public float maxForwardLean = 85f;
        public float maxBackLean = 35f;
        [Tooltip("Lean is this many times slower than a stride (time constant = factor × strideDuration), " +
                 "so the other foot's quick moves do not shake the body.")]
        [Range(1f, 4f)] public float leanLag = 1.75f;

        [Header("Hold poses (one foot held = plane, both held = prone)")]
        [Tooltip("Held leg reaches back straight along the trunk line; length as a fraction of the rest hip→ankle distance.")]
        [Range(0.7f, 1f)] public float heldLegReach = 0.95f;
        [Tooltip("Extra toe point of a held, straight leg (deg).")]
        public float heldToePoint = 0f;
        [Tooltip("Plane pose: pelvis height above the ground at full lean (m); the support knee bends to reach it.")]
        public float planeHipHeight = 0.5f;
        [Tooltip("Plane pose: the support foot ends up this far ahead of the pelvis, under the chest (m).")]
        public float supportAheadOfHip = 0.3f;
        [Tooltip("A planted support foot may fall this far behind the pelvis before it slides along (m).")]
        public float supportMaxBehind = 0.4f;
        [Tooltip("Support knee follows the trunk lean this much (0 = knee points forward, 1 = turns with the trunk).")]
        [Range(0f, 1f)] public float supportKneeFollow = 0.3f;
        [Tooltip("Prone pose: trunk angle (deg, 90 = flat).")]
        public float proneLean = 88f;
        [Tooltip("Prone pose: pelvis height above the ground (m).")]
        public float proneHipHeight = 0.15f;
        [Tooltip("Prone pose: the body dives this far forward (m).")]
        public float proneForward = 0.4f;

        [Header("Arms (pendulum, X only; + = forward)")]
        [Tooltip("Upper arm swing when the controlling foot is at Front (deg).")]
        public float armSwingFront = 45f;
        [Tooltip("Upper arm swing when the controlling foot is at Back (deg).")]
        public float armSwingBack = -40f;
        [Tooltip("Elbow bend at the front of the swing (deg, 0 = straight).")]
        public float elbowFront = 100f;
        [Tooltip("Elbow bend at the back of the swing (deg).")]
        public float elbowBack = 70f;
        [Tooltip("Elbow bend while standing (deg).")]
        public float elbowStanding = 15f;
        [Tooltip("Hanging arm tilted out from the body (deg).")]
        public float armOutward = 8f;
        [Tooltip("Shoulder (clavicle) comes forward with the arm (deg, at full forward swing).")]
        public float shoulderSwing = 10f;
        [Tooltip("Shoulder (clavicle) pulls back with the arm (deg, at full back swing).")]
        public float shoulderSwingBack = 22f;
        [Tooltip("Shoulder lifts when the arm is forward (deg, at full forward swing).")]
        public float shoulderLift = 4f;
        [Tooltip("Shoulder lifts as it pulls back with the arm (deg, at full back swing).")]
        public float shoulderLiftBack = 3f;

        [Header("Hands (pose comes from the scene)")]
        [Tooltip("0 = the hand and finger pose set up on the bones in the scene (running fist). " +
                 "1 = flat hand: fingers straight (model bind pose), palm facing the ground. For the start pose.")]
        [Range(0f, 1f)] public float flatHand = 0f;

        sealed class HandRig
        {
            public Transform hand;
            public Transform[] fingers;
            public Quaternion[] flatLocal;   // straight fingers, from the mesh bind pose
            public Vector3 palmInHand;       // palm normal in hand space (palm faces down in the bind T-pose)
        }

        HandRig[] _hands;

        // Anatomy of the lean (fixed; see Update).
        const float PelvisShareForward = 0.5f;
        const float PelvisShareBack = 0.25f;
        const float BackLeanHipForward = 0.2f;
        // Heel strike (see ComputeLeg).
        const float HeelBehindAnkle = 0.05f;   // heel contact point behind the ankle joint (m)
        const float HeelRollTime = 0.1f;       // heel strike → foot flat (s)
        // Mid is part-way through a full Front → Back stance: the drive picks the stance profile up here.
        const float DriveStanceFrom = 0.4f;

        Vector3 _restHip;
        Quaternion _restHipRot, _restSpineRoot, _restSpineTip;
        float _bob, _lean, _bodyLean, _run, _midZ, _restMidZ, _wBoth, _legReach, _footRestAngle, _profileW;
        MultiAimConstraint _headAim;
        bool _bound;

        /// <summary>Current trunk lean (deg, + forward / − back), including hold poses.</summary>
        public float Lean => _bodyLean;

        Vector3 Local(Transform t) => transform.InverseTransformPoint(t.position);

        /// <summary>Ground height of the ball of the foot (runner space): where it sits when standing.</summary>
        float GroundY(Leg leg) => _bound ? leg.RestBall.y : leg.toeBone ? Local(leg.toeBone).y : 0f;

        void Awake() => Bind();

        void Bind()
        {
            if (_bound) return;
            // Constraints run without maintain-offset, so targets carry rest rotations.
            _restHip = Local(hipTarget);
            _restHipRot = Quaternion.Inverse(transform.rotation) * hipTarget.rotation;
            _restSpineRoot = Quaternion.Inverse(transform.rotation) * spineRootTarget.rotation;
            _restSpineTip = Quaternion.Inverse(transform.rotation) * spineTipTarget.rotation;
            BindLeg(leftLeg, "L");
            BindLeg(rightLeg, "R");
            _restMidZ = _midZ = (leftLeg.RestBall.z + rightLeg.RestBall.z) * 0.5f;
            _legReach = _restHip.y - leftLeg.restAnkle.y;
            // Angle of the rest foot (ankle → ball) below the forward horizontal.
            _footRestAngle = Mathf.Atan2(-leftLeg.ballOffset.y, leftLeg.ballOffset.z) * Mathf.Rad2Deg;
            var aimRig = FindDeep(transform, "Head Aim Rig");
            if (aimRig) _headAim = aimRig.GetComponent<MultiAimConstraint>();
            BindArm(leftArm, -1f, "B-upperArm.L", "B-forearm.L");
            BindArm(rightArm, 1f, "B-upperArm.R", "B-forearm.R");
            _hands = new[] { BindHand("L"), BindHand("R") };
            _bound = true;
        }

        /// <summary>
        /// The running hand is whatever pose the hand/finger bones have in the scene (the Animator keeps it
        /// every frame). Straight fingers are read from the skinned mesh bind pose, so bending the fingers in
        /// the scene does not lose the flat hand.
        /// </summary>
        HandRig BindHand(string side)
        {
            var hand = FindDeep(transform, "B-hand." + side);
            if (!hand) return null;

            SkinnedMeshRenderer skin = null;
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.sharedMesh && System.Array.IndexOf(smr.bones, hand) >= 0) { skin = smr; break; }

            var fingers = new System.Collections.Generic.List<Transform>();
            foreach (var t in hand.GetComponentsInChildren<Transform>(true))
                if (t != hand && !t.name.Contains("Prop")) fingers.Add(t);

            var rig = new HandRig { hand = hand, fingers = fingers.ToArray(), flatLocal = new Quaternion[fingers.Count] };
            Quaternion handBind = BindRotation(skin, hand, hand.rotation);
            rig.palmInHand = Quaternion.Inverse(handBind) * -transform.up;
            for (int i = 0; i < fingers.Count; i++)
            {
                var f = fingers[i];
                rig.flatLocal[i] = Quaternion.Inverse(BindRotation(skin, f.parent, f.parent.rotation)) * BindRotation(skin, f, f.rotation);
            }
            return rig;
        }

        /// <summary>World rotation a bone had when the mesh was bound (falls back to its current rotation).</summary>
        static Quaternion BindRotation(SkinnedMeshRenderer skin, Transform bone, Quaternion fallback)
        {
            if (!skin) return fallback;
            int i = System.Array.IndexOf(skin.bones, bone);
            if (i < 0 || i >= skin.sharedMesh.bindposes.Length) return fallback;
            return (skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[i].inverse).rotation;
        }

        void BindArm(Arm arm, float side, string upperName, string foreName)
        {
            if (!arm.upperArm) arm.upperArm = FindDeep(transform, upperName);
            if (!arm.forearm) arm.forearm = FindDeep(transform, foreName);
            if (arm.target)
            {
                var ik = arm.target.GetComponentInParent<TwoBoneIKConstraint>();
                if (ik) ik.weight = 0f;
            }
            if (!arm.upperArm || !arm.forearm) return;

            Quaternion toRunner = Quaternion.Inverse(transform.rotation);
            Transform parent = arm.upperArm.parent;
            // Only a real clavicle bone is moved; never the chest.
            arm.shoulder = parent.name.ToLowerInvariant().Contains("shoulder") ? parent : null;
            if (arm.shoulder)
            {
                arm.restShoulderLocal = parent.localRotation;
                arm.chestRest = toRunner * parent.parent.rotation;
            }
            arm.restUpperLocal = arm.upperArm.localRotation;
            arm.restForeInParent = Quaternion.Inverse(parent.rotation) * arm.forearm.rotation;
            arm.parentRest = toRunner * parent.rotation;

            arm.restDir = (Local(arm.forearm) - Local(arm.upperArm)).normalized;
            arm.side = side;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        void BindLeg(Leg leg, string side)
        {
            leg.restAnkle = Local(leg.target);
            leg.ballOffset = Local(leg.toeBone) - leg.restAnkle;
            leg.restRot = Quaternion.Inverse(transform.rotation) * leg.target.rotation;

            // IK places the ankle only; the foot angle is applied afterwards relative to the shin.
            var ik = leg.target.GetComponentInParent<TwoBoneIKConstraint>();
            if (ik) ik.data.targetRotationWeight = 0f;
            if (!leg.shinBone) leg.shinBone = FindDeep(transform, "B-shin." + side);
            if (!leg.footBone) leg.footBone = FindDeep(transform, "B-foot." + side);
            if (leg.shinBone && leg.footBone)
            {
                leg.restFootInShin = Quaternion.Inverse(leg.shinBone.rotation) * leg.footBone.rotation;
                leg.restFootRunner = Quaternion.Inverse(transform.rotation) * leg.footBone.rotation;
                leg.toeInFoot = Quaternion.Inverse(leg.footBone.rotation) * (leg.toeBone.position - leg.footBone.position);
                leg.restToeInFoot = Quaternion.Inverse(leg.footBone.rotation) * leg.toeBone.rotation;
                leg.restHingeInThigh = Quaternion.Inverse(leg.shinBone.parent.rotation) * transform.right;
                leg.restHingeInShin = Quaternion.Inverse(leg.shinBone.rotation) * transform.right;
            }
            leg.ball = leg.fromBall = leg.baseBall = leg.RestBall;
            leg.lastState = LegState.Grounded;
        }

        void Update()
        {
            Bind();
            double now = Time.timeAsDouble;
            float dt = Time.deltaTime;
            // Stopped is not a separate pose: the runner stays in the Front pose (feet and arms).
            const float run = 1f;
            float profileTarget = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(accelProfileSpeed, maxProfileSpeed, movement.Speed));
            _profileW = Mathf.Lerp(_profileW, profileTarget, 1f - Mathf.Exp(-4f * dt));

            ComputeLeg(0, leftLeg, now);
            ComputeLeg(1, rightLeg, now);

            // Pelvis height: lowest with the feet together, rising to the open split mid-stride.
            var stride = controller.Legs[0];
            bool striding = stride.State == LegState.Drive || stride.State == LegState.Reach;
            float bobTarget = striding ? bobAmplitude * Mathf.Sin(Mathf.PI * stride.Phase(now, controller.strideDuration)) : 0f;
            _bob = Mathf.Lerp(_bob, bobTarget, 1f - Mathf.Exp(-bobSharpness * dt));

            UpdateLean(dt);

            // Hold poses: one foot held = plane (other foot supports), both held = prone.
            float hw0 = leftLeg.holdW, hw1 = rightLeg.holdW;
            leftLeg.supportW = hw1 * (1f - hw0);
            rightLeg.supportW = hw0 * (1f - hw1);
            float wOne = leftLeg.supportW + rightLeg.supportW;
            _wBoth = hw0 * hw1;
            _bodyLean = Mathf.Lerp(_lean, proneLean, _wBoth);
            float leanT = Mathf.Clamp01(_bodyLean / Mathf.Max(1f, maxForwardLean));

            // Running: roll the pelvis over the feet midpoint so feet → pelvis → trunk stay one line.
            Quaternion rootRot = transform.rotation;
            float ground = (GroundY(leftLeg) + GroundY(rightLeg)) * 0.5f;
            Vector3 hip0 = new Vector3(_restHip.x, ground + (_restHip.y - ground) * runHeight + _bob,
                _restHip.z + pelvisForward * run);
            float radius = hip0.y - ground;
            float pivotZ = _midZ + (hip0.z - _restMidZ);
            // Forward lean: the pelvis rolls over the feet, feet → pelvis → trunk stay one line.
            // Back lean: as in a real body the pelvis stays over the feet and pushes slightly forward while
            // the trunk tilts back above it, so the lower back bends inward, never outward.
            float fwd = Mathf.Max(0f, _bodyLean) * Mathf.Deg2Rad;
            float back = Mathf.Max(0f, -_bodyLean) * Mathf.Deg2Rad;
            Vector3 hip = new Vector3(hip0.x, ground + radius * Mathf.Cos(fwd),
                pivotZ + radius * Mathf.Sin(fwd) + BackLeanHipForward * radius * Mathf.Sin(back));

            // Plane: the body comes forward over the planted support foot, support knee bends deep.
            UpdateSupport(leftLeg, ground);
            UpdateSupport(rightLeg, ground);
            if (wOne > 0.001f)
            {
                float supportZ = (leftLeg.supportLocal.z * leftLeg.supportW + rightLeg.supportLocal.z * rightLeg.supportW) / wOne;
                Vector3 hipPlane = new Vector3(hip0.x, ground + Mathf.Lerp(radius, planeHipHeight, leanT), supportZ - supportAheadOfHip * leanT);
                hip = Vector3.Lerp(hip, hipPlane, Mathf.Clamp01(wOne));
            }
            // Prone: dive forward and lie down.
            Vector3 hipProne = new Vector3(hip0.x, ground + proneHipHeight, pivotZ + proneForward);
            hip = Vector3.Lerp(hip, hipProne, _wBoth);

            hipTarget.position = transform.TransformPoint(hip);
            // The pelvis takes part of the lean, the spine the rest; leaning back the pelvis tilts less
            // than the trunk (lumbar extension).
            float pelvisLean = _bodyLean * (_bodyLean >= 0f ? PelvisShareForward : PelvisShareBack);
            hipTarget.rotation = rootRot * Quaternion.Euler(pelvisTilt * run + pelvisLean, 0f, 0f) * _restHipRot;
            spineRootTarget.rotation = rootRot * Quaternion.Euler(Mathf.Lerp(pelvisLean, _bodyLean, 0.6f), 0f, 0f) * _restSpineRoot;
            spineTipTarget.rotation = rootRot * Quaternion.Euler(_bodyLean, 0f, 0f) * _restSpineTip;

            // Head: looks ahead when upright, follows the trunk down to the ground at full forward lean.
            if (_headAim) _headAim.weight = 1f - leanT;

            FinishLeg(leftLeg, hip, ground, leanT);
            FinishLeg(rightLeg, hip, ground, leanT);
            _run = run;
        }

        /// <summary>
        /// Smoothed per-leg "held back" weight; same lag as the lean. The stride model has no hold,
        /// so the plane / prone poses fade out and stay off until something triggers them again.
        /// </summary>
        void UpdateHoldWeights(float k)
        {
            leftLeg.holdW = Mathf.Lerp(leftLeg.holdW, 0f, k);
            rightLeg.holdW = Mathf.Lerp(rightLeg.holdW, 0f, k);
        }

        /// <summary>A support foot is planted in world space where it was when the other foot got held.</summary>
        void UpdateSupport(Leg leg, float ground)
        {
            if (leg.supportW < 0.01f)
            {
                leg.supportLocked = false;
                return;
            }
            if (!leg.supportLocked)
            {
                leg.supportWorld = transform.TransformPoint(new Vector3(leg.ball.x, ground, leg.ball.z));
                leg.supportLocked = true;
            }
            Vector3 l = transform.InverseTransformPoint(leg.supportWorld);
            if (l.z < -supportMaxBehind)
            {
                l.z = -supportMaxBehind;
                leg.supportWorld = transform.TransformPoint(l);
            }
            l.y = ground;
            leg.supportLocal = l;
        }

        /// <summary>
        /// Final foot: the step-cycle pose, pulled to the planted support point (plane) and/or stretched
        /// straight back along the trunk line (held leg; both legs when prone). Writes IK target and knee hint.
        /// </summary>
        void FinishLeg(Leg leg, Vector3 hip, float ground, float leanT)
        {
            Vector3 ball = leg.baseBall;
            float pitch = leg.basePitch;

            if (leg.supportW > 0f)
            {
                ball = Vector3.Lerp(ball, leg.supportLocal, leg.supportW);
                pitch = Mathf.Lerp(pitch, 0f, leg.supportW);
            }

            float lineW = leg.holdW * leanT;
            if (lineW > 0f)
            {
                float l = _bodyLean * Mathf.Deg2Rad;
                Vector3 ankle = new Vector3(leg.restAnkle.x, hip.y, hip.z)
                    + new Vector3(0f, -Mathf.Cos(l), -Mathf.Sin(l)) * (_legReach * heldLegReach);
                float linePitch = 90f + _bodyLean - _footRestAngle + heldToePoint;
                Vector3 lineBall = ankle + Quaternion.Euler(linePitch, 0f, 0f) * leg.ballOffset;
                ball = Vector3.Lerp(ball, lineBall, lineW);
                pitch = Mathf.Lerp(pitch, linePitch, lineW);
            }

            if (leg.lastState != LegState.Grounded) ball.y = Mathf.Max(ball.y, ground);

            leg.ball = ball;
            leg.pitch = pitch;
            Quaternion footRot = Quaternion.Euler(pitch, 0f, 0f);
            leg.target.position = transform.TransformPoint(ball - footRot * leg.ballOffset);
            leg.target.rotation = transform.rotation * footRot * leg.restRot;

            // Knee bends with the trunk (a support knee mostly keeps pointing forward).
            float kneeAngle = _bodyLean * Mathf.Lerp(1f, supportKneeFollow, leg.supportW);
            leg.hint.position = transform.TransformPoint(new Vector3(leg.RestBall.x, hip.y, hip.z)
                + Quaternion.Euler(kneeAngle, 0f, 0f) * kneeHintOffset);
        }

        // Arms are posed after the Animator/rig has written the skeleton this frame.
        void LateUpdate()
        {
            if (!_bound) return;
            PoseArm(leftArm, rightLeg);  // right foot drives the left arm
            PoseArm(rightArm, leftLeg);  // left foot drives the right arm
            FixLegTwist(leftLeg);
            FixLegTwist(rightLeg);
            PoseAnkle(leftLeg);
            PoseAnkle(rightLeg);
            PoseToes(leftLeg);
            PoseToes(rightLeg);
            if (_hands != null) foreach (var h in _hands) PoseHand(h);
        }

        /// <summary>Blend from the scene hand pose to a flat hand with the palm on the ground (flatHand).</summary>
        void PoseHand(HandRig h)
        {
            if (h == null || flatHand <= 0f) return;
            Vector3 palm = h.hand.rotation * h.palmInHand;
            Quaternion palmDown = Quaternion.FromToRotation(palm, -transform.up) * h.hand.rotation;
            h.hand.rotation = Quaternion.Slerp(h.hand.rotation, palmDown, flatHand);
            for (int i = 0; i < h.fingers.Length; i++)
                h.fingers[i].localRotation = Quaternion.Slerp(h.fingers[i].localRotation, h.flatLocal[i], flatHand);
        }

        /// <summary>
        /// Leg IK gets the knee and ankle where they belong but spins the thigh and shin about their own
        /// length, turning the knee hinge (and with it the foot) inward. Spin both back so the hinge lies
        /// across the leg's bend plane, as a real knee does; hip, knee and ankle positions stay put.
        /// </summary>
        void FixLegTwist(Leg leg)
        {
            if (!leg.shinBone || !leg.footBone) return;

            Transform thigh = leg.shinBone.parent, shin = leg.shinBone;
            Vector3 hip = thigh.position, knee = shin.position, ankle = leg.footBone.position;
            Vector3 hinge = Vector3.Cross(knee - hip, ankle - knee);
            if (hinge.sqrMagnitude < 1e-6f) hinge = transform.right; // straight leg: no bend plane
            else if (Vector3.Dot(hinge, transform.right) < 0f) hinge = -hinge;

            Untwist(thigh, knee - hip, thigh.rotation * leg.restHingeInThigh, hinge);
            // Spinning the thigh swung the shin: point it back at the ankle, then untwist it too.
            shin.rotation = Quaternion.FromToRotation(leg.footBone.position - knee, ankle - knee) * shin.rotation;
            Untwist(shin, ankle - knee, shin.rotation * leg.restHingeInShin, hinge);
        }

        static void Untwist(Transform bone, Vector3 axis, Vector3 hinge, Vector3 wanted)
        {
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(hinge, axis), Vector3.ProjectOnPlane(wanted, axis), axis);
            bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
        }

        /// <summary>
        /// Ankle after IK: the wanted foot pitch (flat on the ground, curve in the air, stretched when held)
        /// is turned into a bend relative to the shin and clamped to the ankle's range, so the foot can
        /// never point somewhere a real ankle cannot. In the air the bend comes from the swing curve instead
        /// (relative to the shin, as a real ankle moves), blended by swingW.
        /// </summary>
        void PoseAnkle(Leg leg)
        {
            if (!leg.shinBone || !leg.footBone) return;

            Vector3 axis = transform.right;
            Quaternion neutral = leg.shinBone.rotation * leg.restFootInShin;
            Quaternion wanted = transform.rotation * Quaternion.Euler(leg.pitch, 0f, 0f) * leg.restFootRunner;
            Vector3 from = Vector3.ProjectOnPlane(neutral * leg.toeInFoot, axis);
            Vector3 to = Vector3.ProjectOnPlane(wanted * leg.toeInFoot, axis);
            float bend = Mathf.Lerp(Vector3.SignedAngle(from, to, axis), leg.swingAnkle, leg.swingW * (1f - leg.holdW));
            bend = Mathf.Clamp(bend, -ankleUpMax, ankleDownMax);
            leg.footBone.rotation = Quaternion.AngleAxis(bend, axis) * neutral;
        }

        /// <summary>
        /// Toes after the ankle. On the ground they lie flat (or roll over their tips at toe-off), so the
        /// ball joint bends by however far the heel is up; in the air they follow the swing curve.
        /// Ground contact is read from where the ball actually is after IK and the ankle clamp.
        /// </summary>
        void PoseToes(Leg leg)
        {
            if (!leg.footBone || !leg.toeBone) return;

            Vector3 axis = transform.right;
            Vector3 restDir = transform.rotation * leg.restFootRunner * leg.toeInFoot;
            float footPitch = Vector3.SignedAngle(Vector3.ProjectOnPlane(restDir, axis),
                Vector3.ProjectOnPlane(leg.footBone.rotation * leg.toeInFoot, axis), axis);

            float lift = toeLength * Mathf.Sin(leg.toeRoll * Mathf.Deg2Rad);
            float height = Local(leg.toeBone).y - GroundY(leg) - lift;
            float onGround = 1f - Mathf.SmoothStep(0f, 1f, (height - 0.01f) / 0.04f);

            float bend = Mathf.Lerp(-leg.swingToe, leg.toeRoll - footPitch, onGround); // + = toes down
            bend = Mathf.Clamp(bend, -toeUpMax, toeDownMax);
            leg.toeBone.rotation = Quaternion.AngleAxis(bend, axis) * (leg.footBone.rotation * leg.restToeInFoot);
        }

        /// <summary>
        /// Lean follows where the feet are: the average of both feet along Front (0) → Back (1).
        /// Both front = sit back, one front one back or both at Mid = upright, back = full forward.
        /// The lean (and the feet midpoint the body rolls over) trail the feet by
        /// leanLag × strideDuration so the feet's quick moves do not shake the body.
        /// </summary>
        void UpdateLean(float dt)
        {
            float r = (FootBack(leftLeg) + FootBack(rightLeg)) * 0.5f;
            float target = leanByFootBack.Evaluate(r);
            float midZ = (leftLeg.baseBall.z + rightLeg.baseBall.z) * 0.5f;

            float k = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, leanLag * controller.strideDuration));
            _lean = Mathf.Clamp(Mathf.Lerp(_lean, target, k), -maxBackLean, maxForwardLean);
            _midZ = Mathf.Lerp(_midZ, midZ, k);
            UpdateHoldWeights(k);
        }

        /// <summary>0 = foot at its Front point, 1 = at its Back point (step cycle position).</summary>
        float FootBack(Leg leg) => Mathf.InverseLerp(Local(leg.front).z, Local(leg.back).z, leg.baseBall.z);

        /// <summary>Foot pose from the step cycle alone (base for the hold poses in FinishLeg).</summary>
        void ComputeLeg(int index, Leg leg, double now)
        {
            var c = controller.Legs[index];
            // A press mid-stride starts a new stride in the same state: restart from where the foot is.
            if (c.State != leg.lastState || c.StateTime != leg.lastStateTime)
            {
                leg.fromBall = leg.ball;
                leg.fromPitch = leg.pitch;
                leg.lastState = c.State;
                leg.lastStateTime = c.StateTime;
            }

            float stride = controller.strideDuration;
            float s = c.Phase(now, stride);
            float t = (float)(now - c.StateTime);
            Vector3 front = Local(leg.front), back = Local(leg.back), mid = Mid(leg);
            Vector3 ball;
            float pitch;
            float touchdownPitch = Mathf.Lerp(accelProfile.touchdownPitch, maxSpeedProfile.touchdownPitch, _profileW);
            leg.toeRoll = 0f;
            leg.swingW = 0f;

            switch (c.State)
            {
                case LegState.Ready:
                    // Landed under the body: the foot rolls down flat over the heel and waits there.
                    ball = mid;
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                    break;
                case LegState.Drive:
                    if (s < strideSplit)
                    {
                        // Push Mid → Back on the ground: foot flat, late heel rise, toes roll at toe-off.
                        float u = s / strideSplit;
                        float p = Mathf.Lerp(DriveStanceFrom, 1f, u);
                        ball = EllipseLoop(leg.fromBall, back, u, -loopHeight);
                        // A stride can start anywhere (e.g. mid-swing): ease from there onto the curve.
                        pitch = Mathf.Lerp(leg.fromPitch, Profile(x => x.stancePitch, p), Mathf.SmoothStep(0f, 1f, u / 0.3f));
                        leg.toeRoll = Profile(x => x.toeRoll, p);
                    }
                    else
                    {
                        // Swing Back → Mid, landing on the heel under the body.
                        float u = (s - strideSplit) / (1f - strideSplit);
                        ball = Swing(leg, back, mid, u, Profile(x => x.stancePitch, 1f), touchdownPitch, out pitch);
                    }
                    break;
                case LegState.Reach:
                    if (s < strideSplit)
                    {
                        // Swing Mid → Front, reaching out ahead of the body.
                        ball = Swing(leg, leg.fromBall, front, s / strideSplit, leg.fromPitch, touchdownPitch, out pitch);
                    }
                    else
                    {
                        // Landed at Front on the heel: rolls flat while the body comes over it, Front → Mid.
                        float u = (s - strideSplit) / (1f - strideSplit);
                        ball = EllipseLoop(front, mid, u, -loopHeight);
                        pitch = Mathf.Lerp(touchdownPitch, 0f, Mathf.SmoothStep(0f, 1f, (s - strideSplit) * stride / HeelRollTime));
                    }
                    break;
                default: // Grounded (stopped): settle into the feet-together pose, same as Ready.
                {
                    float g = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.001f, groundBlendTime));
                    ball = Vector3.Lerp(leg.fromBall, mid, g);
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, g);
                    break;
                }
            }

            // Toes rolling over their tips lift the ball (the tip stays where it is).
            if (leg.toeRoll > 0f)
            {
                float r = leg.toeRoll * Mathf.Deg2Rad;
                ball += new Vector3(0f, Mathf.Sin(r), 1f - Mathf.Cos(r)) * toeLength;
            }

            // Never below the ground: the clipped bottom of the loop is the ground contact.
            if (c.State != LegState.Grounded) ball.y = Mathf.Max(ball.y, GroundY(leg));
            // Toes up: the heel would dip under the ground, so the foot stands on its heel instead.
            float heelY = ball.y + (Quaternion.Euler(pitch, 0f, 0f) * HeelFromBall(leg)).y;
            if (heelY < GroundY(leg)) ball.y += GroundY(leg) - heelY;

            leg.baseBall = ball;
            leg.basePitch = pitch;
        }

        /// <summary>Feet-together point under the body, between Front and Back (midAlong).</summary>
        Vector3 Mid(Leg leg) => Vector3.Lerp(Local(leg.front), Local(leg.back), midAlong);

        /// <summary>
        /// Swing through the air: the ankle follows its own curve relative to the shin (heel kicks up,
        /// then toes pull up), handing over to the heel-strike angle just before the foot lands.
        /// The arc height scales with the distance, so a half swing is a lower arc than Back → Front.
        /// </summary>
        Vector3 Swing(Leg leg, Vector3 from, Vector3 to, float u, float fromPitch, float touchdownPitch, out float pitch)
        {
            float full = Mathf.Max(0.001f, Vector3.Distance(Local(leg.back), Local(leg.front)));
            float height = loopHeight * Mathf.Clamp01(Vector3.Distance(from, to) / full);
            pitch = Mathf.Lerp(fromPitch, touchdownPitch, 0.5f - 0.5f * Mathf.Cos(Mathf.PI * u));
            leg.swingAnkle = Profile(p => p.swingAnkle, u);
            leg.swingToe = Profile(p => p.swingToe, u);
            leg.swingW = Mathf.SmoothStep(0f, 1f, u / 0.15f) * (1f - Mathf.SmoothStep(0f, 1f, (u - 0.75f) / 0.25f));
            return EllipseLoop(from, to, u, height);
        }

        /// <summary>Heel contact point relative to the ball, flat foot (runner space).</summary>
        Vector3 HeelFromBall(Leg leg) => new Vector3(0f, 0f, -(leg.ballOffset.z + HeelBehindAnkle));

        /// <summary>A foot-profile curve at phase s, blended between the acceleration and max-speed profiles.</summary>
        float Profile(System.Func<FootProfile, AnimationCurve> curve, float s)
            => Mathf.Lerp(curve(accelProfile).Evaluate(s), curve(maxSpeedProfile).Evaluate(s), _profileW);

        /// <summary>
        /// Pendulum arm. The arm follows its controlling foot along Front (0) → Back (1): swing and elbow
        /// are interpolated between the front and back poses. Rotations are built in runner space and
        /// applied in the shoulder's frame, so the arm rides along with the trunk lean.
        /// </summary>
        void PoseArm(Arm arm, Leg foot)
        {
            if (!arm.upperArm || !arm.forearm) return;

            float p = FootBack(foot);
            float swing = Mathf.Lerp(0f, Mathf.Lerp(armSwingFront, armSwingBack, p), _run);
            float elbow = Mathf.Lerp(elbowStanding, Mathf.Lerp(elbowFront, elbowBack, p), _run);

            // Shoulder follows the swing (about Y) and lifts a little (about Z); forward and back have
            // their own amounts so the shoulder can pull back further than it comes forward.
            float s = Mathf.Lerp(1f, -1f, p) * _run; // +1 arm front, −1 arm back
            float swingDeg = s >= 0f ? shoulderSwing * s : shoulderSwingBack * s;
            float liftDeg = s >= 0f ? shoulderLift * s : shoulderLiftBack * -s;
            Quaternion shoulderRot = Quaternion.Euler(0f, -arm.side * swingDeg, arm.side * liftDeg);
            if (arm.shoulder)
            {
                Quaternion cInv = Quaternion.Inverse(arm.chestRest);
                arm.shoulder.localRotation = cInv * shoulderRot * arm.chestRest * arm.restShoulderLocal;
            }

            float o = armOutward * Mathf.Deg2Rad;
            Quaternion hang = Quaternion.FromToRotation(arm.restDir, new Vector3(arm.side * Mathf.Sin(o), -Mathf.Cos(o), 0f));
            Quaternion upper = Quaternion.Euler(-swing, 0f, 0f) * hang;           // runner space, forward = −X rotation
            Quaternion fore = Quaternion.Euler(-(swing + elbow), 0f, 0f) * hang;

            // Runner-space rotation R expressed in the shoulder's rest frame: P⁻¹ · R · P.
            Quaternion pInv = Quaternion.Inverse(arm.parentRest);
            arm.upperArm.localRotation = pInv * upper * arm.parentRest * arm.restUpperLocal;
            Transform parent = arm.upperArm.parent;
            arm.forearm.rotation = parent.rotation * (pInv * fore * arm.parentRest * arm.restForeInParent);
        }

        /// <summary>
        /// Upper half of an ellipse from start to end: angle θ = π(1−s).
        /// Horizontal follows the cosine (slow at both ends, fast in the middle), height the sine.
        /// Works from any start point, so an interrupted step still returns on the same shape.
        /// </summary>
        Vector3 EllipseLoop(Vector3 start, Vector3 end, float s, float height)
        {
            float along = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * s);
            Vector3 p = Vector3.Lerp(start, end, along);
            p.y = Mathf.Lerp(start.y, end.y, s) + height * Mathf.Sin(Mathf.PI * Mathf.Pow(s, loopSkew));
            return p;
        }

        void OnDrawGizmos()
        {
            DrawLegGizmos(leftLeg, new Color(0.3f, 0.6f, 1f));
            DrawLegGizmos(rightLeg, new Color(1f, 0.6f, 0.2f));
        }

        void DrawLegGizmos(Leg leg, Color c)
        {
            if (!leg.front || !leg.back) return;
            Gizmos.color = Color.green; Gizmos.DrawWireSphere(leg.front.position, 0.035f);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(leg.back.position, 0.035f);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(Vector3.Lerp(leg.front.position, leg.back.position, midAlong), 0.03f);
            Gizmos.color = c;
            // Preview: upper arc = return, lower arc = step, clipped at the ground (contact).
            Vector3 a = Local(leg.back), b = Local(leg.front);
            float ground = GroundY(leg);
            const int segments = 32;
            foreach (float height in new[] { loopHeight, -loopHeight })
            {
                Vector3 prev = leg.back.position;
                for (int i = 1; i <= segments; i++)
                {
                    Vector3 l = EllipseLoop(a, b, i / (float)segments, height);
                    l.y = Mathf.Max(l.y, ground);
                    Vector3 p = transform.TransformPoint(l);
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
            }
        }
    }
}
