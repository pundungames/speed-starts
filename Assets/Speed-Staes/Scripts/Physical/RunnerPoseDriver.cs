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
    ///        held         — the pushed leg goes on to the hold angles (Hold) and rises behind with the waist
    ///                       lean (one held or both); the leg in front turns into the support angles as the hold builds up;
    ///        no input     — home: the crouch angles (blended to the running home while presses keep coming).
    ///      Every phase starts from the leg's current angles, so a press mid-swing goes on back from there.
    ///   3. The lean comes from the head alone (UpdateLean), down a chain: head → neck → chest (the waist's
    ///      top) → B-spine (its base) → hips, each following the one above through a spring (the head leads,
    ///      the hips come last). Forward the hips take hipShare of the lean the head asks for and the back
    ///      curves on top of them (waistBaseShare, waistTopShare of the lean), back only up to waistBackMax; the
    ///      legs still turn with all of it. The hips pitch within pelvisPitchRange. Legs in the air and held legs turn with the hips' lean; a leg
    ///      on the ground keeps its angle to the ground (FollowHips). Height still comes from the legs (4), so
    ///      while a foot carries the body the head cannot reach the ground.
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
    ///      tipping over as the head goes forward (the hips follow it, and in the air the body tips on by how far
    ///      forward the head is, airTip, past level) and comes down head first: the dive, no special case. Head, pelvis, knees and
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
            /// <summary>In front on the ground while the other leg is still pushing, recovering or held.</summary>
            Front,
            /// <summary>Still held after the push: back and straight, rising behind with the waist lean.</summary>
            Hold,
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
            [HideInInspector] public float swingToe;    // toe bend in the air (deg, + = toes up)
            [HideInInspector] public bool reachNext;    // the other key went down while this leg was going back: reach once back
            [HideInInspector] public bool easeOut;      // this phase starts at the leg's own speed and slows into its target
            [HideInInspector] public bool swing;        // recover, mid and home as one curve (no stop between them)
            [HideInInspector] public double swingStart; // sim time the swing began (end of the push)
            [HideInInspector] public float swingTime;   // recover + mid + home of this swing (s)
            [HideInInspector] public float thighRate;   // the thigh's angular speed last frame (deg/s, + = forward)
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
        [Tooltip("A held leg at a full hold: back, the knee a little bent (not dead straight). It rises behind with the " +
                 "hips' lean, up to heldLegMaxAngle.")]
        public LegAngles holdBack = new LegAngles(-60f, 145f);
        [Tooltip("The leg in front while the other is held, at a full hold, carrying the body. thigh + knee = 180 " +
                 "stands the shin upright, the pelvis behind and above the foot.")]
        public LegAngles holdSupport = new LegAngles(80f, 100f);
        [Tooltip("The knee is never quite straight (the IK snaps at full reach).")]
        public float maxKnee = 177f;
        [Tooltip("A held leg rises behind with the body's lean up to this angle from straight down (deg; 90 = level).")]
        public float heldLegMaxAngle = 90f;
        [Tooltip("A held leg's thigh goes at most this far past the hips' angle (deg; − = short of it, which the slightly " +
                 "bent knee makes up): leaning, held legs trail the body in a nearly straight line instead of rising " +
                 "ahead of it (the reference video's dive).")]
        public float heldLegArch = -10f;

        [Header("Leg timing (s)")]
        [Tooltip("Push: the pressed leg going back, and the other leg's reach forward with it (s). A fixed time of its " +
                 "own, not the sim's stanceTime: too short and the legs jump instead of flowing.")]
        public float pushTime = 0.2f;
        [Tooltip("At max speed the push and the swing take this share of their time (1 = as slow as standing): the " +
                 "faster the run, the quicker the legs and arms, so they keep up with presses on time.")]
        [Range(0.2f, 1f)] public float animSpeedUp = 0.5f;
        [Tooltip("Shin folding up after the push. recover + mid + home are shares of the step: scaled together so " +
                 "push + recover + mid + home fill the time between presses (stepTimeMin to stepTimeMax), one curve " +
                 "with no stop between them.")]
        public float recoverTime = 0.15f;
        [Tooltip("From recovered to the mid angle (a share of the step, see recoverTime).")]
        public float midTime = 0.15f;
        [Tooltip("From the mid angle to home (a share of the step, see recoverTime).")]
        public float homeTime = 0.15f;
        [Tooltip("The leg in front goes home this slowly once the other is no longer pushing or held (the body passes " +
                 "over it); scaled with the step like recoverTime.")]
        public float frontHomeTime = 0.3f;
        [Tooltip("The step never goes quicker than this (s), however fast the presses come.")]
        public float stepTimeMin = 0.2f;
        [Tooltip("The step never goes slower than this (s): sparser presses finish it in this time and the pose waits.")]
        public float stepTimeMax = 0.6f;
        [Tooltip("The swing takes this many times the press-to-press time (after stepTimeMin/Max): above 1 the legs " +
                 "and arms move slower than the presses come, and the next press takes the leg on from where it is.")]
        public float stepSlow = 2f;
        [Tooltip("How much of each new press-to-press time the step takes on (1 = just the last one): one odd press " +
                 "does not jerk the legs.")]
        [Range(0.1f, 1f)] public float stepFollow = 0.5f;
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
        [Tooltip("With nothing carrying the body the whole body (head included) tips forward at up to this rate " +
                 "(deg/s), by how far forward the head is, until the head is on the ground or the hips are level " +
                 "(pelvisPitchRange.y): it comes down and lies flat. It comes back once the legs carry the body again. " +
                 "The reference video: the straight body turns ~45° → ~78° over the half second in the air.")]
        public float airTip = 90f;
        [Tooltip("Once a leg carries the body again the tip goes back to 0 through this spring.")]
        public SpringSettings airTipBack = new SpringSettings(3f, 1f);
        [Tooltip("Head on the ground with no foot carrying: the body settles through this spring — the waist's curve " +
                 "opens, the head comes up off its forward bend and the tip goes on to level — so the body comes down " +
                 "after the head (chest, hips, knees) instead of hanging on it. A foot carrying again takes it back.")]
        public SpringSettings settleSpring = new SpringSettings(1.5f, 1f);
        [Tooltip("Settled on the ground the head comes up this far past the line of the body (deg), looking ahead.")]
        public float settleHeadUp = 15f;

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
        [Tooltip("Share of the head angle the body follows, all the way (the head comes down, the body bends after it).")]
        [Range(0f, 1f)] public float hipFollow = 1f;
        [Tooltip("The top of the waist (chest, neck base) follows the head through this spring, so the head leads.")]
        public SpringSettings hipFollowSpring = new SpringSettings(4f, 0.9f);

        [Header("Body: head → neck → chest → waist base → hips, each following the one above")]
        [Tooltip("B-spine's bend on the hips at rest (deg, its Inspector X); the chain's bend comes on top.")]
        public float waistAngle = 2f;
        [Tooltip("The top of the waist's bend forward on B-spine at rest (deg, on top of the rig's rest): with " +
                 "waistAngle a gentle curve, the base a little back, chest and neck forward.")]
        public float waistTopAngle = 13f;
        [Tooltip("Share of the forward lean the hips take themselves (the legs turn with all of it); the waist bends " +
                 "on top of them. Leaning back the hips take all of it.")]
        [Range(0f, 1f)] public float hipShare = 0.65f;
        [Tooltip("The top of the waist (chest and neck base) bends this share of the lean on B-spine, on top of the hips'.")]
        [Range(0f, 1f)] public float waistTopShare = 0.67f;
        [Tooltip("The base of the waist (B-spine) bends this share of the lean on the hips.")]
        [Range(0f, 1f)] public float waistBaseShare = 0.27f;
        [Tooltip("Each link down the waist follows the one above through this spring (damping 1: it lags, never swings).")]
        public SpringSettings waistSpring = new SpringSettings(3f, 1f);
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
        [Tooltip("Arms angled out sideways from the body (deg), so they clear the trunk.")]
        public float armOutward = 8f;
        [Tooltip("Shoulder (clavicle) follows the arm swing (deg at full swing).")]
        public float shoulderFollow = 12f;

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
        const float MinFlowRate = 50f;         // a leg turning forward slower than this (deg/s) starts its reach from rest
        const float FollowRate = 6f;           // a leg changes between turning with the hips and keeping to the ground this fast (1/s)

        Vector3 _restHip;
        Quaternion _restHipRot, _restSpineRoot, _restSpineTip;
        float _ground, _sole, _profileW, _runHome;
        float _restWaist;                          // B-spine's bend on the hips at rest (deg)
        float _stepping;                           // 1 = a key is down (run stance), 0 = none: Ready crouch
        float _pressGap = float.MaxValue;          // press to press (s), smoothed by stepFollow
        double _lastPress = -1.0;                  // sim time of the last press
        Spring _bodyY, _lean;                      // the waist top's lean after the head (deg)
        Spring _waistBase, _hipsLean;              // B-spine's and the hips' lean down the chain (deg)
        float _headAngle;                          // this frame's head angle (deg, + = forward)
        float _holdL, _holdR;                      // hold weights this frame
        bool _bound, _heightSet;
        float _lastBodyY;                          // last frame's body height (the head bone is last frame's too)
        bool _carried = true, _headDown;           // last frame: a leg carried the body / the head was on the ground
        Spring _tip;                               // the lean the body has tipped over in the air (deg)
        Spring _settle;                            // 0..1: the body settling down after the head touched (no foot carrying)
        MultiAimConstraint _headAim;
        Transform _headBone, _lookTarget, _neckBone;
        float _neckTrimNow;                        // this frame's neck trim (deg), applied after the rig
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
                if (l.phase == LegPhase.Hold) Enter(l, LegPhase.Mid, time, StepPart(midTime));
                return;
            }
            l.held = true;
            l.reachNext = false;
            if (_lastPress >= 0.0)
            {
                // A pause counts as stepTimeMax: it does not leave the next steps sluggish.
                float gap = Mathf.Min((float)(time - _lastPress), stepTimeMax);
                _pressGap = _pressGap == float.MaxValue ? gap : Mathf.Lerp(_pressGap, gap, stepFollow);
            }
            _lastPress = time;
            Enter(l, LegPhase.Push, time, PushTime());
            var other = Legs(1 - leg);
            if (other.HeldBack) return;
            // A leg still going back finishes that first; one coming forward turns to the front as it goes.
            if (other.phase == LegPhase.Push || other.phase == LegPhase.Recover) other.reachNext = true;
            else if (other.phase == LegPhase.Mid) ReachOn(other, time);
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
            float dist = Mathf.Abs(reach.thigh - leg.angles.thigh);
            float push = PushTime();
            if (leg.thighRate > MinFlowRate)
            {
                Enter(leg, LegPhase.Reach, time, Mathf.Clamp(2f * dist / leg.thighRate, push, 2f * StepPart(midTime)));
                leg.easeOut = true;
            }
            else
                Enter(leg, LegPhase.Reach, time,
                    Mathf.Max(push, StepPart(midTime) * dist / Mathf.Max(1f, Mathf.Abs(mid.thigh - recover.thigh))));
        }

        void OnRunReset()
        {
            _lean.Reset();
            _waistBase.Reset();
            _hipsLean.Reset();
            _tip.Reset();
            _settle.Reset();
            _carried = true;
            ResetLeg(leftLeg);
            ResetLeg(rightLeg);
            _heightSet = false;
            _pressGap = float.MaxValue;
            _lastPress = -1.0;
        }

        Leg Legs(int i) => i == 0 ? leftLeg : rightLeg;

        float Speed01() => body && body.Sim != null ? Mathf.Clamp01(body.Sim.Speed / body.Sim.T.maxSpeed) : 0f;

        /// <summary>
        /// A phase's share of the step: push + recover + mid + home fill the time between presses, so the legs (and
        /// the arms after them) flow from press to press, faster as the presses come faster. Between stepTimeMin and
        /// stepTimeMax; before the presses have a rhythm, the sim's StepTime.
        /// </summary>
        float StepPart(float time)
        {
            float gap = _pressGap == float.MaxValue ? (body && body.Sim != null ? body.Sim.StepTime : 0.4f) : _pressGap;
            float step = Mathf.Clamp(gap, stepTimeMin, Mathf.Max(stepTimeMin, stepTimeMax)) * stepSlow * SpeedUp();
            return time * Mathf.Max(0.01f, step - PushTime()) / SwingParts;
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
        }

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
                    if (leg.phase == LegPhase.Hold && !leg.held)
                    {
                        Enter(leg, LegPhase.Mid, time, StepPart(midTime));
                        continue;
                    }
                    // The front foot stays planted ahead until the other leg has pushed and recovered.
                    if (leg.phase == LegPhase.Front && partner.phase != LegPhase.Push && partner.phase != LegPhase.Hold &&
                        partner.phase != LegPhase.Recover)
                    {
                        Enter(leg, LegPhase.Rest, time, StepPart(frontHomeTime));
                        continue;
                    }
                    if (!leg.Timed || time < leg.EndTime) continue;
                    double end = leg.EndTime;
                    switch (leg.phase)
                    {
                        case LegPhase.Push:
                            if (leg.held) Enter(leg, LegPhase.Hold, end, 0f);
                            else
                            {
                                leg.swingStart = end;
                                leg.swingTime = StepPart(recoverTime + midTime + homeTime);
                                EnterSwing(leg, LegPhase.Recover, end);
                            }
                            break;
                        case LegPhase.Recover:
                            if (leg.reachNext) ReachOn(leg, end);
                            else if (leg.swing) EnterSwing(leg, LegPhase.Mid, end);
                            else Enter(leg, LegPhase.Mid, end, StepPart(midTime));
                            break;
                        case LegPhase.Mid:
                            if (leg.swing) EnterSwing(leg, LegPhase.Rest, end);
                            else Enter(leg, LegPhase.Rest, end, StepPart(homeTime));
                            break;
                        case LegPhase.Reach:
                            Enter(leg, LegPhase.Front, end, 0f);
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
                case LegPhase.Recover: return recover;
                case LegPhase.Mid: return mid;
                case LegPhase.Reach: return reach;
                case LegPhase.Front: return LegAngles.Lerp(reach, holdSupport, otherHold);
                case LegPhase.Push:
                case LegPhase.Hold: return LegAngles.Lerp(pushEnd, holdBack, ownHold);
                default: return Home();
            }
        }

        LegAngles Home() => LegAngles.Lerp(crouch, runHome, _runHome);

        /// <summary>This frame's angles (from the phase's start into its target) and the foot's angles with them.</summary>
        void UpdateLeg(Leg leg, double now, float dt, float legTurn, float ownHold, float otherHold)
        {
            float s = leg.duration > 0f ? Mathf.Clamp01((float)(now - leg.phaseTime) / leg.duration) : 1f;
            float lastThigh = leg.angles.thigh;
            // Phases start and end at rest (smooth step); one that takes over a moving leg starts at its speed.
            // A swing is one smooth step over recover + mid + home: it slows only as it comes home.
            float ease = leg.easeOut ? 1f - (1f - s) * (1f - s) : Mathf.SmoothStep(0f, 1f, s);
            if (leg.swing && leg.swingTime > 0f)
            {
                SwingShares(leg.phase, out float a, out float b);
                float g = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((float)(now - leg.swingStart) / leg.swingTime));
                float ga = Mathf.SmoothStep(0f, 1f, a), gb = Mathf.SmoothStep(0f, 1f, b);
                ease = Mathf.Clamp01((g - ga) / Mathf.Max(0.0001f, gb - ga));
            }
            leg.angles = LegAngles.Lerp(leg.from, Target(leg, ownHold, otherHold), ease);
            leg.thighRate = dt > 0f ? (leg.angles.thigh - lastThigh) / dt : 0f;
            leg.angles.knee = Mathf.Min(leg.angles.knee, maxKnee);

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
                case LegPhase.Mid:
                    air = 0.5f + 0.5f * s;
                    break;
                case LegPhase.Reach:
                    pitch = Mathf.Lerp(leg.fromPitch, FrontPitch(), Mathf.SmoothStep(0f, 1f, s));
                    break;
                case LegPhase.Front:
                    pitch = FrontPitch();
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
        /// turning up behind with the hips stops at heldLegMaxAngle (level) and never goes more than heldLegArch past
        /// the line of the body (it trails the body nearly straight). Blended, so a leg landing or lifting
        /// off does not snap.
        /// </summary>
        void FollowHips(Leg leg, Leg other, float legTurn, float hipPitch, float dt)
        {
            bool planted = leg.phase == LegPhase.Rest || leg.phase == LegPhase.Front ||
                           leg.phase == LegPhase.Push;
            leg.follow = Mathf.MoveTowards(leg.follow, planted ? 0f : 1f, FollowRate * dt);
            leg.thighWorld = leg.angles.thigh - legTurn * leg.follow;
            // The limits only stop the turn with the hips: a held leg always gets back to its own hold angle.
            if (leg.phase == LegPhase.Hold)
                leg.thighWorld = Mathf.Max(Mathf.Min(leg.angles.thigh, -Mathf.Min(heldLegMaxAngle, hipPitch + heldLegArch)),
                    leg.thighWorld);
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
                bool stepping = sim.Legs[0].Pressed || sim.Legs[1].Pressed;
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
            _runHome = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(restToBaseSpeed.x, restToBaseSpeed.y, sim.Speed)) * _stepping;
            float profileTarget = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(accelProfileSpeed, maxProfileSpeed, sim.Speed));
            _profileW = Mathf.Lerp(_profileW, profileTarget, 1f - Mathf.Exp(-4f * dt));
            _headAngle = preview ? 0f : sim.HeadAngle;
            // The tip over in the air turns the whole body, head included (else the hips would pass the head).
            float tip = 0f, baseBend = 0f, bend = 0f, legLean = 0f;
            if (!preview)
            {
                lean = UpdateLean(dt, out baseBend, out bend, out legLean);
                tip = UpdateTip(dt, pelvisPitchRange.y - p.pelvisPitch - lean);
                lean += tip;
                // The head on the ground with no foot carrying lets the body settle after it: the waist opens.
                float settle = _carried ? 0f : _headDown ? 1f : _settle.Value;
                _settle.Step(settle, settleSpring.frequency, settleSpring.damping, dt);
                float open = 1f - Mathf.Clamp01(_settle.Value);
                baseBend *= open;
                bend *= open;
            }

            // 3. Forward the hips take their part of the lean (the waist bends the rest on them), within
            //    pelvisPitchRange. Back, the whole body leans about the feet on the ground (below, after the height).
            float backLean = Mathf.Min(0f, lean);
            float hipPitch = Mathf.Clamp(p.pelvisPitch + lean - backLean, pelvisPitchRange.x, pelvisPitchRange.y);
            // The lean part only (the pose's own tilt does not turn the legs), with all of the lean the hips left to the waist.
            float legTurn = hipPitch - p.pelvisPitch + legLean;
            Quaternion pelvisTilt = Quaternion.Euler(hipPitch, 0f, 0f);

            UpdateLeg(leftLeg, now, dt, legTurn, _holdL, _holdR);
            UpdateLeg(rightLeg, now, dt, legTurn, _holdR, _holdL);
            FollowHips(leftLeg, rightLeg, legTurn, hipPitch + legLean, dt);
            FollowHips(rightLeg, leftLeg, legTurn, hipPitch + legLean, dt);
            Vector3 hip = _restHip + new Vector3(0f, 0f, p.pelvis.x);
            LegFK(leftLeg, hip, pelvisTilt, p.width);
            LegFK(rightLeg, hip, pelvisTilt, p.width);

            // 4. Body height from the legs (the whole body moves, legs included); nothing goes into the ground.
            //    A carrying foot the height spring lags under stays on the ground: the IK bends its knee.
            float bodyY = preview ? 0f : UpdateBodyHeight(sim, dt);
            if (!preview) bodyY = KeepAboveGround(hip, bodyY);
            hip.y += bodyY;
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                leg.knee.y += bodyY;
                leg.ankle.y += bodyY;
                leg.ankle.y = Mathf.Max(leg.ankle.y, _ground + HeelLift(leg));
            }
            leftLeg.rock = rightLeg.rock = 0f;
            if (backLean < 0f) SitBack(ref hip, backLean);

            // Write the rig. The waist base sits at waistAngle plus its bend on the hips; the chain (TwistChain)
            // fills the chest in between it and the top, whose angle the neck and head work from (0 = the rig's rest).
            Quaternion rootRot = transform.rotation;
            _neckTrimNow = preview ? 0f : neckTrim * Mathf.SmoothStep(0f, 1f, (_lean.Value + tip) / Mathf.Max(1f, neckTrimLean));
            hipTarget.position = transform.TransformPoint(hip);
            hipTarget.rotation = rootRot * Quaternion.Euler(backLean, 0f, 0f) * pelvisTilt * _restHipRot;
            spineRootTarget.rotation = hipTarget.rotation * Quaternion.Euler(waistAngle + baseBend, 0f, 0f);
            // Leaning back the head keeps looking ahead: it takes only headKeep of the lean, the neck bends the rest.
            WriteHead(hipPitch + backLean + waistAngle - _restWaist + waistTopAngle + bend,
                p.pelvisPitch + waistAngle - _restWaist,
                _headAngle + tip - backLean, backLean, p.headDown, Mathf.Clamp01(_settle.Value), rootRot);
            WriteLeg(leftLeg);
            WriteLeg(rightLeg);

            // Arms: one key, one leg, one arm (the right key's press sends the right leg back and the left arm
            // with it); a leg held back keeps its arm back.
            StepArm(leftArm, p.leftArm, LegBack(rightLeg));
            StepArm(rightArm, p.rightArm, LegBack(leftLeg));
        }

        /// <summary>
        /// Neck and head. The head's own angle sits on the pose's trunk: up to neckMax of it the head still looks
        /// ahead, beyond that it goes down with it (or with it all the way by the pose's headDown); the head angle
        /// comes on top. The trunk itself (the pose's plus the lean the hips have followed) only lags behind it,
        /// so the neck bends the difference, never past neckMax. Sitting back (sit, deg −) the head takes only
        /// headKeep of it and keeps looking ahead: the neck bends forward on the trunk, up to neckMax. The head is
        /// aimed by placing its look target.
        /// </summary>
        void WriteHead(float trunk, float poseTrunk, float head, float sit, float headDown, float settle, Quaternion rootRot)
        {
            float follow = Mathf.Lerp(Mathf.Max(0f, poseTrunk - neckMax), poseTrunk, headDown);
            float lead = Mathf.Lerp(Mathf.Min(neckMax, headLead), neckMax, Mathf.Clamp01(-sit / 10f));
            lead = Mathf.Lerp(lead, -settleHeadUp, settle);   // settling on the ground the head comes up, looking ahead
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
            bool carryL = Carries(leftLeg), carryR = Carries(rightLeg);
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

            // The last foot pushes off: the body leaves the ground going up (never carried on down by the spring).
            if (_carried) _bodyY.Velocity = Mathf.Max(0f, _bodyY.Velocity) + liftKick;
            _carried = false;
            _bodyY.Velocity -= sim.T.gravity * dt;
            _bodyY.Value += _bodyY.Velocity * dt;
            return _bodyY.Value;
        }

        /// <summary>
        /// The tip over with nothing carrying the body (deg, + forward, on top of the head's lean; the whole body,
        /// head included): it tips forward by how far forward the head is (airTip) until the head is on the ground
        /// or the hips are level (max), so the body comes down and lies flat. Once a leg carries the body again it
        /// comes back (airTipBack).
        /// </summary>
        float UpdateTip(float dt, float max)
        {
            if (_carried)
                _tip.Step(0f, airTipBack.frequency, airTipBack.damping, dt);
            else
            {
                if (!_headDown || _settle.Value > 0f) _tip.Value += airTip * Mathf.Clamp01(_headAngle / Mathf.Max(1f, body.Sim.T.headForwardMax)) * dt;
                _tip.Value = Mathf.Min(_tip.Value, Mathf.Max(0f, max));
                _tip.Velocity = 0f;
            }
            return _tip.Value;
        }

        /// <summary>A leg carries the body unless it is held back (a pushing leg carries it to the end of its push).</summary>
        static bool Carries(Leg leg) => leg.phase != LegPhase.Hold;

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
        /// The body's lean (deg, + forward), down a chain: the top of the waist follows the head angle all the way
        /// (hipFollow), no threshold, through a spring so the head leads; the waist base follows the top, the
        /// hips follow the base (waistSpring each), so the bend runs down the back like a wave. Settled, forward
        /// the hips take hipShare of the lean the head asks for and the waist curves on top of them (waistBaseShare,
        /// waistTopShare of the lean); the waist bends back waistBackMax at most. Returns the hips' lean; baseBend
        /// is B-spine's bend on the hips, bend the whole waist's (top on hips), legLean the lean the hips left to the
        /// waist (the legs turn with it all the same). Back it only takes the head's swing past where the head rests (headBack while running): the
        /// head alone tips back there, the body sits back only when the way back throws the head beyond it
        /// (about the feet, see SitBack). Holding leans it too, through the head. Nothing else moves it. How low it gets is
        /// up to the legs: they keep the hips at their height (UpdateBodyHeight), so with a foot carrying the
        /// body the head cannot reach the ground. The sim's own lean stays for the game (stumble losses).
        /// </summary>
        float UpdateLean(float dt, out float baseBend, out float bend, out float legLean)
        {
            float head = _headAngle >= 0f ? _headAngle : Mathf.Min(0f, _headAngle - body.Sim.HeadRest);
            _lean.Step(head * hipFollow, hipFollowSpring.frequency, hipFollowSpring.damping, dt);
            float top = _lean.Value;
            _waistBase.Step(top, waistSpring.frequency, waistSpring.damping, dt);
            _hipsLean.Step(_waistBase.Value, waistSpring.frequency, waistSpring.damping, dt);
            float full = _hipsLean.Value;
            float hips = full > 0f ? full * hipShare : full;
            legLean = full - hips;
            // The curve, plus what the links below have not caught up with yet (the wave).
            baseBend = waistBaseShare * top + (_waistBase.Value - full);
            bend = (waistBaseShare + waistTopShare) * top + (top - full);
            if (bend < -waistBackMax)
            {
                baseBend *= -waistBackMax / bend;
                bend = -waistBackMax;
            }
            return hips;
        }

        /// <summary>The IK reproduces the angles: target = the ankle, hint = in front of the knee, across the thigh.</summary>
        void WriteLeg(Leg leg)
        {
            leg.target.position = transform.TransformPoint(leg.ankle);
            float th = leg.thighWorld * Mathf.Deg2Rad;
            leg.hint.position = transform.TransformPoint(leg.knee + new Vector3(0f, Mathf.Sin(th), Mathf.Cos(th)) * HintDistance);
        }

        /// <summary>−1 = left, +1 = right.</summary>
        static float Side(Leg leg) => leg.restAnkle.x < 0f ? -1f : 1f;

        /// <summary>
        /// How far back the leg's thigh is (0 = home or ahead, 1 = at the end of the push or further). Measured
        /// from the lower of home and the reach, so a leg reaching forward reads 0.
        /// </summary>
        float LegBack(Leg leg)
        {
            float top = Mathf.Min(Home().thigh, reach.thigh);
            return Mathf.Clamp01((top - leg.angles.thigh) / Mathf.Max(1f, top - pushEnd.thigh));
        }

        /// <summary>A foot-profile curve at phase s, blended between the acceleration and max-speed profiles.</summary>
        float Profile(System.Func<FootProfile, AnimationCurve> curve, float s)
            => Mathf.Lerp(curve(accelProfile).Evaluate(s), curve(maxSpeedProfile).Evaluate(s), _profileW);

        /// <summary>
        /// One key moves one leg and one arm: the arm follows the opposite leg's thigh exactly (no spring), but only
        /// the way that leg's own key sends it — back of home (its push, its hold, its way home). The leg reaching
        /// forward because the other key was pressed leaves the arm at rest. The forearm goes back with the upper
        /// arm as one chain (the elbow opens), stays so while held, closes coming home. All the way back the arm is
        /// at armBack / elbowBack whatever the speed or key pose: only its rest comes from the pose.
        /// </summary>
        void StepArm(Arm arm, ArmPose pose, float legBack)
        {
            float back = preview ? 0f : legBack;
            arm.swing = Mathf.Lerp(pose.swing, armBack, back);
            arm.elbow = Mathf.Lerp(pose.elbow, elbowBack, back);
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
            bend = Mathf.Clamp(bend, -ankleUpMax, ankleDownMax);
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
            bend = Mathf.Clamp(bend, -toeUpMax, toeDownMax);
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
