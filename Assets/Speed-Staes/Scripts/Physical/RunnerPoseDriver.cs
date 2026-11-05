using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Visual only: turns RunnerSim's state into Control Rig targets. Never changes speed or timing.
    ///
    /// Per frame:
    ///   1. Key poses are blended: Ready → Run by speed, → Stumble by lean beyond the pose's own. They give
    ///      the trunk, head and arms; the legs come from their own angles (2). The run stance stays while
    ///      presses keep coming (stepTimeMax), not only while a key is down; with none it goes back to the crouch.
    ///   2. Legs are angles, not foot paths: every leg has a thigh angle (at the hip, from straight down) and
    ///      a knee angle (180 = straight). Forward kinematics turns the angles into knee and ankle points, and the
    ///      leg IK only reproduces them (target = ankle, hint = in front of the knee). A running leg lives one
    ///      cycle, from its own press to its own next press (that press-to-press time, smoothed: keys in turn give
    ///      2 x the gap between presses, both keys together the gap, one key again the gap), so no case is special:
    ///        stance   (Push)    — the press puts the foot down and it stays where it is on the ground: the root
    ///                             moves on and the foot goes back with it (its real displacement, not the sim's
    ///                             speed), the leg is solved by two-bone IK from the hip to that ankle and its thigh
    ///                             and knee are written back as the leg's angles. The foot leaves at toe-off (the
    ///                             pushEnd angle, or when the leg runs out of length);
    ///        swing    (Recover, Mid) — one curve (Hermite through recover → mid → kneeDrive → contact) from the
    ///                             toe-off angles to the touchdown pose, timed to end when the next press is due;
    ///        touchdown (Land)   — a leg there before its press waits in the touchdown pose just above the ground.
    ///                             A press that comes early takes the leg on from its angles and speed (a short
    ///                             curve) and puts it down: never a snap, never a stop; a leg already in the touchdown pose
    ///                             or on the ground is planted at once (no catch, so no sliding). A foot that runs out of
    ///                             length behind the hip lets go (toe-off) rather than pulling the pelvis down.
    ///                             Swinging legs stay under the body whatever the lean (only a dive, the hold weight, turns
    ///                             them with the hips), so mashing keeps the feet reaching the ground.
    ///      The stride (how far the knee drives, how far the foot lands and pushes) grows with speed and with
    ///      how well the runner has been running (sim.Form). A key held past holdAfter turns its leg into the hold
    ///      (Hold) and the other leg reaches forward (Reach → Front); a held leg rises behind with the waist lean.
    ///      No input: home, the crouch (blended to the running home while presses keep coming).
    ///      Every phase starts from the leg's current angles.
    ///   3. The lean comes from the head alone (UpdateLean), down a chain: head → neck → chest (the waist's
    ///      top) → B-spine (its base) → hips, each following the one above through a spring (the head leads,
    ///      the hips come last). Forward the hips take hipShare of the lean the head asks for and the back
    ///      curves on top of them (waistBaseShare, waistTopShare of the lean), back only up to waistBackMax; the
    ///      legs still turn with all of it. The hips pitch within pelvisPitchRange. Legs in the air and held legs turn with the hips' lean; a leg
    ///      on the ground keeps its angle to the ground (FollowHips). Height still comes from the gait and legs (4), so
    ///      while a foot carries the body the head cannot reach the ground.
    ///   Head and neck: one head angle, back ↔ upright ↔ forward, computed by the sim (RunnerSim.HeadAngle,
    ///      it also takes speed off presses made with the head down) and drawn here. One input per key: down or
    ///      up — a tap and a hold are the same thing, a hold only lasts longer. While a key is down the head
    ///      goes forward (fast at first, headKick in all, then on at holdPush) and does not come back; with no
    ///      key down it comes back through a spring (headReturnSpring), past upright and back again. Nothing
    ///      judges the press: presses coming before the head is back pile it up (mashing), presses on the
    ///      rhythm balance it about upright, sparse ones let it drop back to headBack. The neck bends what the
    ///      hips have not followed yet.
    ///   4. Body height comes from the gait. A planted foot asks for the height (UpdateBodyHeight): lowest in the
    ///      middle of the stance, the knee bending through the IK, never higher than the leg reaches. With no foot
    ///      down between running steps the pelvis rises in an arc to the next touchdown (flightRise, growing with
    ///      speed), highest when the legs are widest. Legs outside the run (the crouch, a reach, the hold's support
    ///      leg) carry the body by their angles: the pelvis sits so that the lowest carrying foot stands on the
    ///      ground. When the last foot leaves with no leg coming down — both keys held together, the dive — nothing
    ///      carries the body: it flies on with the rise its push gave it, under constant gravity (liftKick: the last
    ///      push throws it up a little), going forward at the run's speed, tipping over as the keys are held
    ///      (a real hold tips the whole body, holdLean, both keys on to bothLean, near level the back humps and
    ///      the head dips) and comes down head first: the dive, no special case. Head, pelvis, knees and feet never go
    ///      into the ground: the body stops on the first that touches (the head too, it does not settle further).
    ///      Letting go, the legs come back under and lift it (softly from low down, ~0.5 s). See UpdateBodyHeight /
    ///      KeepAboveGround. A real hold is the hold weight past a tap's (HeldWeight): every hold effect (tip, the
    ///      leg's reach back, the front leg's support, the end of the hop) is scaled by it, so a tap's brief Hold
    ///      phase changes nothing.
    /// Running, a planted foot stays put until the other foot comes down (or is about to), and a lifted foot does not go back in the air.
    /// Running (gait weight: speed, a key down, no hold, on the ground) the arms pump with the opposite leg's
    /// thigh in both directions, wider and with a more closed elbow the faster and the cleaner the running (the
    /// sim's Form); standing or held they keep the pose's rest and the armBack hold (StepArm). Arms, knees'
    /// twist, ankles and toes are posed in LateUpdate after the rig.
    /// Keys pressed again and again on one leg (the sim's HopBias) make a one-leg hop: that leg hops under the body, the
    /// other is held out in front, nearly straight, in the air (balanceLeg, it carries nothing), the trunk sits back
    /// (hopBackLean) and the arms balance. Both keys together (the legs' presses in step, PairWeight) bounce the body
    /// upright like a sack (hopRise). Both are continuous weights of what the keys did, not modes.
    /// The body pitches around X; on top of that the running gait turns the pelvis about the vertical axis with
    /// the thigh difference and the chest the other way, rolls it a little toward the swinging leg's side, and
    /// leans it forward by speed and acceleration (UpdateGaitLean / UpdateTrunk); all of it is 0 at rest and held.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RunnerPoseDriver : MonoBehaviour
    {
        public enum LegPhase
        {
            /// <summary>Home, waiting (the crouch, or the running home while presses keep coming).</summary>
            Rest,
            /// <summary>Pressed: the foot comes down (a caught swing) and is planted, the thigh going back with the body over it.</summary>
            Push,
            /// <summary>Swing, first part (before the folded pass): the heel kicks up behind, then the leg folds forward.</summary>
            Recover,
            /// <summary>Swing, second part: the folded pass, the knee driven up in front, the foot swept down to the touchdown.</summary>
            Mid,
            /// <summary>Held key only: the other leg: thigh stays forward, shin opens, the foot lands ahead.</summary>
            Reach,
            /// <summary>In front on the ground while the other leg is still pushing, recovering or held.</summary>
            Front,
            /// <summary>Still held after the push: back and straight, rising behind with the waist lean.</summary>
            Hold,
            /// <summary>Swing done: waiting in the touchdown pose, just above the ground, for the leg's press.</summary>
            Land,
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

            [HideInInspector] public Vector3 restAnkle, ballOffset, toeInFoot, tipInFoot, restHingeInThigh, restHingeInShin;
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
            [HideInInspector] public float thighRate;   // the thigh's angular speed last frame (deg/s, + = forward)

            // Run-cycle state: runtime only, not serialized.
            [System.NonSerialized] public bool grounded;    // stance: the ankle is nailed to the ground at footZ
            [System.NonSerialized] public bool needLock;    // the stance begins this frame: nail the ankle where it is
            [System.NonSerialized] public bool toeOff;      // the stance is over: swing next (running: the other foot took over, or the leg ran out of length; else pushEnd, stanceMax)
            [System.NonSerialized] public double plantedAt = -1.0;   // when this stance began (not pressedAt: a landed foot's press time is rewritten)
            [System.NonSerialized] public float swingBackLimit = float.MinValue;    // running: the lowest thigh angle this swing's recover key may take, so the foot does not go back in the air
            [System.NonSerialized] public bool carry;       // this frame the leg carries the body
            [System.NonSerialized] public float idle;       // 0..1: the leg the hops leave out (the other key's), held out in front, straight
            [System.NonSerialized] public float footZ;      // the planted ankle's z (runner space; goes back as the root moves on)
            [System.NonSerialized] public float dz0;        // how far behind the hip joint the foot was put down (m)
            [System.NonSerialized] public float plantGap;   // how far above the ground the ankle was when nailed (m); melts to 0 so the leg does not lengthen at once
            [System.NonSerialized] public float stanceU;    // 0..1 through the stance (the foot's way)
            [System.NonSerialized] public float kneeRate;   // the knee's angular speed last frame (deg/s)
            [System.NonSerialized] public Track thighT, kneeT;   // a free leg's shown angles follow its phase's target through these
            [System.NonSerialized] public float lockBallZ;   // where the ball was in front of the ankle when the foot was nailed (m)
            [System.NonSerialized] public bool pinned;      // a foot that stands on the ground without being planted (rest, front, reach, land) is nailed in the world
            [System.NonSerialized] public Vector3 pinPoint; // ...at this world point
            [System.NonSerialized] public float pinOffset;  // after a pin lets go: how far behind the leg's own place the foot still is (m), melting away
            [System.NonSerialized] public float sitShiftZ;  // last frame: how far the sit-back moved this free leg's ankle along z (m)
            [System.NonSerialized] public LegAngles dispAngles;   // a planted leg's angles as shown (from the sat-back hip to the nailed ankle)
            [System.NonSerialized] public float dispThighWorld, dispW;   // ...its world thigh, and how much it differs from the solved one (0 = not sitting back)
            [System.NonSerialized] public float holdFromWorld, holdFromKnee;   // where a hold began: the thigh (world) and the knee
            [System.NonSerialized] public float holdBoth;   // 0..1: the other key is held too (the dive's legs stay in line)
            [System.NonSerialized] public float holdPeak;   // the largest real-hold weight reached in this Hold (a tap stays near 0.05)
            [System.NonSerialized] public float cycle;      // this leg's own press-to-press time (s), smoothed; 0 = never pressed
            [System.NonSerialized] public float swingLen;   // the swing's time (s)
            [System.NonSerialized] public double pressedAt = -10.0, lastPress = -1.0, swingT0;
            [System.NonSerialized] public LegAngles[] keys, slope;   // swing keys (start, recover, mid, drive, contact) and their slopes
            [System.NonSerialized] public float[] knots;             // the keys' times in the swing (0..1)

            public Vector3 RestBall => restAnkle + ballOffset;
            public double EndTime => phaseTime + duration;
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

        /// <summary>
        /// A limb angle that is only ever steered: a damped spring toward the target with a limit on its speed and on its
        /// acceleration when speeding up (a muscle); braking is the spring's but limited too, far above speeding up (BrakeShare),
        /// so the limb stops near its target (a small overshoot is expected) and does not turn round in one frame. Input and phase
        /// changes move the TARGET; the shown angle and its speed go on from where they were.
        /// </summary>
        public struct Track
        {
            public float x, v;
            public bool set;

            public void Seed(float angle, float speed)
            {
                x = angle;
                v = speed;
                set = true;
            }

            const float BrakeShare = 4f;   // braking may be this many times the speeding-up limit

            public float Step(float target, float dt, SpringSettings spring, float maxSpeed, float maxAccel)
            {
                if (dt <= 0f) return x;
                float w = 2f * Mathf.PI * spring.frequency;
                int steps = Mathf.Clamp(Mathf.CeilToInt(w * dt / 0.5f), 1, 16);
                float h = dt / steps;
                for (int i = 0; i < steps; i++)
                {
                    float a = w * w * (target - x) - 2f * spring.damping * w * v;
                    // The limit is a muscle's on speeding up; braking may be BrakeShare times stronger (free braking turned a limb round
                    // 28 deg in one frame when its target flipped, while no brake sails it far past the target).
                    float limit = a * v >= 0f ? maxAccel : maxAccel * BrakeShare;
                    a = Mathf.Clamp(a, -limit, limit);
                    v = Mathf.Clamp(v + a * h, -maxSpeed, maxSpeed);
                    x += v * h;
                }
                return x;
            }
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
            [System.NonSerialized] public Track swingT, elbowT;   // what is shown follows the pose's target through these
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
        [Tooltip("Home while presses keep coming at speed (running height); blended from the crouch by restToBaseSpeed. " +
                 "Also what the stride keys shrink toward when the stride is short.")]
        public LegAngles runHome = new LegAngles(10f, 160f);
        [Tooltip("Toe-off, the end of the stance: how far back the thigh goes with the foot on the ground before the foot " +
                 "lets go (the leg's length lets go earlier if it comes first). Also the first key of a held leg.")]
        public LegAngles pushEnd = new LegAngles(-40f, 155f);
        [Tooltip("Swing, first key: the heel kicks up behind, the thigh stays back and the shin folds.")]
        public LegAngles recover = new LegAngles(-55f, 85f);
        [Tooltip("Swing, second key: the folded pass under the body, thigh about upright (knee ~60 or less: no straight " +
                 "leg swinging through like a walk).")]
        public LegAngles mid = new LegAngles(15f, 38f);
        [Tooltip("Swing, third key: the knee driven up in front, the shin hanging down from it.")]
        public LegAngles kneeDrive = new LegAngles(68f, 90f);
        [Tooltip("Swing end, touchdown: the foot swept down and back under the knee, the leg nearly (not fully) straight, " +
                 "the foot coming down under the body, not stretched out ahead of it.")]
        public LegAngles contact = new LegAngles(22f, 165f);
        [Tooltip("Plain running only: the touchdown pose with the knee a little bent, the foot about as far ahead as contact " +
                 "(a classic run cycle's contact frame). Blended in by the run weight; a hop or a pair jump keeps contact. " +
                 "The swing's last key, the catch and the hip's height all read it, so the leg does not straighten on landing.")]
        public LegAngles contactRun = new LegAngles(30f, 145f);
        [Tooltip("Hold only: the other leg's reach when a key is held. It opens forward nearly straight and the foot " +
                 "lands well ahead (the reference video: ~47° from straight down).")]
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
        [Tooltip("One key really held (the other free): the held leg goes back on its own to this thigh angle (deg from " +
                 "straight down, − = back), nearly straight, then goes on with the tipping body's line. Both keys held: holdBack.")]
        public float holdThigh = -65f;
        [Tooltip("One key really held: the held leg's knee opens to this (deg, 180 = straight) while it goes back.")]
        public float holdStraightKnee = 165f;
        [Tooltip("The held leg takes this long (s) to reach holdThigh and holdStraightKnee from where it was.")]
        public float holdReachTime = 0.18f;
        [Tooltip("The leg in front of a real hold starts here (thigh, knee) while the body is still upright and opens to " +
                 "holdSupport as the body tips: it does not stand with the shin upright under a body that is not over it yet.")]
        public LegAngles holdSupportStart = new LegAngles(55f, 110f);

        [Header("Run cycle: stance (foot planted) → swing (one curve) → touchdown")]
        [Tooltip("Where the swing's keys fall in its time (0..1): the heel kick (recover), the folded pass (mid), the " +
                 "knee drive (kneeDrive). The swing ends at the touchdown pose at 1, when the leg's next press is due.")]
        public Vector3 swingKeys = new Vector3(0.22f, 0.5f, 0.78f);
        [Tooltip("Plain running's own swing keys (same meaning as swingKeys; blended in by the plain-run weight). The thigh's peak " +
                 "comes earlier, so a nearly straight leg has time to come down before the touchdown.")]
        public Vector3 runSwingKeys = new Vector3(0.22f, 0.45f, 0.68f);
        [Tooltip("From the folded pass to the thigh's peak the knee opens later than the thigh comes forward: 1 = it keeps pace, " +
                 "higher = the leg drives from the hip first and the knee opens late; it is open as the thigh arrives. The ends do not move.")]
        [Range(1f, 4f)] public float kneeOpenDelay = 2f;
        [Tooltip("The swing never takes less than this (s).")]
        public float swingMin = 0.12f;
        [Tooltip("A press that comes while the leg is still swinging takes it down to the touchdown pose at up to this " +
                 "angular speed (deg/s): quick, but a short curve from its own angles and speed, never a snap.")]
        public float catchSpeed = 600f;
        [Tooltip("A foot never stays down longer than this (s), however slowly the body goes over it. Running it only counts " +
                 "while the other leg is not on its way down: the foot waits for the other one.")]
        public float stanceMax = 0.4f;
        [Tooltip("The stride (how far the knee drives, how far the foot lands and pushes back) grows from strideMin at x m/s " +
                 "to the full keys at y m/s.")]
        public Vector2 strideSpeed = new Vector2(1.5f, 9f);
        [Tooltip("Share of the full stride at a standstill and while running badly in plain running (0 = the legs stay at " +
                 "runHome). High, so slow running still opens the legs; hopping and pair jumps use jumpStrideMin.")]
        [Range(0f, 1f)] public float strideMin = 0.8f;
        [Tooltip("The stride floor while hopping (one key) or pair jumping: they keep the narrow stride they always had.")]
        [Range(0f, 1f)] public float jumpStrideMin = 0.25f;
        [Tooltip("Plain running: from x m/s to y m/s the fast angles (recoverFast, kneeOpenFast) come in, linearly.")]
        public Vector2 fastStrideSpeed = new Vector2(2f, 8f);
        [Tooltip("Plain running at fast-stride speed: the heel kick (replaces recover; the hop keeps recover's own key).")]
        public LegAngles recoverFast = new LegAngles(-60f, 125f);
        [Tooltip("Plain running at low speed: the heel kick, the rear leg open behind (replaces recover; recoverFast is its faster pair).")]
        public LegAngles recoverOpen = new LegAngles(-55f, 125f);
        [Tooltip("Plain running: the folded pass, further back than mid (replaces mid; the hop keeps mid's own key).")]
        public LegAngles midRun = new LegAngles(0f, 55f);
        [Tooltip("Plain running at low speed: the knee drive with the shin opened out forward (replaces kneeDrive).")]
        public LegAngles kneeOpen = new LegAngles(78f, 170f);
        [Tooltip("Plain running at fast-stride speed: the knee drive (kneeOpen's faster pair).")]
        public LegAngles kneeOpenFast = new LegAngles(70f, 170f);
        [Tooltip("How much of the stride (and the flight) comes from how well the runner has been running (the sim's " +
                 "Form: presses on time): 0 = speed alone, 1 = a runner with no form takes no stride.")]
        [Range(0f, 1f)] public float formWeight = 0.8f;
        [Tooltip("A held key turns the leg into its hold angles over this time (s).")]
        public float holdTakeTime = 0.12f;

        [Header("Hops: one key again and again (one leg hops, the other held out), two keys together (a sack hop)")]
        [Tooltip("One-key hop (blended in by the hop weight): toe-off, how far back the pressed leg goes with the foot down.")]
        public LegAngles hopPushEnd = new LegAngles(-45f, 172f);
        [Tooltip("One-key hop swing, first key: the leg flexes under the body, no heel kick.")]
        public LegAngles hopRecover = new LegAngles(-35f, 70f);
        [Tooltip("One-key hop swing, second key: the deepest flex, thigh and knee both small.")]
        public LegAngles hopMid = new LegAngles(15f, 70f);
        [Tooltip("One-key hop swing, third key: the leg opens again going down.")]
        public LegAngles hopKneeDrive = new LegAngles(48f, 95f);
        [Tooltip("One-key hop touchdown: the foot lands under the hips, the leg nearly straight.")]
        public LegAngles hopContact = new LegAngles(10f, 165f);
        [Tooltip("The balance leg's thigh sways this many degrees (forward while the body rises) with the one-key hop, so it does not look frozen.")]
        public float hopSway = 12f;
        [Tooltip("The balance leg comes this much lower (deg of thigh) as the body rises through the hop arc (hopRise): held out nearly level " +
                 "with the hip at its lowest (the foot stays 35+ cm up there), a little lower at the top so the foot does not go past ~55 cm.")]
        public float balanceLift = 22f;
        [Tooltip("The leg the hops leave out (not the one pressed again and again): held out in front, nearly straight, in the " +
                 "air, balancing; it carries nothing. Blended in by the sim's HopBias.")]
        public LegAngles balanceLeg = new LegAngles(80f, 140f);
        [Tooltip("The trunk sits back by this much (deg) on one-leg hops: the head-led lean and the gait lean fade out as the " +
                 "hop bias rises.")]
        public float hopBackLean = 42f;
        [Tooltip("The body's extra rise between two steps (m) at full hop bias, and with two keys pressed together (the legs " +
                 "in step): one leg hopping, or both legs bouncing the body like a sack.")]
        public float hopRise = 0.26f;
        [Tooltip("The hopping leg's push: how far the pelvis rises (m, at full hop weight) through the push, up to the toe-off, so the " +
                 "bounce starts with the push; the flight's arc (hopRise) goes on from there. With it the stance's dip is gone.")]
        public float hopPushRise = 0.07f;
        [Tooltip("The hopping leg's stance: how far the pelvis sinks in the middle of it (m, at full hop weight): the body comes down " +
                 "onto the foot (the leg bends, the push has room to reach back) and springs up out of it, a real jump.")]
        public float hopStanceDip = 0.06f;
        [Tooltip("One-key hop: the swing ends this much earlier than the next press is due (s, at full hop weight), so the foot is down " +
                 "and planted when the press comes and the push has room: the stance is the time between touchdown and toe-off.")]
        public float hopLead = 0.05f;
        [Tooltip("Fastest the hip may rise (m/s) at full one-key hop weight (both rise limits, maxRiseSpeed and hipRiseMax, blend to it): " +
                 "the bounce's arc is steeper than a run's and the run's limits flattened its top.")]
        [Min(0.1f)] public float hopHipRiseMax = 3.2f;
        [Tooltip("Fastest the pelvis may go down (m/s) at full one-key hop or pair weight (maxDropSpeed blends to it): the arc comes down " +
                 "as fast as it went up, so the body is low when the foot arrives and the leg does not run out of length on touching.")]
        [Min(0.1f)] public float hopDropSpeed = 3.5f;

        [Header("Leg timing (s)")]
        [Tooltip("The reach's time (s), and the push's pace. Quicker with speed (animSpeedUp). Whether a key is a hold is holdAfter's.")]
        public float pushTime = 0.2f;
        [Tooltip("A key must stay down this long before its leg holds: the pushing leg goes on to the hold angles and the other " +
                 "leg reaches forward (s). A shorter press is a tap, whatever the speed.")]
        public float holdAfter = 0.22f;
        [Tooltip("At max speed pushTime and the legs' going home take this share of their time (1 = as slow as standing).")]
        [Range(0.2f, 1f)] public float animSpeedUp = 0.5f;
        [Tooltip("A leg in front (or landed with nothing more coming) goes home this slowly; quicker with speed.")]
        public float frontHomeTime = 0.3f;
        [Tooltip("With no key the legs go to the waiting stance in this time (s): the way home after a push, quicker than the front foot's way home (frontHomeTime).")]
        public float stockReturnTime = 0.16f;
        [Tooltip("A leg's cycle (its press to its next press) is never shorter than this (s).")]
        public float stepTimeMin = 0.2f;
        [Tooltip("Presses further apart than this (s) end the run stance and the swing is never longer: sparser presses " +
                 "swing for this long and then wait in the touchdown pose. A leg's cycle is at most twice this.")]
        public float stepTimeMax = 0.6f;
        [Tooltip("How much of each new press-to-press time a leg's cycle takes on (1 = just the last one): one odd press " +
                 "does not jerk the legs.")]
        [Range(0.1f, 1f)] public float stepFollow = 0.5f;
        [Tooltip("While a key is down, home is the crouch below x m/s and the running home above y. With no key down " +
                 "home is the crouch: the runner heads straight for it, no upright stance on the way.")]
        public Vector2 restToBaseSpeed = new Vector2(0.5f, 3f);
        [Tooltip("How fast it turns from the run stance to the Ready crouch and back (1/s).")]
        public float stanceBlend = 8f;

        [Header("Body height (a planted foot asks for it; the other legs carry by their angles: the lowest carrying foot stands on the ground)")]
        [Tooltip("The pelvis follows the height the legs give through this spring (a foot it lags under is kept on the ground).")]
        public SpringSettings heightSpring = new SpringSettings(8f, 0.8f);
        [Tooltip("Coming down between steps (the legs ask for a lower body than it has): the pelvis sinks through this " +
                 "softer spring instead, so the front foot reaches the ground a moment before the body settles onto it. " +
                 "Going up stays on heightSpring (a foot never goes into the ground).")]
        public SpringSettings dropSpring = new SpringSettings(3f, 0.6f);
        [Tooltip("Fastest the pelvis may go down while running (m/s). A press that lands on a foot still in the air would " +
                 "otherwise pull the body to the foot's stance height in one frame. A hop or a pair blends to hopDropSpeed. Not used in the dive, over hurdles or getting up.")]
        [Min(0.1f)] public float maxDropSpeed = 1.5f;
        [Tooltip("Fastest the pelvis may go up while running (m/s): a leg's height ask that jumps (mashing) cannot throw the body " +
                 "up in one frame. Not used in the dive, over hurdles or getting up.")]
        [Min(0.1f)] public float maxRiseSpeed = 2.8f;
        [Tooltip("Fastest the hip's FINAL height may rise while running (m/s), after the sit-back and the floors: the hop's " +
                 "toe-off stacked them to 4-5 cm in a frame (the box-run look). 1.75 is 2.9 cm per frame at 60 fps. Not used in " +
                 "the dive, over hurdles or getting up.")]
        [Min(0.1f)] public float hipRiseMax = 2.5f;
        [Tooltip("Rate (1/s) at which the sitting-back pivot moves from the hip to the heels of the legs that carry (and between " +
                 "them): switching it at once moved the hip several cm in one frame. The dive switches at once.")]
        [Min(1f)] public float sitPivotBlend = 8f;
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
        [Tooltip("A key really held (a tap's weight does not count) tips the whole body forward by this much (deg) through " +
                 "holdTipSpring, on top of the head-led lean: the hold's dive starts at once, not as the head slowly builds.")]
        public float holdLean = 32f;
        [Tooltip("The hold's tip goes to holdLean (into the hold) through this spring (damping 1: no swing).")]
        public SpringSettings holdTipSpring = new SpringSettings(5f, 1f);
        [Tooltip("The hold's tip comes back (out of a hold, the target below the value) through this softer spring: the hips " +
                 "dropped 15 deg in one frame on the release with the spring that goes in.")]
        public SpringSettings holdTipBackSpring = new SpringSettings(3f, 1f);
        [Tooltip("The one-leg hop and two-keys-together weights follow their targets through this spring (damping 1: no overshoot). " +
                 "A spring starts slowly: no first-frame jump of the hips or legs.")]
        public SpringSettings gaitBlendSpring = new SpringSettings(5f, 1f);
        [Tooltip("Both keys really held tip the body on by this much more (deg, squared by the weight: quick presses on " +
                 "both barely count): the dive goes on to level (pelvisPitchRange.y).")]
        public float bothLean = 55f;
        [Tooltip("Near level (the dive) the waist's curve turns over into a hump: the whole waist's bend (deg, − = the " +
                 "shoulders above the low back); B-spine takes half of it.")]
        public float spineLevelCurve = -12f;
        [Tooltip("Near level the head dips this much (deg) forward of the body's line.")]
        public float headDip = 20f;
        [Tooltip("A real hold: the chest takes this share of the lean the head-led chain gives it (the rest is the hold's tip).")]
        [Range(0f, 1f)] public float holdChestShare = 0.78f;
        [Tooltip("A real hold: the waist base takes this share of the lean the head-led chain gives it (the rest is the hold's tip).")]
        [Range(0f, 1f)] public float holdWaistShare = 0.6f;
        [Tooltip("The pelvis follows the height a planted foot asks for through this spring (stiff: the dip in the middle of a stance is short).")]
        public SpringSettings stanceSpring = new SpringSettings(12f, 0.9f);
        [Tooltip("Running: how much lower the pelvis is in the middle of a stance than at its ends (m, at the full stride). " +
                 "The knee bends for it through the IK.")]
        public float stanceDip = 0.04f;
        [Tooltip("Plain running keeps this share of the body's up-and-down (the stance's dip and the flight's rise). " +
                 "Hops and pair jumps keep all of theirs.")]
        [Range(0f, 1f)] public float runBounce = 0.5f;
        [Tooltip("Running: the pelvis' rise between two steps, at the top of the flight (m, at speed and good form). " +
                 "None at a standstill.")]
        public float flightRise = 0.15f;
        [Tooltip("Speed (m/s) from x on the flight starts rising, and at y it is flightRise.")]
        public Vector2 riseSpeed = new Vector2(4f, 9f);
        [Tooltip("The body follows the flight's arc through this spring.")]
        public SpringSettings flightSpring = new SpringSettings(14f, 0.9f);
        [Tooltip("A leg waiting in the touchdown pose for its late press hovers this far above the ground (m): " +
                 "the foot does not slide along it.")]
        public float landHover = 0.04f;

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
        [Tooltip("Toe joint (ball) → the shoe's tip (m): the floor is kept under the tip too, not only the ball and the heel. The toes bend up " +
                 "first, and if that is not enough the foot turns up.")]
        public float toeTipLength = 0.08f;
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

        [Header("Gait: the running body answers the legs (all of it fades out at rest, held and in the air)")]
        [Tooltip("Speed (m/s) where the gait starts (x) and is fully in (y): twist, roll, gait lean and the arm pump grow between.")]
        public Vector2 gaitSpeed = new Vector2(1f, 6f);
        [Tooltip("Pelvis turn about the vertical (deg) at full gait with the legs a full stride apart: the hip of the leg " +
                 "that is forward goes forward, so the step also gets longer.")]
        public float pelvisTwist = 22f;
        [Tooltip("The chest's tip turns the other way by this much (deg) at full gait. B-chest, in the middle of the " +
                 "twist chain, takes about 60% of the way from the pelvis's turn to this, so the chest itself turns less.")]
        public float chestTwist = 26f;
        [Tooltip("Thigh difference (deg) that counts as a full stride for the twist.")]
        public float twistThighRange = 90f;
        [Tooltip("Pelvis roll (deg) at full gait: the hip drops on the side of the leg that is swinging forward.")]
        public float pelvisRoll = 5f;
        [Tooltip("The chest rolls the other way by this much (deg) at full gait.")]
        public float chestRoll = 3f;
        [Tooltip("Lowest share of the gait that bad running (the sim's Form 0) keeps: twist, roll, lean and arm width " +
                 "scale between this and 1 with Form.")]
        [Range(0f, 1f)] public float formFloor = 0.5f;
        [Tooltip("Forward lean of the trunk (deg) at full gait speed, on top of what the head asks for.")]
        public float gaitLean = 13f;
        [Tooltip("More forward lean (deg) per m/s² of acceleration (smoothed, only speeding up).")]
        public float accelLean = 2.5f;
        [Tooltip("Head-led lean (deg) at which the gait lean has faded out: mashing (the head far forward) and the dive " +
                 "are not leaned on top. Leaning back fades it too, so sitting back is unchanged.")]
        public float gaitLeanFade = 30f;
        [Tooltip("Share of the gait lean the head follows (0 = the head keeps its world angle, the neck bends back).")]
        [Range(0f, 1f)] public float gaitHeadShare = 0.5f;
        [Tooltip("The gait lean follows its target through this spring (damping 1: no swing).")]
        public SpringSettings gaitLeanSpring = new SpringSettings(2.5f, 1f);
        [Tooltip("Each press pitches the trunk forward by up to this much (deg, times that press's efficiency), then " +
                 "it settles back through pressKickSpring.")]
        public float pressKick = 2.5f;
        [Tooltip("The press pitch settles through this spring (damping 1: no swing).")]
        public SpringSettings pressKickSpring = new SpringSettings(6f, 1f);

        [Header("Limb muscles: what is shown follows the target (input only moves the target, nothing snaps)")]
        [Tooltip("A free leg's thigh and an arm's swing (the first link of the chain) follow their targets through this spring. " +
                 "Damping a little under 1: the end of a move overshoots a hair and comes back, like a muscle. The speed rises and " +
                 "falls like a bell (the spring), it does not sit at a constant speed.")]
        public SpringSettings limbSpring = new SpringSettings(14f, 0.85f);
        [Tooltip("A free leg's knee and an arm's elbow (the second link) follow through this slower spring: the thigh leads, the " +
                 "knee comes after, the shoulder leads and the elbow trails.")]
        public SpringSettings limbLag = new SpringSettings(10f, 0.8f);
        [Tooltip("Fastest a free leg's thigh or knee turns (deg/s): a limit for the extreme case only, above the usual swing, so " +
                 "the speed never flattens at a constant (the look of linear motion).")]
        public float legMaxSpeed = 1800f;
        [Tooltip("Hardest a free leg's thigh or knee speeds up (deg/s²). Slowing down is not limited: the spring stops the limb at its target.")]
        public float legMaxAccel = 19000f;
        [Tooltip("Fastest an arm's swing or elbow turns (deg/s): above the usual swing (see legMaxSpeed).")]
        public float armMaxSpeed = 1700f;
        [Tooltip("Hardest an arm's swing or elbow speeds up (deg/s²). Slowing down is not limited: the spring stops the limb at its target.")]
        public float armMaxAccel = 16000f;

        [Header("Arm pump (deg, + = forward): the arm follows the opposite leg's thigh both ways")]
        [Tooltip("Upper arm with the opposite leg all the way back, at full speed and Form.")]
        public float armRunBack = -48f;
        [Tooltip("Upper arm with the opposite leg forward, at full speed and Form.")]
        public float armRunFront = 44f;
        [Tooltip("Opposite leg's thigh (deg, from straight down) for the arm all the way back (x) and all the way forward (y).")]
        public Vector2 armThighRange = new Vector2(-45f, 55f);
        [Tooltip("Share of the swing width that slow running keeps (0..1).")]
        [Range(0f, 1f)] public float armSlowAmp = 0.35f;
        [Tooltip("Elbow bend (deg) at full speed and Form: about a right angle.")]
        public float armElbowRun = 95f;
        [Tooltip("Elbow bend (deg) slow or untidy: loose and open.")]
        public float armElbowLoose = 60f;
        [Tooltip("The elbow bends this much more (deg; negative = opens) with the arm all the way forward: opening keeps the hand at the chin, off the face and the cap.")]
        public float armElbowFront = 35f;
        [Tooltip("Plain running: the elbow finishes closing when the arm has come this far (0..1) toward its front. High = the forearm keeps rising as the shoulder comes forward.")]
        [Range(0.1f, 1f)] public float armElbowCloseAt = 0.8f;
        [Tooltip("Elbow bend (deg) at full speed with the arm all the way back: the arm opens at the elbow as it goes back (the front arm keeps armElbowRun).")]
        public float armElbowRunBack = 15f;
        [Tooltip("Share of the full-speed elbow bend that even the slowest run keeps (0..1): the elbow works at a jog, not only at a sprint.")]
        [Range(0f, 1f)] public float armElbowMinShare = 0.85f;

        [Header("One-key hop arms (deg, + = forward): both arms throw the body up with each press")]
        [Tooltip("Upper arm at the top of the throw (right after a press): up and forward.")]
        public float armHopUp = 60f;
        [Tooltip("Upper arm as the body comes down: down and back.")]
        public float armHopDown = -68f;
        [Tooltip("Elbow bend (deg) in the hop: about a right angle, the forearms up in front of the chest.")]
        public float armHopElbow = 90f;
        [Tooltip("The hop's elbow (deg) with the arm all the way back: open, the elbow straightens as the arm goes back.")]
        public float armHopElbowBack = 28f;
        [Tooltip("The throw falls back after the press's snap in at most 1/x s (shorter when the presses come faster: it is " +
                 "down before the next one): the attack is instant, the return soft.")]
        public float armHopReturn = 6f;

        [Header("Two keys together (the sack hop): both arms swing together, the trunk takes part")]
        [Tooltip("Both arms loading, back (deg, − = back): they go back this far before the push and are thrown up and forward (armHopUp) at it.")]
        public float armPairBack = -82f;
        [Tooltip("Both arms thrown up and forward at the push of the two-key hop (deg): the one-key hop's armHopUp is lower.")]
        public float armPairUp = 85f;
        [Tooltip("Elbow (deg) with the arms loaded back (open) and thrown up (bent), the two-key hop.")]
        public Vector2 armPairElbow = new Vector2(25f, 105f);
        [Tooltip("The trunk bends forward this much (deg) loading the push, two keys together.")]
        public float pairLean = 30f;
        [Tooltip("The trunk opens back this much (deg) at the throw, two keys together.")]
        public float pairOpen = 10f;
        [Tooltip("One key: the trunk goes back this much (deg) with each press at any speed and returns (the press clock, through the bounce spring).")]
        public float hopRock = 14f;
        [Tooltip("Running: the trunk goes back this much (deg) with each press at any speed and returns (the press clock, through the bounce spring), by the gait weight.")]
        public float runRock = 16f;
        [Tooltip("Share of the run's trunk rock the pelvis takes (the rest is the waist and chest): the rock runs from the seat to the head. " +
                 "The legs do not turn with it.")]
        [Range(0f, 1f)] public float pelvisRockShare = 0.4f;
        [Tooltip("The bounce lean follows its target through this spring: a press moves the target only.")]
        public SpringSettings bounceLeanSpring = new SpringSettings(5f, 0.5f);
        [Tooltip("The chest and neck follow the pelvis's bounce lean through this spring (a little behind it): the seat goes first, the chest after, like a rider's trunk.")]
        public SpringSettings bounceChestSpring = new SpringSettings(6f, 0.55f);
        [Tooltip("Riding: the trunk leans forward this much (deg) as the body goes down and back as it comes up (full at 1.5 m/s of the body's height speed), one key or running.")]
        public float rideBob = 6f;
        [Tooltip("The share of the run's and hop's trunk rock (and rideBob) left at the fastest rhythm, presses 0.2 s apart or less; " +
                 "it grows to the whole at 0.4 s apart. A trunk cannot follow presses that fast: it only trembled.")]
        [Range(0f, 1f)] public float rockFastShare = 0.3f;

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
        const float FootLag = 14f;             // the swing's ankle and toe curves are followed at this rate (1/s): the foot trails the knee
        const float FootBlend = 18f;           // foot pitch / swing weight follow their phase's value (1/s)
        const float HeelBehindAnkle = 0.05f;   // heel contact point behind the ankle joint (m)
        const float ToesUpFree = 5f;           // foot this far toes up (deg) keeps its toes in line instead of flat
        const float MinPelvis = 0.1f;          // the pelvis never goes lower than this above the ground (m)
        const float JumpAnkleFade = 10f;       // after a jump the ankles go back to the run's angles this fast (1/s)
        const float FollowRate = 6f;           // a leg changes between turning with the hips and keeping to the ground this fast (1/s)
        const float FoldKeep = 0.6f;           // share of the knee's fold that stays in a short stride (the thigh shrinks more)
        const float MinCatch = 0.02f;          // a press catching a swinging leg takes at least / at most this long (s)
        const float MaxCatch = 0.15f;
        const float KneeSpeedFactor = 1.5f;    // the knee may catch up this much faster than the thigh
        const float MaxRootStep = 1.5f;        // a root move longer than this (m) in a frame, or backwards, is a jump (reset, lane), not running
        const float PlantMelt = 30f;           // a foot nailed above the ground comes down at this rate (1/s): the leg does not lengthen in one frame
        const float PlantGapEnd = 0.001f;      // ...and is on the ground below this gap (m)
        const float MaxRootSpeed = 20f;       // ...and so is one faster than this (m/s)
        const float RootSpeedBlend = 12f;      // the root's speed for the swing's end speed is smoothed at this rate (1/s)
        const float MinExtent = 0.5f;          // lowest hip height (m) used to turn the root's speed into the leg's angular speed
        const float HeelEndShare = 0.55f;      // running: the heel is fully up when the foot is this share of the leg's length behind the hip
        const float Overstretch = 0.985f;     // a planted leg this near its full length is at toe-off (the knee never locks)
        const float PinStopSpeed = 0.3f;       // a runner slower than this (m/s) with no key is standing: a foot is pinned as usual
        const float PinExit = 1.8f;            // a pinned foot lets go when it is raised this many TouchHeights (hysteresis)
        const float PinMelt = 20f;             // a foot the pin let go of melts to its leg's place at this rate (1/s)
        const float PinLift = 0.06f;           // ...and stays up to this much (m) off the ground while it does
        const float ReachCarryExit = 0.12f;    // ...and stops carrying only above this (hysteresis)
        const float ReachCarryHeight = 0.08f;  // a reaching foot this near the ground (m) carries the body; higher it does not
        const float ReachKneeSpeed = 1.8f;     // the reaching knee opens this much faster than the thigh comes forward
        const float CatchGrace = 0.14f;        // a finished catch waits this long (s) for the foot to be down before it is planted anyway
        const float TouchHeight = 0.03f;       // a swinging foot this near the ground (m), past the fold, is down: a press plants it at once
        const float LockThigh = 10f;           // ... and so is a leg this near the touchdown pose (deg): no catch
        const float LockKnee = 15f;
        const float HopRepeatWindow = 1f;      // the same key again within this (s) is a hop (the sim's repeat)
        const float HopSwaySpeed = 1.5f;       // body rise speed (m/s) at which the balance leg's sway is full
        const float IdleRate = 12f;             // a leg turns into the balance leg (and back) this fast (1/s)
        const float IdleCarry = 0.5f;          // a resting leg more than this much the balance leg carries no weight
        const double PoseMinGap = 0.13;        // a press this soon after the last one the pose took is the sim's alone (about 7.7 steps a second)
        const float PairWindow = 0.04f;        // two presses this close (s) are together (full), twice as far apart are not at all (the sim's togetherWindow)
        const float PressTwin = 0.06f;         // two presses closer than this (s) are one throw (two keys together)
        const float ArmIdle = 10f;             // the arm clock before any press (s): the throw is over
        const float ArmPeriodMin = 0.1f;       // the press-to-press time the arm's return follows lies within these (s)
        const float ArmPeriodMax = 0.5f;
        const float ArmRise = 0.05f;           // the arms' throw eases up to 1 over this (s), not at once
        const float RockRise = 0.06f;         // the trunk rock eases up to its target over this (s), not at once
        const float RockFastPeriod = 0.2f;     // press-to-press time (s) at and under which the rock is down to rockFastShare ...
        const float RockSlowPeriod = 0.4f;     // ... and at and over which it is whole
        const float RideFilter = 10f;          // rideBob's height speed follows the body's this fast (1/s)
        const float ArmReturnShare = 0.85f;   // the arm is back down this share of the press-to-press time in (it waits for the next press)

        const float GaitBlend = 8f;            // the gait weight follows its target this fast (1/s)
        const float AccelBlend = 6f;           // the acceleration is smoothed this fast (1/s)
        const float AccelMax = 4f;             // acceleration (m/s²) that gives the full accelLean
        const float TwistBlend = 40f;          // twist follows the thigh difference this fast (1/s): the thighs are continuous, it only takes the corners off
        const float RollBlend = 18f;           // roll follows the thighs' speeds this fast (1/s): a derivative, it needs more smoothing
        const float RollRate = 300f;           // thigh speed (deg/s) that counts as swinging for the roll
        const float HoldGaitFrom = 0.35f;      // hold weight where the gait starts to fade ...
        const float HoldGaitTo = 0.95f;        // ... and is gone (a tap's weight stays below the first)
        const float SitFadeRange = 6f;         // leaning back this far (deg) takes the gait lean off
        const float HeldFrom = 0.25f;          // a key's hold weight below this is a tap, from here to HeldTo it turns into a real hold
        const float HeldTo = 0.6f;
        const float HoldRealPeak = 0.5f;       // a Hold whose real weight reached this was really held (a tap's ~0.3 weight gives ~0.05)
        const float HoldTippedLean = 30f;      // the hips leaned this far (deg) when a held leg is let go: it comes in under the body

        float _gaitW, _speedCurve, _accel, _lastSpeed, _gaitHead;   // gait weight 0..1; speed curve 0..1; trunk's share of head lean
        Spring _gait, _kick;                       // gait lean and the press pitch (deg, trunk)
        int _lastPresses, _armPresses;
        int _posePresses, _poseLastLeg = -1;        // the presses the pose took (OnTapped, PoseMinGap), and the leg of the last one
        double _poseLastTime = -10.0;
        float _posePrevGap = 10f;                   // time between the last taken press and the one before (s)
        bool _poseSkippedOther;                     // the other key was pressed (and left to the sim) since the last taken press
        Spring _bounceChest;                       // the chest's lean in the bounce: follows _bounceLean a little behind (bounceChestSpring)
        Spring _bounceLean;                        // the trunk's lean in the bounce (deg), through bounceLeanSpring
        float _rockKick;                           // eases up to 1 at a press, back to 0 over the press interval: the run's trunk rock
        float _rideVel;                            // the body height's speed, filtered (m/s): what rideBob reads
        float _armKick;                            // eases up to 1 after a press (ArmRise), then falls back: the hop's arm throw
        float _armClock = ArmIdle, _armPeriod = ArmPeriodMax;   // seconds since the last press; the last press-to-press time
        float _lastHipY;                           // last frame's final hip height (runner space), for the rise limit
        bool _hipFree;                             // last frame's hip height was free to be limited (a run, not a dive)
        readonly float[] _sitW = new float[2];       // each leg's share in the sit-back pivot (blends over sitPivotBlend)
        int _hopPresses, _hopPrevLeg = -1;          // the press count last seen, and the leg of the press before
        double _hopPrevTime = -10.0;
        bool _hopRepeated;                          // the last press taken was the same leg again: the hop is really on
        float _hopTarget;                         // 1 when that press was the same leg again, 0 when it was the other
        Spring _hopSpring, _pairSpring, _bothTip;   // the hop and pair weights' springs (gaitBlendSpring); the both-keys tip's (holdTipSpring)
        float _hop, _pair, _dive;                 // one-leg hop weight, two-keys-together weight, hold weight (dive) 0..1
        float _twist, _roll, _lastThighL, _lastThighR, _chestYaw, _chestRoll;
        bool _trunkInit;
        bool _wasInJump;                           // last frame in a jump: this frame's landing
        Vector3 _restHip;
        Quaternion _restHipRot, _restSpineRoot, _restSpineTip;
        float _ground, _sole, _profileW, _runHome;
        float _restWaist;                          // B-spine's bend on the hips at rest (deg)
        float _stepping;                           // 1 = running stance (presses keep coming), 0 = none: Ready crouch
        bool _idleStock;                           // this frame: no key and no press lately: the legs go to the waiting stance, no step of their own
        bool _stepNow;                             // this frame: a key is down, a jump, or the last press is within stepTimeMax
        bool _jumping;                             // this frame: in a hurdle jump (wind-up included): the old rules, no planted feet
        double _lastPress = -1.0;                  // sim time of the last press
        double _now;                               // this frame's sim time
        float _amp = 1f;                           // this frame's stride (strideMin..1)
        float _dipAmp = 1f;                        // the stride under the old 0.25 floor: sizes the stance dip, so slow running does not sink deeper
        float _plainRun = 1f;                      // 1 = plain running, 0 = hop or pair jump (they keep today's keys)
        float _fastW;                              // 0..1: speed through fastStrideSpeed
        float _extent = 0.8f;                      // hip joint height above the foot in a run (m)
        Vector3 _lastRootPos;                      // the root last frame
        bool _haveRoot;
        float _rootSpeed;                          // the root's own speed (m/s), smoothed
        float _offY, _flightT;                     // the body's height when the last foot left; seconds since
        bool _ballistic;                           // the body is in free fall (the dive), not in a run's arc
        Spring _bodyY, _lean;                      // the waist top's lean after the head (deg)
        Spring _waistBase, _hipsLean;              // B-spine's and the hips' lean down the chain (deg)
        float _headAngle;                          // this frame's head angle (deg, + = forward)
        float _holdL, _holdR;                      // hold weights this frame
        bool _bound, _heightSet;
        float _lastBodyY;                          // last frame's body height (the head bone is last frame's too)
        bool _carried = true, _headDown;           // last frame: a leg carried the body / the head was on the ground
        Spring _holdTip;                           // the lean a real hold has tipped the body over (deg)
        float _heldNow, _bothHeld;                 // this frame: the strongest real hold of a leg in Hold (0..1), both keys' (0 unless both in Hold)
        float _hipsNow, _holdHead, _legTurn;       // this frame: the hips' lean (deg), the hold's share of the head angle, the legs' turn with the hips
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
            _haveRoot = false;   // the root may have been moved while this was off (replay): no step on the first frame
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
                // The shoe's tip points level ahead in the rest pose (toeInFoot, ankle to ball, points down ~40 deg).
                leg.tipInFoot = Quaternion.Inverse(leg.footBone.rotation) * transform.forward;
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
            leg.duration = 0f;
            leg.held = false;
            leg.angles = leg.from = crouch;
            leg.pitch = leg.swingW = leg.toeRoll = 0f;
            leg.thighRate = leg.kneeRate = 0f;
            leg.thighT.set = leg.kneeT.set = false;
            leg.pinned = false;
            leg.pinOffset = 0f;
            leg.grounded = leg.needLock = leg.toeOff = leg.carry = false;
            leg.plantGap = 0f;
            leg.plantedAt = -1.0;
            leg.swingBackLimit = float.MinValue;
            leg.footZ = leg.dz0 = leg.stanceU = 0f;
            leg.cycle = 0f;
            leg.idle = 0f;
            leg.swingLen = 0f;
            leg.pressedAt = -10.0;
            leg.lastPress = -1.0;
            leg.swingT0 = 0.0;
            EnsureBuffers(leg);
        }

        /// <summary>The swing's keys and slopes live in arrays made once (nothing is allocated per frame).</summary>
        static void EnsureBuffers(Leg leg)
        {
            if (leg.keys != null && leg.keys.Length == SwingKeyCount) return;
            leg.keys = new LegAngles[SwingKeyCount];
            leg.slope = new LegAngles[SwingKeyCount];
            leg.knots = new float[SwingKeyCount];
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

        const int SwingKeyCount = 5;   // start, recover, mid, drive, contact

        /// <summary>
        /// A press puts that leg's foot down (BeginStance); a release of a held leg sends it into its swing.
        /// The other leg is left alone: it is in its own cycle (a key held past holdAfter is what sends it forward).
        /// </summary>
        void OnTapped(int leg, bool pressed, double time)
        {
            Bind();
            TickLegs(time);
            var l = Legs(leg);
            if (!pressed)
            {
                l.held = false;
                TickLegs(time);
                return;
            }
            l.held = true;
            _idleStock = false;   // a key is down: the legs are not on their way to the stock pose, not even for the rest of this frame
            // The pose takes only the presses a person's steps can follow. One that comes within PoseMinGap of the last one taken is
            // the sim's alone (it counts it all the same: speed and timing are the sim's): no new stance, no cycle, no throw of the arms, no
            // hop count. A key pressed together with the first of a pair (within the pair window PairWeight counts, after a pause) is the pair's second key, taken.
            double sinceTaken = time - _poseLastTime;
            bool pairSecond = _poseLastLeg >= 0 && leg != _poseLastLeg && sinceTaken <= 2f * PairWindow && _posePrevGap >= PoseMinGap * 1.5;
            if (!_jumping && _poseLastTime > -5.0 && sinceTaken < PoseMinGap && !pairSecond)
            {
                if (leg != _poseLastLeg) _poseSkippedOther = true;
                l.pressedAt = time;   // the key is down: a hold counts from here, not from an old press
                return;
            }
            // Keys in turn pressed faster than that: the step that is taken is the other leg's of the one before (L R L R every 0.08 s is
            // still a run, not one leg hopping); one key pressed again and again stays one leg.
            int use = leg;
            if (_poseSkippedOther && leg == _poseLastLeg) use = 1 - leg;
            _poseSkippedOther = false;
            _posePrevGap = (float)System.Math.Min(10.0, sinceTaken);
            _poseLastTime = time;
            _poseLastLeg = use;
            _posePresses++;
            if (use != leg) l.pressedAt = time;
            var used = Legs(use);
            NoteCycle(used, Legs(1 - use), time);
            if (pairSecond)
            {
                Leg first = Legs(1 - use);
                // The same swing time for both, so a pair lands together.
                if (used.cycle > 0f && first.cycle > 0f) used.cycle = first.cycle = Mathf.Min(used.cycle, first.cycle);
                first.pressedAt = time;
            }
            _lastPress = time;
            if (!_jumping) BeginStance(used, time);
        }

        /// <summary>
        /// This leg's own press-to-press time: the swing is timed to end when the next press is due. A pause counts
        /// as the longest cycle; a leg's first press takes the other leg's cycle (or twice the sim's step time).
        /// </summary>
        void NoteCycle(Leg leg, Leg other, double time)
        {
            float max = 2f * Mathf.Max(stepTimeMin, stepTimeMax);
            if (leg.lastPress >= 0.0)
            {
                float interval = Mathf.Clamp((float)(time - leg.lastPress), stepTimeMin, max);
                leg.cycle = leg.cycle > 0f ? Mathf.Lerp(leg.cycle, interval, stepFollow) : interval;
            }
            else if (other.cycle > 0f) leg.cycle = other.cycle;
            else leg.cycle = Mathf.Clamp(2f * (body && body.Sim != null ? body.Sim.StepTime : 0.3f), stepTimeMin, max);
            leg.lastPress = time;
        }

        /// <summary>
        /// The foot goes down. On the ground or hovering in the touchdown pose it is down where it is; a leg still
        /// swinging is caught on its way (StartCatch); one already down just goes on.
        /// </summary>
        void BeginStance(Leg leg, double time)
        {
            leg.pressedAt = time;
            leg.toeOff = false;
            if (leg.phase == LegPhase.Push) return;
            // A foot still in the air (a swing, a reach, a hover) is caught on its way down, never put on the ground in one frame.
            bool inAir = leg.phase == LegPhase.Recover || leg.phase == LegPhase.Mid ||
                         (!leg.grounded && leg.ankle.y - AnkleGround(leg) > TouchHeight * 1.5f);
            if (inAir)
            {
                StartCatch(leg, time);
                return;
            }
            Enter(leg, LegPhase.Push, time, 0f);
            leg.grounded = leg.needLock = true;
        }

        /// <summary>
        /// An early press: the swinging leg goes on from its angles and speed to the touchdown pose in a short curve
        /// (as fast as catchSpeed allows), ending at the speed the stance starts at; then the foot is planted.
        /// </summary>
        void StartCatch(Leg leg, double time)
        {
            EnsureBuffers(leg);
            LegAngles c = Key(ContactPose(), hopContact);
            // Already there (the touchdown pose, or the foot on the ground past the fold): nothing to catch, plant it now.
            // A catch of even a few frames would slide the foot along the ground at the body's speed. A foot still in the
            // air is not down: it is caught and planted when it touches, or the body would drop to it in one frame.
            bool near = Mathf.Abs(c.thigh - leg.angles.thigh) < LockThigh && Mathf.Abs(c.knee - leg.angles.knee) < LockKnee;
            if ((near && leg.ankle.y - AnkleGround(leg) <= TouchHeight) || Touching(leg))
            {
                Enter(leg, LegPhase.Push, time, 0f);
                leg.grounded = leg.needLock = true;
                return;
            }
            float dur = Mathf.Clamp(Mathf.Max(Mathf.Abs(c.thigh - leg.angles.thigh) / Mathf.Max(1f, catchSpeed),
                Mathf.Abs(c.knee - leg.angles.knee) / Mathf.Max(1f, catchSpeed * KneeSpeedFactor)), MinCatch, MaxCatch);
            Enter(leg, LegPhase.Push, time, dur);
            float spanT = c.thigh - leg.angles.thigh, spanK = c.knee - leg.angles.knee;
            leg.keys[4] = c;
            leg.slope[0] = new LegAngles(LimitSlope(leg.thighRate * dur, spanT), LimitSlope(leg.kneeRate * dur, spanK));
            leg.slope[4] = new LegAngles(LimitSlope(StanceRate() * dur, spanT), 0f);
        }

        /// <summary>A swinging foot that is on the ground past the folded pass (not the heel still lifting off behind): it has landed.</summary>
        bool Touching(Leg leg) =>
            leg.angles.thigh >= Key(mid, hopMid).thigh && leg.ankle.y - AnkleGround(leg) <= TouchHeight;

        /// <summary>The other leg is in the air on its way down: its swing, its touchdown pose, or a press catching it.</summary>
        static bool ComingDown(Leg leg) =>
            leg.phase == LegPhase.Recover || leg.phase == LegPhase.Mid || leg.phase == LegPhase.Land ||
            (leg.phase == LegPhase.Push && !leg.grounded);

        /// <summary>
        /// The other foot has come down after this one, or is about to (past the folded pass, within a touch and a half of the
        /// ground): this foot's turn to lift. Read in TickLegs only, where the ankle is the last frame's final height.
        /// </summary>
        bool OtherTakesOver(Leg leg, Leg other) =>
            (other.grounded && other.plantedAt > leg.plantedAt) ||
            (ComingDown(other) && other.angles.thigh >= Key(mid, hopMid).thigh &&
             other.ankle.y - AnkleGround(other) <= TouchHeight * 1.5f);

        /// <summary>
        /// Toe-off (or the release of a hold): the leg's swing to the touchdown pose, one Hermite curve from where it
        /// is and how fast it is going through recover, mid and kneeDrive, ending when its next press is due.
        /// </summary>
        void StartSwing(Leg leg, double time)
        {
            EnsureBuffers(leg);
            if (leg.cycle <= 0f) leg.cycle = Mathf.Clamp(2f * (body && body.Sim != null ? body.Sim.StepTime : 0.3f), stepTimeMin, 2f * stepTimeMax);
            float elapsed = Mathf.Min((float)(time - leg.pressedAt), leg.cycle * 0.5f);
            float lead = hopLead * _hop * (1f - _pair);   // one-key hop: the foot lands this much before the press is due
            float len = Mathf.Clamp(leg.cycle - elapsed - lead, swingMin, Mathf.Max(swingMin, stepTimeMax));
            Enter(leg, LegPhase.Recover, time, len);
            leg.swingT0 = time;
            leg.swingLen = len;

            LegAngles[] k = leg.keys, m = leg.slope;
            float[] n = leg.knots;
            k[0] = leg.angles;
            n[0] = 0f;
            Vector3 sk = Vector3.Lerp(swingKeys, runSwingKeys, _plainRun);   // plain running peaks earlier; hop and pair keep swingKeys
            n[1] = Mathf.Clamp(sk.x, 0.02f, 0.9f);
            n[2] = Mathf.Clamp(sk.y, n[1] + 0.02f, 0.95f);
            n[3] = Mathf.Clamp(sk.z, n[2] + 0.02f, 0.98f);
            n[4] = 1f;
            // Lifted, the foot does not travel back through the world: the thigh goes on back only as far as the body's own move carries it.
            leg.swingBackLimit = k[0].thigh - Mathf.Rad2Deg * _rootSpeed * n[1] * len / Mathf.Max(MinExtent, _extent);
            k[1] = RecoverKeyFor(leg);
            k[2] = MidKey();
            k[3] = KneeDriveKey();
            k[4] = Key(ContactPose(), hopContact);
            for (int i = 1; i < SwingKeyCount - 1; i++)
            {
                float span = n[i + 1] - n[i - 1];
                m[i] = new LegAngles(KeySlope(k[i - 1].thigh, k[i].thigh, k[i + 1].thigh, span),
                    KeySlope(k[i - 1].knee, k[i].knee, k[i + 1].knee, span));
            }
            // Start at the speed it has (never turned back on the first stretch), end at the speed the stance begins at.
            float d0 = n[1], d1 = 1f - n[3];
            m[0] = new LegAngles(LimitSlope(leg.thighRate * len * d0, k[1].thigh - k[0].thigh) / d0,
                LimitSlope(leg.kneeRate * len * d0, k[1].knee - k[0].knee) / d0);
            m[SwingKeyCount - 1] = new LegAngles(
                LimitSlope(StanceRate() * len * d1, k[4].thigh - k[3].thigh) / d1, 0f);
        }

        /// <summary>
        /// The swing's keys follow the hop weight as it is NOW: a swing that began as a run's and meets a hop (or the reverse) goes on to the
        /// new keys from where it is (the shown angles follow their target through the limb springs), not on the old path to the end.
        /// </summary>
        void RefreshSwingKeys(Leg leg)
        {
            if (leg.keys == null) return;
            LegAngles[] k = leg.keys, m = leg.slope;
            float[] n = leg.knots;
            k[1] = RecoverKeyFor(leg);
            k[2] = MidKey();
            k[3] = KneeDriveKey();
            k[4] = Key(ContactPose(), hopContact);
            for (int i = 1; i < SwingKeyCount - 1; i++)
            {
                float span = n[i + 1] - n[i - 1];
                m[i] = new LegAngles(KeySlope(k[i - 1].thigh, k[i].thigh, k[i + 1].thigh, span),
                    KeySlope(k[i - 1].knee, k[i].knee, k[i + 1].knee, span));
            }
        }

        /// <summary>The slope (angle per unit of the swing) through a key: Catmull-Rom, flat at a peak or a trough.</summary>
        static float KeySlope(float before, float at, float after, float span) =>
            (at - before) * (after - at) <= 0f ? 0f : (after - before) / Mathf.Max(1e-3f, span);

        /// <summary>
        /// An end speed over a stretch (angle change over it) kept between 0 and 3 times the stretch's own average,
        /// and never against its direction: a curve that arrives at the speed it has, without overshooting.
        /// </summary>
        static float LimitSlope(float slope, float span)
        {
            if (Mathf.Abs(span) < 1f) return 0f;
            return Mathf.Clamp(slope / span, 0f, 3f) * span;
        }

        /// <summary>The thigh's angular speed the stance starts with (deg/s, − = going back): the root's own speed over the hip's height.</summary>
        float StanceRate() => -Mathf.Rad2Deg * _rootSpeed / Mathf.Max(MinExtent, _extent);

        static float Herm(float p0, float p1, float m0, float m1, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p0 + (u3 - 2f * u2 + u) * m0 + (-2f * u3 + 3f * u2) * p1 + (u3 - u2) * m1;
        }

        /// <summary>
        /// The swing at this share (0..1) of its time. The thigh keeps the share; past the folded pass the knee runs on a
        /// delayed one, so the leg drives from the hip first and the knee opens late (both ends of the swing stay put).
        /// </summary>
        LegAngles EvalSwing(Leg leg, float share)
        {
            LegAngles at = SwingSegment(leg, share);
            // The leg drives from the hip first; the knee opens late on that way and is open as the thigh arrives.
            float from = leg.knots[2], to = leg.knots[3];   // the folded pass, the thigh's peak
            if (share > from && share < to)
            {
                float u = (share - from) / Mathf.Max(1e-4f, to - from);
                at.knee = SwingSegment(leg, from + (to - from) * Mathf.Pow(u, kneeOpenDelay)).knee;
            }
            return at;
        }

        /// <summary>The Hermite segment between the two keys around this share.</summary>
        static LegAngles SwingSegment(Leg leg, float share)
        {
            int i = 0;
            while (i < SwingKeyCount - 2 && share > leg.knots[i + 1]) i++;
            float dt = Mathf.Max(1e-4f, leg.knots[i + 1] - leg.knots[i]);
            float u = Mathf.Clamp01((share - leg.knots[i]) / dt);
            LegAngles a = leg.keys[i], b = leg.keys[i + 1], ma = leg.slope[i], mb = leg.slope[i + 1];
            return new LegAngles(Herm(a.thigh, b.thigh, ma.thigh * dt, mb.thigh * dt, u),
                Herm(a.knee, b.knee, ma.knee * dt, mb.knee * dt, u));
        }

        /// <summary>The catch: the curve from the angles the press found to the touchdown pose (s = 0..1 through it).</summary>
        LegAngles Catch(Leg leg, float s)
        {
            LegAngles c = leg.keys[4], m0 = leg.slope[0], m1 = leg.slope[4];
            // A knee that is opening comes late here too (same rule as the swing); a closing one is left alone.
            float sk = c.knee > leg.from.knee ? Mathf.Pow(s, kneeOpenDelay) : s;
            return new LegAngles(Herm(leg.from.thigh, c.thigh, m0.thigh, m1.thigh, s),
                Herm(leg.from.knee, c.knee, m0.knee, m1.knee, sk));
        }

        float SwingShare(Leg leg, double now) =>
            leg.swingLen > 0f ? Mathf.Clamp01((float)(now - leg.swingT0) / leg.swingLen) : 1f;

        /// <summary>
        /// A stride key at this frame's stride: between the running home (short stride) and the key itself (full).
        /// The knee's fold stays more than the thigh's reach, so a short stride is not a straight-legged one.
        /// </summary>
        LegAngles Stride(LegAngles key) => new LegAngles(Mathf.Lerp(runHome.thigh, key.thigh, _amp),
            Mathf.Lerp(runHome.knee, key.knee, FoldKeep + (1f - FoldKeep) * _amp));

        /// <summary>Stride floor the stance dip is sized from: the old narrow floor, so a wider plain stride adds no squat.</summary>
        const float DipStrideMin = 0.25f;

        /// <summary>Speed and form as 0..1: how far the stride is from its floor to the full keys.</summary>
        float StrideDrive(RunnerSim sim)
        {
            float speed = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(strideSpeed.x, strideSpeed.y, sim.Speed));
            return speed * Mathf.Lerp(1f - formWeight, 1f, Mathf.Clamp01(sim.Form));
        }

        /// <summary>
        /// This frame's stride (floor..1): from speed and from how well the runner has been running. The floor is the wide
        /// strideMin in plain running and the narrow jumpStrideMin while hopping or pair jumping, so those keep their old stride.
        /// </summary>
        float Amp(RunnerSim sim)
        {
            float floor = Mathf.Lerp(strideMin, jumpStrideMin, Mathf.Max(_hop, _pair));
            return Mathf.Lerp(floor, 1f, StrideDrive(sim));
        }

        /// <summary>Heel-kick key: plain running opens it with speed; a hop or pair jump keeps recover.</summary>
        LegAngles RecoverKey() =>
            Key(LegAngles.Lerp(recover, LegAngles.Lerp(recoverOpen, recoverFast, _fastW), _plainRun), hopRecover);

        /// <summary>Folded-pass key: plain running passes further back than mid; a hop or pair jump keeps mid.</summary>
        LegAngles MidKey() => Key(LegAngles.Lerp(mid, midRun, _plainRun), hopMid);

        /// <summary>The recover key of this leg's swing: running, its thigh is held up to the swing's own back limit (a hop or pair jump: untouched).</summary>
        LegAngles RecoverKeyFor(Leg leg)
        {
            LegAngles k = RecoverKey();
            k.thigh = Mathf.Lerp(k.thigh, Mathf.Max(k.thigh, leg.swingBackLimit), _plainRun);
            return k;
        }

        /// <summary>Knee-drive key: plain running opens the shin forward (more at low speed); a hop or pair jump keeps kneeDrive.</summary>
        LegAngles KneeDriveKey() =>
            Key(LegAngles.Lerp(kneeDrive, LegAngles.Lerp(kneeOpen, kneeOpenFast, _fastW), _plainRun), hopKneeDrive);

        /// <summary>
        /// A key held past holdAfter: the leg goes on to the hold angles, and the other leg (unless it is held back, or
        /// planted from a tap of its own) reaches forward to carry the body.
        /// </summary>
        void ToHold(Leg leg, Leg partner, double time)
        {
            Enter(leg, LegPhase.Hold, time, holdTakeTime);
            leg.holdFromWorld = leg.thighWorld;
            leg.holdFromKnee = leg.angles.knee;
            leg.holdBoth = 0f;
            leg.holdPeak = 0f;
            if (partner.HeldBack || partner.grounded || partner.phase == LegPhase.Reach || partner.phase == LegPhase.Front) return;
            Enter(partner, LegPhase.Reach, time, PushTime());
        }

        /// <summary>The reach's time: pushTime, quicker with speed (animSpeedUp). Not the hold's start (holdAfter).</summary>
        float PushTime() => pushTime * SpeedUp();

        /// <summary>Share of their time the legs take at this speed: 1 standing, animSpeedUp at max speed.</summary>
        float SpeedUp() => Mathf.Lerp(1f, animSpeedUp, Speed01());

        void OnRunReset()
        {
            _lean.Reset();
            _waistBase.Reset();
            _hipsLean.Reset();
            _holdTip.Reset();
            _bothTip.Reset();
            _holdHead = 0f;
            ResetGait();
            _carried = true;
            _ballistic = false;
            _wasInJump = false;
            _haveRoot = false;
            _rootSpeed = 0f;
            _flightT = 0f;
            leftLeg.sideOut = rightLeg.sideOut = leftLeg.jumpW = rightLeg.jumpW = 0f;
            ResetLeg(leftLeg);
            ResetLeg(rightLeg);
            _heightSet = false;
            _lastPress = -1.0;
        }

        Leg Legs(int i) => i == 0 ? leftLeg : rightLeg;

        float Speed01() => body && body.Sim != null ? Mathf.Clamp01(body.Sim.Speed / body.Sim.T.maxSpeed) : 0f;

        /// <summary>Start a phase from the leg's current angles (a planted foot is let go; the caller plants it again).</summary>
        static void Enter(Leg leg, LegPhase phase, double time, float duration)
        {
            // Any way off the ground (toe-off, a hold, a reach) starts from the angles the planted leg was SHOWN with: the solved
            // ones are from the unturned hip, and a leg started from them moved its foot 20-40 cm in one frame (the swap).
            if (leg.grounded && leg.dispW > 0f)
            {
                leg.angles = LegAngles.Lerp(leg.angles, leg.dispAngles, leg.dispW);
                leg.thighWorld = Mathf.Lerp(leg.thighWorld, leg.dispThighWorld, leg.dispW);
                leg.dispW = 0f;
            }
            leg.phase = phase;
            leg.phaseTime = time;
            leg.duration = duration;
            leg.from = leg.angles;
            leg.fromPitch = leg.pitch;
            leg.grounded = leg.needLock = leg.toeOff = false;
            leg.plantGap = 0f;
        }

        /// <summary>
        /// Phases that have run their time hand over. A planted foot that has run out (toe-off) swings; a swing that has run
        /// its time lands and waits in the touchdown pose; a caught leg plants its foot; a key held past holdAfter turns its
        /// leg (pushing or in its first swing) into a hold, and a held leg let go swings; a reach ends in front on the
        /// ground and stays there while the other leg is still pushing, recovering or held, then goes home; a landed
        /// leg with nothing more coming goes home.
        /// </summary>
        void TickLegs(double time)
        {
            if (_jumping) return;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < 2; i++)
                {
                    Leg leg = Legs(i), partner = Legs(1 - i);
                    if (leg.held && (leg.phase == LegPhase.Push || leg.phase == LegPhase.Recover) &&
                        time - leg.pressedAt >= holdAfter)   // not PushTime(): that is 0.1-0.2 s at speed and made 0.18 s taps holds
                    {
                        ToHold(leg, partner, time);
                        continue;
                    }
                    // With no key and no press lately a swing, a reach or a catch is not finished: the leg goes from where it is to the stock pose
                    // (Rest, through the limb springs), and the next press takes it from there.
                    if (_idleStock && !leg.grounded && (leg.phase == LegPhase.Recover || leg.phase == LegPhase.Mid || leg.phase == LegPhase.Reach ||
                                                        leg.phase == LegPhase.Front || leg.phase == LegPhase.Push))
                    {
                        Enter(leg, LegPhase.Rest, time, stockReturnTime);
                        continue;
                    }
                    switch (leg.phase)
                    {
                        case LegPhase.Hold:
                            // In a dive's free fall the legs stay in line until the head is down; a run's short flight lets go at once.
                            if (!leg.held && !(leg.holdPeak >= HoldRealPeak && _ballistic && !_headDown)) ReleaseHold(leg, time);
                            break;
                        case LegPhase.Push:
                            if (!leg.grounded)
                            {
                                // The catch ends and the foot is planted where it is SEEN touching: the planted leg is solved from the
                                // ankle on the ground, so planting a foot still in the air (the shown leg lags its target) snapped
                                // the knee and the foot down in one frame. It waits a moment for the foot to come down.
                                bool low = leg.ankle.y - AnkleGround(leg) <= TouchHeight * 1.5f;
                                if (Touching(leg) || (time >= leg.EndTime && (low || time >= leg.EndTime + CatchGrace))) leg.grounded = leg.needLock = true;
                            }
                            else
                            {
                                // Running, this foot lifts as the other one comes down or has come down (read here: the ankle is final).
                                if (_plainRun > 0.5f && !_idleStock && OtherTakesOver(leg, partner)) leg.toeOff = true;
                                // Both keys together: the feet leave the ground in the same frame. One still down, or still being caught, holds the other until it is done too.
                                if (leg.toeOff && _pair > 0.5f && !_idleStock && !partner.toeOff && partner.phase == LegPhase.Push)
                                    leg.toeOff = false;
                                if (leg.toeOff)
                                {
                                    if (_idleStock) Enter(leg, LegPhase.Rest, time, stockReturnTime);   // no key: the stance ends at home, not in a swing
                                    else StartSwing(leg, time);
                                }
                            }
                            break;
                        case LegPhase.Recover:
                        case LegPhase.Mid:
                            if (time >= leg.swingT0 + leg.swingLen)
                            {
                                leg.phase = LegPhase.Land;
                                leg.phaseTime = time;
                                leg.duration = 0f;
                            }
                            break;
                        case LegPhase.Reach:
                            if (time >= leg.EndTime) Enter(leg, LegPhase.Front, leg.EndTime, 0f);
                            break;
                        case LegPhase.Front:
                            // The front foot stays planted ahead until the other leg has pushed and recovered.
                            if (partner.phase != LegPhase.Push && partner.phase != LegPhase.Hold && partner.phase != LegPhase.Recover)
                                Enter(leg, LegPhase.Rest, time, frontHomeTime * SpeedUp());
                            break;
                        case LegPhase.Land:
                            if (!_stepNow) Enter(leg, LegPhase.Rest, time, frontHomeTime * SpeedUp());
                            else if (partner.phase != LegPhase.Hold && partner.phase != LegPhase.Reach && leg.ankle.y - AnkleGround(leg) <= TouchHeight * 1.5f)
                            {
                                // A foot that lands before its press (a hop's swing ends hopLead early, or any late press) is planted
                                // where it came down, so the press finds it on the ground and only extends the stance; a late press
                                // waits on it and it never slides. No weight, no mode: only a held partner (dive, hold) keeps the old hover.
                                leg.pressedAt = time;
                                Enter(leg, LegPhase.Push, time, 0f);
                                leg.grounded = leg.needLock = true;
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// The angles a leg is heading for in a blended phase (the run's own phases have their own curves, see
        /// UpdateLeg). Home slides from the crouch to the running home while presses keep coming; the leg in front
        /// turns into the support angles as the other's hold builds up; a held one goes to the hold angles by
        /// its own hold weight (from the moment its key went down), rising behind with the hips' lean, see Update.
        /// </summary>
        LegAngles Target(Leg leg, float ownHold, float otherHold)
        {
            switch (leg.phase)
            {
                case LegPhase.Reach: return reach;
                case LegPhase.Front:
                {
                    // The support opens from holdSupportStart to holdSupport as the body tips; only a real hold starts it
                    // lower, so a tap's brief hold of the other leg leaves the front leg's target as it was.
                    float tilt = Mathf.Clamp01(_hipsNow / Mathf.Max(1f, pelvisPitchRange.y));
                    LegAngles support = LegAngles.Lerp(holdSupport, holdSupportStart, HeldWeight(otherHold) * (1f - tilt));
                    return LegAngles.Lerp(reach, support, otherHold);
                }
                case LegPhase.Push:
                case LegPhase.Hold: return LegAngles.Lerp(pushEnd, holdBack, ownHold);
                default: return LegAngles.Lerp(Home(), Balance(), leg.idle);
            }
        }

        /// <summary>A stride key blended toward its hop shape by the one-key hop weight (0 running, pairs and dives: the key itself).</summary>
        LegAngles Key(LegAngles key, LegAngles hopKey) => Stride(LegAngles.Lerp(key, hopKey, _hop * (1f - _pair)));

        /// <summary>The balance leg: held out straight, its thigh swaying a few degrees with the body's bounce (forward going up).</summary>
        LegAngles Balance() => new LegAngles(balanceLeg.thigh - balanceLift * Mathf.Clamp01(_bodyY.Value / Mathf.Max(0.05f, hopRise)) +
                                             hopSway * Mathf.Clamp(_bodyY.Velocity / HopSwaySpeed, -1f, 1f), balanceLeg.knee);

        LegAngles Home() => LegAngles.Lerp(crouch, runHome, _runHome);

        /// <summary>
        /// This frame's angles and the foot's angles with them. A planted leg's angles come later, with the body's
        /// height (SolveStance); a caught leg follows its curve, a swinging one its swing, a landed one stays in the
        /// touchdown pose, the others blend from where they were into their phase's angles.
        /// </summary>
        void UpdateLeg(Leg leg, double now, float dt, float legTurn, float ownHold, float otherHold)
        {
            float lastThigh = leg.angles.thigh, lastKnee = leg.angles.knee;
            float s = leg.duration > 0f ? Mathf.Clamp01((float)(now - leg.phaseTime) / leg.duration) : 1f;
            float ease = Mathf.SmoothStep(0f, 1f, s);

            // Foot: heel up through the push; the reaching foot lands heel first, toes up, and stays so while it
            // is planted ahead, rolling flat only as it goes home; in the air the ankle and toes follow the swing
            // curves (over the whole swing).
            float pitch = 0f, swingW = 0f, air = -1f;
            leg.toeRoll = 0f;
            switch (leg.phase)
            {
                case LegPhase.Push:
                {
                    if (!leg.grounded && !_jumping) leg.angles = Catch(leg, s);
                    float u = leg.grounded ? leg.stanceU : 0f;
                    // One motion: the push's heel rise hands over to the held foot's angle as the key's hold builds up.
                    pitch = Mathf.Lerp(Profile(pr => pr.stancePitch, u), pitchBack + legTurn, ownHold);
                    leg.toeRoll = Profile(pr => pr.toeRoll, u);
                    break;
                }
                case LegPhase.Hold:
                    leg.angles = LegAngles.Lerp(leg.from, Target(leg, ownHold, otherHold), ease);
                    pitch = Mathf.Lerp(Profile(pr => pr.stancePitch, 1f), pitchBack + legTurn, ownHold);
                    leg.toeRoll = Profile(pr => pr.toeRoll, 1f);
                    break;
                case LegPhase.Recover:
                case LegPhase.Mid:
                {
                    float share = SwingShare(leg, now);
                    // Running, the rear leg stays open behind while the other is out in front: it goes through only as that one comes down.
                    Leg other = leg == leftLeg ? rightLeg : leftLeg;
                    if (_plainRun > 0.5f && !_idleStock && dt > 0f && share >= leg.knots[1] && share < leg.knots[2] &&
                        other.phase == LegPhase.Mid && !other.grounded)
                    {
                        leg.swingT0 += dt;   // its clock waits; a press still catches it from here
                        share = SwingShare(leg, now);
                    }
                    RefreshSwingKeys(leg);
                    leg.angles = EvalSwing(leg, share);
                    leg.phase = share < leg.knots[2] ? LegPhase.Recover : LegPhase.Mid;
                    air = share;
                    break;
                }
                case LegPhase.Land:
                    if (leg.keys != null) leg.angles = LegAngles.Lerp(leg.keys[4], Balance(), leg.idle);
                    air = 1f;
                    break;
                case LegPhase.Reach:
                {
                    // The knee opens faster than the thigh comes forward: the foot reaches the ground before the body needs it.
                    LegAngles want = Target(leg, ownHold, otherHold);
                    float kneeEase = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, s * ReachKneeSpeed));
                    leg.angles = new LegAngles(Mathf.Lerp(leg.from.thigh, want.thigh, ease), Mathf.Lerp(leg.from.knee, want.knee, kneeEase));
                    pitch = Mathf.Lerp(leg.fromPitch, FrontPitch(), ease);
                    break;
                }
                case LegPhase.Front:
                    leg.angles = LegAngles.Lerp(leg.from, Target(leg, ownHold, otherHold), ease);
                    pitch = FrontPitch();
                    break;
                default:   // Rest
                    leg.angles = LegAngles.Lerp(leg.from, Target(leg, ownHold, otherHold), ease);
                    pitch = leg.fromPitch * (1f - ease);
                    break;
            }
            leg.angles.knee = Mathf.Min(leg.angles.knee, maxKnee);
            if (!leg.grounded && !preview && dt > 0f)
            {
                // The phases only make targets: the shown angles go on from where they were and how fast they went, whatever
                // phase, press or release came (no reset on a press, an early or a late one alike).
                if (!leg.thighT.set)
                {
                    leg.thighT.Seed(lastThigh, leg.thighRate);
                    leg.kneeT.Seed(lastKnee, leg.kneeRate);
                }
                leg.angles.thigh = leg.thighT.Step(leg.angles.thigh, dt, limbSpring, legMaxSpeed, legMaxAccel);
                leg.angles.knee = Mathf.Min(leg.kneeT.Step(leg.angles.knee, dt, limbLag, legMaxSpeed, legMaxAccel), maxKnee);
            }
            if (!leg.grounded)
            {
                leg.thighRate = dt > 0f ? (leg.angles.thigh - lastThigh) / dt : 0f;
                leg.kneeRate = dt > 0f ? (leg.angles.knee - lastKnee) / dt : 0f;
            }
            if (air >= 0f)
            {
                swingW = 1f;
                // The foot is the last link of the chain (thigh, knee, foot): its swing curve is followed with a lag, so it trails the knee.
                float kf = 1f - Mathf.Exp(-FootLag * Mathf.Max(0f, dt));
                leg.swingAnkle = Mathf.Lerp(leg.swingAnkle, Profile(pr => pr.swingAnkle, air), kf);
                leg.swingToe = Mathf.Lerp(leg.swingToe, Profile(pr => pr.swingToe, air), kf);
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
            // A running leg (swing, touchdown) stays under the body whatever the lean: the foot reaches the ground even
            // with the head far forward (mashing). Only a dive (the hold weight) turns the legs with the hips.
            bool running = leg.phase == LegPhase.Recover || leg.phase == LegPhase.Mid || leg.phase == LegPhase.Land;
            leg.follow = Mathf.MoveTowards(leg.follow, planted ? 0f : running ? _dive : 1f, FollowRate * dt);
            leg.thighWorld = leg.angles.thigh - legTurn * leg.follow;
            // The limits only stop the turn with the hips: a held leg always gets back to its own hold angle.
            if (leg.phase == LegPhase.Hold)
            {
                leg.thighWorld = Mathf.Max(Mathf.Min(leg.angles.thigh, -Mathf.Min(heldLegMaxAngle, hipPitch + heldLegArch)),
                    leg.thighWorld);
                // A key really held with the other free: the leg goes back on its own (thigh and knee, a short curve from where
                // the hold began) and on with the tipping body once that is further back; both held it stays in the body's line.
                // Scaled by the real hold, so a tap's brief Hold phase is as it was.
                leg.holdBoth = Mathf.MoveTowards(leg.holdBoth, other.held ? 1f : 0f, FollowRate * dt);
                float real = HeldWeight(leg == leftLeg ? _holdL : _holdR);
                leg.holdPeak = Mathf.Max(leg.holdPeak, real);
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((float)(_now - leg.phaseTime) / Mathf.Max(0.01f, holdReachTime)));
                float aimThigh = Mathf.Lerp(holdThigh, holdBack.thigh, leg.holdBoth);
                float aimKnee = Mathf.Lerp(holdStraightKnee, holdBack.knee, leg.holdBoth);
                float ramp = Mathf.Min(leg.thighWorld, Mathf.Lerp(leg.holdFromWorld, aimThigh, p));
                leg.thighWorld = Mathf.Lerp(leg.thighWorld, ramp, real);
                leg.angles.knee = Mathf.Min(Mathf.Lerp(leg.angles.knee, Mathf.Lerp(leg.holdFromKnee, aimKnee, p), real), maxKnee);
            }
        }

        /// <summary>How much of a key's hold weight is a real hold (0 for quick presses, which stay near 0.3).</summary>
        static float HeldWeight(float weight) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HeldFrom, HeldTo, weight));

        /// <summary>
        /// A held leg let go. A tap's (the Hold phase lasted a few frames): the swing, as ever. A real hold: the leg's shown angle
        /// is taken over into its own frame (the clamp that held it stops here, nothing jumps), then upright it swings up behind like
        /// after a push; tipped well forward (or in a dive's fall) folding up behind would throw the foot over the hips, so it comes
        /// straight in under the body.
        /// </summary>
        void ReleaseHold(Leg leg, double time)
        {
            // The reach-back ramp moved the shown thigh and knee off the tracker's, even in a hold let go below the threshold:
            // take the shown angle over (holdPeak 0, a tap that never reached the ramp, stays exactly as it was).
            if (leg.holdPeak > 0f)
            {
                leg.angles.thigh = leg.thighWorld + _legTurn * leg.follow;
                leg.thighT.set = leg.kneeT.set = false;
            }
            if (leg.holdPeak < HoldRealPeak)
            {
                StartSwing(leg, time);
                return;
            }
            leg.thighRate = leg.kneeRate = 0f;
            bool tipped = _hipsNow > HoldTippedLean;
            if (_ballistic || tipped) Enter(leg, LegPhase.Rest, time, frontHomeTime * SpeedUp());
            else StartSwing(leg, time);
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
        /// How far the ankle of a leg that is not planted (a hold, a swing) must stand over its resting height so that the
        /// foot LateUpdate will pose does not go into the ground, heel or ball. The wanted pitch is not the foot's: PoseAnkle
        /// turns it into a bend on the shin, clamped, and blends the swing curve in, so the foot's real pitch is predicted
        /// the same way (shinTurn: how far the shin has been turned since the angles, sitting back). Only ever raises.
        /// </summary>
        float FreeFootRise(Leg leg, float shinTurn)
        {
            float shinDown = -(leg.thighWorld - (180f - leg.angles.knee)) + shinTurn;   // toes down the shin alone gives the foot
            float bend = Mathf.Lerp(Mathf.Clamp(leg.pitch + leg.rock - shinDown, -ankleUpMax, ankleDownMax),
                leg.swingAnkle, leg.swingW * leg.strideW);
            bend = Mathf.Clamp(Mathf.Lerp(bend, leg.jumpAnkle, leg.jumpW), -ankleUpMax, ankleDownMax);
            float pitch = (shinDown + bend) * Mathf.Deg2Rad;
            float down = Mathf.Max(0f, pitch), up = Mathf.Max(0f, -pitch);
            float heel = Mathf.Max(0f, HeelBehindAnkle * Mathf.Sin(up) + leg.ballOffset.y * (1f - Mathf.Cos(up)));
            float ball = Mathf.Max(0f, leg.ballOffset.z * Mathf.Sin(down) + leg.ballOffset.y * (1f - Mathf.Cos(down)));
            return heel + ball;
        }

        /// <summary>The thigh joint (runner space) on the tilted pelvis, before the body's height is added.</summary>
        Vector3 Joint(Leg leg, Vector3 hip, Quaternion pelvisTilt, float width) =>
            hip + pelvisTilt * leg.hipJointFromHip + new Vector3(Side(leg) * width, 0f, 0f);

        /// <summary>
        /// Forward kinematics of one leg (runner space): from the thigh joint (on the tilted pelvis) down the
        /// thigh at its angle, then down the shin at thigh − (180 − knee). Sets the leg's knee and ankle.
        /// </summary>
        void LegFK(Leg leg, Vector3 hip, Quaternion pelvisTilt, float width)
        {
            float th = leg.thighWorld * Mathf.Deg2Rad;
            float sh = (leg.thighWorld - (180f - leg.angles.knee)) * Mathf.Deg2Rad;
            Vector3 joint = Joint(leg, hip, pelvisTilt, width);
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

        // ---- The planted foot: nailed to the ground, the leg solved from the hip to it. ----

        /// <summary>
        /// Vertical reach of a leg at these angles (m): the thigh joint's height over the ankle. Standing straight
        /// is the leg's whole length, the crouch much less.
        /// </summary>
        static float Extent(Leg leg, LegAngles a) =>
            leg.thighLength * Mathf.Cos(a.thigh * Mathf.Deg2Rad) +
            leg.shinLength * Mathf.Cos((a.thigh - (180f - a.knee)) * Mathf.Deg2Rad);

        /// <summary>The joint's height over the ground in a stance, before its dip: the crouch standing, the touchdown pose running.</summary>
        float StanceExtent(Leg leg) => Mathf.Lerp(Extent(leg, crouch), Extent(leg, Stride(ContactPose())), _runHome);

        /// <summary>Plain running's touchdown pose blended in; a hop or a pair keeps contact.</summary>
        LegAngles ContactPose() => LegAngles.Lerp(contact, contactRun, _plainRun);

        /// <summary>The longest the leg can be (m): hip joint to ankle at the largest knee angle.</summary>
        float MaxReach(Leg leg)
        {
            float l1 = leg.thighLength, l2 = leg.shinLength;
            return Mathf.Sqrt(Mathf.Max(0f, l1 * l1 + l2 * l2 - 2f * l1 * l2 * Mathf.Cos(maxKnee * Mathf.Deg2Rad)));
        }

        /// <summary>How far in front of the ankle the ball of the foot is (m, runner space) with the foot pitched this much (deg, + = toes down).</summary>
        static float BallZ(Leg leg, float pitchDeg)
        {
            float a = pitchDeg * Mathf.Deg2Rad;
            return leg.ballOffset.y * Mathf.Sin(a) + leg.ballOffset.z * Mathf.Cos(a);
        }

        /// <summary>Where the planted ankle stands (runner space height): on the ground, raised by the heel coming up onto the ball.</summary>
        float AnkleGround(Leg leg) => _ground + HeelLift(leg) + ToeRise(leg);

        /// <summary>How far the ankle must rise as the heel comes up (foot pitched toes down) so the ball stays on the ground.</summary>
        static float ToeRise(Leg leg)
        {
            float a = Mathf.Max(0f, leg.pitch + leg.rock) * Mathf.Deg2Rad;
            return Mathf.Max(0f, leg.ballOffset.z * Mathf.Sin(a) + leg.ballOffset.y * (1f - Mathf.Cos(a)));
        }

        /// <summary>
        /// The root's move forward this frame (m, runner's own forward): what a planted foot goes back by. A move
        /// backwards or too long for a frame is a jump (run reset, lane, replay) and counts as none.
        /// </summary>
        float RootStep(float dt)
        {
            Vector3 pos = transform.position;
            float d = 0f;
            if (_haveRoot)
            {
                d = Vector3.Dot(pos - _lastRootPos, transform.forward);
                if (d < 0f || d > Mathf.Max(MaxRootStep, MaxRootSpeed * dt)) d = 0f;
            }
            _lastRootPos = pos;
            _haveRoot = true;
            if (dt > 0f) _rootSpeed = Mathf.Lerp(_rootSpeed, d / dt, 1f - Mathf.Exp(-RootSpeedBlend * dt));
            return d;
        }

        /// <summary>
        /// A planted foot: put down where the ankle is (needLock), then it goes back with the root's real move. How far
        /// it is through its stance (stanceU) comes from where it is behind the hip; at the end, or after stanceMax,
        /// it is toe-off. Running, a foot waits for the other one instead (TickLegs lifts it when that one comes down);
        /// SolveStance and UpdateBodyHeight still lift a leg that runs out of length.
        /// </summary>
        void AdvanceStance(Leg leg, float rootStep, double now, Vector3 joint)
        {
            if (leg.needLock)
            {
                // Sitting back, the free leg was shown moved by the sit-back (sitShiftZ): the nail goes where the foot was seen.
                // The ankle read here is LegFK's, before PinFeet (which runs later): a foot that was pinned or melting was shown elsewhere,
                // and nailing the unpinned place threw it 46-81 cm in the frame it was pressed. Take the place it was drawn at.
                float drawnZ = leg.ankle.z + leg.sitShiftZ + leg.pinOffset;   // melting after a pin: still shown moved by pinOffset
                if (leg.pinned) drawnZ = Mathf.Lerp(drawnZ, transform.InverseTransformPoint(leg.pinPoint).z, 1f - _dive);   // as PinFeet drew it
                // A free foot drawn last frame follows the body, and the root has moved on since: LegFK's angles are frozen, so its place
                // is this frame's by the hip. Where it was drawn in the world is rootStep behind (a pinned foot already is the world point).
                if (!leg.pinned && leg.pinOffset == 0f) drawnZ -= rootStep;
                leg.footZ = drawnZ;
                // Nailed above the ground (the touch height, the catch's grace): the ankle comes down by melting, not by one longer leg.
                leg.plantGap = Mathf.Max(0f, leg.ankle.y + _bodyY.Value - AnkleGround(leg));
                leg.dz0 = joint.z - leg.footZ;
                leg.lockBallZ = BallZ(leg, leg.pitch + leg.rock);
                leg.needLock = false;
                leg.plantedAt = now;
            }
            else leg.footZ -= rootStep;   // a planted foot is still in the world: it goes back with the root, all of it
            float behind = joint.z - leg.footZ;
            LegAngles end = Key(pushEnd, hopPushEnd);
            float endBehind = -(leg.thighLength * Mathf.Sin(end.thigh * Mathf.Deg2Rad) +
                                leg.shinLength * Mathf.Sin((end.thigh - (180f - end.knee)) * Mathf.Deg2Rad));
            endBehind = Mathf.Max(endBehind, leg.dz0 + 0.05f);
            // Running, a foot waiting behind stays on its toes longer: the heel is up (and the leg longer by the toe rise) before it runs out.
            float heelEnd = Mathf.Lerp(endBehind, Mathf.Max(leg.dz0 + 0.05f, Mathf.Min(endBehind, HeelEndShare * MaxReach(leg))), _plainRun);
            leg.stanceU = Mathf.Clamp01(Mathf.InverseLerp(leg.dz0, heelEnd, behind));
            Leg other = leg == leftLeg ? rightLeg : leftLeg;
            // Running, a foot stays until the other takes over (TickLegs); only a leg out of length lifts earlier (SolveStance, StanceCap).
            bool waits = _plainRun > 0.5f && !_idleStock && (ComingDown(other) || other.grounded);
            if (!waits && (behind >= endBehind || now - leg.pressedAt > stanceMax)) leg.toeOff = true;
        }

        /// <summary>
        /// The leg from the hip joint to the planted ankle (two-bone solution by the law of cosines): thigh and knee
        /// angles written back as the leg's own, so the arms, the ankle and the toes read them like any other. The
        /// ankle stays on the ground whatever the body's height; a leg too short reaches as far as it can (toe-off).
        /// </summary>
        void SolveStance(Leg leg, Vector3 hip, Quaternion pelvisTilt, float width, float bodyY, float legTurn, float dt)
        {
            Vector3 joint = Joint(leg, hip, pelvisTilt, width);
            // The heel coming up turns the foot about the ball: the ball stays where it was put, the ankle moves for it.
            float ballShift = leg.lockBallZ - BallZ(leg, leg.pitch + leg.rock);
            float dy = AnkleGround(leg) + leg.plantGap - bodyY - joint.y, dz = leg.footZ + ballShift - joint.z;
            leg.plantGap = leg.plantGap > PlantGapEnd ? leg.plantGap * Mathf.Exp(-PlantMelt * Mathf.Max(0f, dt)) : 0f;
            float l1 = leg.thighLength, l2 = leg.shinLength;
            float reach = MaxReach(leg), shortest = Mathf.Abs(l1 - l2) + 0.02f;
            float d = Mathf.Sqrt(dy * dy + dz * dz);
            // Out of length with the foot behind the hip: toe-off. Looked at one frame ahead (the foot goes back with the root): the
            // frame that overstretched the leg turned the thigh 25 deg and the knee 30 deg in one step, so the swing now starts from
            // the last frame that was still a stance and goes on at its speed.
            float stepBack = _rootSpeed * Mathf.Max(0f, dt), dzNext = dz - stepBack;
            float dNext = Mathf.Sqrt(dy * dy + dzNext * dzNext);
            if (dz < 0f && (d >= reach * Overstretch || dNext >= reach * Overstretch)) leg.toeOff = true;
            float c = Mathf.Clamp(d, shortest, reach);
            float knee = Mathf.Acos(Mathf.Clamp((l1 * l1 + l2 * l2 - c * c) / (2f * l1 * l2), -1f, 1f)) * Mathf.Rad2Deg;
            float bend = Mathf.Acos(Mathf.Clamp((l1 * l1 + c * c - l2 * l2) / (2f * l1 * c), -1f, 1f)) * Mathf.Rad2Deg;
            float thigh = Mathf.Atan2(dz, -dy) * Mathf.Rad2Deg + bend;   // the knee is in front of the line hip → ankle
            knee = Mathf.Min(knee, maxKnee);

            float lastThigh = leg.angles.thigh, lastKnee = leg.angles.knee;
            leg.thighWorld = thigh;
            leg.angles = new LegAngles(thigh + legTurn * leg.follow, knee);
            leg.thighRate = dt > 0f ? (leg.angles.thigh - lastThigh) / dt : 0f;
            leg.kneeRate = dt > 0f ? (knee - lastKnee) / dt : 0f;
            leg.thighT.set = leg.kneeT.set = false;   // lifting off, the free leg goes on from these angles and their speed

            float th = thigh * Mathf.Deg2Rad, sh = (thigh - (180f - knee)) * Mathf.Deg2Rad;
            leg.knee = joint + new Vector3(0f, -Mathf.Cos(th), Mathf.Sin(th)) * l1;
            leg.ankle = leg.knee + new Vector3(0f, -Mathf.Cos(sh), Mathf.Sin(sh)) * l2;
            leg.width = width;
            leg.sideTurn = Quaternion.identity;
        }

        /// <summary>Body height a planted foot asks for: the joint over the ankle at the stance's height, lowest in the middle of it.</summary>
        float StanceNeed(Leg leg, Vector3 joint)
        {
            // One key hopping the body does not sink into the stance (the dip fades with the hop weight) and rises through the push
            // instead, so the flight's arc starts from the push and the hop reads as a bounce, not a squat and a stretch.
            float hopW = _hop * (1f - _pair);
            float bounce = Mathf.Lerp(1f, runBounce, _plainRun);
            float extent = StanceExtent(leg) - (stanceDip * _dipAmp * (1f - hopW) * bounce + hopStanceDip * hopW) * Mathf.Sin(Mathf.PI * leg.stanceU) +
                           hopPushRise * hopW * Mathf.SmoothStep(0f, 1f, leg.stanceU);
            return extent + AnkleGround(leg) - joint.y;
        }

        /// <summary>Highest body a planted foot lets (the leg at full length from the hip joint to the ankle).</summary>
        float StanceCap(Leg leg, Vector3 joint)
        {
            float behind = joint.z - leg.footZ, reach = MaxReach(leg) * Overstretch;
            return Mathf.Sqrt(Mathf.Max(0f, reach * reach - behind * behind)) + AnkleGround(leg) - joint.y;
        }

        /// <summary>
        /// Which legs carry the body this frame: a planted foot, or a leg standing at home or in front by its angles
        /// (home, reach, front). A landed leg carries only for a partner held back. In a jump the old rule holds:
        /// every leg but a held one.
        /// </summary>
        void UpdateCarry()
        {
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i), partner = Legs(1 - i);
                if (_jumping) { leg.carry = leg.phase != LegPhase.Hold; continue; }
                // The balance leg of the one-key hop carries nothing in any phase: a leg left on the ground held the body and the hop never started.
                if (leg.idle > IdleCarry && leg.phase != LegPhase.Hold && _dive <= 0.3f) { leg.carry = false; continue; }
                switch (leg.phase)
                {
                    case LegPhase.Push: leg.carry = leg.grounded; break;
                    case LegPhase.Rest: leg.carry = leg.idle < IdleCarry; break;
                    case LegPhase.Front:
                    case LegPhase.Reach:
                        // A reaching foot carries the body once it is on the ground (or near it); in the air with its knee folded it does not: the
                        // body used to sink to it (hip 0.88 -> 0.78 on a late press). The hold (_dive) keeps the old rule.
                        leg.carry = _dive > 0.3f || leg.ankle.y - AnkleGround(leg) <= (leg.carry ? ReachCarryExit : ReachCarryHeight);   // with a margin: no flicker
                        break;
                    case LegPhase.Land: leg.carry = partner.phase == LegPhase.Hold; break;
                    default: leg.carry = false; break;
                }
            }
        }

        /// <summary>The leg that will touch down next while nothing carries the body, and in how long (s); false if none is coming.</summary>
        bool TryIncoming(out Leg incoming, out float remaining)
        {
            incoming = null;
            remaining = float.MaxValue;
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                float r;
                switch (leg.phase)
                {
                    case LegPhase.Recover:
                    case LegPhase.Mid: r = (float)(leg.swingT0 + leg.swingLen - _now); break;
                    case LegPhase.Push:
                    case LegPhase.Reach: r = (float)(leg.EndTime - _now); break;
                    case LegPhase.Land: r = 0f; break;
                    default: continue;
                }
                r = Mathf.Max(0f, r);
                if (r < remaining)
                {
                    remaining = r;
                    incoming = leg;
                }
            }
            return incoming != null;
        }

        void Update()
        {
            Bind();
            if (!body || body.Sim == null) return;
            var sim = body.Sim;
            float dt = Time.deltaTime;
            float rootStep = RootStep(dt);
            double now = sim.Time;
            _now = now;
            _jumping = !preview && sim.InJump;
            _amp = Amp(sim);
            _dipAmp = Mathf.Lerp(DipStrideMin, 1f, StrideDrive(sim));
            _plainRun = 1f - Mathf.Max(_hop, _pair);
            _fastW = Mathf.InverseLerp(fastStrideSpeed.x, fastStrideSpeed.y, sim.Speed);
            _stepNow = false;

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
                // The Ready crouch is where the runner always heads; the run stance lasts while a key is down or
                // presses keep coming (stepTimeMax), so with no input it goes straight back to the crouch (no upright
                // stance on the way) and between presses it stays in the run.
                _stepNow = sim.Legs[0].Pressed || sim.Legs[1].Pressed || sim.InJump ||
                           (_lastPress >= 0.0 && now - _lastPress < stepTimeMax);
                _stepping = Mathf.Lerp(_stepping, _stepNow ? 1f : 0f, 1f - Mathf.Exp(-stanceBlend * dt));
                p = PoseData.Lerp(Get(ready, RunnerPoseDefaults.Ready), Get(run, RunnerPoseDefaults.Run),
                    Mathf.SmoothStep(0f, 1f, sim.Speed / Mathf.Max(0.01f, runPoseSpeed)) * _stepping);
                float excess = sim.Lean - sim.PoseLean;
                float stumbleW = Mathf.SmoothStep(0f, 1f,
                    (excess - (sim.T.stumbleLean - stumbleBlendRange)) / Mathf.Max(0.01f, stumbleBlendRange));
                p = PoseData.Lerp(p, Get(stumble, RunnerPoseDefaults.Stumble), stumbleW);
                lean = 0f;    // the head decides it below
            }
            // No key and no press within stepTimeMax (a slow rhythm of 0.5 s is still running): the legs stop their plan and go to the stock pose.
            _idleStock = !preview && !_stepNow;
            _holdL = preview ? 0f : sim.Legs[0].WeightValue;
            _holdR = preview ? 0f : sim.Legs[1].WeightValue;
            _dive = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HoldGaitFrom, HoldGaitTo, Mathf.Max(_holdL, _holdR)));
            // A leg in Hold for a few frames after a tap is not a hold: only the weight past a tap's counts (HeldWeight).
            _heldNow = Mathf.Max(leftLeg.phase == LegPhase.Hold ? HeldWeight(_holdL) : 0f,
                                 rightLeg.phase == LegPhase.Hold ? HeldWeight(_holdR) : 0f);
            // Both keys: the weaker one, and no more than the real hold (quick presses on both must not tip the pair).
            _bothHeld = leftLeg.phase == LegPhase.Hold && rightLeg.phase == LegPhase.Hold ? Mathf.Min(_holdL, _holdR) * _heldNow : 0f;
            UpdateHop(sim, now, dt);

            // 2. Legs: phases → angles → knee and ankle points (a planted foot's legs are solved after the height).
            TickLegs(now);
            if (!preview) StepJumpLanding(sim, now);
            if (_jumping)
            {
                leftLeg.grounded = rightLeg.grounded = leftLeg.needLock = rightLeg.needLock = false;
            }
            UpdateCarry();
            _runHome = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(restToBaseSpeed.x, restToBaseSpeed.y, sim.Speed)) * _stepping;
            float profileTarget = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(accelProfileSpeed, maxProfileSpeed, sim.Speed));
            _profileW = Mathf.Lerp(_profileW, profileTarget, 1f - Mathf.Exp(-4f * dt));
            _extent = StanceExtent(leftLeg);
            _headAngle = preview ? 0f : sim.HeadAngle * (1f - _hop);   // a one-leg hop does not throw the head forward
            float baseBend = 0f, bend = 0f, legLean = 0f;
            if (!preview)
            {
                lean = UpdateLean(dt, out baseBend, out bend, out legLean);
                _hipsNow = lean;
                UpdateGaitLean(sim, dt, ref lean, ref baseBend, ref bend, ref legLean);
                UpdateBounceLean(dt, ref lean, ref baseBend, ref bend);
                lean -= hopBackLean * _hop;   // hopping on one leg the trunk sits back (SitBack below)
                lean += jumpLean * JumpWeight(sim) - hitSit * sim.HitWeight;
            }
            else _holdHead = _hipsNow = 0f;

            // 3. Forward the hips take their part of the lean (the waist bends the rest on them), within
            //    pelvisPitchRange. Back, the whole body leans about the feet on the ground (below, after the height).
            float backLean = Mathf.Min(0f, lean);
            float hipPitch = Mathf.Clamp(p.pelvisPitch + lean - backLean, pelvisPitchRange.x, pelvisPitchRange.y);
            // The lean part only (the pose's own tilt does not turn the legs), with all of the lean the hips left to the waist.
            float legTurn = hipPitch - p.pelvisPitch + legLean;
            _legTurn = legTurn;
            Quaternion pelvisTilt = Quaternion.Euler(hipPitch, 0f, 0f);

            UpdateLeg(leftLeg, now, dt, legTurn, _holdL, _holdR);
            UpdateLeg(rightLeg, now, dt, legTurn, _holdR, _holdL);
            FollowHips(leftLeg, rightLeg, legTurn, hipPitch + legLean, dt);
            FollowHips(rightLeg, leftLeg, legTurn, hipPitch + legLean, dt);
            UpdateTrunk(sim, dt, hipPitch, ref pelvisTilt);
            if (!preview) PoseJump(sim);
            Vector3 hip = _restHip + new Vector3(0f, 0f, p.pelvis.x);
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                if (!leg.grounded || leg.needLock) LegFK(leg, hip, pelvisTilt, p.width);
            }
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                if (leg.grounded) AdvanceStance(leg, rootStep, now, Joint(leg, hip, pelvisTilt, p.width));
            }

            // 4. Body height: a planted foot asks for it, a run's flight arcs to the next touchdown, the other legs carry
            //    by their angles (the whole body moves, legs included); nothing goes into the ground.
            float bodyY = preview ? 0f : UpdateBodyHeight(sim, dt, hip, pelvisTilt, p.width);
            if (!preview) bodyY = KeepAboveGround(hip, bodyY) + sim.JumpY;
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                if (leg.grounded) SolveStance(leg, hip, pelvisTilt, p.width, bodyY, legTurn, dt);
            }
            hip.y += bodyY;
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                leg.knee.y += bodyY;
                leg.ankle.y += bodyY;
                leg.ankle.y = Mathf.Max(leg.ankle.y, _ground + HeelLift(leg));
                // A held or swinging foot is posed toes down after the IK: its ball stands under the ankle's level, so the
                // ankle floor is the foot's own pitch's, not just the heel's (the ball went 4 cm under in holds and swings).
                if (!preview && !leg.grounded && !leg.carry) leg.ankle.y = Mathf.Max(leg.ankle.y, _ground + FreeFootRise(leg, 0f));
            }
            leftLeg.rock = rightLeg.rock = 0f;
            if (backLean < 0f) SitBack(ref hip, backLean);
            else leftLeg.sitShiftZ = rightLeg.sitShiftZ = 0f;
            // Whatever moved the body last (the height spring, the sit-back's pivot) is limited, then the floors are kept:
            // the last step that moves anything is the one that clamps (docs/TUZAKLAR.md: after the sit-back, the foot went in).
            if (!preview) LimitHipRise(ref hip, dt);
            // Sitting back turns the legs that do not carry about the heels, below the ground: nothing stays under it
            // (the heel, and the ball of a foot pitched toes down or rocked back).
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                if (backLean < 0f) leg.ankle.y = Mathf.Max(leg.ankle.y, AnkleGround(leg));
                if (!preview && !leg.grounded && !leg.carry) leg.ankle.y = Mathf.Max(leg.ankle.y, _ground + FreeFootRise(leg, Mathf.Min(0f, backLean)));
            }
            if (!preview) ShowPlantedAngles(hip, pelvisTilt, p.width, backLean, legTurn);
            PinFeet(hip, backLean < 0f ? Quaternion.Euler(backLean, 0f, 0f) * pelvisTilt : pelvisTilt, p.width, dt);

            // Write the rig. The waist base sits at waistAngle plus its bend on the hips; the chain (TwistChain)
            // fills the chest in between it and the top, whose angle the neck and head work from (0 = the rig's rest).
            Quaternion rootRot = transform.rotation;
            _neckTrimNow = preview ? 0f : neckTrim * Mathf.SmoothStep(0f, 1f, (_lean.Value + _holdTip.Value) / Mathf.Max(1f, neckTrimLean));
            hipTarget.position = transform.TransformPoint(hip);
            hipTarget.rotation = rootRot * Quaternion.Euler(backLean, 0f, 0f) * pelvisTilt * _restHipRot;
            spineRootTarget.rotation = hipTarget.rotation * Quaternion.Euler(waistAngle + baseBend, 0f, 0f);
            // Leaning back the head keeps looking ahead: it takes only headKeep of the lean, the neck bends the rest.
            WriteHead(hipPitch + backLean + waistAngle - _restWaist + waistTopAngle + bend,
                p.pelvisPitch + waistAngle - _restWaist,
                _headAngle + _holdHead - backLean + _gaitHead, backLean, p.headDown, rootRot);
            WriteLeg(leftLeg);
            WriteLeg(rightLeg);

            // Arms: one key, one leg, one arm (the right key's press sends the right leg back and the left arm
            // with it); a leg held back keeps its arm back.
            StepArm(leftArm, p.leftArm, LegBack(rightLeg), rightLeg, leftLeg, dt);
            StepArm(rightArm, p.rightArm, LegBack(leftLeg), leftLeg, rightLeg, dt);
        }

        /// <summary>
        /// The hop weight (from the sim's HopBias: the same leg pressed again and again) and the pair weight (the two legs'
        /// presses in step, so the keys are pressed together), both smoothed; and which leg the hops leave out (the one not
        /// pressed last): it turns into the balance leg by that weight. No mode: alternating keys give 0 for both.
        /// </summary>
        void UpdateHop(RunnerSim sim, double now, float dt)
        {
            // The arm's throw is a function of the time since the press, and its fall takes the shorter of 1/armHopReturn and
            // most of the last press-to-press time: an exponential fall never got there inside a 0.25 s tap (22% of the way left).
            _armClock += dt;
            if (_posePresses != _armPresses)
            {
                _armPresses = _posePresses;
                // The second key of a pair, a few ms after the first, is the same throw: it neither restarts the clock nor sets the period.
                if (_armClock >= PressTwin)
                {
                    _armPeriod = Mathf.Clamp(_armClock, ArmPeriodMin, ArmPeriodMax);
                    _armClock = 0f;
                }
            }
            float armFall = Mathf.Min(1f / Mathf.Max(0.1f, armHopReturn), ArmReturnShare * _armPeriod);
            // It eases up over ArmRise like the rock: a jump to 1 on a press stepped the arms' target (28 deg turned in a frame at a pair press).
            // The fall's steepest slope (1.5 / armFall, 17.6 per s at the fastest period) stays under 1 / ArmRise, so it still tracks.
            float armTarget = 1f - Mathf.SmoothStep(0f, 1f, _armClock / armFall);
            _armKick = Mathf.MoveTowards(_armKick, armTarget, dt / ArmRise);
            // The run's rock comes back over the whole press interval (a slow rhythm: gently there and back), not in 0.17 s.
            // It stays at the top for the first 0.35 of the fall, so the spring reaches the target even at a fast rhythm.
            float rockFall = Mathf.Max(0.1f, ArmReturnShare * _armPeriod);
            float rockU = Mathf.Clamp01((_armClock / rockFall - 0.35f) / 0.65f);
            // It eases up over RockRise: a jump to 1 on a press was a step in the target at every press (trembling at a fast rhythm).
            float rockTarget = 1f - Mathf.SmoothStep(0f, 1f, rockU);
            _rockKick = Mathf.MoveTowards(_rockKick, rockTarget, dt / RockRise);
            bool off = preview || sim.InJump;
            // The hop target comes from the presses themselves (not the sim's slowly built bias, which keeps ~0.5 for a while after a
            // change of key): the same leg again within a second is 1, the other leg is 0, and a second without a press is 0.
            if (_posePresses != _hopPresses)
            {
                _hopPresses = _posePresses;
                int leg = _poseLastLeg;
                double at = _poseLastTime;
                float wasTarget = now - _hopPrevTime < HopRepeatWindow ? _hopTarget : 0f;
                bool within = leg >= 0 && at - _hopPrevTime < HopRepeatWindow;
                // Both keys together are a pair, never a hop: neither the second key's change, nor the first key of a pair repeating the
                // leg the last pair ended on (_pair is still the last frame's here: it is smoothed below).
                bool together = leftLeg.lastPress >= 0.0 && rightLeg.lastPress >= 0.0 &&
                                System.Math.Abs(leftLeg.lastPress - rightLeg.lastPress) < 2f * PairWindow;
                bool same = within && leg == _hopPrevLeg && !together && _pair < 0.5f;
                // A change of key in a hop is a hop on the other leg, not a step of a run: dropping the hop for one press brought both
                // feet down and up together. Two changes in a row (the last press was itself a change) are a run.
                bool swap = within && !same && !together && _hopRepeated && wasTarget > 0.5f;
                _hopTarget = same || swap ? 1f : 0f;
                _hopRepeated = same;
                // The hop opens (or changes leg): the other leg leaves the ground whatever phase it is in, so the switch from a run is the same every time
                // (a leg that stayed down carried the body and the flight spring never ran: only the pressed foot moved). A planted foot
                // starts from the angles it was shown with (Enter); the pin lets go by itself once the leg is the balance leg (PinFeet).
                if (_hopTarget > 0.5f && (wasTarget < 0.5f || swap) && !off && _dive <= 0.3f)
                {
                    Leg other = Legs(1 - leg);
                    bool free = other.phase == LegPhase.Rest && !other.grounded;
                    if (other.phase != LegPhase.Hold && !free) Enter(other, LegPhase.Rest, now, stockReturnTime);
                }
                _hopPrevLeg = leg;
                _hopPrevTime = at;
            }
            float hopTarget = now - _hopPrevTime < HopRepeatWindow ? _hopTarget : 0f;
            hopTarget *= 1f - _heldNow;   // a real hold ends the hop: else the trunk sits back (hopBackLean) against the forward tip
            hopTarget *= 1f - PairWeight(now);   // a pair gives way: the arms already read _pair, the legs and trunk must too
            // Springs, not a lerp: a lerp is fastest in its first frame (0.26 of the way = 11 deg/frame on the hips via hopBackLean).
            _hopSpring.Step(off ? 0f : hopTarget, gaitBlendSpring.frequency, gaitBlendSpring.damping, dt);
            _pairSpring.Step(off ? 0f : PairWeight(now), gaitBlendSpring.frequency, gaitBlendSpring.damping, dt);
            _hop = Mathf.Clamp01(_hopSpring.Value);
            _pair = Mathf.Clamp01(_pairSpring.Value);
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                leg.idle = Mathf.MoveTowards(leg.idle, i != _poseLastLeg ? _hop * (1f - _pair) : 0f, IdleRate * dt);
            }
        }

        /// <summary>
        /// 1 when the two legs' last presses were together (their real time apart is within the pair window), 0 when they were
        /// not: keys in turn are never a pair at any rhythm, one key pressed again after a pair takes it down (its partner's
        /// last press is then far back), and it fades when the presses stop. No modulo of a cycle: only the time between the two presses.
        /// </summary>
        float PairWeight(double now)
        {
            Leg a = leftLeg, b = rightLeg;
            if (a.lastPress < 0.0 || b.lastPress < 0.0) return 0f;
            float old = (float)(now - System.Math.Min(a.lastPress, b.lastPress));
            float recent = 1f - Mathf.SmoothStep(0f, 1f, (old - stepTimeMax) / Mathf.Max(0.01f, stepTimeMax));
            float gap = (float)System.Math.Abs(a.lastPress - b.lastPress);
            float together = 1f - Mathf.SmoothStep(0f, 1f, (gap - PairWindow) / PairWindow);
            return together * recent;
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
                float home = frontHomeTime * SpeedUp();
                lead.angles.thigh = jumpLandThigh;
                lead.angles.knee = jumpLandKnee;
                lead.follow = 0f;
                Enter(lead, LegPhase.Rest, now, home);
                trail.angles.thigh = jumpLeadThigh;
                trail.angles.knee = jumpLeadKnee;
                trail.follow = 0f;
                Enter(trail, LegPhase.Rest, now, home);
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
            // Hopping on one key the head looks ahead (world pitch toward level) whatever the trunk does; alternating keys, pairs and dives have 0 here.
            pitch = Mathf.Lerp(pitch, 0f, _hop * (1f - _pair));
            float neck = Mathf.Clamp(pitch - trunk, -neckMax, neckMax) * NeckShare;
            spineTipTarget.rotation = rootRot * Quaternion.Euler(trunk + neck, _chestYaw, _chestRoll) * _restSpineTip;
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
            // The pivot's weights follow which legs carry through a short blend: switching it at once moved the hip
            // by several cm in one frame (the hip swings about the heels, whose place differs from the hip's own).
            float step = _ballistic ? 1f : sitPivotBlend * Time.deltaTime;
            float sumW = 0f;
            Vector3 sum = Vector3.zero;
            // Hopping on one key the trunk sits back about the hip, not the heel: the heel pivot moved the hip ~20 cm back over the
            // planted foot, so the leg SHOWN was 15-20 deg more forward than the one SolveStance solved (the push never reached
            // back on screen) and the foot jumped at the toe-off. The pivot's heel share fades with the hop weight.
            float heelShare = 1f - _hop * (1f - _pair);
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                _sitW[i] = Mathf.MoveTowards(_sitW[i], leg.carry ? heelShare : 0f, step);
                if (_sitW[i] <= 0f) continue;
                sum += Heel(leg) * _sitW[i];
                sumW += _sitW[i];
            }
            float hipW = Mathf.Max(0f, 1f - sumW);
            Vector3 pivot = (hip * hipW + sum) / (hipW + sumW);
            Quaternion r = Quaternion.Euler(backLean, 0f, 0f);
            // Hopping on one key the foot lands flat: the heel rock fades with the hop weight.
            float rock = backLean * heelRock * (1f - _hop * (1f - _pair));
            Quaternion foot = Quaternion.Euler(rock, 0f, 0f);
            Vector3 oldHip = hip;
            hip = pivot + r * (hip - pivot);
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                if (!leg.carry)
                {
                    // A leg that is not held hangs from the hip's new place without turning with the trunk (the run's legs stay
                    // under it; only a hold and a dive, _dive, turn the legs with it). Turning it about the hip moved its foot by
                    // ~20 cm: at the touchdown the foot was nailed where the unturned leg is, and again at the toe-off.
                    Quaternion turn = Quaternion.Slerp(Quaternion.identity, r, leg.phase == LegPhase.Hold ? 1f : _dive);
                    float oldZ = leg.ankle.z;
                    leg.knee = hip + turn * (leg.knee - oldHip);
                    leg.ankle = hip + turn * (leg.ankle - oldHip);
                    leg.sitShiftZ = leg.ankle.z - oldZ;
                    continue;
                }
                leg.knee = pivot + r * (leg.knee - pivot);
                Vector3 heel = Heel(leg);
                leg.ankle = heel + foot * (leg.ankle - heel);
                leg.rock = rock;
            }
        }

        /// <summary>
        /// The hip's final height may rise only so fast while running (hipRiseMax). The height spring's own limit
        /// (maxRiseSpeed) is on the body before the sit-back; the sit-back's pivot moving from the heel to the hip and the floors'
        /// push stack on top of it, and one-key hops threw the hip up 4-5 cm in a frame. The legs that are not planted
        /// go down with the hip (their hang stays), a planted one keeps its foot and bends. Not in a dive or a jump.
        /// </summary>
        void LimitHipRise(ref Vector3 hip, float dt)
        {
            bool free = !_ballistic && !_jumping;
            float cut = free && _hipFree && dt > 0f ? hip.y - (_lastHipY + Mathf.Lerp(hipRiseMax, hopHipRiseMax, _hop * (1f - _pair)) * dt) : 0f;
            if (cut > 0f)
            {
                hip.y -= cut;
                for (int i = 0; i < 2; i++)
                {
                    Leg leg = Legs(i);
                    leg.knee.y -= leg.grounded ? cut * 0.5f : cut;
                    if (!leg.grounded) leg.ankle.y -= cut;
                }
            }
            _lastHipY = hip.y;
            _hipFree = free;
        }

        /// <summary>
        /// The last layer, every leg in every phase that is not planted: a foot that stands on the ground stays where it is in the
        /// world while the body moves on (the planted stance does it with footZ; resting, front, reaching and landed feet were
        /// carried by their angles and slid). The pin is a world point, so the root's own move is used directly, no sum of steps.
        /// When the leg runs out of length, or the foot is lifted, the pin lets go and the foot melts to where the leg puts it
        /// (pinOffset), raised while it does: no snap, no slide. The dive, a jump and the hold's back leg are left to
        /// their own rules (the pin fades with the hold weight).
        /// </summary>
        void PinFeet(Vector3 hip, Quaternion tilt, float width, float dt)
        {
            bool allowed = !preview && !_jumping && !_ballistic;
            float strength = 1f - _dive;
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                bool was = leg.pinned;
                Vector3 pinLocal = was ? transform.InverseTransformPoint(leg.pinPoint) : Vector3.zero;
                bool phaseOk = leg.phase == LegPhase.Rest || leg.phase == LegPhase.Front || leg.phase == LegPhase.Reach || leg.phase == LegPhase.Land;
                float gap = leg.ankle.y - AnkleGround(leg);
                // With no key a body that still glides does not nail a foot again and again (the pin lets go at the leg's length, the foot
                // came forward and was pinned anew: a step of its own): the foot that let go stays with the body until it stops.
                bool entry = gap <= TouchHeight && Mathf.Abs(leg.pinOffset) <= 0.002f && (!_idleStock || _rootSpeed < PinStopSpeed);
                bool isBalanceLeg = leg.idle > IdleCarry && leg.phase != LegPhase.Hold && _dive <= 0.3f;
                bool pin = allowed && !isBalanceLeg && !leg.grounded && phaseOk && (was ? gap <= TouchHeight * PinExit : entry);
                if (pin && !was)
                {
                    leg.pinPoint = transform.TransformPoint(leg.ankle);
                    pinLocal = leg.ankle;
                }
                if (pin)
                {
                    float targetZ = Mathf.Lerp(leg.ankle.z, pinLocal.z, strength);
                    Vector3 joint = Joint(leg, hip, tilt, width);
                    float dy = leg.ankle.y - joint.y, dz = targetZ - joint.z;
                    if (Mathf.Sqrt(dy * dy + dz * dz) >= MaxReach(leg) * Overstretch) pin = false;   // out of length: the foot lets go
                    else
                    {
                        leg.knee.z += (targetZ - leg.ankle.z) * 0.5f;
                        leg.ankle.z = targetZ;
                    }
                }
                // A planted leg took the pinned place as its nail (footZ), so nothing melts there; a lifted or released one melts from it.
                if (leg.grounded) leg.pinOffset = 0f;
                else if (!pin && was) leg.pinOffset = Mathf.Clamp(pinLocal.z - leg.ankle.z, -0.5f, 0.5f);
                leg.pinned = pin;
                if (!pin && leg.pinOffset != 0f)
                {
                    leg.pinOffset *= Mathf.Exp(-PinMelt * Mathf.Max(0f, dt));
                    if (Mathf.Abs(leg.pinOffset) < 0.002f) leg.pinOffset = 0f;
                    leg.ankle.z += leg.pinOffset;
                    leg.knee.z += leg.pinOffset * 0.5f;
                    leg.ankle.y += Mathf.Min(PinLift, Mathf.Abs(leg.pinOffset) * 0.6f);
                }
            }
        }

        /// <summary>
        /// A planted leg's angles as they are SHOWN: from the sat-back hip's joint to the nailed ankle (SolveStance solves from the
        /// unturned hip). A leg that lifts off starts its swing from these (StartSwing), so the foot leaves where it was and not
        /// 20 cm off. Weighted by how far the body sits back (0 = the solved angles: a run is untouched).
        /// </summary>
        void ShowPlantedAngles(Vector3 hip, Quaternion pelvisTilt, float width, float backLean, float legTurn)
        {
            float w = Mathf.Clamp01(-backLean / 2f);
            Quaternion tilt = Quaternion.Euler(backLean, 0f, 0f) * pelvisTilt;
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                leg.dispW = 0f;
                if (!leg.grounded || w <= 0f) continue;
                Vector3 joint = Joint(leg, hip, tilt, width);
                float dy = leg.ankle.y - joint.y, dz = leg.ankle.z - joint.z;
                float l1 = leg.thighLength, l2 = leg.shinLength;
                float c = Mathf.Clamp(Mathf.Sqrt(dy * dy + dz * dz), Mathf.Abs(l1 - l2) + 0.02f, MaxReach(leg));
                float knee = Mathf.Acos(Mathf.Clamp((l1 * l1 + l2 * l2 - c * c) / (2f * l1 * l2), -1f, 1f)) * Mathf.Rad2Deg;
                float bend = Mathf.Acos(Mathf.Clamp((l1 * l1 + c * c - l2 * l2) / (2f * l1 * c), -1f, 1f)) * Mathf.Rad2Deg;
                leg.dispThighWorld = Mathf.Atan2(dz, -dy) * Mathf.Rad2Deg + bend;
                leg.dispAngles = new LegAngles(leg.dispThighWorld + legTurn * leg.follow, Mathf.Min(knee, maxKnee));
                leg.dispW = w;
            }
        }

        /// <summary>Where a foot on the ground touches it at the heel (runner space): behind the ankle, on the ground.</summary>
        Vector3 Heel(Leg leg) => new Vector3(leg.ankle.x, _sole, leg.ankle.z - HeelBehindAnkle);

        /// <summary>
        /// Body height (m, runner space, from the rest height). Carrying legs: a planted foot (the run) asks for the
        /// height of its stance and caps it at the leg's full length; the others (home, reach, front, a jump's legs)
        /// ask for the height at which their foot stands on the ground by their angles, the lowest wins. All through
        /// a spring (stanceSpring planted; heightSpring up and dropSpring down otherwise; low down, getting up off the
        /// ground, the softer liftSpring blended in). With no foot down: if a leg is coming down (the run between
        /// steps) the body arcs to where that touchdown asks for, rising flightRise at the top; if none is (the dive:
        /// both keys held) it flies on under the sim's constant gravity with the speed its last push gave it
        /// (liftKick), until something is on the ground (KeepAboveGround).
        /// </summary>
        float UpdateBodyHeight(RunnerSim sim, float dt, Vector3 hip, Quaternion pelvisTilt, float width)
        {
            // Over a hurdle no foot is on the ground to ask for a height: the legs' folding must not sink the body.
            if (sim.JumpInAir)
            {
                _carried = true;
                return _bodyY.Value;
            }
            if (leftLeg.carry || rightLeg.carry)
            {
                // A dive's or a jump's landing may come down as fast as it falls.
                // _ballistic still holds last frame's value here, so the first carried frame after a dive stays exempt.
                bool limitDrop = !_ballistic && !_jumping;
                float prevY = _bodyY.Value;
                float need = float.MinValue, cap = float.MaxValue;
                bool planted = false;
                for (int i = 0; i < 2; i++)
                {
                    Leg leg = Legs(i);
                    if (!leg.carry) continue;
                    if (leg.grounded)
                    {
                        Vector3 joint = Joint(leg, hip, pelvisTilt, width);
                        planted = true;
                        float legNeed = StanceNeed(leg, joint), legCap = StanceCap(leg, joint);
                        need = Mathf.Max(need, legNeed);
                        // A foot behind the hip that can no longer reach the body's height lets go (toe-off) instead of
                        // pulling the pelvis down: the cap falls steeply there, a frame's drop would be several cm.
                        if (joint.z - leg.footZ > 0f && legCap < Mathf.Max(legNeed, _bodyY.Value)) leg.toeOff = true;
                        else cap = Mathf.Min(cap, legCap);
                    }
                    else need = Mathf.Max(need, LegNeed(leg));
                }
                need = Mathf.Min(need, cap);
                if (!_heightSet) _bodyY.Reset(need);
                _heightSet = true;
                float low = Mathf.SmoothStep(0f, 1f, (-_bodyY.Value - liftBelow) / Mathf.Max(0.01f, liftRange));
                // Coming down the body sinks softly onto the front foot; going up it keeps to the legs.
                SpringSettings spring = planted ? stanceSpring : need < _bodyY.Value ? dropSpring : heightSpring;
                _bodyY.Step(need, Mathf.Lerp(spring.frequency, liftSpring.frequency, low),
                    Mathf.Lerp(spring.damping, liftSpring.damping, low), dt);
                if (_bodyY.Value > cap)
                {
                    _bodyY.Value = cap;
                    _bodyY.Velocity = Mathf.Min(0f, _bodyY.Velocity);
                }
                if (limitDrop) LimitHeightSpeed(prevY, dt);
                _carried = true;
                _ballistic = false;
                _flightT = 0f;
                return _bodyY.Value;
            }

            if (_carried)
            {
                _offY = _bodyY.Value;
                _flightT = 0f;
            }
            _carried = false;
            _flightT += dt;
            bool wasBallistic = _ballistic;
            float flightPrevY = _bodyY.Value;
            if (TryIncoming(out Leg incoming, out float remaining))
            {
                // The run between two steps: an arc from where the last foot left to where the next touches down.
                _ballistic = false;
                float total = _flightT + remaining;
                float w = total > 1e-4f ? Mathf.Clamp01(_flightT / total) : 1f;
                Vector3 joint = Joint(incoming, hip, pelvisTilt, width);
                float landY = StanceExtent(incoming) + AnkleGround(incoming) - joint.y;
                float speed = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(riseSpeed.x, riseSpeed.y, sim.Speed));
                float rise = flightRise * speed * Mathf.Lerp(1f - formWeight, 1f, Mathf.Clamp01(sim.Form)) *
                             Mathf.Lerp(1f, runBounce, _plainRun) +
                             hopRise * Mathf.Max(_hop, _pair);
                float target = Mathf.Lerp(_offY, landY, Mathf.SmoothStep(0f, 1f, w)) + rise * 4f * w * (1f - w);
                if (incoming.phase == LegPhase.Land) target += landHover;
                _bodyY.Step(target, flightSpring.frequency, flightSpring.damping, dt);
                if (!wasBallistic && !_jumping) LimitHeightSpeed(flightPrevY, dt);
                return _bodyY.Value;
            }

            // The last foot pushes off with nothing coming down: the body leaves the ground going up (never carried on down by the spring).
            if (!_ballistic) _bodyY.Velocity = Mathf.Max(0f, _bodyY.Velocity) + liftKick * _dive;
            _ballistic = true;
            _bodyY.Velocity -= sim.T.gravity * dt;
            _bodyY.Value += _bodyY.Velocity * dt;
            return _bodyY.Value;
        }

        /// <summary>Keeps this frame's body height step within maxDropSpeed down and maxRiseSpeed up (running only).</summary>
        void LimitHeightSpeed(float prevY, float dt)
        {
            // A hop's or pair's arc falls as fast as it rises (plain running keeps maxDropSpeed: both weights are 0 there).
            float drop = Mathf.Lerp(maxDropSpeed, hopDropSpeed, Mathf.Max(_hop, _pair));
            float y = Mathf.Clamp(_bodyY.Value, prevY - drop * dt, prevY + Mathf.Lerp(maxRiseSpeed, hopHipRiseMax, _hop * (1f - _pair)) * dt);
            if (y == _bodyY.Value) return;
            _bodyY.Value = y;
            _bodyY.Velocity = Mathf.Clamp(_bodyY.Velocity, -drop, Mathf.Lerp(maxRiseSpeed, hopHipRiseMax, _hop * (1f - _pair)));
        }

        /// <summary>
        /// Body height a carrying leg asks for by its angles: its foot on the ground, and its knee not in it
        /// (folding under the body getting up, the knee lifts it through the height springs, not at once).
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
            for (int i = 0; i < 2; i++)
            {
                Leg leg = Legs(i);
                if (leg.carry) continue;
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
        /// (about the feet, see SitBack). A real hold tips it at once (holdLean, both keys on to bothLean; the chain blends toward the
        /// hold's shares and, near level, the hump), by the tip's build-up, so a tap changes nothing. Nothing else moves it. How low it gets is
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

            // A key really held (HeldWeight: a tap's does not count) tips the whole body at once, holdLean, and both keys on
            // toward level. The chain's own lean stays under it, at holdChestShare / holdWaistShare, and near level the curve
            // turns over into a hump with the head dipping. All of it by the tip's own build-up (w): a tap leaves it at 0, and
            // a press or a release only moves targets.
            // Out of a hold (the target below the value) the softer back spring: the hips dropped 15 deg in a frame on the release.
            float holdTarget = holdLean * _heldNow, bothTarget = bothLean * _bothHeld * _bothHeld;
            SpringSettings holdIn = holdTarget < _holdTip.Value ? holdTipBackSpring : holdTipSpring;
            SpringSettings bothIn = bothTarget < _bothTip.Value ? holdTipBackSpring : holdTipSpring;
            _holdTip.Step(holdTarget, holdIn.frequency, holdIn.damping, dt);
            // The both-keys part goes through the same springs: read raw it threw the head 31 deg in a frame into a dive and the hips 14 out of one.
            _bothTip.Step(bothTarget, bothIn.frequency, bothIn.damping, dt);
            float w = Mathf.Clamp01(_holdTip.Value / Mathf.Max(1f, holdLean));
            if (w > 0f)
            {
                float low = hipShare * Mathf.Max(0f, full);
                float whole = _holdTip.Value + _bothTip.Value;
                float holdHips = low + whole;
                float level = Mathf.Max(1f, pelvisPitchRange.y);
                float hump = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f * level, level, holdHips));
                float holdBase = Mathf.Lerp(holdWaistShare * Mathf.Max(0f, _waistBase.Value) - low, spineLevelCurve * 0.5f, hump);
                float holdBend = Mathf.Lerp(holdChestShare * Mathf.Max(0f, top) - low, spineLevelCurve, hump);
                hips = Mathf.Lerp(hips, holdHips, w);
                baseBend = Mathf.Lerp(baseBend, holdBase, w);
                bend = Mathf.Lerp(bend, holdBend, w);
                legLean = Mathf.Lerp(legLean, 0f, w);
                _holdHead = whole + headDip * hump * w;
            }
            else _holdHead = 0f;
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
            leg.hint.position = transform.TransformPoint(leg.knee + leg.sideTurn * new Vector3(0f, Mathf.Sin(th), Mathf.Cos(th)) * HintDistance);
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
        /// Standing, starting off and held: the arm follows the opposite leg's thigh exactly (no spring) back of home
        /// (its push, its hold, its way home) and rests at the pose's angles otherwise; all the way back it is at
        /// armBack / elbowBack whatever the speed or key pose. Running (gait weight) it pumps instead: the opposite
        /// leg's thigh, back or forward, gives the swing, between armRunBack and armRunFront; the width and the
        /// elbow's closing grow with speed and the sim's Form (slow or untidy: small and loose).
        /// </summary>
        void StepArm(Arm arm, ArmPose pose, float legBack, Leg opposite, Leg own, float dt)
        {
            float back = preview ? 0f : legBack;
            float swing = Mathf.Lerp(pose.swing, armBack, back);
            float elbow = Mathf.Lerp(pose.elbow, elbowBack, back);
            if (!preview && _gaitW > 0.001f && body && body.Sim != null)
            {
                float form = Mathf.Lerp(formFloor, 1f, body.Sim.Form);
                float amp = Mathf.Lerp(armSlowAmp, 1f, _speedCurve) * form;
                // The opposite leg held out (one-leg hops) does not swing: the arm balances against its own side's leg instead.
                float drive = Mathf.Lerp(opposite.thighWorld, armThighRange.x + armThighRange.y - own.thighWorld, opposite.idle);
                float t = Mathf.Clamp(Mathf.InverseLerp(armThighRange.x, armThighRange.y, drive), -0.1f, 1.1f);
                float mid = 0.5f * (armRunBack + armRunFront);
                float pump = mid + (Mathf.LerpUnclamped(armRunBack, armRunFront, t) - mid) * amp;
                // The arm going back opens at the elbow too (the back half of the swing), the forward one stays bent with the hand at the chin.
                float open = 1f - Mathf.SmoothStep(0f, armElbowCloseAt, Mathf.Clamp01(t));
                float bend = Mathf.Lerp(armElbowLoose, Mathf.Lerp(armElbowRun + armElbowFront, armElbowRunBack, open),
                    Mathf.Lerp(armElbowMinShare, 1f, _speedCurve * form));
                swing = Mathf.Lerp(swing, pump, _gaitW);
                elbow = Mathf.Lerp(elbow, bend, _gaitW);
            }
            // One-key hop: both arms throw up and forward with the press, down and back as the body comes down.
            float hw = preview ? 0f : _hop * (1f - _pair);
            if (hw > 0.001f)
            {
                // Across, like a stride: the arm opposite the hopping leg (the leg that is not the balance leg) goes back as it pushes,
                // the arm on its side goes forward; they change as the body comes down. By the balance leg's weight, continuous.
                float kick = Mathf.Lerp(_armKick, 1f - _armKick, 1f - opposite.idle);
                swing = Mathf.Lerp(swing, Mathf.Lerp(armHopDown, armHopUp, kick), hw);
                // Thrown up the elbow closes to a right angle at most (the hand stays at the chin, never over the face), coming down it opens.
                // The elbow opens with the arm going back and closes to a right angle as it comes up (like the run's open curve).
                float hopOpen = 1f - Mathf.SmoothStep(0f, 0.45f, kick);
                elbow = Mathf.Lerp(elbow, Mathf.Lerp(armHopElbow, armHopElbowBack, hopOpen), hw);
            }
            float pw = preview ? 0f : _pair;
            if (pw > 0.001f)
            {
                // Both arms together, far back loading, up and forward at the push (the same clock as the hop's throw).
                swing = Mathf.Lerp(swing, Mathf.Lerp(armPairBack, armPairUp, _armKick), pw);
                elbow = Mathf.Lerp(elbow, Mathf.Lerp(armPairElbow.x, armPairElbow.y, _armKick), pw);
            }
            if (!preview && dt > 0f)
            {
                if (!arm.swingT.set)
                {
                    arm.swingT.Seed(swing, 0f);
                    arm.elbowT.Seed(elbow, 0f);
                }
                swing = arm.swingT.Step(swing, dt, limbSpring, armMaxSpeed, armMaxAccel);
                elbow = arm.elbowT.Step(elbow, dt, limbLag, armMaxSpeed, armMaxAccel);
            }
            arm.swing = swing;
            arm.elbow = elbow;
        }

        /// <summary>
        /// The upper body takes part in the bounce, through a spring so a press moves the target, not the pose: two keys together
        /// (the sack hop) load forward before the push (pairLean), open back a little at the throw (pairOpen) and sit back in the air;
        /// one key rocks the trunk a little with each hop (hopRock): forward at the push, back as it comes down. The chest, neck and
        /// head follow as the lean spreads down the chain like the gait lean. The dive and the hold fade it.
        /// </summary>
        void UpdateBounceLean(float dt, ref float lean, ref float baseBend, ref float bend)
        {
            float pw = _pair * (1f - _dive), hw = _hop * (1f - _pair) * (1f - _dive);
            float rw = _stepping * (1f - _hop) * (1f - _pair) * (1f - _dive);   // the run's rock does not wait for the gait: it is there at a slow run too
            // Presses too fast for a trunk to follow rock it less (the pair's throw is not a rhythm of single presses, so it keeps its size).
            float rock = Mathf.Lerp(rockFastShare, 1f, Mathf.InverseLerp(RockFastPeriod, RockSlowPeriod, _armPeriod));
            _rideVel += (_bodyY.Velocity - _rideVel) * (1f - Mathf.Exp(-RideFilter * dt));   // the raw height speed trembled with every step
            float target = pw * (pairLean * (1f - _armKick) - pairOpen * _armKick) + rock * hw * -hopRock * _rockKick +
                           rock * rw * -runRock * _rockKick;
            target += rock * rideBob * Mathf.Clamp(-_rideVel / 1.5f, -1f, 1f) * (rw + hw);   // down: forward, up: back
            _bounceLean.Step(target, bounceLeanSpring.frequency, bounceLeanSpring.damping, dt);
            _bounceChest.Step(_bounceLean.Value, bounceChestSpring.frequency, bounceChestSpring.damping, dt);
            // Only the waist and the chest move (baseBend, bend): the hips' lean and the legs' turn stay as they were, so the hip joint
            // and the feet do not move with the upper body.
            baseBend += _bounceLean.Value * 0.4f;
            bend += _bounceChest.Value;
            // The pelvis takes a small share in the run, so the rock goes from the seat to the head (the legs' turn, legLean, is not touched).
            lean += _bounceLean.Value * pelvisRockShare * (rw + hw);
        }

        void ResetGait()
        {
            _bounceLean.Reset();
            _bounceChest.Reset();
            _gait.Reset();
            _kick.Reset();
            _gaitW = _speedCurve = _accel = _lastSpeed = _gaitHead = _hop = _pair = _hopTarget = 0f;
            _rideVel = 0f;
            _hopSpring.Reset();
            _pairSpring.Reset();
            _hopPrevLeg = -1;
            _hopRepeated = false;
            _hopPrevTime = -10.0;
            _posePresses = _armPresses = _hopPresses = 0;
            _poseLastLeg = -1;
            _poseLastTime = -10.0;
            _posePrevGap = 10f;
            _poseSkippedOther = false;
            _sitW[0] = _sitW[1] = 0f;
            _armKick = 0f;
            _armClock = ArmIdle;
            leftArm.swingT.set = rightArm.swingT.set = false;
            _hipFree = false;
            _lastPresses = 0;
            _twist = _roll = _chestYaw = _chestRoll = 0f;
            _trunkInit = false;
        }

        /// <summary>
        /// The gait weight (0..1) and the forward lean it brings: gaitLean by speed, accelLean while speeding up and a
        /// small pitch per press (pressKick × its efficiency), spread down the chain like the head's lean (the chest
        /// ends up with the lean itself). All of it fades with the head-led lean (mashing, the dive), with a lean back,
        /// a hold, a jump, the air and settling: dive, sit-back and the hold poses stay what they were. The head follows
        /// part of it (gaitHeadShare) so it keeps looking ahead.
        /// </summary>
        void UpdateGaitLean(RunnerSim sim, float dt, ref float lean, ref float baseBend, ref float bend, ref float legLean)
        {
            _speedCurve = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(gaitSpeed.x, Mathf.Max(gaitSpeed.x + 0.01f, gaitSpeed.y), sim.Speed));
            float holdW = Mathf.Max(_holdL, _holdR);
            // Not in free fall (the dive): the short flights between a run's steps are part of the gait (a gate on "a foot
            // carries" flipped it every step and halved the twist and the arms).
            float gate = _stepping * (_ballistic ? 0f : 1f) * (sim.InJump ? 0f : 1f) *
                         (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HoldGaitFrom, HoldGaitTo, holdW)));
            _gaitW = Mathf.Lerp(_gaitW, _speedCurve * gate, 1f - Mathf.Exp(-GaitBlend * dt));

            float a = dt > 0f ? Mathf.Clamp((sim.Speed - _lastSpeed) / dt, -10f, 10f) : 0f;
            _lastSpeed = sim.Speed;
            _accel = Mathf.Lerp(_accel, a, 1f - Mathf.Exp(-AccelBlend * dt));

            float full = _hipsLean.Value;
            float fade = Mathf.Clamp01(1f - Mathf.Max(0f, full) / Mathf.Max(1f, gaitLeanFade)) *
                         Mathf.Clamp01(1f + Mathf.Min(0f, full) / SitFadeRange);
            float form = Mathf.Lerp(formFloor, 1f, sim.Form);
            // One-leg hops sit back and two keys together bounce upright: no forward gait lean for either.
            float upright = (1f - _hop) * (1f - _pair);
            float target = (gaitLean * form * _speedCurve + accelLean * Mathf.Clamp(_accel, 0f, AccelMax)) * gate * fade * upright;
            _gait.Step(target, gaitLeanSpring.frequency, gaitLeanSpring.damping, dt);

            if (sim.Presses != _lastPresses)
            {
                // The impulse that peaks at pressKick × efficiency for a critically damped spring (peak = v / (w·e)).
                if (sim.Presses > _lastPresses)
                    _kick.Velocity += sim.LastEfficiency * pressKick * 2f * Mathf.PI * pressKickSpring.frequency * 2.71828f * gate * fade * upright;
                _lastPresses = sim.Presses;
            }
            _kick.Step(0f, pressKickSpring.frequency, pressKickSpring.damping, dt);

            float extra = (_gait.Value + _kick.Value) / Mathf.Max(0.5f, hipShare + waistBaseShare + waistTopShare);
            lean += extra * hipShare;
            legLean += extra * (1f - hipShare);
            baseBend += extra * waistBaseShare;
            bend += extra * (waistBaseShare + waistTopShare);
            _gaitHead = (_gait.Value + _kick.Value) * gaitHeadShare;
        }

        /// <summary>
        /// Twist and roll of the running pelvis and chest, from the legs this frame (thighWorld, after FollowHips):
        /// the leg that is forward brings its hip forward (yaw from the thigh difference), the chest turns the other
        /// way; the hip of the leg swinging forward drops a little (roll from the thighs' speeds), the chest the other
        /// way. Smoothed, scaled by the gait weight and Form, so rest, hold, the air and the finish give 0. The head
        /// is not turned: it looks ahead through its look target.
        /// </summary>
        void UpdateTrunk(RunnerSim sim, float dt, float hipPitch, ref Quaternion pelvisTilt)
        {
            float lt = leftLeg.thighWorld, rt = rightLeg.thighWorld;
            float rateL = 0f, rateR = 0f;
            if (_trunkInit && dt > 0f)
            {
                rateL = (lt - _lastThighL) / dt;
                rateR = (rt - _lastThighR) / dt;
            }
            _lastThighL = lt;
            _lastThighR = rt;
            _trunkInit = true;
            if (preview)
            {
                _gaitHead = 0f;
                _chestYaw = _chestRoll = 0f;
                return;
            }

            float k = 1f - Mathf.Exp(-TwistBlend * dt), kr = 1f - Mathf.Exp(-RollBlend * dt);
            _twist = Mathf.Lerp(_twist, Mathf.Clamp((rt - lt) / Mathf.Max(1f, twistThighRange), -1f, 1f), k);
            _roll = Mathf.Lerp(_roll, Mathf.Clamp((rateL - rateR) / (2f * RollRate), -1f, 1f), kr);
            float amp = _gaitW * Mathf.Lerp(formFloor, 1f, sim.Form);
            // + yaw takes the right hip back, so the forward right leg (rt > lt) asks for a negative one.
            float yaw = -pelvisTwist * _twist * amp;
            float roll = pelvisRoll * _roll * amp;   // + roll drops the left hip
            pelvisTilt = Quaternion.Euler(hipPitch, yaw, roll);
            _chestYaw = chestTwist * _twist * amp;
            _chestRoll = -chestRoll * _roll * amp;
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

            // The last word on the floor is the foot as it is now posed: if the ball of it is under the ground (the predicted pitch,
            // FreeFootRise, can be a few degrees off, and a reaching or resting leg is not raised by it at all) the foot turns up
            // until it stands on it. The floor is the one thing nothing after this step moves.
            if (preview || !leg.toeBone) return;
            float floor = GroundY(leg) - 0.003f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 ball = Local(leg.toeBone), ankle = Local(leg.footBone);
                if (ball.y >= floor || bend <= -ankleUpMax) break;
                float arm = Mathf.Max(0.06f, Mathf.Abs(ball.z - ankle.z));
                bend = Mathf.Max(-ankleUpMax, bend - Mathf.Min(25f, Mathf.Rad2Deg * (floor - ball.y) / arm));
                leg.footBone.rotation = Quaternion.AngleAxis(bend, axis) * neutral;
            }
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

            // The floor under the shoe's tip as well (the toe bone is the ball): toes down put the tip ~8 cm past it into the ground. The
            // toes bend up first (the tip level), and what that cannot do the foot turns up. The last step on the foot.
            if (preview) return;
            float floorY = GroundY(leg) - 0.003f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = transform.InverseTransformDirection(Quaternion.AngleAxis(bend, axis) * (leg.footBone.rotation * leg.tipInFoot)).normalized;
                Vector3 tip = Local(leg.toeBone) + dir * toeTipLength;
                if (tip.y >= floorY) break;
                float deficit = floorY - tip.y;
                if (bend > -toeUpMax + 0.5f)
                    bend = Mathf.Max(-toeUpMax, bend - Mathf.Min(40f, Mathf.Rad2Deg * deficit / Mathf.Max(0.03f, toeTipLength)));
                else
                {
                    float arm = Mathf.Max(0.1f, Mathf.Abs(tip.z - Local(leg.footBone).z));
                    leg.footBone.rotation = Quaternion.AngleAxis(-Mathf.Min(20f, Mathf.Rad2Deg * deficit / arm), axis) * leg.footBone.rotation;
                }
                leg.toeBone.rotation = Quaternion.AngleAxis(bend, axis) * (leg.footBone.rotation * leg.restToeInFoot);
            }
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
