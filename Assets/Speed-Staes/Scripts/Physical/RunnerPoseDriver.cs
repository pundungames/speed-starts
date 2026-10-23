using SpeedStars.Runner;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Visual only: turns RunnerSim's state into Control Rig targets. Never changes speed or timing.
    ///
    /// Per frame:
    ///   1. Key poses are blended: Ready → Run by speed, → Stumble by lean beyond the pose's own, then the
    ///      hold corners bilinearly by the two hold springs (left held = LungeLeft, right = LungeRight,
    ///      both = Dive).
    ///   2. Feet: the step cycle of the earlier procedural driver (faded out by holding). Each press runs a
    ///      LegCycle pair: the pressed foot Steps back along the ground (lower arc of the Front–Back loop,
    ///      heel up at toe-off) while the other Reaches forward through the air and lands on its heel first;
    ///      release returns the back foot home through the air, then the front one along the ground.
    ///      The loop through Front and Back dips just far enough to touch the ground at one point, the base
    ///      (taban): there a foot lands and there the feet meet when waiting. See ComputeLeg / GetLoop.
    ///   3. The physical lean tips the body about the planted feet (a held foot rides with the body), so a
    ///      lunge leans over its support foot and a release swings back past upright on its own.
    ///   4. Body height comes from the feet, not from a jump: a planted foot carries the body, so as it
    ///      rises in the body's frame during the push the body sinks onto it; when it lifts off, the push
    ///      throws the body up (higher for a better press) and a spring brings it back — with a short
    ///      flight when both feet are off the ground. See UpdateBodyHeight.
    /// Arms swing crosswise with the opposite foot through their own springs (they lag and overshoot);
    /// they, the knees' twist, the ankles and toes are posed in LateUpdate after the rig.
    /// The body only pitches around X; Y and Z rotations stay 0.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RunnerPoseDriver : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Leg
        {
            public Transform target;
            public Transform hint;
            [Tooltip("Found by name if empty (B-shin.L/R, B-foot.L/R, B-toe.L/R).")]
            public Transform shinBone;
            public Transform footBone;
            public Transform toeBone;
            [Header("Step points (ball of foot)")]
            public Transform front;
            public Transform back;

            [HideInInspector] public Vector3 restAnkle, restHintFromHip, ballOffset, toeInFoot, restHingeInThigh, restHingeInShin;
            [HideInInspector] public Quaternion restFootInShin, restFootRunner, restToeInFoot;
            public readonly LegCycle cycle = new LegCycle();
            [HideInInspector] public LegState lastState;
            [HideInInspector] public double lastStateTime;
            [HideInInspector] public Vector3 fromBall, baseBall;
            [HideInInspector] public Vector3 midBall;   // home: the loop's base point (taban), on the ground
            [HideInInspector] public float height;      // ball height above the ground in the body's frame (m)
            [HideInInspector] public bool contact;      // this foot is planted and carries the body
            [HideInInspector] public bool liftedOff;    // pushed off during the current step
            [HideInInspector] public float fromPitch, basePitch;
            [HideInInspector] public float pitch;       // final foot pitch (deg), applied after IK
            [HideInInspector] public float strideW;     // 1 = the step cycle drives this foot, 0 = the hold pose
            [HideInInspector] public float width;       // extra sideways spread from the pose (m)
            [HideInInspector] public float toeRoll;     // toes rolling over their tips (deg, end of stance)
            [HideInInspector] public float swingAnkle;  // ankle bend relative to the shin in the air (deg)
            [HideInInspector] public float swingW;      // 1 = ankle follows swingAnkle, 0 = foot pitch
            [HideInInspector] public float swingToe;    // toe bend in the air (deg, + = toes up)
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
            [Tooltip("Found by name if empty (B-upperArm.L/R, B-forearm.L/R).")]
            public Transform upperArm;
            public Transform forearm;
            [Tooltip("Arm IK target: its constraint is turned off at runtime.")]
            public Transform ikTarget;

            [HideInInspector] public Transform shoulder;
            [HideInInspector] public Quaternion restShoulderLocal, chestRest, restUpperLocal, restForeInParent, parentRest;
            [HideInInspector] public Vector3 restDir;
            [HideInInspector] public float side;
            public Spring swing, elbow;
        }

        public RunnerBody body;

        [Header("Rig")]
        public Transform hipTarget;
        public Transform spineRootTarget;
        public Transform spineTipTarget;
        public Leg leftLeg = new Leg();
        public Leg rightLeg = new Leg();
        public Arm leftArm = new Arm();
        public Arm rightArm = new Arm();

        [Header("Key poses (empty = built-in default)")]
        public RunnerPose ready;
        public RunnerPose run;
        public RunnerPose lungeLeft;
        public RunnerPose lungeRight;
        public RunnerPose dive;
        public RunnerPose stumble;

        [Header("Blend")]
        [Tooltip("Speed (m/s) at which the Run pose has fully replaced Ready.")]
        public float runPoseSpeed = 4f;
        [Tooltip("Lean beyond the pose's own (deg) over which Stumble comes in, ending at the sim's stumbleLean.")]
        public float stumbleBlendRange = 15f;

        [Header("Step cycle (s) — the feet move as a pair")]
        [Tooltip("Pressed foot → back, started by the press.")]
        public float stepDuration = 0.12f;
        [Tooltip("Other foot → front, started by the same press. Shorter than the step: it arrives first.")]
        public float reachDuration = 0.08f;
        [Tooltip("Back foot → Mid after release.")]
        public float returnDuration = 0.25f;
        [Tooltip("Front foot → Mid, starting once the back foot has reached Mid.")]
        public float frontReturnDuration = 0.15f;

        [Header("Foot")]
        [Tooltip("Foot angle while a foot is held back (deg).")]
        public float pitchBack = 50f;

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
        public float maxProfileSpeed = 7.5f;

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
        [Tooltip("Blend time into the home pose after a reset (s).")]
        public float groundBlendTime = 0.25f;

        [Header("Ankle (relative to the shin)")]
        [Tooltip("Max toes-up bend of the ankle (dorsiflexion, deg).")]
        public float ankleUpMax = 20f;
        [Tooltip("Max toes-down bend of the ankle (plantarflexion, deg).")]
        public float ankleDownMax = 50f;

        [Header("Body")]
        [Tooltip("Knee hints turn with the lean this much for a planted leg (a held leg turns fully).")]
        [Range(0f, 1f)] public float kneeLeanFollow = 0.4f;
        [Tooltip("Leg length share the hip may be from a planted ankle (keeps the knee from snapping straight).")]
        [Range(0.8f, 1f)] public float maxLegReach = 0.97f;

        [Header("Body height (the planted foot carries the body)")]
        [Tooltip("How far the body may sink onto a planted foot during the push before the foot lifts off (m).")]
        public float stanceCompression = 0.06f;
        [Tooltip("Keeps a planted foot on the ground (fast, no overshoot).")]
        public SpringSettings contactSpring = new SpringSettings(14f, 1f);
        [Tooltip("Body after lift-off: rises with the push, comes back, may overshoot (flight).")]
        public SpringSettings bodySpring = new SpringSettings(3f, 0.35f);
        [Tooltip("Upward speed a push gives the body at lift-off (m/s × press efficiency; half of it when standing).")]
        public float liftKick = 0.5f;

        [Header("Secondary motion")]
        public SpringSettings armSpring = new SpringSettings(3.5f, 0.4f);
        [Tooltip("Lean (deg) from which the head fully follows the trunk.")]
        public float headFollowLean = 25f;

        [Header("Arms (deg, + = forward)")]
        public float armFront = 45f;
        public float armBack = -40f;
        [Tooltip("Elbow bends this much more at the front of the swing, less at the back.")]
        public float elbowSwing = 15f;
        public float armOutward = 8f;
        [Tooltip("Shoulder (clavicle) follows the arm swing (deg at full swing).")]
        public float shoulderFollow = 12f;

        [Header("Preview (play mode): hold one pose; edit the asset live, changes stay")]
        public bool preview;
        public RunnerPose previewPose;
        [Range(-45f, 85f)] public float previewLean;

        // Heel strike (see ComputeLeg).
        const float HeelBehindAnkle = 0.05f;   // heel contact point behind the ankle joint (m)
        const float HeelRollTime = 0.1f;       // heel strike → foot flat (s)

        Vector3 _restHip;
        Quaternion _restHipRot, _restSpineRoot, _restSpineTip;
        float _ground, _legLength, _profileW;
        Spring _bodyY;
        bool _supported = true, _bound;
        MultiAimConstraint _headAim;

        LegTimings Timings => new LegTimings
        {
            Step = stepDuration,
            Reach = reachDuration,
            Return = returnDuration,
            FrontReturn = frontReturnDuration,
        };

        Vector3 Local(Transform t) => transform.InverseTransformPoint(t.position);

        /// <summary>Ground height of the ball of the foot (runner space): where it sits when standing.</summary>
        float GroundY(Leg leg) => leg.RestBall.y;

        static PoseData Get(RunnerPose asset, PoseData fallback) => asset ? asset.pose : fallback;

        void Awake() => Bind();

        void OnEnable()
        {
            if (!body) body = GetComponent<RunnerBody>();
            if (!body) return;
            body.Tapped += OnTapped;
            body.RunReset += OnRunReset;
        }

        void OnDisable()
        {
            if (!body) return;
            body.Tapped -= OnTapped;
            body.RunReset -= OnRunReset;
        }

        void Bind()
        {
            if (_bound) return;
            if (!body) body = GetComponent<RunnerBody>();
            // Constraints run without maintain-offset, so targets carry rest rotations.
            _restHip = Local(hipTarget);
            _restHipRot = Quaternion.Inverse(transform.rotation) * hipTarget.rotation;
            _restSpineRoot = Quaternion.Inverse(transform.rotation) * spineRootTarget.rotation;
            _restSpineTip = Quaternion.Inverse(transform.rotation) * spineTipTarget.rotation;
            BindLeg(leftLeg, "L");
            BindLeg(rightLeg, "R");
            _ground = (leftLeg.restAnkle.y + rightLeg.restAnkle.y) * 0.5f;
            _legLength = _restHip.y - _ground;
            var aimRig = FindDeep(transform, "Head Aim Rig");
            if (aimRig) _headAim = aimRig.GetComponent<MultiAimConstraint>();
            BindArm(leftArm, -1f, "B-upperArm.L", "B-forearm.L");
            BindArm(rightArm, 1f, "B-upperArm.R", "B-forearm.R");
            _bound = true;
        }

        void BindLeg(Leg leg, string side)
        {
            leg.restAnkle = Local(leg.target);
            leg.restHintFromHip = Local(leg.hint) - _restHip;
            // IK places the ankle only; the foot angle is applied afterwards relative to the shin.
            var ik = leg.target.GetComponentInParent<TwoBoneIKConstraint>();
            if (ik) ik.data.targetRotationWeight = 0f;
            if (!leg.shinBone) leg.shinBone = FindDeep(transform, "B-shin." + side);
            if (!leg.footBone) leg.footBone = FindDeep(transform, "B-foot." + side);
            if (!leg.toeBone) leg.toeBone = FindDeep(transform, "B-toe." + side);
            leg.ballOffset = leg.toeBone ? Local(leg.toeBone) - leg.restAnkle : new Vector3(0f, -0.1f, 0.15f);
            leg.fromBall = leg.baseBall = leg.midBall = leg.RestBall;
            leg.lastState = LegState.Grounded;
            leg.cycle.SetGrounded(0.0);
            if (!leg.shinBone || !leg.footBone) return;
            leg.restFootInShin = Quaternion.Inverse(leg.shinBone.rotation) * leg.footBone.rotation;
            leg.restFootRunner = Quaternion.Inverse(transform.rotation) * leg.footBone.rotation;
            if (leg.toeBone)
            {
                leg.toeInFoot = Quaternion.Inverse(leg.footBone.rotation) * (leg.toeBone.position - leg.footBone.position);
                leg.restToeInFoot = Quaternion.Inverse(leg.footBone.rotation) * leg.toeBone.rotation;
            }
            leg.restHingeInThigh = Quaternion.Inverse(leg.shinBone.parent.rotation) * transform.right;
            leg.restHingeInShin = Quaternion.Inverse(leg.shinBone.rotation) * transform.right;
        }

        void BindArm(Arm arm, float side, string upperName, string foreName)
        {
            if (!arm.upperArm) arm.upperArm = FindDeep(transform, upperName);
            if (!arm.forearm) arm.forearm = FindDeep(transform, foreName);
            if (arm.ikTarget)
            {
                var ik = arm.ikTarget.GetComponentInParent<TwoBoneIKConstraint>();
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

        // ---- Step cycle: the same pair logic the earlier step controller ran, on the sim's clock. ----

        void OnTapped(int leg, bool pressed, double time)
        {
            Bind();
            TickLegs(time);
            var c = Legs(leg).cycle;
            if (!pressed)
            {
                c.Release(time, Timings);
                return;
            }
            c.Press(time, Timings);
            // The other foot comes forward, unless it is held back too (both back).
            var other = Legs(1 - leg).cycle;
            if (!other.Holding) other.StartReach(time, Timings);
        }

        void OnRunReset()
        {
            leftLeg.cycle.SetGrounded(0.0);
            rightLeg.cycle.SetGrounded(0.0);
        }

        Leg Legs(int i) => i == 0 ? leftLeg : rightLeg;

        void TickLegs(double time)
        {
            var d = Timings;
            // Twice: the front foot's wait ends on the other foot reaching Mid in the same tick.
            for (int pass = 0; pass < 2; pass++)
            {
                leftLeg.cycle.Tick(time, d, rightLeg.cycle);
                rightLeg.cycle.Tick(time, d, leftLeg.cycle);
            }
        }

        void Update()
        {
            Bind();
            if (!body || body.Sim == null) return;
            var sim = body.Sim;
            float dt = Time.deltaTime;

            // 1. Key poses.
            PoseData p;
            float lean, strideW;
            if (preview && previewPose)
            {
                p = previewPose.pose;
                lean = previewLean;
                strideW = 0f;
            }
            else
            {
                p = PoseData.Lerp(Get(ready, RunnerPoseDefaults.Ready), Get(run, RunnerPoseDefaults.Run),
                    Mathf.SmoothStep(0f, 1f, sim.Speed / Mathf.Max(0.01f, runPoseSpeed)));
                float excess = sim.Lean - sim.PoseLean;
                float stumbleW = Mathf.SmoothStep(0f, 1f,
                    (excess - (sim.T.stumbleLean - stumbleBlendRange)) / Mathf.Max(0.01f, stumbleBlendRange));
                p = PoseData.Lerp(p, Get(stumble, RunnerPoseDefaults.Stumble), stumbleW);

                float hl = sim.Legs[0].HoldValue, hr = sim.Legs[1].HoldValue;
                PoseData top = PoseData.Lerp(p, Get(lungeRight, RunnerPoseDefaults.LungeRight), hr);
                PoseData bottom = PoseData.Lerp(Get(lungeLeft, RunnerPoseDefaults.LungeLeft), Get(dive, RunnerPoseDefaults.Dive), hr);
                p = PoseData.Lerp(top, bottom, hl);
                lean = sim.Lean;
                strideW = 1f - Mathf.Max(hl, hr);
            }

            // 2. Feet: step cycle.
            double now = sim.Time;
            TickLegs(now);
            float profileTarget = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(accelProfileSpeed, maxProfileSpeed, sim.Speed));
            _profileW = Mathf.Lerp(_profileW, profileTarget, 1f - Mathf.Exp(-4f * dt));
            ComputeLeg(leftLeg, now);
            ComputeLeg(rightLeg, now);
            float bodyY = preview ? 0f : UpdateBodyHeight(sim, dt) * strideW;
            Vector3 aL = Foot(leftLeg, p.left, p.width, strideW, out float pitchL, out Vector2 kneeL);
            Vector3 aR = Foot(rightLeg, p.right, p.width, strideW, out float pitchR, out Vector2 kneeR);
            Vector3 hip = _restHip + new Vector3(0f, p.pelvis.y, p.pelvis.x);

            // 3. Lean about the planted feet; held feet ride with the body.
            float wL = 1f - p.left.withBody, wR = 1f - p.right.withBody;
            Vector3 pivot = wL + wR > 0.05f ? (aL * wL + aR * wR) / (wL + wR) : (aL + aR) * 0.5f;
            pivot.y = _ground;
            Quaternion r = Quaternion.Euler(lean, 0f, 0f);
            hip = pivot + r * (hip - pivot);
            aL = Vector3.Lerp(aL, pivot + r * (aL - pivot), p.left.withBody);
            aR = Vector3.Lerp(aR, pivot + r * (aR - pivot), p.right.withBody);
            pitchL += lean * p.left.withBody;
            pitchR += lean * p.right.withBody;
            hip = KeepInReach(hip, aL, wL);
            hip = KeepInReach(hip, aR, wR);

            // 4. Body height from the planted foot / the push (the whole body moves, feet included).
            hip.y += bodyY;
            aL.y += bodyY;
            aR.y += bodyY;

            // Write the rig.
            Quaternion rootRot = transform.rotation;
            hipTarget.position = transform.TransformPoint(hip);
            hipTarget.rotation = rootRot * Quaternion.Euler(p.pelvisPitch + lean, 0f, 0f) * _restHipRot;
            spineRootTarget.rotation = rootRot * Quaternion.Euler(lean + Mathf.Lerp(p.pelvisPitch, p.trunkPitch, 0.6f), 0f, 0f) * _restSpineRoot;
            spineTipTarget.rotation = rootRot * Quaternion.Euler(lean + p.trunkPitch, 0f, 0f) * _restSpineTip;
            if (_headAim)
                _headAim.weight = 1f - Mathf.Max(p.headDown, Mathf.SmoothStep(0f, 1f, (lean + p.trunkPitch) / Mathf.Max(1f, headFollowLean)));

            WriteLeg(leftLeg, aL, pitchL, hip, kneeL, lean, p.left.withBody);
            WriteLeg(rightLeg, aR, pitchR, hip, kneeR, lean, p.right.withBody);

            // Arms: crosswise with the opposite foot (left arm goes back as the right foot goes back).
            StepArm(leftArm, p.leftArm, preview ? 0f : p.armSwing, FootSwing(rightLeg), dt);
            StepArm(rightArm, p.rightArm, preview ? 0f : p.armSwing, FootSwing(leftLeg), dt);
        }

        /// <summary>Ankle of a foot placed by a pose (runner space): the rig's standing place + the pose's offset.</summary>
        Vector3 PoseAnkle(Leg leg, FootPose pose, float width) =>
            leg.restAnkle + new Vector3(Side(leg) * width, pose.ankle.y, pose.ankle.x);

        /// <summary>
        /// Body height (m, runner space) from the feet. A planted foot carries the body: the body sits
        /// so that the lowest planted foot is on the ground, following it through a stiff spring. When
        /// the last planted foot lifts off, the push gives the body an upward speed (by the press's
        /// efficiency) and a softer spring takes it back to its own height — overshooting into a short
        /// flight while no foot is down.
        /// </summary>
        float UpdateBodyHeight(RunnerSim sim, float dt)
        {
            float support = float.MaxValue;
            if (leftLeg.contact) support = Mathf.Min(support, -leftLeg.height);
            if (rightLeg.contact) support = Mathf.Min(support, -rightLeg.height);

            if (support < float.MaxValue)
            {
                _bodyY.Step(support, contactSpring.frequency, contactSpring.damping, dt);
                _supported = true;
            }
            else
            {
                if (_supported)
                {
                    float share = Mathf.Lerp(0.5f, 1f, sim.Speed / sim.T.maxSpeed);
                    _bodyY.Velocity = Mathf.Max(_bodyY.Velocity, 0f) + liftKick * sim.LastEfficiency * share;
                }
                _supported = false;
                _bodyY.Step(0f, bodySpring.frequency, bodySpring.damping, dt);
            }
            return _bodyY.Value;
        }

        /// <summary>
        /// Ankle of one foot (runner space): the step cycle, handing over to the hold pose's feet as the
        /// hold comes in. Never a sum of the two. The pose's width opens both.
        /// </summary>
        Vector3 Foot(Leg leg, FootPose pose, float width, float strideW, out float pitch, out Vector2 knee)
        {
            Vector3 ankle = PoseAnkle(leg, pose, width);
            pitch = pose.pitch;
            knee = pose.knee;
            leg.strideW = strideW;
            leg.width = width;
            if (strideW <= 0f) return ankle;

            Vector3 cycleAnkle = leg.baseBall - Quaternion.Euler(leg.basePitch, 0f, 0f) * leg.ballOffset
                                 + new Vector3(Side(leg) * width, 0f, 0f);
            pitch = Mathf.Lerp(pose.pitch, leg.basePitch, strideW);
            return Vector3.Lerp(ankle, cycleAnkle, strideW);
        }

        Vector3 KeepInReach(Vector3 hip, Vector3 ankle, float planted)
        {
            if (planted < 0.5f) return hip;
            float reach = _legLength * maxLegReach;
            Vector3 d = hip - ankle;
            return d.magnitude > reach ? ankle + d.normalized * reach : hip;
        }

        void WriteLeg(Leg leg, Vector3 ankle, float pitch, Vector3 hip, Vector2 knee, float lean, float withBody)
        {
            leg.pitch = pitch;
            leg.target.position = transform.TransformPoint(ankle);
            // Knee hint: the step cycle's offset in front of the hip (running), blended to the rig's rest
            // hint plus the pose's knee offset (holds); it turns with the lean.
            Vector3 cycleHint = new Vector3(leg.RestBall.x, 0f, 0f) + kneeHintOffset;
            Vector3 poseHint = leg.restHintFromHip + new Vector3(0f, knee.y, knee.x);
            float kneeLean = lean * Mathf.Lerp(kneeLeanFollow, 1f, withBody);
            Vector3 hint = hip + Quaternion.Euler(kneeLean, 0f, 0f) * Vector3.Lerp(poseHint, cycleHint, leg.strideW)
                           + new Vector3(Side(leg) * leg.width, 0f, 0f);
            leg.hint.position = transform.TransformPoint(hint);
        }

        /// <summary>−1 = left, +1 = right.</summary>
        static float Side(Leg leg) => leg.restAnkle.x < 0f ? -1f : 1f;

        /// <summary>Where the foot is on the step cycle: +1 = Front, 0 = the base (home), −1 = Back.</summary>
        float FootSwing(Leg leg)
        {
            float z = leg.baseBall.z, home = leg.midBall.z;
            return z >= home
                ? Mathf.Clamp01((z - home) / Mathf.Max(0.01f, StepPoint(leg, true).z - home))
                : -Mathf.Clamp01((home - z) / Mathf.Max(0.01f, home - StepPoint(leg, false).z));
        }

        Vector3 StepPoint(Leg leg, bool front)
        {
            Transform t = front ? leg.front : leg.back;
            if (t) return Local(t);
            // No step point set: the earlier scene's numbers (ball of foot).
            return front
                ? new Vector3(leg.RestBall.x, leg.RestBall.y + 0.05f, 0.56f)
                : new Vector3(leg.RestBall.x, leg.RestBall.y + 0.32f, -0.67f);
        }

        /// <summary>Foot pose from the step cycle alone (the base for the hold poses).</summary>
        void ComputeLeg(Leg leg, double now)
        {
            var c = leg.cycle;
            // A new press mid-step restarts the same state: start again from where the foot is.
            if (c.State != leg.lastState || c.StateTime != leg.lastStateTime)
            {
                leg.fromBall = leg.baseBall;
                leg.fromPitch = leg.basePitch;
                leg.lastState = c.State;
                leg.lastStateTime = c.StateTime;
                leg.liftedOff = false;
            }

            float t = (float)(now - c.StateTime);
            var loop = GetLoop(leg, GroundY(leg));
            Vector3 front = loop.front, back = loop.back, mid = loop.Base;
            leg.midBall = mid;
            Vector3 ball;
            float pitch;
            float touchdownPitch = Mathf.Lerp(accelProfile.touchdownPitch, maxSpeedProfile.touchdownPitch, _profileW);
            leg.toeRoll = 0f;
            leg.swingW = 0f;

            switch (c.State)
            {
                case LegState.Ready:
                    // Feet together at the base, on the ground: the foot rolls down flat and waits.
                    ball = mid;
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                    break;
                case LegState.FrontWait:
                    // Waits at Front, in the air, until the other foot is home.
                    ball = front;
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                    break;
                case LegState.Reach:
                    // The other foot steps back: this one swings forward to Front, arriving first.
                    ball = Swing(leg, front, Mathf.Clamp01(t / Mathf.Max(0.001f, reachDuration)), touchdownPitch, out pitch);
                    break;
                case LegState.Hold:
                    ball = back;
                    pitch = pitchBack;
                    leg.toeRoll = Profile(pr => pr.toeRoll, 1f);
                    break;
                case LegState.Step:
                {
                    // Stance (push): foot flat, late heel rise, toes roll at toe-off.
                    float s = Mathf.Clamp01(t / Mathf.Max(0.001f, stepDuration));
                    // Along the lower arc to Back (it touches the ground only at the base).
                    ball = AlongLowerArc(loop, leg.fromBall, 1f, s);
                    // A step can start anywhere (e.g. mid-return): ease from there onto the curve.
                    pitch = Mathf.Lerp(leg.fromPitch, Profile(pr => pr.stancePitch, s), Mathf.SmoothStep(0f, 1f, s / 0.3f));
                    leg.toeRoll = Profile(pr => pr.toeRoll, s);
                    break;
                }
                case LegState.Return:
                {
                    float s = Mathf.Clamp01(t / Mathf.Max(0.001f, c.ReturnDuration(Timings)));
                    if (c.FromFront)
                    {
                        // From Front (after the other foot is home): down the lower arc to the base,
                        // touching the ground only there, rolling flat.
                        ball = AlongLowerArc(loop, leg.fromBall, loop.baseU, s);
                        pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                    }
                    else
                    {
                        // From Back: swings through the air and lands on the heel at the base.
                        ball = Swing(leg, mid, s, touchdownPitch, out pitch);
                    }
                    break;
                }
                default: // Grounded (reset): settle onto the base, same as Ready.
                {
                    float s = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.001f, groundBlendTime));
                    ball = Vector3.Lerp(leg.fromBall, mid, s);
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, s);
                    break;
                }
            }

            // Toes rolling over their tips lift the ball (the tip stays where it is).
            if (leg.toeRoll > 0f)
            {
                float rr = leg.toeRoll * Mathf.Deg2Rad;
                ball += new Vector3(0f, Mathf.Sin(rr), 1f - Mathf.Cos(rr)) * toeLength;
            }

            // Never below the ground (the loop only touches it at the base).
            if (c.State != LegState.Grounded) ball.y = Mathf.Max(ball.y, GroundY(leg));
            // Toes up: the heel would dip under the ground, so the foot stands on its heel instead.
            float heelY = ball.y + (Quaternion.Euler(pitch, 0f, 0f) * HeelFromBall(leg)).y;
            if (heelY < GroundY(leg)) ball.y += GroundY(leg) - heelY;

            leg.baseBall = ball;
            leg.basePitch = pitch;
            leg.height = ball.y - GroundY(leg);
            UpdateContact(leg);
        }

        /// <summary>
        /// Is this foot planted (carrying the body)? Only a foot that is really on the ground — at the base.
        /// Waiting with no key down, both feet are there. A foot waiting at Front, or anywhere else on the
        /// loop, simply stays in the air (the body is not pulled down to it). A pushing foot that started
        /// on the ground stays planted until it has risen stanceCompression in the body's frame, then it
        /// lifts off for the rest of the step.
        /// </summary>
        void UpdateContact(Leg leg)
        {
            const float onGround = 0.01f; // ball this close to the ground counts as touching it (m)
            var c = leg.cycle;
            switch (c.State)
            {
                case LegState.Ready:
                case LegState.Grounded:
                    leg.contact = true;
                    break;
                case LegState.Step:
                    if (leg.contact && leg.height > stanceCompression) leg.liftedOff = true;
                    leg.contact = !leg.liftedOff && (leg.contact || leg.height <= onGround);
                    break;
                case LegState.Hold:
                    leg.contact = false;
                    break;
                default:
                    leg.contact = leg.height <= onGround;
                    break;
            }
        }

        /// <summary>
        /// The step loop of one foot (ball, runner space): the upper arc is the swing, the lower arc runs
        /// Front (u = 0) → Back (u = 1) and dips just far enough to touch the ground once — the base
        /// (taban) at baseU, where feet land and meet.
        /// </summary>
        struct Loop
        {
            public Vector3 front, back;
            public float depth, baseU;
            public Vector3 Base;
        }

        Loop GetLoop(Leg leg, float ground) => MakeLoop(StepPoint(leg, true), StepPoint(leg, false), ground);

        Loop MakeLoop(Vector3 front, Vector3 back, float ground)
        {
            var loop = new Loop { front = front, back = back, depth = float.MaxValue, baseU = 0.5f };
            // Every point of the arc is linear in the depth: the smallest depth that brings one point
            // down to the ground is the one that touches it exactly once, at that point.
            const int samples = 96;
            for (int i = 1; i < samples; i++)
            {
                float u = i / (float)samples;
                float sag = Mathf.Sin(Mathf.PI * Mathf.Pow(u, loopSkew));
                if (sag < 1e-4f) continue;
                float d = (Mathf.Lerp(front.y, back.y, u) - ground) / sag;
                if (d < loop.depth)
                {
                    loop.depth = d;
                    loop.baseU = u;
                }
            }
            loop.depth = Mathf.Max(0f, loop.depth);
            loop.Base = LowerArc(loop, loop.baseU);
            loop.Base.y = Mathf.Max(loop.Base.y, ground);
            return loop;
        }

        Vector3 LowerArc(Loop loop, float u) => EllipseLoop(loop.front, loop.back, u, -loop.depth);

        /// <summary>Loop parameter of a point by its forward position (inverse of the arc's cosine easing).</summary>
        static float LoopU(Loop loop, Vector3 p)
        {
            float along = Mathf.Clamp01(Mathf.InverseLerp(loop.front.z, loop.back.z, p.z));
            return Mathf.Acos(1f - 2f * along) / Mathf.PI;
        }

        /// <summary>
        /// From where the foot was, along the lower arc to <paramref name="toU"/>: starts on the arc at the
        /// foot's forward position and eases out any height difference over the first third.
        /// </summary>
        Vector3 AlongLowerArc(Loop loop, Vector3 from, float toU, float s)
        {
            float u0 = LoopU(loop, from);
            Vector3 p = LowerArc(loop, Mathf.Lerp(u0, toU, s));
            return p + (from - LowerArc(loop, u0)) * (1f - Mathf.SmoothStep(0f, 1f, s / 0.3f));
        }

        /// <summary>Home of the foot: the loop's base (taban), on the ground.</summary>
        Vector3 Mid(Leg leg) => leg.midBall;

        /// <summary>
        /// Swing through the air from where the foot was to <paramref name="end"/>: the ankle follows its
        /// own curve relative to the shin (heel kicks up, then toes pull up), handing over to the
        /// heel-strike angle just before the foot lands. The arc is lower for a shorter swing.
        /// </summary>
        Vector3 Swing(Leg leg, Vector3 end, float s, float touchdownPitch, out float pitch)
        {
            float full = Mathf.Max(0.001f, Vector3.Distance(StepPoint(leg, false), StepPoint(leg, true)));
            float height = loopHeight * Mathf.Clamp01(Vector3.Distance(leg.fromBall, end) / full);
            pitch = Mathf.Lerp(leg.fromPitch, touchdownPitch, 0.5f - 0.5f * Mathf.Cos(Mathf.PI * s));
            leg.swingAnkle = Profile(pr => pr.swingAnkle, s);
            leg.swingToe = Profile(pr => pr.swingToe, s);
            leg.swingW = Mathf.SmoothStep(0f, 1f, s / 0.15f) * (1f - Mathf.SmoothStep(0f, 1f, (s - 0.75f) / 0.25f));
            return EllipseLoop(leg.fromBall, end, s, height);
        }

        /// <summary>Heel contact point relative to the ball, flat foot (runner space).</summary>
        Vector3 HeelFromBall(Leg leg) => new Vector3(0f, 0f, -(leg.ballOffset.z + HeelBehindAnkle));

        /// <summary>A foot-profile curve at phase s, blended between the acceleration and max-speed profiles.</summary>
        float Profile(System.Func<FootProfile, AnimationCurve> curve, float s)
            => Mathf.Lerp(curve(accelProfile).Evaluate(s), curve(maxSpeedProfile).Evaluate(s), _profileW);

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

        void StepArm(Arm arm, ArmPose pose, float swingAmount, float oppositeSwing, float dt)
        {
            // −1 … +1: the arm is back when the opposite foot is back, level when it is home.
            float s = oppositeSwing * swingAmount;
            float swing = pose.swing + (s >= 0f ? armFront * s : -armBack * s);
            float elbow = pose.elbow + elbowSwing * s;
            arm.swing.Step(swing, armSpring.frequency, armSpring.damping, dt);
            arm.elbow.Step(elbow, armSpring.frequency, armSpring.damping, dt);
        }

        // Arms, knees, ankles and toes are posed after the Animator/rig has written the skeleton this frame.
        void LateUpdate()
        {
            if (!_bound) return;
            PoseArm(leftArm);
            PoseArm(rightArm);
            FixLegTwist(leftLeg);
            FixLegTwist(rightLeg);
            PoseAnkle(leftLeg);
            PoseAnkle(rightLeg);
            PoseToes(leftLeg);
            PoseToes(rightLeg);
        }

        /// <summary>
        /// Pendulum arm from the shoulder (X only) with an elbow bend and a small clavicle follow.
        /// Rotations are built in runner space and applied in the shoulder's frame, so the arm rides
        /// along with the trunk lean.
        /// </summary>
        void PoseArm(Arm arm)
        {
            if (!arm.upperArm || !arm.forearm) return;
            float swing = arm.swing.Value;
            float elbow = Mathf.Max(0f, arm.elbow.Value);

            if (arm.shoulder)
            {
                float follow = Mathf.Clamp(swing / 45f, -1f, 1f) * shoulderFollow;
                Quaternion shoulderRot = Quaternion.Euler(0f, -arm.side * follow, 0f);
                Quaternion cInv = Quaternion.Inverse(arm.chestRest);
                arm.shoulder.localRotation = cInv * shoulderRot * arm.chestRest * arm.restShoulderLocal;
            }

            float o = armOutward * Mathf.Deg2Rad;
            Quaternion hang = Quaternion.FromToRotation(arm.restDir, new Vector3(arm.side * Mathf.Sin(o), -Mathf.Cos(o), 0f));
            Quaternion upper = Quaternion.Euler(-swing, 0f, 0f) * hang;            // runner space, forward = −X rotation
            Quaternion fore = Quaternion.Euler(-(swing + elbow), 0f, 0f) * hang;

            // Runner-space rotation R expressed in the shoulder's rest frame: P⁻¹ · R · P.
            Quaternion pInv = Quaternion.Inverse(arm.parentRest);
            arm.upperArm.localRotation = pInv * upper * arm.parentRest * arm.restUpperLocal;
            Transform parent = arm.upperArm.parent;
            arm.forearm.rotation = parent.rotation * (pInv * fore * arm.parentRest * arm.restForeInParent);
        }

        /// <summary>
        /// Leg IK gets the knee and ankle right but spins the thigh and shin about their length, turning the
        /// knee hinge inward. Spin both back so the hinge lies across the bend plane, as a real knee does.
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
            shin.rotation = Quaternion.FromToRotation(leg.footBone.position - knee, ankle - knee) * shin.rotation;
            Untwist(shin, ankle - knee, shin.rotation * leg.restHingeInShin, hinge);
        }

        static void Untwist(Transform bone, Vector3 axis, Vector3 hinge, Vector3 wanted)
        {
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(hinge, axis), Vector3.ProjectOnPlane(wanted, axis), axis);
            bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
        }

        /// <summary>
        /// Ankle after IK: the wanted foot pitch is turned into a bend relative to the shin and clamped to
        /// the ankle's range. In the air the bend comes from the swing curve instead (relative to the shin,
        /// as a real ankle moves), blended by swingW; hold poses use the pitch only.
        /// </summary>
        void PoseAnkle(Leg leg)
        {
            if (!leg.shinBone || !leg.footBone) return;
            Vector3 axis = transform.right;
            Quaternion neutral = leg.shinBone.rotation * leg.restFootInShin;
            Quaternion wanted = transform.rotation * Quaternion.Euler(leg.pitch, 0f, 0f) * leg.restFootRunner;
            Vector3 from = Vector3.ProjectOnPlane(neutral * leg.toeInFoot, axis);
            Vector3 to = Vector3.ProjectOnPlane(wanted * leg.toeInFoot, axis);
            float bend = Mathf.Lerp(Vector3.SignedAngle(from, to, axis), leg.swingAnkle, leg.swingW * leg.strideW);
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

            float lift = toeLength * Mathf.Sin(leg.toeRoll * leg.strideW * Mathf.Deg2Rad);
            float height = Local(leg.toeBone).y - GroundY(leg) - lift;
            float onGround = 1f - Mathf.SmoothStep(0f, 1f, (height - 0.01f) / 0.04f);

            float bend = Mathf.Lerp(-leg.swingToe * leg.strideW, leg.toeRoll * leg.strideW - footPitch, onGround); // + = toes down
            bend = Mathf.Clamp(bend, -toeUpMax, toeDownMax);
            leg.toeBone.rotation = Quaternion.AngleAxis(bend, axis) * (leg.footBone.rotation * leg.restToeInFoot);
        }

        void OnDrawGizmos()
        {
            DrawLegGizmos(leftLeg, new Color(0.3f, 0.6f, 1f));
            DrawLegGizmos(rightLeg, new Color(1f, 0.6f, 0.2f));
        }

        /// <summary>
        /// The step loop, in edit mode too: green = Front, red = Back, yellow = the base (taban, the only
        /// point on the ground), upper arc = swing, lower arc = push. Outside play mode the ground comes
        /// straight from the toe bone (nothing is bound, so nothing in the rig is changed).
        /// </summary>
        void DrawLegGizmos(Leg leg, Color c)
        {
            if (!leg.front || !leg.back) return;
            float ground;
            if (_bound) ground = GroundY(leg);
            else
            {
                var toe = leg.toeBone ? leg.toeBone : FindDeep(transform, Local(leg.front).x < 0f ? "B-toe.L" : "B-toe.R");
                if (!toe) return;
                ground = Local(toe).y;
            }
            var loop = MakeLoop(Local(leg.front), Local(leg.back), ground);

            Gizmos.color = Color.green; Gizmos.DrawWireSphere(leg.front.position, 0.035f);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(leg.back.position, 0.035f);
            Vector3 baseWorld = transform.TransformPoint(loop.Base);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(baseWorld, 0.03f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(baseWorld + transform.up * 0.06f, "Taban");
#endif
            Gizmos.color = c;
            const int segments = 40;
            Vector3 prevUp = leg.back.position, prevDown = leg.front.position;
            for (int i = 1; i <= segments; i++)
            {
                float s = i / (float)segments;
                Vector3 up = transform.TransformPoint(EllipseLoop(loop.back, loop.front, s, loopHeight));
                Vector3 down = transform.TransformPoint(LowerArc(loop, s));
                Gizmos.DrawLine(prevUp, up);
                Gizmos.DrawLine(prevDown, down);
                prevUp = up;
                prevDown = down;
            }
        }
    }
}
