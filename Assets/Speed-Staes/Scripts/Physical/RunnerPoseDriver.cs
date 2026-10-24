using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Visual only: turns RunnerSim's state into Control Rig targets. Never changes speed or timing.
    ///
    /// Per frame:
    ///   1. Key poses are blended: Ready → Run by speed, → Stumble by lean beyond the pose's own. There are
    ///      no hold poses: holding is built from the feet (see 2, 3 and Holds).
    ///   2. Feet: a step cycle (faded out by holding). The feet always move as a pair and at a speed, not
    ///      in a fixed time: a press sends that foot back along the ground (lower arc of the Front–Back loop,
    ///      heel up at toe-off) while the other comes forward through the air, both at strideSpeed, so the
    ///      one with less way to go arrives first. With no key down both go home together at returnSpeed —
    ///      the front one back along the ground, the back one forward through the air — nobody waits for
    ///      the other; the nearer one is home first. See OnTapped / BeginReturn / TickLegs.
    ///      The loop through Front and Back dips just far enough to touch the ground at one point, the base
    ///      (taban), where a running foot lands. With no input the feet stand on their Rest points instead:
    ///      Rest sits higher in the body's frame, so standing on it the body crouches; the first press takes
    ///      the feet onto the loop and the body rises off them, and when both come home it lands back into
    ///      the crouch. See ComputeLeg / GetLoop / Home.
    ///   3. The waist lean (UpdateWaist: centres over the feet, holding pushes it forward, 1× per held foot).
    ///      Holds: one foot held back stays at Back while the other waits at Front, comes down to the ground
    ///      and carries the body; the pelvis slides over it as the hold builds up (holdShiftSpring), lower
    ///      by holdCrouch. Both held is no special case: both feet wait at Back and each held foot adds its
    ///      lean (and pushes the head) — the dive comes out of that.
    ///      Forward the lean always bends the body at the waist: the pelvis takes only pelvisWaistShare of
    ///      it, the spine takes all of it, and a held leg turns up behind about its hip as the trunk goes
    ///      down (the plane pose), never past level. Backward (the swing past upright after a release) it
    ///      sits the whole body back about the feet.
    ///   Head and neck (UpdateHead): one head angle, back ↔ upright ↔ forward, read off the same press
    ///      results that give the speed — the other way round: a press on time keeps the head upright, a
    ///      wasted one (early, leg still back) pushes it forward by 1 − efficiency, so mashing gets no speed
    ///      but piles the head forward. Between presses it drops back; held feet push it forward too. The
    ///      neck takes it up to neckMax, the rest pulls the waist after it: the body follows the head.
    ///   Going down is no target pose: mashing only takes the trunk low while the legs keep running; with
    ///      both feet held back no foot can carry the body, so it falls and stops on the ground (see 4).
    ///   4. Body height comes from the feet, not from a jump: a planted foot carries the body, so as it
    ///      rises in the body's frame during the push the body sinks onto it; when it lifts off, the push
    ///      throws the body up (higher for a better press) and a spring brings it back — with a short
    ///      flight when both feet are off the ground. See UpdateBodyHeight.
    /// Arms are locked to the opposite foot (right arm ↔ left foot): back when it is back, holds included,
    /// no lag; the elbow opens a little as the arm swings back past the body. They, the knees' twist, the
    /// ankles and toes are posed in LateUpdate after the rig.
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
            [Tooltip("Where the foot stands with no input (on the ground). Higher than the loop's base = deeper " +
                     "crouch. Empty = the loop's base.")]
            public Transform rest;

            [HideInInspector] public Vector3 restAnkle, restHintFromHip, ballOffset, toeInFoot, restHingeInThigh, restHingeInShin;
            [HideInInspector] public Quaternion restFootInShin, restFootRunner, restToeInFoot;
            public readonly FootCycle cycle = new FootCycle();
            [HideInInspector] public FootState lastState;
            [HideInInspector] public double lastStateTime;
            [HideInInspector] public Vector3 fromBall, baseBall;
            [HideInInspector] public Vector3 midBall;   // home: the Rest point (or the loop's base), on the ground
            [HideInInspector] public float height;      // ball height above the ground in the body's frame (m)
            [HideInInspector] public bool contact;      // this foot is planted and carries the body
            [HideInInspector] public float support;     // how much of its height the body sinks onto (0..1)
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
            [HideInInspector] public float swing, elbow;   // this frame's pose (deg)
            [HideInInspector] public float lastFoot, back; // the opposite foot last frame; 1 = arm swinging back
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
        public RunnerPose stumble;

        [Header("Blend")]
        [Tooltip("Speed (m/s) at which the Run pose has fully replaced Ready.")]
        public float runPoseSpeed = 4f;
        [Tooltip("Lean beyond the pose's own (deg) over which Stumble comes in, ending at the sim's stumbleLean.")]
        public float stumbleBlendRange = 15f;

        [Header("Step cycle — the feet move as a pair, at a speed (nearer foot arrives first)")]
        [Tooltip("Speed (m/s) of the pressed foot going back and of the other coming forward. The same for " +
                 "both, so the one with less way to go arrives first.")]
        public float strideSpeed = 6f;
        [Tooltip("Speed (m/s) both feet go home at when no key is down. They start together; the nearer one " +
                 "(usually the front one) is home first.")]
        public float returnSpeed = 2.5f;
        [Tooltip("Shortest move (s), so a very short way is not a snap.")]
        public float minMoveTime = 0.05f;
        [Tooltip("While stepping, where the feet go home: Rest (crouch) below x m/s, the loop's base (running " +
                 "height) above y. Only while presses keep coming (see stanceKeepTime): with no input home is Rest.")]
        public Vector2 restToBaseSpeed = new Vector2(0.5f, 3f);
        [Tooltip("The run stance (feet home at the base, Run pose) is kept this long after the last press (s). " +
                 "After that, with no key down, the runner heads straight for the Ready crouch however fast it " +
                 "still goes — no upright in-between stance.")]
        public float stanceKeepTime = 0.6f;
        [Tooltip("How fast it turns from the run stance to the Ready crouch and back (1/s).")]
        public float stanceBlend = 8f;

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

        [Header("Waist (lean: centres itself over the feet, holding pushes it forward)")]
        [Tooltip("Forward lean per held foot (deg): one foot held = 1×, both = 2×.")]
        public float holdLean = 40f;
        [Tooltip("How strongly the waist leans to stay balanced over the feet: feet behind their home (mashing " +
                 "keeps them back) lean it forward, feet ahead lean it back (× the angle of the feet behind the hip).")]
        public float balanceGain = 1f;
        [Tooltip("Lean at max speed (deg).")]
        public float speedLean = 8f;
        [Tooltip("Forward nudge a push gives the waist (deg/s × press efficiency): the small spring of each step.")]
        public float stepKick = 30f;
        [Tooltip("Waist spring. Low damping = it swings past centre — the sit back after a hold is released.")]
        public SpringSettings waistSpring = new SpringSettings(1.2f, 0.35f);

        [Header("Holds (built from the feet: no hold poses)")]
        [Tooltip("How the pelvis slides over the support foot as a one-foot hold builds up (slow = it creeps forward).")]
        public SpringSettings holdShiftSpring = new SpringSettings(0.8f, 1f);
        [Tooltip("Pelvis drop over the support foot at a full one-foot hold (m): the support knee bends.")]
        public float holdCrouch = 0.2f;
        [Tooltip("A held leg rises behind with the waist lean up to this angle from straight down (deg; 90 = level).")]
        public float heldLegMaxAngle = 90f;

        [Header("Head & neck (deg, + = forward): the other side of the press efficiency")]
        [Tooltip("Where the head drops back to when no press comes after the stride has landed.")]
        public float headBack = -15f;
        [Tooltip("How fast the head drops back after landing.")]
        public SpringSettings headFallSpring = new SpringSettings(2f, 1f);
        [Tooltip("How far a late press brings the head back up to upright (1 = fully).")]
        [Range(0f, 1f)] public float lateRecover = 0.6f;
        [Tooltip("Forward push of a wasted press (efficiency 0); a press on time pushes nothing. × (1 − efficiency).")]
        public float earlyKick = 25f;
        [Tooltip("How fast the forward pile-up wears off (deg/s): presses coming faster than this pile up.")]
        public float headRecover = 30f;
        [Tooltip("Small nod every press gives the head (deg/s), whatever the press: keeps it alive.")]
        public float headNod = 60f;
        [Tooltip("Forward push per held foot (× its hold spring).")]
        public float holdHead = 20f;
        [Tooltip("Most a one-foot hold pushes the head (in line with the trunk, looking ahead-down); both held: no cap.")]
        public float oneFootMax = 25f;
        [Tooltip("The head follows its target through this spring (low damping = it swings).")]
        public SpringSettings headSpring = new SpringSettings(6f, 0.5f);
        [Tooltip("Most the neck bends on its own; beyond it the head pulls the waist after it. Also how far " +
                 "the trunk may lean while the head still looks ahead.")]
        public float neckMax = 35f;
        [Tooltip("Share of the head beyond neckMax that the waist takes (the body follows the head).")]
        public float trunkFollow = 1f;

        [Header("Falling: no foot can carry the body (both held back)")]
        [Tooltip("How far the pelvis stays above the ground when the body comes down on it (m): its thickness.")]
        public float pelvisClearance = 0.14f;

        [Header("Body")]
        [Tooltip("Share of the forward (waist) lean the pelvis takes; the spine above it takes the whole of it, " +
                 "so the trunk bends at the waist while the pelvis stays over the feet.")]
        [Range(0f, 1f)] public float pelvisWaistShare = 0.3f;
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

        [Header("Arms (deg, + = forward): locked to the opposite foot, right arm ↔ left foot")]
        public float armFront = 45f;
        public float armBack = -40f;
        [Tooltip("Elbow bends this much more at the front of the swing, less at the back.")]
        public float elbowSwing = 15f;
        [Tooltip("Elbow opens this much as the arm swings back past the body (most at the middle; not on the way forward).")]
        public float elbowOpenBack = 40f;
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
        const float NeckShare = 0.5f;          // share of the head angle the neck (spine tip) bends; the head aims the rest

        Vector3 _restHip;
        Quaternion _restHipRot, _restSpineRoot, _restSpineTip;
        float _ground, _legLength, _profileW, _runHome;
        float _stepping;                           // 1 = presses keep coming (run stance), 0 = none: Ready crouch
        Spring _bodyY, _waist, _shift;
        Spring _head, _headFall;                   // the head angle; its drop back between presses (deg)
        float _headFwd;                            // forward pile-up of wasted presses (deg)
        float _headAngle;                          // this frame's head angle (deg, + = forward)
        float _carry;                              // 1 = a foot can carry the body, 0 = both held back (it falls)
        float _holdL, _holdR;                      // hold springs this frame (feet need them)
        bool _supported = true, _bound;
        MultiAimConstraint _headAim;
        Transform _headBone, _lookTarget;
        float _lookDistance;

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
            if (_headAim)
            {
                // The head is aimed through its look target: placing it sets the head's angle.
                _headBone = _headAim.data.constrainedObject;
                _lookTarget = _headAim.data.sourceObjects.Count > 0 ? _headAim.data.sourceObjects.GetTransform(0) : null;
                if (_headBone && _lookTarget)
                    _lookDistance = Mathf.Max(0.3f, Vector3.Distance(_headBone.position, _lookTarget.position));
            }
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
            leg.lastState = FootState.Grounded;
            leg.cycle.Enter(FootState.Grounded, 0.0);
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

        // ---- Step cycle: pair rules on the sim's clock. ----

        /// <summary>
        /// A press sends that foot back to Back and the other forward to Front (unless that one is held back
        /// too), both from wherever they are, at the same speed. A release of a foot held back sends the free
        /// feet home.
        /// </summary>
        void OnTapped(int leg, bool pressed, double time)
        {
            Bind();
            TickLegs(time);
            var l = Legs(leg);
            if (!pressed)
            {
                l.cycle.Held = false;
                if (l.cycle.State == FootState.Hold) BeginReturn(time);
                return;
            }
            l.cycle.Held = true;
            // Every push nudges the waist forward a little, by how good it was; the waist spring settles it.
            if (body && body.Sim != null)
            {
                _waist.Velocity += stepKick * body.Sim.LastEfficiency;
                HeadPress(body.Sim);
            }
            Move(l, FootState.Step, time, StepPoint(l, false), strideSpeed);
            var other = Legs(1 - leg);
            if (!other.cycle.Holding) Move(other, FootState.Reach, time, StepPoint(other, true), strideSpeed);
        }

        /// <summary>
        /// A press seen by the head, from the same result and efficiency that give the speed: on time brings it
        /// upright, late only part of the way (it stays a little back), and whatever efficiency the press lost
        /// pushes it forward — the less speed a press gets, the more the head goes. Every press nods it a little.
        /// </summary>
        void HeadPress(RunnerSim sim)
        {
            switch (sim.LastResult)
            {
                case PressResult.Late:
                    _headFall.Value *= 1f - lateRecover;
                    break;
                default:
                    _headFall.Value = 0f;
                    _headFwd += earlyKick * (1f - sim.LastEfficiency);
                    break;
            }
            _headFall.Velocity = 0f;
            _head.Velocity += headNod;
        }

        void OnRunReset()
        {
            _head.Reset();
            _headFall.Reset();
            _headFwd = 0f;
            leftLeg.cycle.Enter(FootState.Grounded, 0.0);
            rightLeg.cycle.Enter(FootState.Grounded, 0.0);
        }

        Leg Legs(int i) => i == 0 ? leftLeg : rightLeg;

        /// <summary>Start a move to <paramref name="to"/>: its time is the way there over the speed.</summary>
        void Move(Leg leg, FootState state, double time, Vector3 to, float speed, bool fromFront = false)
        {
            float way = Vector3.Distance(leg.baseBall, to);
            leg.cycle.Enter(state, time, Mathf.Max(minMoveTime, way / Mathf.Max(0.01f, speed)), fromFront);
        }

        /// <summary>
        /// No key down any more: every foot that is not held back and not home sets off home now, together,
        /// at returnSpeed — a foot ahead of home comes back along the ground, one behind swings through the
        /// air. The nearer one is home first; nobody waits for the other.
        /// </summary>
        void BeginReturn(double time)
        {
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                var c = leg.cycle;
                if (c.Holding || c.AtHome || c.State == FootState.Return) continue;
                Move(leg, FootState.Return, time, leg.midBall, returnSpeed, leg.baseBall.z > leg.midBall.z);
            }
        }

        /// <summary>
        /// Moves that have run their time: a step ends held back (Hold) or, with the key up, sends both feet
        /// home; a reach ends at Front, waiting there only while the other foot is still pushing or held back;
        /// a return ends home.
        /// </summary>
        void TickLegs(double time)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var leg in new[] { leftLeg, rightLeg })
                {
                    var c = leg.cycle;
                    var partner = (leg == leftLeg ? rightLeg : leftLeg).cycle;
                    // A foot left back with its key up (however it got there) goes home.
                    if (c.State == FootState.Hold && !c.Held)
                    {
                        BeginReturn(System.Math.Max(c.StateTime, time - 1e-3));
                        continue;
                    }
                    if (c.State == FootState.FrontWait && !partner.Holding && partner.State != FootState.Step)
                    {
                        BeginReturn(System.Math.Max(c.StateTime, partner.StateTime));
                        continue;
                    }
                    if (!c.Moving || time < c.EndTime) continue;
                    double end = c.EndTime;
                    switch (c.State)
                    {
                        case FootState.Step:
                            if (c.Held) c.Enter(FootState.Hold, end);
                            else BeginReturnFrom(leg, end);
                            break;
                        case FootState.Reach:
                            c.Enter(FootState.FrontWait, end);
                            break;
                        case FootState.Return:
                            c.Enter(FootState.Ready, end);
                            break;
                    }
                }
            }
        }

        /// <summary>A step just ended with its key up: that foot is at Back now, then both go home.</summary>
        void BeginReturnFrom(Leg stepped, double time)
        {
            // Leaves from Back (the frame may not have drawn the very end of the step yet).
            stepped.baseBall = StepPoint(stepped, false);
            BeginReturn(time);
        }

        void Update()
        {
            Bind();
            if (!body || body.Sim == null) return;
            var sim = body.Sim;
            float dt = Time.deltaTime;

            // 1. Key poses.
            PoseData p;
            float lean, strideW, stumbleW = 0f;
            if (preview && previewPose)
            {
                p = previewPose.pose;
                lean = previewLean;
                strideW = 0f;
            }
            else
            {
                // The Ready crouch is where the runner always heads; the run stance only lasts while presses
                // keep coming, so stopping goes straight back to the crouch (no upright stance on the way).
                bool stepping = sim.Legs[0].Pressed || sim.Legs[1].Pressed || sim.Time - sim.LastPressTime < stanceKeepTime;
                _stepping = Mathf.Lerp(_stepping, stepping ? 1f : 0f, 1f - Mathf.Exp(-stanceBlend * dt));
                p = PoseData.Lerp(Get(ready, RunnerPoseDefaults.Ready), Get(run, RunnerPoseDefaults.Run),
                    Mathf.SmoothStep(0f, 1f, sim.Speed / Mathf.Max(0.01f, runPoseSpeed)) * _stepping);
                float excess = sim.Lean - sim.PoseLean;
                stumbleW = Mathf.SmoothStep(0f, 1f,
                    (excess - (sim.T.stumbleLean - stumbleBlendRange)) / Mathf.Max(0.01f, stumbleBlendRange));
                p = PoseData.Lerp(p, Get(stumble, RunnerPoseDefaults.Stumble), stumbleW);
                lean = 0f;    // the waist decides it below, from the feet
                strideW = 1f; // the feet always come from the step cycle, holds included
            }
            _holdL = preview ? 0f : sim.Legs[0].HoldValue;
            _holdR = preview ? 0f : sim.Legs[1].HoldValue;
            // Which feet ride with the body (held back) and which carry it.
            float withL = preview ? p.left.withBody : _holdL, withR = preview ? p.right.withBody : _holdR;

            // 2. Feet: step cycle.
            double now = sim.Time;
            TickLegs(now);
            _runHome = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(restToBaseSpeed.x, restToBaseSpeed.y, sim.Speed)) * _stepping;
            float profileTarget = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(accelProfileSpeed, maxProfileSpeed, sim.Speed));
            _profileW = Mathf.Lerp(_profileW, profileTarget, 1f - Mathf.Exp(-4f * dt));
            ComputeLeg(leftLeg, now);
            ComputeLeg(rightLeg, now);
            _headAngle = preview ? 0f : UpdateHead(sim, dt);
            if (!preview) lean = UpdateWaist(sim, dt);
            // A foot held back cannot land: with both held nothing carries the body.
            _carry = 1f - Mathf.Min(_holdL, _holdR);
            float bodyY = preview ? 0f : UpdateBodyHeight(sim, dt) * strideW;
            Vector3 aL = Foot(leftLeg, p.left, p.width, strideW, out float pitchL, out Vector2 kneeL);
            Vector3 aR = Foot(rightLeg, p.right, p.width, strideW, out float pitchR, out Vector2 kneeR);
            Vector3 hip = _restHip + new Vector3(0f, p.pelvis.y, p.pelvis.x);

            // One-foot hold: the pelvis creeps over the support foot (the one not held) and sinks onto it.
            float oneL = _holdL * (1f - _holdR), oneR = _holdR * (1f - _holdL);   // left held / right held
            float one = oneL + oneR;
            _shift.Step(one, holdShiftSpring.frequency, holdShiftSpring.damping, dt);
            if (one > 0.001f)
            {
                float supportZ = (aR.z * oneL + aL.z * oneR) / one;
                float s = Mathf.Clamp01(_shift.Value);
                hip.z = Mathf.Lerp(hip.z, supportZ, s);
                hip.y -= holdCrouch * s;
            }

            // 3. Lean. Forward lean always bends at the waist: the pelvis stays where the feet put it and the
            //    trunk pitches about it, held legs rising behind as the trunk goes down (see-saw) — one foot
            //    held or both, no special case. Backward lean (the swing back after a release) sits the whole
            //    body back about the feet.
            float wL = 1f - withL, wR = 1f - withR;
            float waistLean = Mathf.Max(0f, lean);
            float feetLean = Mathf.Min(0f, lean);
            Vector3 pivot = wL + wR > 0.05f ? (aL * wL + aR * wR) / (wL + wR) : (aL + aR) * 0.5f;
            pivot.y = _ground;
            Quaternion r = Quaternion.Euler(feetLean, 0f, 0f);
            hip = pivot + r * (hip - pivot);
            aL = Vector3.Lerp(aL, pivot + r * (aL - pivot), withL);
            aR = Vector3.Lerp(aR, pivot + r * (aR - pivot), withR);
            hip = KeepInReach(hip, aL, wL);
            hip = KeepInReach(hip, aR, wR);
            aL = SeeSaw(leftLeg, aL, hip, waistLean, withL);
            aR = SeeSaw(rightLeg, aR, hip, waistLean, withR);
            pitchL += (feetLean + waistLean) * withL;
            pitchR += (feetLean + waistLean) * withR;
            // A foot under the body keeps the pelvis over it; with nothing carrying it the pelvis leans along.
            float pelvisLean = feetLean + waistLean * Mathf.Lerp(1f, pelvisWaistShare, _carry);

            // 4. Body height from the planted foot / the push (the whole body moves, feet included).
            hip.y += bodyY;
            aL.y += bodyY;
            aR.y += bodyY;
            // Falling, the body stops on the ground: the pelvis on its own thickness, the feet on their soles
            // (the held legs fold as the body comes down on them — nothing straightens them).
            float under = _ground + pelvisClearance - hip.y;
            if (under > 0f)
            {
                hip.y += under;
                _bodyY.Value += under;
                _bodyY.Velocity = Mathf.Max(0f, _bodyY.Velocity);
            }
            aL.y = Mathf.Max(aL.y, _ground);
            aR.y = Mathf.Max(aR.y, _ground);

            // Write the rig.
            Quaternion rootRot = transform.rotation;
            hipTarget.position = transform.TransformPoint(hip);
            hipTarget.rotation = rootRot * Quaternion.Euler(p.pelvisPitch + pelvisLean, 0f, 0f) * _restHipRot;
            spineRootTarget.rotation = rootRot * Quaternion.Euler(lean + Mathf.Lerp(p.pelvisPitch, p.trunkPitch, 0.6f), 0f, 0f) * _restSpineRoot;
            WriteHead(lean + p.trunkPitch, p.headDown, rootRot);

            // Knees turn with the lean their leg takes part in: a planted leg only with the body tipping about
            // the feet, a held leg with the waist lean too.
            WriteLeg(leftLeg, aL, pitchL, hip, kneeL, feetLean + waistLean * withL, withL);
            WriteLeg(rightLeg, aR, pitchR, hip, kneeR, feetLean + waistLean * withR, withR);

            // Arms: locked to the opposite foot (left arm goes back as the right foot goes back), holds and
            // lying down included — a foot held back keeps its arm back.
            float swing = preview ? 0f : p.armSwing;
            StepArm(leftArm, p.leftArm, swing, FootSwing(rightLeg), dt);
            StepArm(rightArm, p.rightArm, swing, FootSwing(leftLeg), dt);
        }

        /// <summary>
        /// The head angle (deg, + = forward): the drop back between presses (only while presses keep coming;
        /// standing in the crouch the head looks ahead), the forward pile-up of wasted presses wearing off at
        /// headRecover, and the push of held feet — one foot capped at oneFootMax, both not. The head follows
        /// it through its own spring, so it lags and swings a little.
        /// </summary>
        float UpdateHead(RunnerSim sim, float dt)
        {
            // It drops back only once the stride has landed and no press has come since (a held foot stays
            // down too, but holding pushes the head forward, never back).
            if (sim.LandTime > sim.LastPressTime && !sim.Legs[0].Pressed && !sim.Legs[1].Pressed)
                _headFall.Step(headBack, headFallSpring.frequency, headFallSpring.damping, dt);
            // Capped where the waist is fully forward: more would only take longer to wear off.
            _headFwd = Mathf.Clamp(_headFwd - headRecover * dt, 0f, neckMax + sim.T.maxForwardLean);
            float hold = Mathf.Min(holdHead * (_holdL + _holdR), Mathf.Lerp(oneFootMax, 2f * holdHead, _holdL * _holdR));
            float target = _headFall.Value * _stepping + _headFwd + hold;
            _head.Step(target, headSpring.frequency, headSpring.damping, dt);
            return _head.Value;
        }

        /// <summary>
        /// Neck and head from the trunk's angle and the head angle. Up to neckMax of trunk lean the head still
        /// looks ahead, beyond that it goes down with the trunk (or with it all the way by the pose's headDown);
        /// the head angle comes on top. The neck bends with it, never past neckMax. The head is aimed by placing
        /// its look target.
        /// </summary>
        void WriteHead(float trunk, float headDown, Quaternion rootRot)
        {
            float neck = Mathf.Clamp(_headAngle, -neckMax, neckMax) * NeckShare;
            spineTipTarget.rotation = rootRot * Quaternion.Euler(trunk + neck, 0f, 0f) * _restSpineTip;
            if (!_headAim || !_headBone || !_lookTarget) return;

            float follow = Mathf.Lerp(Mathf.Max(0f, trunk - neckMax), trunk, headDown);
            float pitch = Mathf.Clamp(follow + _headAngle, trunk - neckMax + headBack, trunk + neckMax);
            _headAim.weight = 1f;
            _lookTarget.position = _headBone.position + rootRot * (Quaternion.Euler(pitch, 0f, 0f) * Vector3.forward) * _lookDistance;
        }

        /// <summary>Ankle of a foot placed by a pose (runner space): the rig's standing place + the pose's offset.</summary>
        Vector3 PoseAnkle(Leg leg, FootPose pose, float width) =>
            leg.restAnkle + new Vector3(Side(leg) * width, pose.ankle.y, pose.ankle.x);

        /// <summary>
        /// Body height (m, runner space) from the feet. A planted foot carries the body: the body sits
        /// so that the lowest planted foot is on the ground, following it through a stiff spring. When
        /// the last planted foot lifts off, the push gives the body an upward speed (by the press's
        /// efficiency) and a softer spring takes it back to its own height — overshooting into a short
        /// flight while no foot is down. With both feet held back no foot can land: the body falls under
        /// gravity and comes to rest on the ground (pelvisClearance).
        /// </summary>
        float UpdateBodyHeight(RunnerSim sim, float dt)
        {
            float support = float.MaxValue;
            if (leftLeg.contact) support = Mathf.Min(support, -leftLeg.height * leftLeg.support);
            if (rightLeg.contact) support = Mathf.Min(support, -rightLeg.height * rightLeg.support);

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
                // A foot that can land brings the body back to its height; with both held back nothing will
                // land, so the body falls until it is on the ground (stopped in Update).
                Spring back = _bodyY;
                back.Step(0f, bodySpring.frequency, bodySpring.damping, dt);
                float fallVelocity = _bodyY.Velocity - sim.T.gravity * dt;
                float fall = _bodyY.Value + fallVelocity * dt;
                _bodyY.Value = Mathf.Lerp(fall, back.Value, _carry);
                _bodyY.Velocity = Mathf.Lerp(fallVelocity, back.Velocity, _carry);
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

        /// <summary>
        /// The waist's own lean (deg, + forward), a spring that keeps it centred over the feet:
        ///   balance — feet behind their home (as mashing keeps them) lean it forward, ahead lean it back;
        ///   hold    — every held foot adds holdLean (both held = twice), so a hold leans it on and on;
        ///   speed   — a little more forward the faster the run;
        ///   head    — the head angle beyond neckMax pulls it after the head (× trunkFollow).
        /// Each push kicks it forward a little (see OnTapped) and the spring settles it with a small bounce;
        /// when a hold ends the target drops and the spring swings past centre: the sit back.
        /// The sim's own lean stays for the game (stumble losses); the look comes from here.
        /// </summary>
        float UpdateWaist(RunnerSim sim, float dt)
        {
            float held = sim.Legs[0].HoldValue + sim.Legs[1].HoldValue;
            float feet = (leftLeg.baseBall.z + rightLeg.baseBall.z) * 0.5f;
            float home = (leftLeg.midBall.z + rightLeg.midBall.z) * 0.5f;
            float balance = Mathf.Atan2(home - feet, Mathf.Max(0.1f, _legLength)) * Mathf.Rad2Deg * balanceGain;
            // The head beyond what the neck bends pulls the waist after it: the body follows the head.
            float headPull = Mathf.Max(0f, _headAngle - neckMax) * trunkFollow;
            float target = holdLean * held + balance + speedLean * sim.Speed / sim.T.maxSpeed * _stepping + headPull;
            _waist.Step(target, waistSpring.frequency, waistSpring.damping, dt);
            if (_waist.Value > sim.T.maxForwardLean || _waist.Value < -sim.T.maxBackLean)
            {
                _waist.Value = Mathf.Clamp(_waist.Value, -sim.T.maxBackLean, sim.T.maxForwardLean);
                _waist.Velocity = 0f;
            }
            return _waist.Value;
        }

        /// <summary>
        /// A held foot turns about its hip joint with the waist lean: as the trunk goes down in front, the
        /// leg held back rises behind, trunk and leg staying one line (the plane pose) — but never past
        /// heldLegMaxAngle from straight down (level). A planted foot stays.
        /// </summary>
        Vector3 SeeSaw(Leg leg, Vector3 ankle, Vector3 hip, float waistLean, float withBody)
        {
            if (withBody <= 0f || waistLean <= 0f) return ankle;
            Vector3 joint = hip + new Vector3(leg.restAnkle.x, 0f, 0f);
            Vector3 d = ankle - joint;
            float behind = Mathf.Atan2(-d.z, Mathf.Max(0.01f, -d.y)) * Mathf.Rad2Deg;   // leg angle back from straight down
            float turn = Mathf.Clamp(waistLean, 0f, Mathf.Max(0f, heldLegMaxAngle - behind));
            Vector3 turned = joint + Quaternion.Euler(turn, 0f, 0f) * d;
            turned.y = Mathf.Max(turned.y, _ground);
            return Vector3.Lerp(ankle, turned, withBody);
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

        /// <summary>Where the foot is on the step cycle: +1 = Front, 0 = home, −1 = Back.</summary>
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
            Vector3 front = loop.front, back = loop.back, mid = Home(leg, loop);
            leg.midBall = mid;
            Vector3 ball;
            float pitch;
            float touchdownPitch = Mathf.Lerp(accelProfile.touchdownPitch, maxSpeedProfile.touchdownPitch, _profileW);
            leg.toeRoll = 0f;
            leg.swingW = 0f;

            leg.support = 1f;
            switch (c.State)
            {
                case FootState.Ready:
                    // Home, on the ground: the foot rolls down flat and waits (the body crouches onto it).
                    ball = mid;
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                    break;
                case FootState.FrontWait:
                    // Waits at Front while the other foot is pushing or held back: in the air, but as a hold
                    // of the other foot builds up it comes down to the ground and carries the body.
                    ball = front;
                    ball.y = Mathf.Lerp(front.y, GroundY(leg), leg == leftLeg ? _holdR : _holdL);
                    pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                    break;
                case FootState.Reach:
                    // The other foot steps back: this one swings forward to Front.
                    ball = Swing(leg, front, Mathf.Clamp01(t / Mathf.Max(0.001f, c.Duration)), touchdownPitch, out pitch);
                    break;
                case FootState.Hold:
                    // Held back at Back (if the body comes down on the ground the foot stops on it, see Update).
                    ball = back;
                    pitch = pitchBack;
                    leg.toeRoll = Profile(pr => pr.toeRoll, 1f);
                    break;
                case FootState.Step:
                {
                    // Stance (push): foot flat, late heel rise, toes roll at toe-off.
                    float s = Mathf.Clamp01(t / Mathf.Max(0.001f, c.Duration));
                    // Along the lower arc to Back (it touches the ground only at the base).
                    ball = AlongLowerArc(loop, leg.fromBall, 1f, s);
                    // A step can start anywhere (e.g. mid-return): ease from there onto the curve.
                    pitch = Mathf.Lerp(leg.fromPitch, Profile(pr => pr.stancePitch, s), Mathf.SmoothStep(0f, 1f, s / 0.3f));
                    leg.toeRoll = Profile(pr => pr.toeRoll, s);
                    break;
                }
                case FootState.Return:
                {
                    float s = Mathf.Clamp01(t / Mathf.Max(0.001f, c.Duration));
                    if (c.FromFront)
                    {
                        // From ahead: comes back home along the ground, rolling flat, and the body comes
                        // down onto it as it goes (home sits higher in the body's frame = the crouch).
                        // Without a Rest point home is the base: down the lower arc, touching the ground only there.
                        ball = leg.rest
                            ? EllipseLoop(leg.fromBall, mid, s, 0f)
                            : AlongLowerArc(loop, leg.fromBall, loop.baseU, s);
                        pitch = Mathf.Lerp(leg.fromPitch, 0f, Mathf.SmoothStep(0f, 1f, t / HeelRollTime));
                        leg.support = Mathf.SmoothStep(0f, 1f, s);
                    }
                    else
                    {
                        // From Back: swings through the air and lands on the heel at home.
                        ball = Swing(leg, mid, s, touchdownPitch, out pitch);
                    }
                    break;
                }
                default: // Grounded (reset): settle onto home, same as Ready.
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
            if (c.State != FootState.Grounded) ball.y = Mathf.Max(ball.y, GroundY(leg));
            // Toes up: the heel would dip under the ground, so the foot stands on its heel instead.
            float heelY = ball.y + (Quaternion.Euler(pitch, 0f, 0f) * HeelFromBall(leg)).y;
            if (heelY < GroundY(leg)) ball.y += GroundY(leg) - heelY;

            leg.baseBall = ball;
            leg.basePitch = pitch;
            leg.height = ball.y - GroundY(leg);
            UpdateContact(leg);
        }

        /// <summary>
        /// Is this foot planted (carrying the body)? A foot home is (both, waiting with no key down), and so
        /// is a foot coming back home from ahead along the ground — the body sinks onto it as it goes (see
        /// support). A foot waiting at Front or anywhere else on the loop stays in the air (the body is not
        /// pulled down to it). A pushing foot that started on the ground stays planted until it has risen
        /// stanceCompression in the body's frame, then it lifts off for the rest of the step.
        /// </summary>
        void UpdateContact(Leg leg)
        {
            const float onGround = 0.01f; // ball this close to the ground counts as touching it (m)
            var c = leg.cycle;
            switch (c.State)
            {
                case FootState.Ready:
                case FootState.Grounded:
                    leg.contact = true;
                    break;
                case FootState.Return:
                    leg.contact = c.FromFront || leg.height <= onGround;
                    break;
                case FootState.Step:
                    if (leg.contact && leg.height > stanceCompression) leg.liftedOff = true;
                    leg.contact = !leg.liftedOff && (leg.contact || leg.height <= onGround);
                    break;
                case FootState.Hold:
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

        /// <summary>
        /// Where the foot goes home: standing still its Rest point (the crouch), running the loop's base
        /// (running height), sliding between the two by speed (restToBaseSpeed). Without a Rest point the base.
        /// </summary>
        Vector3 Home(Leg leg, Loop loop) => leg.rest ? Vector3.Lerp(Local(leg.rest), loop.Base, _runHome) : loop.Base;

        /// <summary>Home of the foot (set each frame by ComputeLeg).</summary>
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

        /// <summary>
        /// The arm follows the opposite foot exactly (no spring): back when it is back, level when it is home.
        /// Swinging back the elbow opens as the arm passes the body and closes again behind; forward it does not.
        /// </summary>
        void StepArm(Arm arm, ArmPose pose, float swingAmount, float oppositeSwing, float dt)
        {
            // −1 … +1: the arm is back when the opposite foot is back, level when it is home.
            float s = oppositeSwing * swingAmount;
            // Which way it is going: kept while the foot stands still (a hold keeps its last direction).
            float moved = oppositeSwing - arm.lastFoot;
            arm.lastFoot = oppositeSwing;
            if (Mathf.Abs(moved) > 1e-4f)
                arm.back = Mathf.MoveTowards(arm.back, moved < 0f ? 1f : 0f, dt * 12f);
            arm.swing = pose.swing + (s >= 0f ? armFront * s : -armBack * s);
            arm.elbow = pose.elbow + elbowSwing * s - elbowOpenBack * arm.back * (1f - Mathf.Abs(s));
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
            float swing = arm.swing;
            float elbow = Mathf.Max(0f, arm.elbow);

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
        /// point of the loop on the ground), cyan = Rest (where the foot stands with no input), upper arc =
        /// swing, lower arc = push. Outside play mode the ground comes straight from the toe bone (nothing is
        /// bound, so nothing in the rig is changed).
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
            if (leg.rest)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(leg.rest.position, 0.03f);
                // How far the body crouches standing on it: Rest above the ground in the body's frame.
                Vector3 groundBelow = transform.TransformPoint(new Vector3(Local(leg.rest).x, ground, Local(leg.rest).z));
                Gizmos.DrawLine(leg.rest.position, groundBelow);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(leg.rest.position + transform.up * 0.06f, "Bekleme");
#endif
            }
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
