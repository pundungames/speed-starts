using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Visual only: turns RunnerSim's state into Control Rig targets. Never changes speed or timing.
    ///
    /// Per frame:
    ///   1. Key poses are blended: Ready → Run by speed, → Stumble by lean beyond the pose's own. They give
    ///      the trunk, head and arms; the legs come from their own angles (2).
    ///   2. Legs are angles, not foot paths: every leg has a thigh angle (at the hip, from straight down) and
    ///      a knee angle (180 = straight). The motion starts at the thigh; the shin gets its angle by the way
    ///      the leg is going, and wherever the foot ends up is the step. Forward kinematics turns the angles
    ///      into knee and ankle points, and the leg IK only reproduces them (target = ankle, hint = in front
    ///      of the knee). Phases (see OnTapped / TickLegs / Target):
    ///        press        — that leg pushes: thigh back, knee opens straight (Push); the other leg reaches:
    ///                       its thigh stays forward, only the shin opens forward and the foot lands ahead
    ///                       (Reach → Front). A leg still going back (Push, Recover) finishes that and then
    ///                       reaches instead of going to mid; a leg already coming forward (Mid) turns its way
    ///                       to the front at the speed it has — never a snap (ReachOn);
    ///        after a push — the thigh stays back while the shin folds up (Recover), then the leg goes on its
    ///                       own to a mid angle (Mid) and home (Rest);
    ///        held         — the pushed leg goes on into the line of the hips (Hold, HoldLine) and goes up behind
    ///                       as they tip; the leg in front turns into the support angles as the hold builds up;
    ///        no input     — home: the crouch angles (blended to the running home while presses keep coming).
    ///      Every phase starts from the leg's current angles, so a press mid-swing goes on back from there.
    ///   3. The lean runs down a chain from the head (UpdateLean): the sim's head angle is the measure; chest,
    ///      waist base and hips take smaller and smaller shares of it (chestShare, waistShare, hipShare), each
    ///      following the one above through a spring, so the back rounds like a chain. Speed (hipRunLean) and both
    ///      keys held (bothLean, toward level: the dive, with its hump, spineLevelCurve) lean the whole body. The
    ///      hips pitch within pelvisPitchRange. Legs in the air and held legs turn with the hips; a leg on the
    ///      ground keeps its angle to the ground (FollowHips). Height still comes from the legs (4).
    ///   Head and neck: one head angle, back ↔ upright ↔ forward, computed by the sim (RunnerSim.HeadAngle,
    ///      it also takes speed off presses made with the head down) and drawn here. One input per key: down or
    ///      up — a tap and a hold are the same thing, a hold only lasts longer. While a key is down the head
    ///      goes forward (fast at first, headKick in all, then on at holdPush) and does not come back; with no
    ///      key down it comes back through a spring (headReturnSpring), past upright and back again. Nothing
    ///      judges the press: presses coming before the head is back pile it up (mashing), presses on the
    ///      rhythm balance it about upright, sparse ones let it drop back to headBack. The neck bends what the
    ///      hips have not followed yet.
    ///   4. Body height comes from the leg angles: the pelvis sits so that the lowest carrying foot (any leg not
    ///      held back) stands on the ground. Wider angles = longer legs = the body rises; folding them brings
    ///      it down. When the last foot leaves the ground — however it gets there, both keys together or one
    ///      after the other — nothing carries the body: it flies on with the rise its push gave it, under
    ///      constant gravity (liftKick: the last push throws it up a little), going forward at the run's speed,
    ///      the hips tipped by the held keys, and comes down onto the ground: the dive, no special case. Head, pelvis, knees and
    ///      feet never go into the ground: the body stops on the first that touches. Letting go, the legs come
    ///      back under and lift it (softly from low down, ~0.5 s). See UpdateBodyHeight / KeepAboveGround.
    /// One key moves one leg and one arm (right key: right leg + left arm): the arm follows that leg's thigh
    /// going back (its push, its hold, its way home) and stays at rest while the leg reaches forward; no lag.
    /// Arms, knees' twist, ankles and toes are posed in LateUpdate after the rig.
    /// The body only pitches around X; Y and Z rotations stay 0.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RunnerPoseDriver : MonoBehaviour
    {
        public enum LegPhase
        {
            /// <summary>Home, waiting (the crouch, or the running home while presses keep coming).</summary>
            Rest,
            /// <summary>Pressed: thigh back, knee opening straight.</summary>
            Push,
            /// <summary>After the push: thigh stays back, shin folds up.</summary>
            Recover,
            /// <summary>On its own to the mid angle (thigh about upright, knee folded), then home.</summary>
            Mid,
            /// <summary>The other leg was pressed: thigh stays forward, shin opens, the foot lands ahead.</summary>
            Reach,
            /// <summary>
            /// Landed in front, on the ground while the other leg is still pushing, recovering or held: the foot stays
            /// put, the leg turns back toward home at the body's speed.
            /// </summary>
            Front,
            /// <summary>Still held after the push: back and straight, rising behind with the waist lean.</summary>
            Hold,
            /// <summary>
            /// Recovered and waiting behind, shin folded, for the other key (it then comes forward at once): the legs
            /// stay apart between sparse presses instead of drifting home. No press for openHoldTime: on to mid.
            /// </summary>
            Wait,
        }

        /// <summary>One leg's angles (deg): thigh from straight down (+ = forward), knee between thigh and shin.</summary>
        [System.Serializable]
        public struct LegAngles
        {
            [Tooltip("Thigh at the hip, from straight down (deg, + = forward).")]
            public float thigh;
            [Tooltip("Knee, between thigh and shin (deg, 180 = straight).")]
            public float knee;

            public LegAngles(float thigh, float knee)
            {
                this.thigh = thigh;
                this.knee = knee;
            }

            public static LegAngles Lerp(LegAngles a, LegAngles b, float t) =>
                new LegAngles(Mathf.LerpUnclamped(a.thigh, b.thigh, t), Mathf.LerpUnclamped(a.knee, b.knee, t));
        }

        [System.Serializable]
        public sealed class Leg
        {
            [Tooltip("Leg IK (TwoBoneIK) target: the ankle is placed here.")]
            public Transform target;
            [Tooltip("Leg IK hint: placed in front of the knee.")]
            public Transform hint;
            [Tooltip("Found by name if empty (B-shin.L/R, B-foot.L/R, B-toe.L/R).")]
            public Transform shinBone;
            [Tooltip("Found by name if empty.")]
            public Transform footBone;
            [Tooltip("Found by name if empty.")]
            public Transform toeBone;

            [HideInInspector] public Vector3 restAnkle, ballOffset, toeInFoot, restHingeInThigh, restHingeInShin;
            [HideInInspector] public Vector3 hipJointFromHip;               // thigh joint from the hip target (runner space)
            [HideInInspector] public float thighLength, shinLength;
            [HideInInspector] public Quaternion restFootInShin, restFootRunner, restToeInFoot;
            [HideInInspector] public LegPhase phase;
            [HideInInspector] public double phaseTime;  // sim time the phase began
            [HideInInspector] public float duration;    // blend from the angles at the start into the phase's (s)
            [HideInInspector] public bool held;         // this leg's key is down
            [HideInInspector] public LegAngles from, angles;   // thigh relative to the hips' lean (see follow)
            [HideInInspector] public float follow;      // 1 = turns with the hips' lean (in the air, held), 0 = keeps to the ground
            [HideInInspector] public float thighWorld;  // the thigh this frame after the hips' turn (deg from straight down)
            [HideInInspector] public Vector3 knee, ankle; // this frame, from the angles (runner space)
            [HideInInspector] public float pitch;       // final foot pitch (deg), applied after IK
            [HideInInspector] public float rock;        // extra foot pitch sitting back on the heel (deg, − = toes up)
            [HideInInspector] public float fromPitch;   // foot pitch when the phase began
            [HideInInspector] public float strideW = 1f;
            [HideInInspector] public float width;       // extra sideways spread from the pose (m)
            [HideInInspector] public float toeRoll;     // toes rolling over their tips (deg, end of the push)
            [HideInInspector] public float swingAnkle;  // ankle bend relative to the shin in the air (deg)
            [HideInInspector] public float swingW;      // 1 = ankle follows swingAnkle, 0 = foot pitch
            [HideInInspector] public float jumpAnkle;   // ankle bend on the shin in a jump (deg, + = toes down)
            [HideInInspector] public float jumpW;       // 1 = the ankle takes jumpAnkle, toes in line
            [HideInInspector] public float sideOut;     // thigh turned out of the leg's plane (deg, + = away from the body)
            [HideInInspector] public Quaternion sideTurn = Quaternion.identity; // that turn, this frame
            [HideInInspector] public float swingToe;    // toe bend in the air (deg, + = toes up)
            [HideInInspector] public bool reachNext;    // the other key went down while this leg was going back: reach once back
            [HideInInspector] public bool easeOut;      // this phase starts at the leg's own speed and slows into its target
            [HideInInspector] public bool swing;        // recover, mid and home as one curve (no stop between them)
            [HideInInspector] public double swingStart; // sim time the swing began (end of the push)
            [HideInInspector] public float swingTime;   // recover + mid + home of this swing (s)
            [HideInInspector] public float thighRate;   // the thigh's angular speed last frame (deg/s, + = forward)
            [System.NonSerialized] public bool grounded = true; // last frame its foot was down on the ground
            [System.NonSerialized] public double landStart = -1.0; // reached out: sim time it began coming down to land (− = not yet)
            [System.NonSerialized] public bool stay;    // in front and staying there while the other key is pressed again
            [System.NonSerialized] public Spring kneeLag;   // the knee trailing its target in the air (kneeSpring)
            [System.NonSerialized] public float holdFromWorld, holdFromKnee; // where a hold began (thigh world, knee)
            public Vector3 RestBall => restAnkle + ballOffset;
            public double EndTime => phaseTime + duration;
            /// <summary>Push, recover, mid and reach run their time and hand over; the others wait.</summary>
            public bool Timed => phase == LegPhase.Push || phase == LegPhase.Recover || phase == LegPhase.Mid ||
                                 phase == LegPhase.Reach;
            /// <summary>Key down and pushing or held back: it stays back (the other leg cannot bring it forward).</summary>
            public bool HeldBack => held && (phase == LegPhase.Push || phase == LegPhase.Hold);
        }

        /// <summary>
        /// Foot motion over the push and the swing, as a function of phase (0..1).
        /// Heel-strike running: the foot lands toes up on the heel and rolls down flat, stays flat while
        /// loaded, then the heel rises late and fast and the toes roll over their tips at toe-off; in the air
        /// the ankle plantarflexes after toe-off (heel kicks up), then pulls the toes up for the next landing.
        /// </summary>
        [System.Serializable]
        public sealed class FootProfile
        {
            [Tooltip("Front foot angle (deg, − = toes up): it lands heel first and stays so while planted ahead, " +
                     "rolling flat only as it goes home (the reference video: ~17° toes up).")]
            public float touchdownPitch = -17f;
            [Tooltip("Push: foot angle to the ground (deg, + = heel up). 0 = push starts (foot flat), 1 = toe-off.")]
            public AnimationCurve stancePitch;
            [Tooltip("Push: toes roll over their tips, lifting the ball (deg).")]
            public AnimationCurve toeRoll;
            [Tooltip("Swing (recover + mid): ankle bend relative to the shin (deg, + = toes down / plantarflexion).")]
            public AnimationCurve swingAnkle;
            [Tooltip("Swing (recover + mid): toe bend relative to the foot (deg, + = toes up).")]
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
            [Tooltip("Found by name if empty.")]
            public Transform forearm;
            [Tooltip("Arm IK target: its constraint is turned off at runtime.")]
            public Transform ikTarget;

            [HideInInspector] public Transform shoulder;
            [HideInInspector] public Quaternion restShoulderLocal, chestRest, restUpperLocal, restForeInParent, parentRest;
            [HideInInspector] public Vector3 restDir;
            [HideInInspector] public float side;
            [HideInInspector] public float swing, elbow;   // this frame's pose (deg)
            // The tween toward the opposite leg's phase (StepArm).
            [System.NonSerialized] public float fromSwing, fromElbow, toSwing, toElbow, tweenTime;
            [System.NonSerialized] public double tweenStart = -1.0;
            [System.NonSerialized] public LegPhase legPhase;
            [System.NonSerialized] public double legPhaseTime = -1.0;
        }

        /// <summary>The shape of a move over its time (0..1 → 0..1).</summary>
        public enum EaseShape
        {
            /// <summary>The curve next to it (edit it in the Inspector).</summary>
            Custom,
            Linear,
            /// <summary>Slow out, quick at the end.</summary>
            EaseInCirc,
            /// <summary>Quick out, slowing down into its place at the end.</summary>
            EaseOutQuint,
        }

        static float Ease(EaseShape shape, AnimationCurve custom, float s)
        {
            s = Mathf.Clamp01(s);
            switch (shape)
            {
                case EaseShape.Linear: return s;
                case EaseShape.EaseInCirc: return 1f - Mathf.Sqrt(1f - s * s);
                case EaseShape.EaseOutQuint: return 1f - Mathf.Pow(1f - s, 5f);
                default: return custom != null && custom.length > 0 ? Mathf.Clamp01(custom.Evaluate(s)) : s;
            }
        }

        [Tooltip("The physics this draws (found on this object if empty). The visual never changes speed or timing.")]
        public RunnerBody body;

        [Header("Rig")]
        [Tooltip("Hip target (MultiParent → B-hips): the pelvis' place and pitch.")]
        public Transform hipTarget;
        [Tooltip("Spine Root Target (TwistChain root, B-spine): the waist base's angle.")]
        public Transform spineRootTarget;
        [Tooltip("Spine Tip Target (TwistChain tip, B-neck): the waist top's angle; B-chest is filled in between.")]
        public Transform spineTipTarget;
        [Tooltip("Left leg: IK target, hint and bones.")]
        public Leg leftLeg = new Leg();
        [Tooltip("Right leg: IK target, hint and bones.")]
        public Leg rightLeg = new Leg();
        [Tooltip("Left arm: bones and its IK target (turned off in play).")]
        public Arm leftArm = new Arm();
        [Tooltip("Right arm: bones and its IK target (turned off in play).")]
        public Arm rightArm = new Arm();

        [Header("Key poses (empty = built-in default): trunk, head, arms, feet width")]
        [Tooltip("Waiting with no input: the crouch at the line.")]
        public RunnerPose ready;
        [Tooltip("Running at speed (fully from runPoseSpeed on).")]
        public RunnerPose run;
        [Tooltip("Blended in while the sim's lean is past its pose's own (stumbling).")]
        public RunnerPose stumble;

        [Header("Blend")]
        [Tooltip("Speed (m/s) at which the Run pose has fully replaced Ready.")]
        public float runPoseSpeed = 4f;
        [Tooltip("Lean beyond the pose's own (deg) over which Stumble comes in, ending at the sim's stumbleLean.")]
        public float stumbleBlendRange = 15f;

        [Header("Leg angles (deg): thigh from straight down, + = forward; knee between thigh and shin, 180 = straight")]
        [Tooltip("Waiting with no input (the crouch). How low the body stands comes from these angles.")]
        public LegAngles crouch = new LegAngles(40f, 100f);
        [Tooltip("Home while presses keep coming at speed (running height); blended from the crouch by restToBaseSpeed.")]
        public LegAngles runHome = new LegAngles(10f, 160f);
        [Tooltip("End of a push: the pressed leg back, the knee opened, the toes still on the ground behind.")]
        public LegAngles pushEnd = new LegAngles(-30f, 150f);
        [Tooltip("After the push: the thigh goes on back, the shin folds up behind (heel up to about hip height).")]
        public LegAngles recover = new LegAngles(-40f, 100f);
        [Tooltip("Where the leg goes on its own after recovering (thigh about upright, knee folded); then home.")]
        public LegAngles mid = new LegAngles(15f, 70f);
        [Tooltip("The other leg on a press: it opens forward nearly straight and the foot lands well ahead " +
                 "(the reference video: ~47° from straight down).")]
        public LegAngles reach = new LegAngles(47f, 175f);
        [Tooltip("pushEnd at stride speed (strideSpeed.y): the faster the run, the further back the push ends (the " +
                 "reference video: the legs open ~85° slow, ~110° fast).")]
        public LegAngles pushEndFast = new LegAngles(-60f, 160f);
        [Tooltip("recover at stride speed (strideSpeed.y): the thigh further back, the shin less folded.")]
        public LegAngles recoverFast = new LegAngles(-75f, 135f);
        [Tooltip("reach at stride speed (strideSpeed.y): the leg opens further forward, the knee a little bent.")]
        public LegAngles reachFast = new LegAngles(70f, 145f);
        [Tooltip("Speed (m/s) from which (x) to which (y) pushEnd, recover and reach turn into their Fast angles " +
                 "(linearly): the stride opens with speed.")]
        public Vector2 strideSpeed = new Vector2(2f, 8f);
        [Tooltip("Coming forward into its reach the knee first folds (heel up) to this angle, then the shin opens out " +
                 "forward to the reach's knee: x slow, y fast (strideSpeed). Lower = the heel comes up higher and the " +
                 "shin flicks out more. A leg already more folded only opens.")]
        public Vector2 reachFoldKnee = new Vector2(100f, 55f);
        [Tooltip("Share of the thigh's way forward by which the knee is folded (heel up behind).")]
        [Range(0.05f, 0.9f)] public float reachFoldAt = 0.45f;
        [Tooltip("The knee stays folded until the thigh has come this share of its way to its place in front; only " +
                 "then does the shin open out forward (1 = only once the thigh is there).")]
        [Range(0.1f, 0.99f)] public float reachOpenAt = 0.85f;
        [Tooltip("Reached out and still in the air, the leg comes down and back to this touchdown pose before the foot " +
                 "lands (real sprinting: the foot lands just ahead of the body, not at full reach), so the body does not " +
                 "sink to meet a leg held out level.")]
        public LegAngles reachLand = new LegAngles(35f, 155f);
        [Tooltip("How long (s) after its reach the leg takes to come down to reachLand.")]
        public float reachLandTime = 0.1f;
        [Tooltip("A held leg's knee at a full hold (deg): a little bent, not dead straight. Its thigh trails the hips " +
                 "in their line (heldLegArch), so as the hips tip forward the leg goes up behind with them.")]
        public float holdKnee = 145f;
        [Header("One key held (the other free): the held leg's own way back, then the body comes into its line")]
        [Tooltip("One key held: the held leg goes back to this thigh angle (deg from straight down, world) on its own, " +
                 "while the body is still upright; once the tipping body's line is further back it goes on with it. " +
                 "The reference game: ~−65°, the knee straight, then the body tips into one line with it.")]
        public float holdThigh = -65f;
        [Tooltip("One key held: the knee opens from the push's bend to this (deg) as the thigh goes back (same way).")]
        public float holdStraightKnee = 165f;
        [Tooltip("One key held: how long (s) the leg takes from the end of its push to holdThigh / holdStraightKnee.")]
        public float holdReachTime = 0.18f;
        [Tooltip("The leg in front while the other is held, as the hold starts (the body still upright): it opens to " +
                 "holdSupport as the body tips forward over the planted foot (the shin stays upright).")]
        public LegAngles holdSupportStart = new LegAngles(55f, 110f);
        [Tooltip("The leg in front while the other is held, at a full hold, carrying the body. thigh + knee = 180 " +
                 "stands the shin upright, the pelvis behind and above the foot.")]
        public LegAngles holdSupport = new LegAngles(80f, 100f);
        [Tooltip("The knee is never quite straight (the IK snaps at full reach).")]
        public float maxKnee = 177f;
        [Tooltip("A held leg's thigh trails the line of the hips this far past it (deg; − = short of it, which the " +
                 "slightly bent knee makes up): the held legs and the body make one nearly straight line (the dive).")]
        public float heldLegArch = -10f;

        [Header("Leg timing (s)")]
        [Tooltip("The other leg's reach forward on a press (s), quicker with speed (animSpeedUp), and the shortest the " +
                 "landed front leg takes going back toward home (see frontGroundMax).")]
        public float pushTime = 0.2f;
        [Tooltip("The pressed leg's push back (s): x slow, y fast (strideSpeed). Short and quick at any speed, as in " +
                 "the reference game (the leg is back about 0.15 s after the press, with the fold after it).")]
        public Vector2 pushBackTime = new Vector2(0.12f, 0.08f);
        [Tooltip("The push's shape over its time: EaseInCirc = slow out of home/mid, quick at the end; EaseOutQuint = " +
                 "quick out, slowing down into the back; Linear; Custom = pushCurve below.")]
        public EaseShape pushEase = EaseShape.EaseOutQuint;
        [Tooltip("pushEase Custom: how the thigh goes over the push's time (0..1 → 0..1): an ease-in toward easeInCirc, starting slow " +
                 "out of home/mid and speeding up to the end (the reference game), its end slope kept moderate so the " +
                 "leg does not flick in the last frames. The fold after it takes over at that speed; the knee opens on " +
                 "a smooth curve of its own.")]
        public AnimationCurve pushCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.5f, 0.22f, 0.9f, 0.9f), new Keyframe(1f, 1f, 2.2f, 2.2f));
        [Tooltip("The leg reaching forward (the other key's press) over its time: EaseOutQuint = quick out, slowing " +
                 "into its place in front; EaseInCirc = slow out, quick at the end; Linear; Custom = reachCurve. The " +
                 "knee folds and opens by how far the thigh has come (reachFoldAt / reachOpenAt), so it follows along.")]
        public EaseShape reachEase = EaseShape.EaseOutQuint;
        [Tooltip("reachEase Custom: the reach over its time (0..1 → 0..1).")]
        public AnimationCurve reachCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("In the air (folding up, coming through, waiting behind, reaching) the knee follows its angle through " +
                 "this spring instead of exactly: the thigh leads and the shin trails it a moment, as a loose limb does, " +
                 "so the lower leg never snaps (lower frequency = more lag). On the ground the knee is exact. " +
                 "Frequency 0 = off.")]
        public SpringSettings kneeSpring = new SpringSettings(9f, 0.9f);
        [Tooltip("At max speed the push and the swing take this share of their time (1 = as slow as standing): the " +
                 "faster the run, the quicker the legs and arms, so they keep up with presses on time.")]
        [Range(0.2f, 1f)] public float animSpeedUp = 0.5f;
        [Tooltip("Shin folding up after the push. recover + mid + home are shares of the swing: scaled together so " +
                 "push + recover + mid + home take swingSteps of the sim's step time, one curve with no stop between them.")]
        public float recoverTime = 0.15f;
        [Tooltip("From recovered to the mid angle (a share of the step, see recoverTime).")]
        public float midTime = 0.15f;
        [Tooltip("From the mid angle to home (a share of the step, see recoverTime).")]
        public float homeTime = 0.15f;
        [Tooltip("The leg in front goes home this slowly once the other is no longer pushing or held (the body passes " +
                 "over it); scaled with the step like recoverTime.")]
        public float frontHomeTime = 0.3f;
        [Tooltip("A leg that has pushed and folded up waits behind for the other key this long at most (s) after the " +
                 "last press (the reference game: between sparse presses the legs stay apart, then change over quickly); " +
                 "with no press by then it comes on home.")]
        public float openHoldTime = 0.6f;
        [Tooltip("A leg landed in front goes back toward home at the body's speed (the foot stays where it landed) but " +
                 "takes at most this long (s): slow, the legs stay apart longer.")]
        public float frontGroundMax = 0.5f;
        [Tooltip("Between presses the leg that pushed folds up and comes in under the body to wait here for the next " +
                 "press (the reference game: ~+10…+15° / ~85°, the foot under the hips).")]
        public LegAngles waitPose = new LegAngles(12f, 85f);
        [Tooltip("How long (s) the leg takes from its fold to waitPose.")]
        public float returnTime = 0.18f;
        [Tooltip("The same key pressed again with the other leg already in front: that leg stays in front at this pose " +
                 "(the reference game: ~+40° / ~160°, nearly straight), going with the body (the foot slides a little).")]
        public LegAngles frontStay = new LegAngles(40f, 160f);
        [Tooltip("Slow (strideSpeed.x), while the other leg waits under the body, the staying front leg's knee gives to " +
                 "this (deg); fast it stays at frontStay's knee.")]
        public float frontStayBentKnee = 135f;
        [Tooltip("How long (s) a leg takes to go from where it is into frontStay.")]
        public float frontStayTime = 0.15f;
        [Tooltip("The other key held on this long (s) after its push counts as a real hold: the leg staying in front " +
                 "in the air then comes down to take the body (quicker presses keep it in the air).")]
        public float holdLandDelay = 0.1f;
        [Tooltip("A leg's whole cycle (push + recover + mid + home) takes this many of the sim's steps (its StepTime, " +
                 "from the speed): 2 = one stride, the legs keep the body's pace however sparse the presses; a leg done " +
                 "early waits at home for the next press. Lower = quicker legs.")]
        public float swingSteps = 2f;
        [Tooltip("While a key is down, home is the crouch below x m/s and the running home above y. With no key down " +
                 "home is the crouch: the runner heads straight for it, no upright stance on the way.")]
        public Vector2 restToBaseSpeed = new Vector2(0.5f, 3f);
        [Tooltip("How fast it turns from the run stance to the Ready crouch and back (1/s).")]
        public float stanceBlend = 8f;

        [Header("Body height (from the leg angles: the lowest carrying foot stands on the ground)")]
        [Tooltip("The pelvis follows the height the legs give through this spring (a foot it lags under is kept on the ground).")]
        public SpringSettings heightSpring = new SpringSettings(8f, 0.8f);
        [Tooltip("Coming down between steps (the legs ask for a lower body than it has): the pelvis sinks through this " +
                 "softer spring instead, so the front foot reaches the ground a moment before the body settles onto it. " +
                 "Going up stays on heightSpring (a foot never goes into the ground).")]
        public SpringSettings dropSpring = new SpringSettings(3f, 0.6f);
        [Tooltip("Getting up from low down (the body more than liftBelow under its standing height, fully so liftRange " +
                 "further down): the height goes through this softer spring instead (~0.5 s up off the ground). Running and " +
                 "the crouch stay above liftBelow and keep heightSpring.")]
        public SpringSettings liftSpring = new SpringSettings(0.9f, 1f);
        [Tooltip("How far under its standing height (m) the body starts getting up through liftSpring.")]
        public float liftBelow = 0.25f;
        [Tooltip("Over how much further down (m) liftSpring takes over fully.")]
        public float liftRange = 0.2f;
        [Tooltip("Upward speed the last foot's push gives the body as it leaves the ground with nothing left to carry it (m/s).")]
        public float liftKick = 1.5f;
        [Tooltip("Running, both feet off the ground (the leg in front still reaching, the other folded behind): the body " +
                 "flies on, leaving with at least this upward speed (m/s), under flightGravity, until a foot lands. " +
                 "It never drops onto a foot still in the air. With flightGravity: 0.9 / 15 = a hop of about 3 cm, " +
                 "0.12 s up and down.")]
        public float flightLift = 0.9f;
        [Tooltip("Gravity of that running flight (m/s²): higher = a heavier, shorter hop (9.8 is real; the sim's own " +
                 "gravity stays for the dive).")]
        public float flightGravity = 15f;
        [Tooltip("A leg reached out forward stays so in the air until its foot comes down on the ground, but at most " +
                 "this long after its reach is done (s).")]
        public float reachAirMax = 0.5f;

        [Header("Ground: nothing goes into it (with no foot carrying, the body stops on the first part down)")]
        [Tooltip("Head bone height above the ground when the head lies on it (m).")]
        public float headRest = 0.15f;
        [Tooltip("Knee joint height above the ground when the knee is on it (m).")]
        public float kneeRest = 0.05f;

        [Header("Foot")]
        [Tooltip("Foot angle while a foot is held back (deg).")]
        public float pitchBack = 35f;

        [Header("Foot motion (blended by speed: acceleration → max velocity)")]
        public FootProfile accelProfile = new FootProfile
        {
            touchdownPitch = -17f,
            stancePitch = Curve(0f, 0f, 0.35f, 3f, 0.6f, 14f, 0.8f, 32f, 1f, 52f),
            toeRoll = Curve(0f, 0f, 0.7f, 0f, 1f, 22f),
            swingAnkle = Curve(0f, 24f, 0.15f, 28f, 0.4f, 14f, 0.65f, -4f, 0.85f, -12f, 1f, -10f),
            swingToe = Curve(0f, 30f, 0.3f, 8f, 0.7f, 14f, 1f, 12f),
        };
        public FootProfile maxSpeedProfile = new FootProfile
        {
            touchdownPitch = -15f,
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

        [Header("Ankle (relative to the shin)")]
        [Tooltip("Max toes-up bend of the ankle (dorsiflexion, deg).")]
        public float ankleUpMax = 20f;
        [Tooltip("Max toes-down bend of the ankle (plantarflexion, deg).")]
        public float ankleDownMax = 50f;

        [Header("Head & neck (deg, + = forward): the head angle itself and its springs are in RunnerBody → Tuning → Head")]
        [Tooltip("Most the neck bends on its own. Also how far the pose's trunk may lean while the head still looks ahead.")]
        public float neckMax = 35f;
        [Tooltip("Most the head goes on ahead of the trunk (deg): past it the head stays in line with the body (the dive: " +
                 "the hips stop at level, the head does not fold down under them).")]
        public float headLead = 30f;
        [Tooltip("Sitting back the head takes this share of the body's lean back (world); the neck bends the rest " +
                 "forward, so the head keeps looking ahead, chin in (the reference video).")]
        [Range(0f, 1f)] public float headKeep = 0.33f;
        [Tooltip("Sitting back the feet on the ground rock onto their heels by this share of the lean: toes up, the " +
                 "whole body turning about the heels.")]
        [Range(0f, 1f)] public float heelRock = 1f;
        [Tooltip("Leaning forward B-neck bends this much less (deg) than the waist chain gives it; the head keeps its " +
                 "own angle. The head stays in line with the body, so the body, not a curled-in head, comes down to " +
                 "the ground. Comes in with the forward lean, all of it from neckTrimLean (deg) on.")]
        public float neckTrim = 10f;
        [Tooltip("Forward lean (deg) from which all of neckTrim is in.")]
        public float neckTrimLean = 30f;
        [Tooltip("The top of the chain (chest, neck base) follows the head through this spring: the head moves first.")]
        public SpringSettings hipFollowSpring = new SpringSettings(4f, 0.9f);

        [Header("Body: a chain from the head (the sim's head angle, the measure) down to the hips")]
        [Tooltip("Share of the head's forward angle the chest (top of the waist) takes. Each link down takes less, so " +
                 "every link bends a little on the one below: the back rounds like a chain, never one stiff line.")]
        [Range(0f, 1f)] public float chestShare = 0.78f;
        [Tooltip("Share of the head's forward angle the base of the waist (B-spine) takes (below chestShare).")]
        [Range(0f, 1f)] public float waistShare = 0.6f;
        [Tooltip("Share of the head's forward angle the hips take (the last link; the legs turn with them).")]
        [Range(0f, 1f)] public float hipShare = 0.45f;
        [Tooltip("Each link down (waist base, hips) follows the one above through this spring (damping 1: it lags, " +
                 "never swings): the bend flows down the back, deeper while the head moves.")]
        public SpringSettings waistSpring = new SpringSettings(3f, 1f);
        [Tooltip("The whole body's forward lean while running (deg, every link alike): x standing, y at max speed.")]
        public Vector2 hipRunLean = new Vector2(3f, 8f);
        [Tooltip("A key held down tips the whole body forward this much (deg) at once, over the front foot, through " +
                 "holdTipSpring (the reference game: ~30–35° within 0.15 s of the hold; the head's slower lean takes it " +
                 "on to ~75° by 1.5 s). The held leg stays in the line of the hips, so it goes back with them. Quick " +
                 "presses never get there (their hold weight stays low).")]
        public float holdLean = 32f;
        [Tooltip("How quickly the body tips into holdLean and back (frequency, damping).")]
        public SpringSettings holdTipSpring = new SpringSettings(7f, 1f);
        [Tooltip("More forward lean for the whole body (deg) with both keys held, at the lower of the two hold " +
                 "weights: with the head down it takes the hips to level — the dive comes out of it.")]
        public float bothLean = 55f;
        [Tooltip("Most the body (on the heels) leans back with the head swinging back (deg).")]
        public float hipBackMax = 12f;
        [Tooltip("Near level (the dive) the curve turns over: the low back arches up this much on the hips (deg, − = " +
                 "up), the shoulders above the line of the hips and legs, the head dipping in front (headDip) — the " +
                 "reference game's hump. It blends in from half of level on.")]
        public float spineLevelCurve = -12f;
        [Tooltip("Near level the head dips forward past the chest by up to this (deg): the head below the shoulders.")]
        public float headDip = 20f;
        [Tooltip("B-spine's bend on the hips at rest (deg, its Inspector X); the chain's bend comes on top.")]
        public float waistAngle = 2f;
        [Tooltip("The top of the waist's bend forward on B-spine at rest (deg, on top of the rig's rest): with " +
                 "waistAngle a gentle curve, the base a little back, chest and neck forward.")]
        public float waistTopAngle = 13f;
        [Tooltip("Most the waist bends back (deg); a lean back past it is the whole body sitting back on the heels.")]
        public float waistBackMax = 7f;
        [Tooltip("Hip pitch range (deg, + = forward): the forward lean — the head's pull, the dive — turns " +
                 "the hips, never past these (the pose's own pelvisPitch included). Leaning back is the whole body " +
                 "about the feet instead (see Update), not the hips.")]
        public Vector2 pelvisPitchRange = new Vector2(-10f, 90f);

        [Header("Arms (deg, + = forward): one key moves one leg and one arm — right key: right leg + left arm")]
        [Tooltip("Upper arm (deg, −) with its leg all the way back — fixed: speed and the key poses do not move it, " +
                 "so a held leg keeps its arm right here.")]
        public float armBack = -40f;
        [Tooltip("Elbow bend with the arm all the way back (deg): the forearm has turned back with the upper arm " +
                 "and points down-back (stays so while held).")]
        public float elbowBack = 30f;
        [Tooltip("Upper arm (deg, +) with its leg all the way forward (the opposite leg reaching): the arm swings both " +
                 "ways, in proportion to that leg (its back angle → armBack, its reach → armFront).")]
        public float armFront = 50f;
        [Tooltip("Elbow bend with the arm all the way forward (deg): bent, the hand up in front (the reference game " +
                 "~80–90°). Between back and front the elbow goes with the upper arm.")]
        public float elbowFront = 95f;
        [Tooltip("The arm's move toward the opposite leg's phase: EaseOutQuint = quick out, slowing into place; " +
                 "EaseInCirc = slow out, quick at the end; Linear; Custom = armCurve.")]
        public EaseShape armEase = EaseShape.EaseOutQuint;
        [Tooltip("armEase Custom: the arm's move over its time (0..1 → 0..1).")]
        public AnimationCurve armCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("An arm's move starts with its leg's phase and lasts that phase's time, but at least this long (s): " +
                 "a quick push does not snap the arm.")]
        public float armMinTime = 0.2f;
        [Tooltip("Arms angled out sideways from the body (deg), so they clear the trunk.")]
        public float armOutward = 8f;
        [Tooltip("Shoulder (clavicle) follows the arm swing (deg at full swing).")]
        public float shoulderFollow = 12f;

        [Header("Hurdle jump (deg, + = forward): on top of the run, by how far through the jump")]
        [Tooltip("The leg going over first: thigh and knee over the bar (knee 180 = straight).")]
        public float jumpLeadThigh = 97f;
        public float jumpLeadKnee = 150f;
        [Tooltip("The other leg in the wind-up, foot still planted under the body.")]
        public float jumpStanceThigh = -5f;
        public float jumpStanceKnee = 165f;
        [Tooltip("The trailing thigh's way over the hurdle: where it points (x = out to its side, y = up, z = forward), " +
                 "from behind, out to the side and level, round to the front, in, and over at the hip. It goes round the " +
                 "side between the points, never under the body.")]
        public Vector3[] jumpTrailPath =
        {
            new Vector3(0.26f, -0.67f, -0.69f), new Vector3(0.71f, -0.14f, -0.69f), new Vector3(0.29f, -0.14f, 0.95f),
            new Vector3(0f, 0.12f, 0.99f),
        };
        [Tooltip("How far through the air (0..1) the thigh reaches each point of jumpTrailPath.")]
        public float[] jumpTrailTimes = { 0f, 0.35f, 0.7f, 1f };
        [Tooltip("The trailing knee behind the body (180 = straight); it ends at jumpLeadKnee, in the front pose.")]
        public float jumpTrailKnee = 105f;
        [Tooltip("The leading leg as its foot comes to the ground, where the input takes it over.")]
        public float jumpLandThigh = 20f;
        public float jumpLandKnee = 170f;
        [Tooltip("The leading leg's ankle on its shin (+ = toes down): about square to the shin, toes up, not in line with it.")]
        public float jumpLeadAnkle = -17f;
        [Tooltip("The trailing leg's ankle on its shin (+ = toes down): loose, toes hanging a little.")]
        public float jumpTrailAnkle = 25f;
        [Tooltip("Forward lean over the bar.")]
        public float jumpLean = 25f;
        [Tooltip("Leaning back at the hit of a knocked hurdle (the runner stays on its feet).")]
        public float hitSit = 18f;

        [Header("Preview (play mode): hold one pose; edit the asset live, changes stay")]
        [Tooltip("Play mode: hold previewPose instead of running, to edit pose assets live.")]
        public bool preview;
        [Tooltip("The pose held while preview is on.")]
        public RunnerPose previewPose;
        [Tooltip("Lean (deg, + = forward) put on the previewed pose.")]
        [Range(-45f, 85f)] public float previewLean;

        const float NeckShare = 0.5f;          // share of the head angle the neck (spine tip) bends; the head aims the rest
        const float HintDistance = 0.4f;       // knee hint in front of the knee, across the thigh (m)
        const float FootBlend = 25f;           // foot pitch / swing weight follow their phase's value (1/s)
        const float HeelBehindAnkle = 0.05f;   // heel contact point behind the ankle joint (m)
        const float ToesUpFree = 5f;           // foot this far toes up (deg) keeps its toes in line instead of flat
        const float MinPelvis = 0.1f;          // the pelvis never goes lower than this above the ground (m)
        const float MinPushTime = 0.06f;       // a push never goes quicker than this (s), however fast the run
        const float GroundContact = 0.01f;     // a foot this close above the ground (m) has landed
        const float GroundKeep = 0.05f;        // a carrying foot counts as on the ground up to this high (m)
        const float GlideGap = 0.015f;         // a reach held out in the air glides this far over the ground (m)
        const float MinFlowRate = 50f;        // a leg turning forward slower than this (deg/s) starts its reach from rest
        const float JumpAnkleFade = 10f;       // after a jump the ankles go back to the run's angles this fast (1/s)
        const float FollowRate = 6f;           // a leg changes between turning with the hips and keeping to the ground this fast (1/s)

        bool _wasInJump;                           // last frame in a jump: this frame's landing
        Vector3 _restHip;
        Quaternion _restHipRot, _restSpineRoot, _restSpineTip;
        float _ground, _sole, _profileW, _runHome;
        float _restWaist;                          // B-spine's bend on the hips at rest (deg)
        float _stepping;                           // 1 = a key is down (run stance), 0 = none: Ready crouch
        Spring _bodyY, _lean;                      // the waist top's lean after the head (deg)
        Spring _waistBase, _hipsLean;              // B-spine's and the hips' lean down the chain (deg)
        float _headAngle;                          // this frame's head angle (deg, + = forward)
        float _holdL, _holdR;                      // hold weights this frame
        bool _bound, _heightSet;
        bool _inFlight;                            // running with both feet in the air (not a dive)
        float _flightFrom;                         // body height the running flight left the ground at
        float _lastBodyY;                          // last frame's body height (the head bone is last frame's too)
        bool _carried = true, _headDown;           // last frame: a leg carried the body / the head was on the ground
        float _pelvisNow;                          // this frame's pose pelvis pitch (deg): a held leg trails its line
        Spring _holdTip;                           // the quick forward tip of a key held (deg, whole body)
        MultiAimConstraint _headAim;
        Transform _headBone, _lookTarget, _neckBone;
        float _neckTrimNow;                        // this frame's neck trim (deg), applied after the rig
        float _lookDistance;
        double _lastPress = -10.0;                 // sim time of the last press (a leg waiting behind waits from it)

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
            _sole = (GroundY(leftLeg) + GroundY(rightLeg)) * 0.5f;
            var spine = FindDeep(transform, "B-spine");
            _restWaist = spine ? Mathf.DeltaAngle(0f, spine.localEulerAngles.x) : 0f;
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
            _neckBone = _headBone ? _headBone.parent : FindDeep(transform, "B-neck");
            BindArm(leftArm, -1f, "B-upperArm.L", "B-forearm.L");
            BindArm(rightArm, 1f, "B-upperArm.R", "B-forearm.R");
            _bound = true;
        }

        void BindLeg(Leg leg, string side)
        {
            leg.restAnkle = Local(leg.target);
            // IK places the ankle only; the foot angle is applied afterwards relative to the shin.
            var ik = leg.target.GetComponentInParent<TwoBoneIKConstraint>();
            if (ik) ik.data.targetRotationWeight = 0f;
            if (!leg.shinBone) leg.shinBone = FindDeep(transform, "B-shin." + side);
            if (!leg.footBone) leg.footBone = FindDeep(transform, "B-foot." + side);
            if (!leg.toeBone) leg.toeBone = FindDeep(transform, "B-toe." + side);
            leg.ballOffset = leg.toeBone ? Local(leg.toeBone) - leg.restAnkle : new Vector3(0f, -0.1f, 0.15f);
            leg.strideW = 1f;
            ResetLeg(leg);
            if (!leg.shinBone || !leg.footBone) return;
            Vector3 thigh = Local(leg.shinBone.parent), shin = Local(leg.shinBone), foot = Local(leg.footBone);
            leg.hipJointFromHip = thigh - _restHip;
            leg.thighLength = Vector3.Distance(thigh, shin);
            leg.shinLength = Vector3.Distance(shin, foot);
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

        /// <summary>Home in the crouch, nothing pressed.</summary>
        void ResetLeg(Leg leg)
        {
            leg.phase = LegPhase.Rest;
            leg.phaseTime = 0.0;
            leg.grounded = true;
            leg.reachNext = false;
            leg.duration = 0f;
            leg.held = false;
            leg.angles = leg.from = crouch;
            leg.pitch = leg.swingW = leg.toeRoll = 0f;
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

        // ---- Leg phases, on the sim's clock. ----

        /// <summary>
        /// A press sends that leg into its push and the other into its reach (unless that one is held back
        /// too), both from whatever angles they have now. A release of a held leg sends it to the mid angle.
        /// </summary>
        void OnTapped(int leg, bool pressed, double time)
        {
            Bind();
            TickLegs(time);
            var l = Legs(leg);
            if (!pressed)
            {
                l.held = false;
                // A held leg let go folds up behind first, as after any push (then waits or comes on).
                // Flying in a dive the legs stay in line with the body until it is down (TickLegs takes them on).
                if (l.phase == LegPhase.Hold && (_carried || _headDown || l.grounded)) ReleaseHold(l, time);
                return;
            }
            l.held = true;
            l.reachNext = false;
            _lastPress = time;
            Enter(l, LegPhase.Push, time, PushDuration(l));
            var other = Legs(1 - leg);
            if (other.HeldBack) return;
            // A leg still going back finishes that first; one coming forward (or waiting behind) turns to the front
            // as it goes.
            if (other.phase == LegPhase.Push || other.phase == LegPhase.Recover) other.reachNext = true;
            else if (other.phase == LegPhase.Mid || other.phase == LegPhase.Wait) ReachOn(other, time);
            // Already in front (the same key pressed again): it stays in front, it does not reach out anew.
            else if (other.phase == LegPhase.Reach || other.phase == LegPhase.Front)
            {
                if (!other.stay) Enter(other, LegPhase.Front, time, frontStayTime);
                other.stay = true;
            }
            else Enter(other, LegPhase.Reach, time, PushTime());
        }

        /// <summary>The push, and the other leg's reach with it: pushTime, quicker with speed (animSpeedUp).</summary>
        float PushTime() => pushTime * SpeedUp();

        /// <summary>Share of their time the legs take at this speed: 1 standing, animSpeedUp at max speed.</summary>
        float SpeedUp() => Mathf.Lerp(1f, animSpeedUp, Speed01());

        /// <summary>
        /// A leg in the air heads for the front instead of mid, without a snap: coming forward it goes on at the
        /// speed it has and slows into the reach (its time is whatever that speed needs); from rest (just back
        /// from recovering) it starts as its way to mid would, at mid's pace over the longer way. Never quicker
        /// than a push.
        /// </summary>
        void ReachOn(Leg leg, double time)
        {
            leg.reachNext = false;
            float dist = Mathf.Abs(Reach().thigh - leg.angles.thigh);
            float push = PushTime();
            if (leg.thighRate > MinFlowRate)
            {
                Enter(leg, LegPhase.Reach, time, Mathf.Clamp(2f * dist / leg.thighRate, push, 2f * StepPart(midTime)));
                leg.easeOut = true;
            }
            else
                Enter(leg, LegPhase.Reach, time,
                    Mathf.Max(push, StepPart(midTime) * dist / Mathf.Max(1f, Mathf.Abs(mid.thigh - Recover().thigh))));
        }

        void OnRunReset()
        {
            _lean.Reset();
            _waistBase.Reset();
            _hipsLean.Reset();
            _holdTip.Reset();
            _carried = true;
            _wasInJump = false;
            _inFlight = false;
            _lastPress = -10.0;
            leftLeg.sideOut = rightLeg.sideOut = leftLeg.jumpW = rightLeg.jumpW = 0f;
            ResetLeg(leftLeg);
            ResetLeg(rightLeg);
            _heightSet = false;
        }

        Leg Legs(int i) => i == 0 ? leftLeg : rightLeg;

        float Speed01() => body && body.Sim != null ? Mathf.Clamp01(body.Sim.Speed / body.Sim.T.maxSpeed) : 0f;

        /// <summary>How far the stride has opened with speed: 0 at strideSpeed.x, 1 at strideSpeed.y.</summary>
        float Stride01() => body && body.Sim != null
            ? Mathf.InverseLerp(strideSpeed.x, strideSpeed.y, body.Sim.Speed) : 0f;

        LegAngles PushEnd() => LegAngles.Lerp(pushEnd, pushEndFast, Stride01());
        LegAngles Recover() => LegAngles.Lerp(recover, recoverFast, Stride01());
        LegAngles Reach() => LegAngles.Lerp(reach, reachFast, Stride01());

        /// <summary>How far forward of the thigh joint the foot is at these angles (runner space, m).</summary>
        static float FootAhead(Leg leg, LegAngles a) =>
            leg.thighLength * Mathf.Sin(a.thigh * Mathf.Deg2Rad) +
            leg.shinLength * Mathf.Sin((a.thigh - (180f - a.knee)) * Mathf.Deg2Rad);

        /// <summary>
        /// A push is quick whatever the speed (the reference game: the leg goes back in about 0.1 s): pushBackTime,
        /// slow to fast over strideSpeed. Slow, the foot slides back a little on the ground; the look comes first.
        /// </summary>
        float PushDuration(Leg leg) => Mathf.Max(MinPushTime, Mathf.Lerp(pushBackTime.x, pushBackTime.y, Stride01()));

        /// <summary>
        /// The time a foot on the ground takes from the leg's angles now to these, going back under the body at the
        /// body's speed (between MinPushTime and most).
        /// </summary>
        float GroundTime(Leg leg, LegAngles to, float most)
        {
            float speed = body && body.Sim != null ? body.Sim.Speed : 0f;
            float way = Mathf.Max(0f, FootAhead(leg, leg.angles) - FootAhead(leg, to));
            return Mathf.Clamp(way / Mathf.Max(0.01f, speed), MinPushTime, Mathf.Max(MinPushTime, most));
        }

        /// <summary>
        /// A phase's share of the swing: push + recover + mid + home take swingSteps of the sim's step time (from the
        /// body's speed, not from how the presses come), so the legs and the arms after them keep the body's pace.
        /// push = the push this swing follows (s; − = the usual PushTime).
        /// </summary>
        float StepPart(float time, float push = -1f)
        {
            float cycle = (body && body.Sim != null ? body.Sim.StepTime : 0.25f) * Mathf.Max(0.1f, swingSteps);
            return time * Mathf.Max(0.01f, cycle - (push < 0f ? PushTime() : push)) / SwingParts;
        }

        float SwingParts => Mathf.Max(0.01f, recoverTime + midTime + homeTime);

        /// <summary>The swing's share done when this phase starts and ends (recover, mid, home in turn).</summary>
        void SwingShares(LegPhase phase, out float a, out float b)
        {
            float r = recoverTime / SwingParts, m = midTime / SwingParts;
            a = phase == LegPhase.Recover ? 0f : phase == LegPhase.Mid ? r : r + m;
            b = phase == LegPhase.Recover ? r : phase == LegPhase.Mid ? r + m : 1f;
        }

        /// <summary>Start a phase from the leg's current angles.</summary>
        static void Enter(Leg leg, LegPhase phase, double time, float duration)
        {
            leg.phase = phase;
            leg.phaseTime = time;
            leg.duration = duration;
            leg.from = leg.angles;
            leg.fromPitch = leg.pitch;
            leg.easeOut = false;
            leg.swing = false;
            leg.landStart = -1.0;
            leg.stay = false;
        }

        /// <summary>
        /// Reached out in front while the other leg is still behind (folding up or waiting for its key): the leg holds
        /// its reach in the air (the body glides just above the ground, UpdateBodyHeight) instead of landing at once;
        /// it comes down when the other leg comes on or the next key is pressed.
        /// </summary>
        static bool HoldsReach(Leg leg, Leg partner, double now) =>
            leg.phase == LegPhase.Reach && now >= leg.EndTime && leg.landStart < 0.0 &&
            (partner.phase == LegPhase.Recover || partner.phase == LegPhase.Wait);

        /// <summary>How far (0…1) a leg waiting behind has come in under the body (0 while it is not waiting).</summary>
        float UnderBody(Leg leg, double now)
        {
            if (leg.phase != LegPhase.Wait) return 0f;
            return leg.duration > 0f ? Mathf.Clamp01((float)(now - leg.phaseTime) / leg.duration) : 1f;
        }

        /// <summary>A key held on past a quick press: held back for holdLandDelay after its push.</summary>
        bool ReallyHeld(Leg leg) => leg.held && leg.phase == LegPhase.Hold && body && body.Sim != null &&
                                    body.Sim.Time - leg.phaseTime >= holdLandDelay;

        /// <summary>
        /// A leg staying in front keeps in the air, the body gliding just over its foot, while the other pushes and
        /// comes back in under the body — unless that one is really held: then something must take the body, the
        /// front foot comes down.
        /// </summary>
        bool StayGlides(Leg leg, Leg partner, double now) =>
            leg.phase == LegPhase.Front && leg.stay && !ReallyHeld(partner) &&
            (partner.phase == LegPhase.Push || partner.phase == LegPhase.Hold || partner.phase == LegPhase.Recover ||
             (partner.phase == LegPhase.Wait && UnderBody(partner, now) < 1f));

        /// <summary>Hand a swing on to its next phase (its share of the swing's time), still on the one curve.</summary>
        void EnterSwing(Leg leg, LegPhase phase, double time)
        {
            SwingShares(phase, out float a, out float b);
            Enter(leg, phase, time, leg.swingTime * (b - a));
            leg.swing = true;
        }

        /// <summary>
        /// Phases that have run their time hand over: a push goes on as a hold (key still down: the same motion,
        /// it is already on its way to the hold angles) or becomes a recovery, a
        /// recovery goes on to the mid angle and that one home; a reach ends in front on the ground and stays
        /// there while the other leg is still pushing, recovering or held, then goes home. A held leg let go goes to mid.
        /// </summary>
        void TickLegs(double time)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var leg in new[] { leftLeg, rightLeg })
                {
                    var partner = leg == leftLeg ? rightLeg : leftLeg;
                    // Let go: it folds up — but flying in a dive only once the body is down (head on the ground), so
                    // the legs do not swing round over the body in the air.
                    if (leg.phase == LegPhase.Hold && !leg.held && (_carried || _headDown || leg.grounded))
                    {
                        ReleaseHold(leg, time);
                        continue;
                    }
                    // The front foot stays planted ahead until the other leg has pushed and recovered (or while it
                    // waits behind for the next press).
                    if (leg.phase == LegPhase.Front && partner.phase != LegPhase.Push && partner.phase != LegPhase.Hold &&
                        partner.phase != LegPhase.Recover && partner.phase != LegPhase.Wait)
                    {
                        Enter(leg, LegPhase.Rest, time, StepPart(frontHomeTime));
                        continue;
                    }
                    // Staying in front in the air: once the other is in under the body (or really held) it comes down —
                    // landed, it is a front foot on the ground like any other (the body passes over it).
                    if (leg.phase == LegPhase.Front && leg.stay && !StayGlides(leg, partner, time) &&
                        (leg.grounded || ReallyHeld(partner)))
                    {
                        Enter(leg, LegPhase.Front, time, 0f);
                        leg.duration = GroundTime(leg, Home(), Mathf.Max(PushTime(), frontGroundMax));
                        continue;
                    }
                    // Waiting behind: no press for openHoldTime, it comes on home by itself.
                    if (leg.phase == LegPhase.Wait)
                    {
                        if (time - _lastPress >= openHoldTime) Enter(leg, LegPhase.Mid, time, StepPart(midTime));
                        continue;
                    }
                    if (!leg.Timed || time < leg.EndTime) continue;
                    double end = leg.EndTime;
                    switch (leg.phase)
                    {
                        case LegPhase.Push:
                            if (leg.held)
                            {
                                Enter(leg, LegPhase.Hold, end, 0f);
                                leg.holdFromWorld = leg.thighWorld;
                                leg.holdFromKnee = leg.angles.knee;
                            }
                            else
                            {
                                leg.swingStart = end;
                                leg.swingTime = StepPart(recoverTime + midTime + homeTime, leg.duration);
                                EnterSwing(leg, LegPhase.Recover, end);
                            }
                            break;
                        case LegPhase.Recover:
                            if (leg.reachNext) ReachOn(leg, end);
                            // Presses still coming: wait behind for the other key instead of drifting home.
                            else if (end - _lastPress < openHoldTime) Enter(leg, LegPhase.Wait, end, returnTime);
                            else if (leg.swing) EnterSwing(leg, LegPhase.Mid, end);
                            else Enter(leg, LegPhase.Mid, end, StepPart(midTime));
                            break;
                        case LegPhase.Mid:
                            if (leg.swing) EnterSwing(leg, LegPhase.Rest, end);
                            else Enter(leg, LegPhase.Rest, end, StepPart(homeTime));
                            break;
                        case LegPhase.Reach:
                            // Reached out, it stays so in the air until the foot comes down on the ground (the body
                            // flying on, see UpdateBodyHeight); never longer than reachAirMax.
                            // Held out while the other leg is behind; then it comes down (landStart) and lands.
                            if (HoldsReach(leg, partner, time)) break;
                            if (leg.landStart < 0.0) leg.landStart = time;
                            if (!leg.grounded && time - leg.landStart < reachAirMax) break;
                            // Landed ahead: the body passes over the foot, so the leg goes on back toward home at
                            // the body's speed (the foot stays where it landed).
                            Enter(leg, LegPhase.Front, time, 0f);
                            leg.duration = GroundTime(leg, Home(), Mathf.Max(PushTime(), frontGroundMax));
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// The angles a leg is heading for in its phase. Home slides from the crouch to the running home while
        /// presses keep coming; the leg in front turns into the support angles as the other's hold builds up;
        /// a pushing leg is already on its way into the hold angles as its own hold builds up (from the moment its
        /// key went down: a push and a hold are one motion), and a held one goes on there (it rises behind with
        /// the hips' lean, see Update: held legs turn with the hips).
        /// </summary>
        LegAngles Target(Leg leg, float ownHold, float otherHold)
        {
            switch (leg.phase)
            {
                case LegPhase.Recover: return Recover();
                case LegPhase.Wait: return waitPose;
                case LegPhase.Mid: return mid;
                case LegPhase.Reach: return Reach();
                case LegPhase.Front:
                {
                    // Staying in front (the other key pressed again): the leg keeps its place in front, nearly
                    // straight, the knee giving a little at a slow run while the other waits under the body.
                    // The knee does not give before the other leg has come in under the body: while it pushes and
                    // returns the shin stays forward (a quick press does not pull it into the support either).
                    LegAngles rest = Home();
                    LegAngles plain;
                    if (leg.stay)
                    {
                        var partner = leg == leftLeg ? rightLeg : leftLeg;
                        rest = frontStay;
                        float under = body && body.Sim != null ? UnderBody(partner, body.Sim.Time) : 0f;
                        float bent = Mathf.Lerp(frontStayBentKnee, frontStay.knee, Stride01());
                        rest.knee = Mathf.Lerp(frontStay.knee, bent, under * under);
                        plain = rest;
                    }
                    else plain = LegAngles.Lerp(rest, holdSupport, otherHold);
                    // The other leg really held (quick presses stay out of it): the support opens with the body's
                    // tip, from holdSupportStart upright to holdSupport at level.
                    float held = HeldWeight(otherHold);
                    if (held <= 0f) return plain;
                    float tilt = Mathf.Clamp01((_hipsLean.Value + _holdTip.Value) / Mathf.Max(1f, pelvisPitchRange.y));
                    LegAngles support = LegAngles.Lerp(holdSupportStart, holdSupport, tilt);
                    return LegAngles.Lerp(plain, LegAngles.Lerp(Home(), support, otherHold), held);
                }
                case LegPhase.Push:
                case LegPhase.Hold: return LegAngles.Lerp(PushEnd(), HoldLine(), ownHold);
                default: return Home();
            }
        }

        LegAngles Home() => LegAngles.Lerp(crouch, runHome, _runHome);

        /// <summary>
        /// The knee through a reach, by how far the thigh has come forward (not by time): it folds first, heel up
        /// (reachFoldKnee, deeper the faster, by reachFoldAt of the thigh's way), stays folded while the thigh drives
        /// on, and only once the thigh is reachOpenAt of the way to its place in front does the shin open out
        /// forward into the reach's knee for the landing.
        /// </summary>
        float ReachKnee(Leg leg, float s)
        {
            float goal = Reach().thigh;
            float way = Mathf.Abs(goal - leg.from.thigh) < 1f
                ? s : Mathf.Clamp01(Mathf.InverseLerp(leg.from.thigh, goal, leg.angles.thigh));
            float foldAt = Mathf.Clamp(reachFoldAt, 0.05f, 0.9f);
            float openAt = Mathf.Clamp(reachOpenAt, foldAt, 0.99f);
            float fold = Mathf.Min(leg.from.knee, Mathf.Lerp(reachFoldKnee.x, reachFoldKnee.y, Stride01()));
            if (way < foldAt) return Mathf.Lerp(leg.from.knee, fold, Mathf.SmoothStep(0f, 1f, way / foldAt));
            if (way < openAt) return fold;
            return Mathf.Lerp(fold, Reach().knee, Mathf.SmoothStep(0f, 1f, (way - openAt) / (1f - openAt)));
        }

        /// <summary>This frame's angles (from the phase's start into its target) and the foot's angles with them.</summary>
        /// <summary>The swing's curve over its time: it starts at speed (taking over from the push) and slows to rest.</summary>
        static float SwingCurve(float x) => 1f - (1f - x) * (1f - x);

        void UpdateLeg(Leg leg, double now, float dt, float legTurn, float ownHold, float otherHold)
        {
            float s = leg.duration > 0f ? Mathf.Clamp01((float)(now - leg.phaseTime) / leg.duration) : 1f;
            float lastThigh = leg.angles.thigh;
            // Phases start and end at rest (smooth step); one that takes over a moving leg starts at its speed.
            // A swing is one smooth step over recover + mid + home: it slows only as it comes home.
            // A landed front foot goes back at the body's steady speed (no ease); a push follows pushCurve (slow
            // out, quick at the end).
            float ease = leg.phase == LegPhase.Push ? Ease(pushEase, pushCurve, s)
                : leg.phase == LegPhase.Reach ? Ease(reachEase, reachCurve, s)
                : leg.phase == LegPhase.Front ? s
                : leg.easeOut ? 1f - (1f - s) * (1f - s) : Mathf.SmoothStep(0f, 1f, s);
            if (leg.swing && leg.swingTime > 0f)
            {
                // The swing after a push starts at the push's speed and only slows as it comes home.
                SwingShares(leg.phase, out float a, out float b);
                float g = SwingCurve(Mathf.Clamp01((float)(now - leg.swingStart) / leg.swingTime));
                float ga = SwingCurve(a), gb = SwingCurve(b);
                ease = Mathf.Clamp01((g - ga) / Mathf.Max(0.0001f, gb - ga));
            }
            LegAngles target = Target(leg, ownHold, otherHold);
            leg.angles = LegAngles.Lerp(leg.from, target, ease);
            // Pushing, the knee opens on a smooth curve of its own: the thigh's quick end must not flick the shin.
            if (leg.phase == LegPhase.Push)
                leg.angles.knee = Mathf.Lerp(leg.from.knee, target.knee, Mathf.SmoothStep(0f, 1f, s));
            if (leg.phase == LegPhase.Reach)
            {
                leg.angles.knee = ReachKnee(leg, s);
                // Reached out with the foot still in the air: once the other leg comes on (or a key is pressed) it
                // comes down and back to land (reachLand).
                if (leg.landStart >= 0.0)
                    leg.angles = LegAngles.Lerp(leg.angles, reachLand,
                        Mathf.SmoothStep(0f, 1f, (float)(now - leg.landStart) / Mathf.Max(0.01f, reachLandTime)));
            }
            leg.thighRate = dt > 0f ? (leg.angles.thigh - lastThigh) / dt : 0f;
            leg.angles.knee = Mathf.Min(leg.angles.knee, maxKnee);
            // In the air the shin trails the thigh (kneeSpring); on the ground the knee is exact (the foot stays true).
            bool kneeFree = leg.phase == LegPhase.Recover || leg.phase == LegPhase.Mid ||
                            leg.phase == LegPhase.Wait || leg.phase == LegPhase.Reach;
            if (kneeFree && kneeSpring.frequency > 0f)
            {
                leg.kneeLag.Step(leg.angles.knee, kneeSpring.frequency, kneeSpring.damping, dt);
                leg.angles.knee = Mathf.Min(leg.kneeLag.Value, maxKnee);
            }
            else
                leg.kneeLag.Reset(leg.angles.knee);

            // Foot: heel up through the push; the reaching foot lands heel first, toes up, and stays so while it
            // is planted ahead, rolling flat only as it goes home; in the air the ankle and toes follow the swing
            // curves (recover = first half, mid = second half).
            float pitch = 0f, swingW = 0f, air = -1f;
            leg.toeRoll = 0f;
            switch (leg.phase)
            {
                case LegPhase.Push:
                case LegPhase.Hold:
                    // One motion: the push's heel rise hands over to the held foot's angle (turning up with its
                    // leg) as the key's hold weight builds up.
                    float push = leg.phase == LegPhase.Push ? s : 1f;
                    pitch = Mathf.Lerp(Profile(pr => pr.stancePitch, push), pitchBack + legTurn, ownHold);
                    leg.toeRoll = Profile(pr => pr.toeRoll, push);
                    break;
                case LegPhase.Recover:
                    air = 0.5f * s;
                    break;
                case LegPhase.Wait:
                    air = 0.5f;   // the foot as at the end of its recovery
                    break;
                case LegPhase.Mid:
                    air = 0.5f + 0.5f * s;
                    break;
                case LegPhase.Reach:
                    pitch = Mathf.Lerp(leg.fromPitch, FrontPitch(), Mathf.SmoothStep(0f, 1f, s));
                    break;
                case LegPhase.Front:
                    // Heel down on landing, rolling flat as the body passes over the foot.
                    pitch = Mathf.Lerp(FrontPitch(), 0f, s);
                    break;
                case LegPhase.Rest:
                    pitch = leg.fromPitch * (1f - Mathf.SmoothStep(0f, 1f, s));
                    break;
            }
            if (air >= 0f)
            {
                swingW = 1f;
                leg.swingAnkle = Profile(pr => pr.swingAnkle, air);
                leg.swingToe = Profile(pr => pr.swingToe, air);
            }
            float k = 1f - Mathf.Exp(-FootBlend * dt);
            leg.pitch = Mathf.Lerp(leg.pitch, pitch, k);
            leg.swingW = Mathf.Lerp(leg.swingW, swingW, k);
        }

        /// <summary>
        /// Legs turn with the hips' lean, except a leg on the ground: standing home, waiting in front (the support
        /// leg of a hold) or pushing off keeps its angle to the ground, so the foot stays where it is. A held leg
        /// turns with the hips all the way (its angle is the line of the hips, HoldLine), so it goes up behind as
        /// they tip. Blended, so a leg landing or lifting off does not snap.
        /// </summary>
        void FollowHips(Leg leg, Leg partner, float legTurn, double now, float dt)
        {
            bool planted = leg.phase == LegPhase.Rest || (leg.phase == LegPhase.Front && !leg.stay) ||
                           leg.phase == LegPhase.Push;
            leg.follow = Mathf.MoveTowards(leg.follow, planted ? 0f : 1f, FollowRate * dt);
            leg.thighWorld = leg.angles.thigh - legTurn * leg.follow;
            // One key held (the other free): the leg goes back on its own to holdThigh, its knee opening on the
            // same way; once the tipping body's line is further back, it goes on with the body. (Both held — the
            // dive — the legs simply stay in the body's line.)
            if (leg.phase == LegPhase.Hold && !partner.held)
            {
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((float)(now - leg.phaseTime) / Mathf.Max(0.01f, holdReachTime)));
                leg.thighWorld = Mathf.Min(leg.thighWorld, Mathf.Lerp(leg.holdFromWorld, holdThigh, p));
                leg.angles.knee = Mathf.Min(Mathf.Lerp(leg.holdFromKnee, holdStraightKnee, p), maxKnee);
            }
        }

        /// <summary>How much of a key's hold weight is a real hold (0 for quick presses, which stay at ~0.3).</summary>
        static float HeldWeight(float weight) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.6f, weight));

        /// <summary>
        /// A held leg at a full hold, on the hips (turned with them): the thigh in the line of the hips, heldLegArch
        /// short of it, the knee at holdKnee — the held legs and the body make one nearly straight line.
        /// </summary>
        LegAngles HoldLine() => new LegAngles(-_pelvisNow - heldLegArch, holdKnee);

        /// <summary>
        /// A held leg let go: carried by the other foot it folds up behind as after any push; with the body down on
        /// the ground (nothing carrying) it comes straight in under the body to stand on (home, keeping to the
        /// ground), so it never swings up over the tipped hips.
        /// </summary>
        void ReleaseHold(Leg leg, double time)
        {
            // Body still upright: fold up behind as after a push. Tipped well forward (or down): folding up behind
            // would throw the foot up over the hips, so it comes straight in under the body to stand on.
            bool tipped = _hipsLean.Value + _holdTip.Value > 30f;
            if (_carried && !tipped) Enter(leg, LegPhase.Recover, time, StepPart(recoverTime));
            else Enter(leg, LegPhase.Rest, time, StepPart(homeTime));
        }

        /// <summary>The front foot's angle (deg, − = toes up): heel down, toes up, by speed between the two profiles.</summary>
        float FrontPitch() => Mathf.Lerp(accelProfile.touchdownPitch, maxSpeedProfile.touchdownPitch, _profileW);

        /// <summary>
        /// How far the ankle must rise so a toes-up foot stands on its heel instead of sinking it into the ground:
        /// the heel (HeelBehindAnkle behind, ankle height below) turns down as the toes go up.
        /// </summary>
        static float HeelLift(Leg leg)
        {
            float a = Mathf.Max(0f, -leg.pitch) * Mathf.Deg2Rad;
            return Mathf.Max(0f, HeelBehindAnkle * Mathf.Sin(a) + leg.ballOffset.y * (1f - Mathf.Cos(a)));
        }

        /// <summary>
        /// Forward kinematics of one leg (runner space): from the thigh joint (on the tilted pelvis) down the
        /// thigh at its angle, then down the shin at thigh − (180 − knee). Sets the leg's knee and ankle.
        /// </summary>
        void LegFK(Leg leg, Vector3 hip, Quaternion pelvisTilt, float width)
        {
            float th = leg.thighWorld * Mathf.Deg2Rad;
            float sh = (leg.thighWorld - (180f - leg.angles.knee)) * Mathf.Deg2Rad;
            Vector3 joint = hip + pelvisTilt * leg.hipJointFromHip + new Vector3(Side(leg) * width, 0f, 0f);
            leg.knee = joint + new Vector3(0f, -Mathf.Cos(th), Mathf.Sin(th)) * leg.thighLength;
            leg.ankle = leg.knee + new Vector3(0f, -Mathf.Cos(sh), Mathf.Sin(sh)) * leg.shinLength;
            leg.width = width;
            // Out of the leg's plane the whole leg turns about the hip joint (only over a hurdle: 0 in the run).
            leg.sideTurn = Quaternion.identity;
            if (Mathf.Approximately(leg.sideOut, 0f)) return;
            Vector3 thigh = new Vector3(0f, -Mathf.Cos(th), Mathf.Sin(th));
            Vector3 axis = Vector3.Cross(thigh, new Vector3(Side(leg), 0f, 0f));
            leg.sideTurn = Quaternion.AngleAxis(leg.sideOut, axis);
            leg.knee = joint + leg.sideTurn * (leg.knee - joint);
            leg.ankle = joint + leg.sideTurn * (leg.ankle - joint);
        }

        void Update()
        {
            Bind();
            if (!body || body.Sim == null) return;
            var sim = body.Sim;
            float dt = Time.deltaTime;

            // 1. Key poses.
            PoseData p;
            float lean;
            if (preview && previewPose)
            {
                p = previewPose.pose;
                lean = previewLean;
            }
            else
            {
                // The Ready crouch is where the runner always heads; the run stance only lasts while a key is
                // down, so with no input it goes straight back to the crouch (no upright stance on the way).
                // A jump is still the run, and so is the time between presses while they keep coming (openHoldTime
                // since the last one): no crouch between steps; it crouches once the presses stop.
                bool stepping = sim.Legs[0].Pressed || sim.Legs[1].Pressed || sim.InJump ||
                                sim.Time - _lastPress < openHoldTime;
                _stepping = Mathf.Lerp(_stepping, stepping ? 1f : 0f, 1f - Mathf.Exp(-stanceBlend * dt));
                p = PoseData.Lerp(Get(ready, RunnerPoseDefaults.Ready), Get(run, RunnerPoseDefaults.Run),
                    Mathf.SmoothStep(0f, 1f, sim.Speed / Mathf.Max(0.01f, runPoseSpeed)) * _stepping);
                float excess = sim.Lean - sim.PoseLean;
                float stumbleW = Mathf.SmoothStep(0f, 1f,
                    (excess - (sim.T.stumbleLean - stumbleBlendRange)) / Mathf.Max(0.01f, stumbleBlendRange));
                p = PoseData.Lerp(p, Get(stumble, RunnerPoseDefaults.Stumble), stumbleW);
                lean = 0f;    // the head decides it below
            }
            _holdL = preview ? 0f : sim.Legs[0].WeightValue;
            _holdR = preview ? 0f : sim.Legs[1].WeightValue;

            // 2. Legs: phases → angles → knee and ankle points.
            double now = sim.Time;
            TickLegs(now);
            if (!preview) StepJumpLanding(sim, now);
            _runHome = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(restToBaseSpeed.x, restToBaseSpeed.y, sim.Speed)) * _stepping;
            float profileTarget = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(accelProfileSpeed, maxProfileSpeed, sim.Speed));
            _profileW = Mathf.Lerp(_profileW, profileTarget, 1f - Mathf.Exp(-4f * dt));
            _headAngle = preview ? 0f : sim.HeadAngle;
            // The hips lead (from the input), the spine curves on them, the head comes last (UpdateLean).
            float baseBend = 0f, bend = 0f, headLean = 0f;
            if (!preview)
            {
                lean = UpdateLean(dt, out baseBend, out bend, out headLean);
                lean += jumpLean * JumpWeight(sim) - hitSit * sim.HitWeight;
            }

            // 3. Forward the hips lean (the spine curves on them), within pelvisPitchRange. Back, the whole body
            //    leans about the feet on the ground (below, after the height).
            float backLean = Mathf.Min(0f, lean);
            float hipPitch = Mathf.Clamp(p.pelvisPitch + lean - backLean, pelvisPitchRange.x, pelvisPitchRange.y);
            // The lean part only (the pose's own tilt does not turn the legs).
            float legTurn = hipPitch - p.pelvisPitch;
            Quaternion pelvisTilt = Quaternion.Euler(hipPitch, 0f, 0f);
            _pelvisNow = p.pelvisPitch;

            UpdateLeg(leftLeg, now, dt, legTurn, _holdL, _holdR);
            UpdateLeg(rightLeg, now, dt, legTurn, _holdR, _holdL);
            FollowHips(leftLeg, rightLeg, legTurn, now, dt);
            FollowHips(rightLeg, leftLeg, legTurn, now, dt);
            if (!preview) PoseJump(sim);
            Vector3 hip = _restHip + new Vector3(0f, 0f, p.pelvis.x);
            LegFK(leftLeg, hip, pelvisTilt, p.width);
            LegFK(rightLeg, hip, pelvisTilt, p.width);

            // 4. Body height from the legs (the whole body moves, legs included); nothing goes into the ground.
            //    A carrying foot the height spring lags under stays on the ground: the IK bends its knee.
            float bodyY = preview ? 0f : UpdateBodyHeight(sim, dt);
            if (!preview) bodyY = KeepAboveGround(hip, bodyY) + sim.JumpY;
            hip.y += bodyY;
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                leg.knee.y += bodyY;
                leg.ankle.y += bodyY;
                float floor = _ground + HeelLift(leg);
                // Landing takes the foot right down; a foot already carrying stays so a little above (the height
                // spring may overshoot), so it does not flicker between carrying and flying.
                leg.grounded = leg.ankle.y <= floor + (Carries(leg) ? GroundKeep : GroundContact);
                leg.ankle.y = Mathf.Max(leg.ankle.y, floor);
            }
            leftLeg.rock = rightLeg.rock = 0f;
            if (backLean < 0f) SitBack(ref hip, backLean);

            // Write the rig. The waist base sits at waistAngle plus its bend on the hips; the chain (TwistChain)
            // fills the chest in between it and the top, whose angle the neck and head work from (0 = the rig's rest).
            Quaternion rootRot = transform.rotation;
            _neckTrimNow = preview ? 0f : neckTrim * Mathf.SmoothStep(0f, 1f, _lean.Value / Mathf.Max(1f, neckTrimLean));
            hipTarget.position = transform.TransformPoint(hip);
            hipTarget.rotation = rootRot * Quaternion.Euler(backLean, 0f, 0f) * pelvisTilt * _restHipRot;
            spineRootTarget.rotation = hipTarget.rotation * Quaternion.Euler(waistAngle + baseBend, 0f, 0f);
            // Leaning back the head keeps looking ahead: it takes only headKeep of the lean, the neck bends the rest.
            WriteHead(hipPitch + backLean + waistAngle - _restWaist + waistTopAngle + bend,
                p.pelvisPitch + waistAngle - _restWaist,
                headLean - backLean, backLean, p.headDown, rootRot);
            WriteLeg(leftLeg);
            WriteLeg(rightLeg);

            // Arms: one key, one leg, one arm (the right key's press sends the right leg back and the left arm
            // with it); a leg held back keeps its arm back.
            StepArm(leftArm, p.leftArm, rightLeg, now);
            StepArm(rightArm, p.rightArm, leftLeg, now);
        }

        /// <summary>How much of the jump's pose is on: in over the wind-up, out over the last quarter of the air.</summary>
        static float JumpWeight(RunnerSim sim)
        {
            if (!sim.InJump) return 0f;
            return Mathf.SmoothStep(0f, 1f, sim.JumpWindupProgress) *
                   (1f - Mathf.SmoothStep(0f, 1f, (sim.JumpProgress - 0.75f) / 0.25f));
        }

        /// <summary>
        /// The legs over a hurdle: in the wind-up the leading leg comes up nearly straight over the other, planted
        /// foot and comes down to the ground by the landing; in the air the other goes out to the side and round
        /// to the front pose (jumpTrailPath). Past the landing see StepJumpLanding.
        /// </summary>
        void PoseJump(RunnerSim sim)
        {
            Leg lead = sim.JumpLeg == 0 ? leftLeg : rightLeg, trail = sim.JumpLeg == 0 ? rightLeg : leftLeg;
            lead.sideOut = trail.sideOut = 0f;
            if (!sim.InJump)
            {
                float fade = JumpAnkleFade * Time.deltaTime;
                lead.jumpW = Mathf.MoveTowards(lead.jumpW, 0f, fade);
                trail.jumpW = Mathf.MoveTowards(trail.jumpW, 0f, fade);
                return;
            }

            float s = sim.JumpProgress;
            float up = Mathf.SmoothStep(0f, 1f, sim.JumpWindupProgress);
            float down = Mathf.SmoothStep(0f, 1f, (s - 0.7f) / 0.3f);
            lead.thighWorld = Mathf.Lerp(lead.thighWorld, Mathf.Lerp(jumpLeadThigh, jumpLandThigh, down), up);
            lead.angles.knee = Mathf.Lerp(lead.angles.knee, Mathf.Lerp(jumpLeadKnee, jumpLandKnee, down), up);
            lead.jumpAnkle = jumpLeadAnkle;
            lead.jumpW = up * (1f - down);

            float planted = Mathf.SmoothStep(0f, 1f, sim.JumpWindupProgress * 2f);
            float off = Mathf.SmoothStep(0f, 1f, s / 0.3f);
            float through = Mathf.SmoothStep(0f, 1f, (s - 0.35f) / 0.65f);
            Vector3 aim = Vector3.Slerp(LegPlane(jumpStanceThigh), TrailPathPoint(s), off);
            aim = Vector3.Slerp(LegPlane(trail.thighWorld), aim, planted);
            // Split into the leg's own swing and the turn out of its plane, so the knee keeps bending the way it can.
            trail.thighWorld = Mathf.Atan2(aim.z, -aim.y) * Mathf.Rad2Deg;
            trail.sideOut = Mathf.Asin(Mathf.Clamp(aim.x, -1f, 1f)) * Mathf.Rad2Deg;
            float knee = Mathf.Lerp(jumpStanceKnee, Mathf.Lerp(jumpTrailKnee, jumpLeadKnee, through), off);
            trail.angles.knee = Mathf.Lerp(trail.angles.knee, knee, planted);
            trail.jumpAnkle = Mathf.Lerp(jumpTrailAnkle, jumpLeadAnkle, through);
            trail.jumpW = planted * off;
        }

        /// <summary>
        /// The legs change roles at the landing: the leading leg, now on the ground, is the input's again; the other
        /// goes straight on from the front pose to its place, without stopping, and a press takes it from wherever it is.
        /// </summary>
        void StepJumpLanding(RunnerSim sim, double now)
        {
            bool inJump = sim.InJump;
            if (_wasInJump && !inJump)
            {
                Leg lead = sim.JumpLeg == 0 ? leftLeg : rightLeg, trail = sim.JumpLeg == 0 ? rightLeg : leftLeg;
                lead.angles.thigh = jumpLandThigh;
                lead.angles.knee = jumpLandKnee;
                lead.follow = 0f;
                Enter(lead, LegPhase.Rest, now, StepPart(frontHomeTime));
                trail.angles.thigh = jumpLeadThigh;
                trail.angles.knee = jumpLeadKnee;
                trail.follow = 0f;
                Enter(trail, LegPhase.Rest, now, StepPart(frontHomeTime));
                trail.easeOut = true;   // already moving: no slow start
            }
            _wasInJump = inJump;
        }

        /// <summary>Where a thigh at this angle points (x = out to its side, y = up, z = forward), in the leg's plane.</summary>
        static Vector3 LegPlane(float thigh) =>
            new Vector3(0f, -Mathf.Cos(thigh * Mathf.Deg2Rad), Mathf.Sin(thigh * Mathf.Deg2Rad));

        /// <summary>Where the trailing thigh points this far through the air: round the path, on the sphere about the hip.</summary>
        Vector3 TrailPathPoint(float s)
        {
            int last = Mathf.Min(jumpTrailPath.Length, jumpTrailTimes.Length) - 1;
            for (int i = 0; i < last; i++)
            {
                if (s > jumpTrailTimes[i + 1]) continue;
                float k = Mathf.InverseLerp(jumpTrailTimes[i], jumpTrailTimes[i + 1], s);
                return Vector3.Slerp(jumpTrailPath[i].normalized, jumpTrailPath[i + 1].normalized, Mathf.SmoothStep(0f, 1f, k));
            }
            return jumpTrailPath[last].normalized;
        }

        /// <summary>
        /// Neck and head. The head's own angle sits on the pose's trunk: up to neckMax of it the head still looks
        /// ahead, beyond that it goes down with it (or with it all the way by the pose's headDown); the head angle
        /// comes on top. The trunk itself (the pose's plus the lean the hips have followed) only lags behind it,
        /// so the neck bends the difference, never past neckMax. Sitting back (sit, deg −) the head takes only
        /// headKeep of it and keeps looking ahead: the neck bends forward on the trunk, up to neckMax. The head is
        /// aimed by placing its look target.
        /// </summary>
        void WriteHead(float trunk, float poseTrunk, float head, float sit, float headDown, Quaternion rootRot)
        {
            float follow = Mathf.Lerp(Mathf.Max(0f, poseTrunk - neckMax), poseTrunk, headDown);
            float lead = Mathf.Lerp(Mathf.Min(neckMax, headLead), neckMax, Mathf.Clamp01(-sit / 10f));
            float pitch = Mathf.Clamp(follow + head + sit * headKeep, trunk - neckMax + body.Sim.T.headBack, trunk + lead);
            float neck = Mathf.Clamp(pitch - trunk, -neckMax, neckMax) * NeckShare;
            spineTipTarget.rotation = rootRot * Quaternion.Euler(trunk + neck, 0f, 0f) * _restSpineTip;
            if (!_headAim || !_headBone || !_lookTarget) return;

            _headAim.weight = 1f;
            _lookTarget.position = _headBone.position + rootRot * (Quaternion.Euler(pitch, 0f, 0f) * Vector3.forward) * _lookDistance;
        }

        /// <summary>
        /// Leaning back (deg, −) is the whole body rocking back on the heels of the feet on the ground: the pelvis
        /// swings back and a little down around them, the legs in the air go round with it, and the feet on the
        /// ground turn onto their heels (heelRock of the lean: toes up, the ankle going round the heel). The leg IK
        /// bends those knees, the legs end up angled forward. Trunk and head turn with it (Update). With no foot on
        /// the ground it turns about the pelvis.
        /// </summary>
        void SitBack(ref Vector3 hip, float backLean)
        {
            Vector3 pivot = hip;
            int n = 0;
            Vector3 sum = Vector3.zero;
            foreach (var leg in new[] { leftLeg, rightLeg })
                if (Carries(leg)) { sum += Heel(leg); n++; }
            if (n > 0) pivot = sum / n;
            Quaternion r = Quaternion.Euler(backLean, 0f, 0f);
            Quaternion foot = Quaternion.Euler(backLean * heelRock, 0f, 0f);
            hip = pivot + r * (hip - pivot);
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                leg.knee = pivot + r * (leg.knee - pivot);
                if (!Carries(leg)) { leg.ankle = pivot + r * (leg.ankle - pivot); continue; }
                Vector3 heel = Heel(leg);
                leg.ankle = heel + foot * (leg.ankle - heel);
                leg.rock = backLean * heelRock;
            }
        }

        /// <summary>Where a foot on the ground touches it at the heel (runner space): behind the ankle, on the ground.</summary>
        Vector3 Heel(Leg leg) => new Vector3(leg.ankle.x, _sole, leg.ankle.z - HeelBehindAnkle);

        /// <summary>
        /// Body height (m, runner space, from the rest height) from the leg angles: the pelvis sits so that the
        /// lowest carrying foot — any leg not held back — stands on the ground, through heightSpring. Opening the
        /// legs lifts the body (a push lifts it on the pushing leg), folding them lowers it; a leg folded up in
        /// the air is higher than the other, so it never pulls the body down. Low down (getting up off the ground)
        /// the height spring is softer (liftSpring, blended by how low the body is). With no foot carrying — the last
        /// one has pushed off and stays back — the body flies on with the speed it has under constant gravity
        /// (the sim's), until something is on the ground (KeepAboveGround).
        /// </summary>
        float UpdateBodyHeight(RunnerSim sim, float dt)
        {
            // Over a hurdle no foot is on the ground to ask for a height: the legs' folding must not sink the body.
            if (sim.JumpInAir)
            {
                _carried = true;
                return _bodyY.Value;
            }
            bool carryL = Carries(leftLeg), carryR = Carries(rightLeg);
            // Running with both feet in the air (not both held: that is the dive below): the body flies on from
            // where it is, never starting down, under the sim's gravity until a foot lands. No tip over, no settle.
            // A body already falling from a dive (not carried) goes on falling as a dive does, keys let go or not.
            bool dive = leftLeg.phase == LegPhase.Hold && rightLeg.phase == LegPhase.Hold;
            if (!carryL && !carryR && !dive && _heightSet && _carried)
            {
                // Every hop leaves at flightLift: not the height spring's own speed (it may be catching the body
                // sharply just then, and would throw it high).
                if (!_inFlight) { _bodyY.Velocity = flightLift; _flightFrom = _bodyY.Value; }
                _inFlight = true;
                _bodyY.Velocity -= flightGravity * dt;
                _bodyY.Value += _bodyY.Velocity * dt;
                // A leg held out in front while the other is behind: the body comes down no lower than just over
                // that foot and glides there (the split held near the ground, no landing yet, no sliding foot).
                float glide = float.MinValue;
                if (HoldsReach(leftLeg, rightLeg, sim.Time)) glide = Mathf.Max(glide, LegNeed(leftLeg) + GlideGap);
                if (HoldsReach(rightLeg, leftLeg, sim.Time)) glide = Mathf.Max(glide, LegNeed(rightLeg) + GlideGap);
                // A leg staying in front in the air: the body comes down no lower than it left (the push's lift
                // goes up and back down) and no lower than just over that foot, until it lands.
                if (StayGlides(leftLeg, rightLeg, sim.Time)) glide = Mathf.Max(glide, Mathf.Max(_flightFrom, LegNeed(leftLeg) + GlideGap));
                if (StayGlides(rightLeg, leftLeg, sim.Time)) glide = Mathf.Max(glide, Mathf.Max(_flightFrom, LegNeed(rightLeg) + GlideGap));
                if (_bodyY.Value < glide)
                {
                    _bodyY.Value = glide;
                    _bodyY.Velocity = Mathf.Max(0f, _bodyY.Velocity);
                }
                _carried = true;
                return _bodyY.Value;
            }
            _inFlight = false;
            if (carryL || carryR)
            {
                float need = float.MinValue;
                if (carryL) need = Mathf.Max(need, LegNeed(leftLeg));
                if (carryR) need = Mathf.Max(need, LegNeed(rightLeg));
                // Pushing off, a leg throws the body, it does not take it down (both pushing together: the dive).
                if ((!carryL || leftLeg.phase == LegPhase.Push) && (!carryR || rightLeg.phase == LegPhase.Push) && _heightSet)
                    need = Mathf.Max(need, _bodyY.Value);
                if (!_heightSet) _bodyY.Reset(need);
                _heightSet = true;
                float low = Mathf.SmoothStep(0f, 1f, (-_bodyY.Value - liftBelow) / Mathf.Max(0.01f, liftRange));
                // Coming down the body sinks softly onto the front foot; going up it keeps to the legs.
                SpringSettings spring = need < _bodyY.Value ? dropSpring : heightSpring;
                _bodyY.Step(need, Mathf.Lerp(spring.frequency, liftSpring.frequency, low),
                    Mathf.Lerp(spring.damping, liftSpring.damping, low), dt);
                _carried = true;
                return _bodyY.Value;
            }

            // The last foot pushes off: the body leaves the ground going up at the kick's speed, always the same (not
            // the height spring's or a run flight's speed on top: the dive stays low, as in the reference game).
            if (_carried) _bodyY.Velocity = liftKick;
            _carried = false;
            _bodyY.Velocity -= sim.T.gravity * dt;
            _bodyY.Value += _bodyY.Velocity * dt;
            return _bodyY.Value;
        }

        /// <summary>
        /// A leg carries the body only with its foot on the ground: home, landed in front, or pushing (to the end of
        /// its push) once its foot is down. A leg in the air (reaching, folding up, coming through, waiting behind,
        /// or pressed while still in the air) or held back does not: the body never sinks to put a foot that is still
        /// in the air on the ground, and never stays up on one either (it comes down onto it first).
        /// </summary>
        static bool Carries(Leg leg) =>
            leg.grounded && (leg.phase == LegPhase.Rest || (leg.phase == LegPhase.Front && !leg.stay) ||
                             leg.phase == LegPhase.Push);

        /// <summary>
        /// Body height a carrying leg asks for: its foot on the ground, and its knee not in it (folding under the
        /// body getting up, the knee lifts it through the height springs, not at once).
        /// </summary>
        float LegNeed(Leg leg) =>
            Mathf.Max(_ground + HeelLift(leg) - leg.ankle.y, _sole + kneeRest - leg.knee.y);

        /// <summary>
        /// Nothing goes into the ground: the pelvis (MinPelvis), the knees and feet of legs that do not carry (a
        /// carrying leg asks for its height through the springs instead, LegNeed: its foot is kept on the ground by
        /// bending its knee) and the head (headRest; its bone as last frame left it, moved by the body since). The
        /// body stops on the first that touches.
        /// </summary>
        float KeepAboveGround(Vector3 hip, float bodyY)
        {
            float up = _sole + MinPelvis - (hip.y + bodyY);
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                if (Carries(leg)) continue;
                up = Mathf.Max(up, _sole + kneeRest - (leg.knee.y + bodyY));
                up = Mathf.Max(up, _ground + HeelLift(leg) - (leg.ankle.y + bodyY));
            }
            float headUp = _headBone && _heightSet ? _sole + headRest - (Local(_headBone).y + bodyY - _lastBodyY) : -1f;
            up = Mathf.Max(up, headUp);
            _headDown = headUp > -0.01f;
            if (up > 0f)
            {
                bodyY += up;
                _bodyY.Value += up;
                _bodyY.Velocity = Mathf.Max(0f, _bodyY.Velocity);
            }
            _lastBodyY = bodyY;
            return bodyY;
        }

        /// <summary>
        /// The body's lean (deg, + forward), down a chain from the head: the sim's head angle is the measure (on the
        /// beat upright, mashing or holding forward). The chest takes chestShare of it (through hipFollowSpring), the
        /// waist base waistShare, the hips hipShare, each following the one above through waistSpring — every link
        /// bends a little on the one below, so the back rounds like a chain, and the bend flows down. The whole
        /// body also leans with speed (hipRunLean) and with both keys held (bothLean, toward level: the dive, where
        /// the curve turns into the hump, spineLevelCurve, the head dipping, headDip). Back only by the head swinging
        /// back past its rest (hipBackMax; SitBack). Returns the hips' lean; baseBend is B-spine's bend on the hips,
        /// bend the whole waist's (top on hips), headLean the head's angle. How low the body gets is up to the legs.
        /// </summary>
        float UpdateLean(float dt, out float baseBend, out float bend, out float headLean)
        {
            // The measure is the sim's head angle: presses on the beat keep it upright, mashing or holding take it
            // forward. The whole body leans a little with speed and, with both keys held, toward level (the dive).
            float head = Mathf.Max(0f, _headAngle);
            // Both keys held (squared: quick presses on both, mashing, barely count; both held down do).
            float both = Mathf.Min(_holdL, _holdR);
            float whole = Mathf.Lerp(hipRunLean.x, hipRunLean.y, Speed01()) * _stepping + bothLean * both * both;
            // Back only by the head swinging back past its rest: every link alike (the body on its heels).
            float back = _headAngle < 0f ? Mathf.Max(-hipBackMax, Mathf.Min(0f, _headAngle - body.Sim.HeadRest)) : 0f;

            // A chain down from the head: chest, waist base, hips, each a smaller share of the head's angle (so each
            // bends a little on the one below: a rounded back, not a rod) and each following the one above a moment
            // later (the bend flows down; the hips come last).
            _lean.Step(whole + back + chestShare * head, hipFollowSpring.frequency, hipFollowSpring.damping, dt);
            float chest = _lean.Value;
            float under = Mathf.Max(0.01f, chestShare);
            _waistBase.Step(whole + back + (chest - whole - back) * waistShare / under,
                waistSpring.frequency, waistSpring.damping, dt);
            float waist = _waistBase.Value;
            float overW = Mathf.Max(0.01f, waistShare);
            _hipsLean.Step(whole + back + (waist - whole - back) * hipShare / overW,
                waistSpring.frequency, waistSpring.damping, dt);
            float hips = _hipsLean.Value;

            // A key held tips the whole body forward at once (holdLean, quick spring), over the chain's own lean.
            float held = HeldWeight(Mathf.Max(_holdL, _holdR));
            _holdTip.Step(holdLean * held, holdTipSpring.frequency, holdTipSpring.damping, dt);
            hips += _holdTip.Value;
            chest += _holdTip.Value;
            waist += _holdTip.Value;

            baseBend = waist - hips;
            bend = chest - hips;
            // Near level (the dive) the curve turns over into the hump: the low back up, the shoulders above.
            float level = Mathf.Max(1f, pelvisPitchRange.y);
            float hump = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f * level, level, hips));
            baseBend = Mathf.Lerp(baseBend, spineLevelCurve * 0.5f, hump);
            bend = Mathf.Lerp(bend, spineLevelCurve, hump);
            if (bend < -waistBackMax)
            {
                baseBend *= -waistBackMax / bend;
                bend = -waistBackMax;
            }
            // The head itself: the measure, on the same whole-body lean, dipping in front near level.
            headLean = whole + _holdTip.Value + _headAngle + headDip * hump;
            return hips;
        }

        /// <summary>The IK reproduces the angles: target = the ankle, hint = in front of the knee, across the thigh.</summary>
        void WriteLeg(Leg leg)
        {
            leg.target.position = transform.TransformPoint(leg.ankle);
            float th = leg.thighWorld * Mathf.Deg2Rad;
            leg.hint.position = transform.TransformPoint(leg.knee + leg.sideTurn * new Vector3(0f, Mathf.Sin(th), Mathf.Cos(th)) * HintDistance);
        }

        /// <summary>−1 = left, +1 = right.</summary>
        static float Side(Leg leg) => leg.restAnkle.x < 0f ? -1f : 1f;

        /// <summary>A foot-profile curve at phase s, blended between the acceleration and max-speed profiles.</summary>
        float Profile(System.Func<FootProfile, AnimationCurve> curve, float s)
            => Mathf.Lerp(curve(accelProfile).Evaluate(s), curve(maxSpeedProfile).Evaluate(s), _profileW);

        /// <summary>
        /// One key moves one leg and the opposite arm: each time the opposite leg starts a phase, the arm starts a
        /// move (armEase) toward where that phase takes the leg, in proportion — the leg's back angle is armBack /
        /// elbowBack, its reach armFront / elbowFront, in between in between (the elbow goes with the upper arm: bent
        /// in front, open behind). The move lasts the leg's phase time but at least armMinTime. At home (standing)
        /// the arm goes to the pose's own rest.
        /// </summary>
        void StepArm(Arm arm, ArmPose pose, Leg leg, double now)
        {
            if (preview)
            {
                arm.swing = pose.swing;
                arm.elbow = pose.elbow;
                arm.tweenStart = -1.0;
                return;
            }
            float toSwing, toElbow;
            if (leg.phase == LegPhase.Rest && _runHome < 0.5f)
            {
                toSwing = pose.swing;
                toElbow = pose.elbow;
            }
            else
            {
                // Where the leg really is (its thigh this frame, with the hips' tip): a held leg going back with the
                // tipping body sends the arm back too.
                float back = Recover().thigh, front = Reach().thigh;
                float k = Mathf.InverseLerp(back, front, leg.thighWorld);
                toSwing = Mathf.Lerp(armBack, armFront, k);
                toElbow = Mathf.Lerp(elbowBack, elbowFront, k);
            }
            // A new phase of the leg starts a new move, from wherever the arm is.
            if (arm.tweenStart < 0.0 || leg.phase != arm.legPhase || leg.phaseTime != arm.legPhaseTime)
            {
                arm.fromSwing = arm.tweenStart < 0.0 ? toSwing : arm.swing;
                arm.fromElbow = arm.tweenStart < 0.0 ? toElbow : arm.elbow;
                arm.tweenStart = now;
                arm.tweenTime = Mathf.Max(armMinTime, leg.duration);
                arm.legPhase = leg.phase;
                arm.legPhaseTime = leg.phaseTime;
            }
            arm.toSwing = toSwing;
            arm.toElbow = toElbow;
            float e = Ease(armEase, armCurve, (float)(now - arm.tweenStart) / Mathf.Max(0.01f, arm.tweenTime));
            arm.swing = Mathf.Lerp(arm.fromSwing, arm.toSwing, e);
            arm.elbow = Mathf.Lerp(arm.fromElbow, arm.toElbow, e);
        }

        // Arms, knees, ankles and toes are posed after the Animator/rig has written the skeleton this frame.
        void LateUpdate()
        {
            if (!_bound) return;
            TrimNeck();
            PoseArm(leftArm);
            PoseArm(rightArm);
            FixLegTwist(leftLeg);
            FixLegTwist(rightLeg);
            PoseAnkle(leftLeg);
            PoseAnkle(rightLeg);
            PoseToes(leftLeg);
            PoseToes(rightLeg);
        }

        /// <summary>B-neck turns back by this frame's trim after the rig; the head keeps the angle the rig gave it.</summary>
        void TrimNeck()
        {
            if (!_neckBone || !_headBone || _neckTrimNow <= 0f) return;
            Quaternion head = _headBone.rotation;
            _neckBone.rotation = Quaternion.AngleAxis(-_neckTrimNow, transform.right) * _neckBone.rotation;
            _headBone.rotation = head;
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
        /// as a real ankle moves), blended by swingW.
        /// </summary>
        void PoseAnkle(Leg leg)
        {
            if (!leg.shinBone || !leg.footBone) return;
            Vector3 axis = transform.right;
            Quaternion neutral = leg.shinBone.rotation * leg.restFootInShin;
            Quaternion wanted = transform.rotation * Quaternion.Euler(leg.pitch + leg.rock, 0f, 0f) * leg.restFootRunner;
            Vector3 from = Vector3.ProjectOnPlane(neutral * leg.toeInFoot, axis);
            Vector3 to = Vector3.ProjectOnPlane(wanted * leg.toeInFoot, axis);
            // The pitch's bend is clamped before the blend: a leg folding back up has its shin near level, where
            // the ground pitch asks for an impossible bend that would drag the swing curve far toes up.
            float bend = Mathf.Lerp(Mathf.Clamp(Vector3.SignedAngle(from, to, axis), -ankleUpMax, ankleDownMax),
                leg.swingAnkle, leg.swingW * leg.strideW);
            bend = Mathf.Clamp(Mathf.Lerp(bend, leg.jumpAnkle, leg.jumpW), -ankleUpMax, ankleDownMax);
            leg.footBone.rotation = Quaternion.AngleAxis(bend, axis) * neutral;
        }

        /// <summary>
        /// Toes after the ankle. On the ground they lie flat (or roll over their tips at toe-off), so the
        /// ball joint bends by however far the heel is up — but not on a foot standing on its heel, toes up,
        /// where they stay in line with it; in the air they follow the swing curve.
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
            // Standing on the heel, toes up (the front foot): the toes stay in line with the foot, not laid flat.
            onGround *= 1f - Mathf.Clamp01(-(leg.pitch + leg.rock) / ToesUpFree);

            float bend = Mathf.Lerp(-leg.swingToe * leg.strideW * leg.swingW, leg.toeRoll * leg.strideW - footPitch, onGround); // + = toes down
            bend = Mathf.Clamp(Mathf.Lerp(bend, 0f, leg.jumpW), -toeUpMax, toeDownMax);
            leg.toeBone.rotation = Quaternion.AngleAxis(bend, axis) * (leg.footBone.rotation * leg.restToeInFoot);
        }

        void OnDrawGizmos()
        {
            if (!_bound || !Application.isPlaying) return;
            DrawLegGizmos(leftLeg, new Color(0.3f, 0.6f, 1f));
            DrawLegGizmos(rightLeg, new Color(1f, 0.6f, 0.2f));
        }

        /// <summary>The legs as their angles give them (hip joint → knee → ankle), play mode only.</summary>
        void DrawLegGizmos(Leg leg, Color c)
        {
            Vector3 knee = transform.TransformPoint(leg.knee), ankle = transform.TransformPoint(leg.ankle);
            Gizmos.color = c;
            Gizmos.DrawLine(leg.shinBone ? leg.shinBone.parent.position : knee, knee);
            Gizmos.DrawLine(knee, ankle);
            Gizmos.DrawWireSphere(ankle, 0.025f);
        }
    }
}
