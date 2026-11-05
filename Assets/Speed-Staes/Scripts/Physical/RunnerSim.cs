using UnityEngine;

namespace SpeedStars.Physical
{
    public enum PressResult
    {
        None,
        /// <summary>First push from standing.</summary>
        Start,
        /// <summary>Pressed right as the previous stride landed.</summary>
        Good,
        /// <summary>Pressed a while after landing: momentum already lost.</summary>
        Late,
        /// <summary>Pressed in the air or while the other foot was still pushing: wasted, tips the body.</summary>
        Early,
    }

    /// <summary>All numbers of the runner's reduced physics. Defaults are tuned to the reference videos.</summary>
    [System.Serializable]
    public sealed class RunnerTuning
    {
        [Header("Speed (m/s)")]
        [Tooltip("Hard limit on speed (m/s). The real top speed comes from the losses (dragQuadratic) catching up with the pushes.")]
        public float maxSpeed = 10.5f;
        [Tooltip("Speed a full push gives (m/s), the same at any speed: every press is a real push, and the losses " +
                 "between presses take it back — the runner speeds up on each push and slows down until the next.")]
        public float pushGain = 1.5f;
        [Tooltip("Time over which a push's gain is applied (s).")]
        public float pushTime = 0.12f;
        [Tooltip("Constant loss (m/s²).")]
        public float dragConstant = 0.4f;
        [Tooltip("Loss growing with speed (per second).")]
        public float dragLinear = 0f;
        [Tooltip("Loss growing with the square of speed (per metre): small slow, large fast — it sets the top speed " +
                 "and makes the slowing between presses visible at speed.")]
        public float dragQuadratic = 0.1f;
        [Tooltip("Stopping: once no key has been down for stopDelay (s) the runner has stopped running and brakes " +
                 "with its feet — a constant loss (m/s²) like real friction, coming in over stopRamp (s): speed falls " +
                 "in a straight line, faster and faster for what is left, and stops. Any key down takes it away.")]
        public float stopBrake = 3.5f;
        public float stopDelay = 0.6f;
        public float stopRamp = 0.3f;

        [Header("Stride")]
        [Tooltip("Press → take-off: how long the foot pushes on the ground (s).")]
        public float stanceTime = 0.08f;
        [Tooltip("The step's tempo (s), standing (x) and at max speed (y): a full push lands this long after its press, " +
                 "so the next press lands on time then (the flight is this minus stanceTime). The visual step " +
                 "(RunnerPoseDriver) is done by then too, so a press on time comes as the foot is in front.")]
        public Vector2 stepTime = new Vector2(0.255f, 0.21f);
        [Tooltip("Gravity (m/s²): the hop between strides here, and the body's fall in a dive (RunnerPoseDriver).")]
        public float gravity = 7f;

        [Header("Push efficiency (read from the physical state, no timing curve)")]
        [Tooltip("A press this soon (s) after the other foot's is not early: the two push together. Later in " +
                 "the other foot's stance it is early. (Fast runs in the videos press 0.05–0.08 s apart.)")]
        public float togetherWindow = 0.08f;
        [Tooltip("Share of a push the second key of a pair gets, when it comes within togetherWindow of the other foot's: " +
                 "two keys together are one push, not two (0 = the second gives nothing, 1 = it pushes in full). " +
                 "The pair's head kick, nod and lean kick are scaled the same way.")]
        [Range(0f, 1f)] public float togetherPush = 0f;
        [Tooltip("Efficiency of a press in the air or while the other foot is still pushing.")]
        [Range(0f, 1f)] public float earlyEfficiency = 0.15f;
        [Tooltip("A press this long (s) before the landing is not wasted: it counts from the landing, its push growing from " +
                 "earlyEfficiency to full as the landing nears, so being a little early costs a little.")]
        public float earlySoftTime = 0.05f;
        [Tooltip("After landing, presses within this time (s) are full.")]
        public float lateGrace = 0.04f;
        [Tooltip("Time after landing (s) where a late press reaches lateFloor.")]
        public float lateFloorTime = 0.2f;
        [Tooltip("Efficiency of a press made lateFloorTime or more after landing.")]
        [Range(0f, 1f)] public float lateFloor = 0.55f;
        [Tooltip("Speed (m/s) from which lateness counts fully (no momentum to lose when slow).")]
        public float lateSpeed = 3f;
        [Tooltip("The pressed leg's extension: below x it is back under the body (full), above y it is still out (0).")]
        public Vector2 legReadyExt = new Vector2(0.25f, 0.85f);
        [Tooltip("Below this speed (m/s) every tap pushes at least tapFloorGain, whatever its timing or the head: mashing never leaves the runner standing.")]
        public float tapFloorSpeed = 3.2f;
        [Tooltip("Least push (m/s) of a tap while the runner is below tapFloorSpeed.")]
        public float tapFloorGain = 0.6f;
        [Tooltip("Same leg twice in a row within a second.")]
        [Range(0f, 1f)] public float sameLegFactor = 0.4f;
        [Tooltip("Same leg again when standing still or slow: there is no stride to repeat yet, so each tap pushes at this factor instead, blending to sameLegFactor by sameLegStartSpeed.")]
        [Range(0f, 1f)] public float sameLegStartFactor = 0.75f;
        [Tooltip("Speed (m/s) at which a repeated key has only sameLegFactor.")]
        public float sameLegStartSpeed = 3f;
        [Tooltip("Same leg again: its push is scaled by gap / hopFullGap below this gap (s), so tapping one key faster than this gains nothing.")]
        [Range(0.01f, 1f)] public float hopFullGap = 0.25f;
        [Tooltip("Keys in turn: a press's push is scaled by gap / pressFullGap below this gap (s), so tapping faster than a runner can step gains no more.")]
        [Range(0.01f, 1f)] public float pressFullGap = 0.15f;

        [Header("Hold")]
        [Tooltip("A press shorter than this still makes a full stride; longer is a hold (s).")]
        public float minStepTime = 0.14f;
        [Tooltip("Forward push per held foot (m/s²), for holdThrustTime after the hold starts.")]
        public float holdThrust = 1f;
        [Tooltip("How long the hold thrust lasts after a hold starts (s).")]
        public float holdThrustTime = 0.6f;
        [Tooltip("Extra loss in a hold pose (m/s²): the planted feet slide.")]
        public float holdDrag = 0f;

        [Header("Springs")]
        [Tooltip("Leg extension while pushing back.")]
        public SpringSettings extRise = new SpringSettings(6f, 0.9f);
        [Tooltip("Leg extension coming back forward (swing).")]
        public SpringSettings extFall = new SpringSettings(3.2f, 0.9f);
        [Tooltip("Hold pose coming in (slow: the body stretches out).")]
        public SpringSettings holdRise = new SpringSettings(1.2f, 1f);
        [Tooltip("Hold pose going away after release (fast: this is what throws the body back).")]
        public SpringSettings holdFall = new SpringSettings(4.5f, 1f);
        [Tooltip("Whole-body lean. Low damping = the swing back past upright after a release.")]
        public SpringSettings lean = new SpringSettings(1f, 0.3f);

        [Header("Lean (deg, + forward)")]
        [Tooltip("Lean a held foot asks for (deg, per held foot): not stumbling.")]
        public float lungeLean = 40f;
        [Tooltip("Lean at max speed.")]
        public float runLean = 8f;
        [Tooltip("Forward lean kick of a push (deg/s).")]
        public float leanKick = 40f;
        [Tooltip("A wasted press kicks harder: × (1 + this × (1 − efficiency)).")]
        public float stumbleKick = 3f;
        [Tooltip("Backward kick when a hold is let go (deg/s × hold weight): the bent support leg straightens.")]
        public float releaseKick = 140f;
        [Tooltip("Lean past this (deg) blends in the Stumble pose (visual only; the loss comes from the head, below).")]
        public float stumbleLean = 40f;
        [Tooltip("Limit of the sim's lean spring forward (deg). Game logic only (stumble, HUD): the body you see leans by the head (RunnerPoseDriver).")]
        public float maxForwardLean = 85f;
        [Tooltip("Limit of the sim's lean spring back (deg). Game logic only, like maxForwardLean.")]
        public float maxBackLean = 45f;

        [Header("Head (deg, + = forward): a key down takes it forward, no key down brings it back")]
        [Tooltip("Where the head comes back to when no key is down, while running (standing in the crouch: upright).")]
        public float headBack = -15f;
        [Tooltip("Speed (m/s) from which the head rests at headBack (x: upright below, y: fully at headBack).")]
        public Vector2 headBackSpeed = new Vector2(0.5f, 3f);
        [Tooltip("How far the first moment of a key down takes the head forward at most (deg, all of it within ~0.1 s), " +
                 "from wherever the head is. How much of it a press takes comes from its timing: see goodKickShare and wasteKickShare.")]
        public float headKick = 12f;
        [Tooltip("Share of headKick a press with perfect timing takes (a press on time barely throws the head forward).")]
        [Range(0f, 2f)] public float goodKickShare = 0.03f;
        [Tooltip("Share of headKick a wasted press (efficiency 0: in the air, very late, leg not back) takes. In between " +
                 "it follows the press's timing efficiency, so bad presses pile the head up and good ones keep it low.")]
        [Range(0f, 5f)] public float wasteKickShare = 0.03f;
        [Tooltip("Key still down: the head goes on forward at this rate (deg/s × the key's hold weight; both keys = twice).")]
        public float holdPush = 70f;
        [Tooltip("No key down: the head comes back to headBack through this spring. Presses before it is back pile it up.")]
        public SpringSettings headReturnSpring = new SpringSettings(2.25f, 0.6f);
        [Tooltip("Past headBack the head (and the body after it) goes on through this softer, well damped spring: it sits " +
                 "back as far as the way back threw it, then comes back slowly, never swinging forward again.")]
        public SpringSettings headSettleSpring = new SpringSettings(0.4f, 1f);
        [Tooltip("After the last key comes up the way back starts soft and reaches full strength over this long (s): " +
                 "a press coming sooner finds it still weak, so quick taps pile the head up like a hold; presses on " +
                 "the rhythm leave it time to bring the head back.")]
        public float headReturnDelay = 0.1f;
        [Tooltip("Most the head goes forward (deg).")]
        public float headForwardMax = 80f;
        [Tooltip("Small nod every press gives the head (deg/s), whatever the press: keeps it alive.")]
        public float headNod = 60f;
        [Tooltip("The head follows its target through this spring (low damping = it swings).")]
        public SpringSettings headSpring = new SpringSettings(6f, 0.5f);
        [Tooltip("A key's hold weight (0 = up, 1 = held a while) builds up from the moment it goes down — no threshold " +
                 "between a tap and a hold — and goes away after it comes up. holdPush and the legs' hold angles use it.")]
        public SpringSettings holdIn = new SpringSettings(2f, 1f);
        [Tooltip("How fast a key's hold weight goes away after the key comes up.")]
        public SpringSettings holdOut = new SpringSettings(4.5f, 1f);

        [Header("Strain: wasted presses build it slowly, on-time presses and rest wear it off; it leans the head forward (over seconds)")]
        [Tooltip("Strain (0..1) a press adds at most: × how early it comes (strainWastePower, strainWasteHalf) and × its push scale squared: " +
                 "over-tapping takes seconds to lean the runner, not one press, and tapping faster than a runner can step does not pile it up faster per second.")]
        public float strainGain = 0.065f;
        [Tooltip("Strain each on-time press takes away, × its timing^8 (a wasted press takes none): back on the rhythm the head comes up within a couple of seconds.")]
        public float strainRelief = 0.2f;
        [Tooltip("An on-time press only wears strain off as far as the runner has been pressing on time lately: its relief is × Form^this. " +
                 "Mashing lands every other press on the beat by chance; without this the good ones wipe out what the wasted ones built. 0 = every on-time press relieves in full.")]
        public float strainReliefFormPower = 5f;
        [Tooltip("Steepness of how early a press (the time to its landing, in step times) takes the strain it adds: high = a cadence only a little too fast adds almost none, mashing adds it all.")]
        public float strainWastePower = 8f;
        [Tooltip("How early a press (the time to its landing, in step times) takes half of the strain a wasted press adds: a cadence only a little too fast stays well below it, mashing is well above.")]
        [Range(0.05f, 1f)] public float strainWasteHalf = 0.23f;
        [Tooltip("Strain lost per second whatever happens (1/s).")]
        public float strainLeak = 0.05f;
        [Tooltip("Extra leak (× strainLeak) when no press has come for about 0.5 s: keys up, the runner gets his breath.")]
        public float strainRest = 8f;
        [Tooltip("Share of headForwardMax a full strain leans the head to.")]
        [Range(0f, 2f)] public float strainHead = 1.3f;

        [Header("Head → speed: the further forward the head, the less a press gives and the more the runner loses")]
        [Tooltip("Head angle (deg) at which a press gives nothing; upright or back a press gives all. In between it " +
                 "falls off smoothly (no threshold): 1 − (head / headSpoil)^headSpoilPower.")]
        public float headSpoil = 90f;
        [Tooltip("Shape of that fall-off: 1 = straight; above 1 a slightly forward head costs less, a far forward one as much.")]
        public float headSpoilPower = 1.1f;
        [Tooltip("Extra loss with the head all the way forward (m/s² at headForwardMax, in proportion below).")]
        public float headDrag = 0.8f;

        [Header("Form (how well the runner has been running; read by the visual, no effect on speed)")]
        [Tooltip("Each press moves Form this share of the way to that press's timing efficiency (moving average; 1 = only the last press counts).")]
        [Range(0f, 1f)] public float formFollow = 0.3f;
        [Tooltip("With no press for formHoldTime, Form fades toward 0 at this rate (1/s).")]
        public float formDecay = 1.5f;
        [Tooltip("How long after the last press Form stays as it is before it starts to fade (s).")]
        public float formHoldTime = 0.5f;

        [Header("Hop bias (one key again and again → one-leg hops; read by the visual, no effect on speed)")]
        [Tooltip("Each press moves HopBias this share of the way to 1 when it is the same leg as the press before, to 0 when it is the other " +
                 "leg (keys in turn, or two keys together, which come one after the other).")]
        [Range(0f, 1f)] public float hopBiasFollow = 0.45f;
        [Tooltip("HopBias fades toward 0 at this rate (1/s) as time passes, so a stray double press does not stay.")]
        public float hopBiasDecay = 0.5f;

        [Header("Hurdle jump")]
        [Tooltip("Before leaving the ground the leading leg comes up with the other foot still planted (s), standing (x) and at max speed (y).")]
        public Vector2 jumpWindup = new Vector2(0.2f, 0.05f);
        [Tooltip("Time in the air (s), standing (x) and at max speed (y). Speed does not fall in the air.")]
        public Vector2 jumpTime = new Vector2(0.3f, 0.18f);
        [Tooltip("How high the body rises at the top of a jump (m).")]
        public float jumpHeight = 0.3f;
        [Tooltip("Shape of the jump: 2 = a plain arc, higher = up quickly, a moment hanging at the top over the bar, down quickly.")]
        public float jumpHang = 4f;
        [Tooltip("Share of the speed a knocked hurdle takes away.")]
        [Range(0f, 1f)] public float hitLoss = 0.3f;
        [Tooltip("How long the stumble after a knocked hurdle lasts (s): the runner stays on its feet.")]
        public float hitTime = 0.45f;

        [Header("Energy")]
        [Tooltip("Full energy, at the start of a run.")]
        public float energyMax = 100f;
        [Tooltip("Energy every press uses.")]
        public float drainPerPush = 0.25f;
        [Tooltip("Energy used per second while a key is held.")]
        public float drainHoldPerSecond = 2f;
        [Tooltip("Recovered per second while standing still.")]
        public float regenPerSecond = 4f;
        [Tooltip("Below this the pushes weaken.")]
        public float lowEnergy = 25f;
        [Tooltip("Push strength at 0 energy.")]
        [Range(0f, 1f)] public float lowEnergyFactor = 0.6f;
    }

    public sealed class LegSim
    {
        public bool Pressed;
        public double PressTime = -10.0;
        /// <summary>0 = under the body, 1 = pushed all the way back.</summary>
        public Spring Ext;
        /// <summary>0 = running, 1 = in the hold pose.</summary>
        public Spring Hold;
        /// <summary>Held longer than a stride.</summary>
        public bool Holding;
        /// <summary>Coming back forward through the air.</summary>
        public bool Swinging;
        /// <summary>Hold weight from the moment the key went down (no threshold): 0 = up, 1 = held a while.</summary>
        public Spring Weight;

        public float ExtValue => Mathf.Clamp01(Ext.Value);
        public float HoldValue => Mathf.Clamp01(Hold.Value);
        public float WeightValue => Mathf.Clamp01(Weight.Value);
        /// <summary>Head kick (deg) of this key's last press: set from the press's timing in <see cref="RunnerSim.Press"/>.</summary>
        public float Kick;

        public void Reset()
        {
            Pressed = Holding = Swinging = false;
            Kick = 0f;
            PressTime = -10.0;
            Ext.Reset();
            Hold.Reset();
            Weight.Reset();
        }
    }

    /// <summary>
    /// The runner as a handful of physical quantities: forward speed (mass + impulses + losses), one
    /// extension and one hold spring per leg, a whole-body lean spring and a vertical hop under gravity.
    /// How good a press is comes only from this state: is the body on the ground, how long since it
    /// landed, is the pressed leg back under the body. Mashing therefore wastes presses and their lean
    /// kicks pile up until the runner stumbles; a release lets the lean spring swing back past upright.
    /// The head angle lives here too (the visual draws it): every key down takes it forward, no key down
    /// brings it back; the further forward it is, the less a press gives (HeadUpright) and the more the
    /// runner loses (headDrag) — so mashing spoils itself and the presses get their full push back only as
    /// the head comes up again. How hard a key down first throws the head forward comes from the press's timing
    /// (a press on time barely does, a wasted one does hard), so good and bad running look different, and
    /// <see cref="Form"/> keeps the running average of that timing for the visual. Two keys within togetherWindow
    /// are one push (togetherPush), not two. No threshold anywhere.
    /// Pure C#: no scene, no input, steps itself in small fixed sub-steps.
    /// </summary>
    public sealed class RunnerSim
    {
        const float SubStep = 1f / 240f;
        const float PressBurst = 0.03f;   // a key down gives its headKick over about this long (s, time constant)
        const float HeldStop = 20f;       // a key down stops the head's way back this fast (1/s)

        public readonly RunnerTuning T;
        public readonly LegSim[] Legs = { new LegSim(), new LegSim() };
        public Spring LeanSpring;

        public double Time { get; private set; }
        public float Speed { get; private set; }
        public float Distance { get; private set; }
        public float HopY { get; private set; }
        public float Energy { get; private set; }
        public int Presses { get; private set; }
        public int LastLeg { get; private set; }
        public double LastPressTime { get; private set; }
        public PressResult LastResult { get; private set; }
        public float LastEfficiency { get; private set; }

        public float Lean => LeanSpring.Value;
        /// <summary>The head angle (deg, + = forward).</summary>
        public float HeadAngle => _head.Value;
        /// <summary>0..1: how much over-tapping has built up (slow); it leans the head.</summary>
        public float Strain { get; private set; }
        /// <summary>How well the runner has been running (0..1): the moving average of the presses' timing efficiency
        /// (before head and energy), fading when no press comes. For the visual; it changes no speed.</summary>
        public float Form => _form;
        /// <summary>0..1: how much the presses have been the same leg again and again (1) rather than the other leg or both (0). For the visual (a one-leg hop); it changes no speed.</summary>
        public float HopBias => _hopBias;
        /// <summary>Where the head rests with no key down (deg): headBack while running, upright standing.</summary>
        public float HeadRest => T.headBack * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(T.headBackSpeed.x, T.headBackSpeed.y, Speed));
        /// <summary>How much of its push a press gets at this head angle (1 upright or back, 0 at headSpoil).</summary>
        public float HeadUpright => 1f - Mathf.Pow(Mathf.Clamp01(HeadAngle / Mathf.Max(1f, T.headSpoil)), T.headSpoilPower);
        /// <summary>The part of the lean the hold poses ask for (deg); Lean − PoseLean beyond stumbleLean is stumbling.</summary>
        public float PoseLean { get; private set; }
        public bool Airborne => HopY > 0f || JumpInAir;
        /// <summary>From the jump being asked for to the landing, the wind-up on the ground included.</summary>
        public bool InJump => _windupLeft > 0f || _jumpLeft > 0f;
        /// <summary>Off the ground in a jump: only this clears a hurdle.</summary>
        public bool JumpInAir => _windupLeft <= 0f && _jumpLeft > 0f;
        /// <summary>How far through the wind-up on the ground (0..1), 1 once in the air.</summary>
        public float JumpWindupProgress => !InJump ? 0f : _windupLeft > 0f ? 1f - _windupLeft / _windupTotal : 1f;
        /// <summary>How far through the air (0..1), 0 on the ground.</summary>
        public float JumpProgress => JumpInAir ? 1f - _jumpLeft / _jumpTotal : 0f;
        /// <summary>The body's rise in the jump (m).</summary>
        public float JumpY => JumpInAir ? T.jumpHeight * (1f - Mathf.Pow(Mathf.Abs(2f * JumpProgress - 1f), T.jumpHang)) : 0f;
        /// <summary>The leg going over first: the one that did not push last.</summary>
        public int JumpLeg { get; private set; }
        /// <summary>The stumble after a knocked hurdle (1 at the hit, 0 when over).</summary>
        public float HitWeight => Mathf.Clamp01(_hitLeft / Mathf.Max(0.01f, T.hitTime));
        /// <summary>Press to press on time at this speed (s): a full push lands then (stepTime, standing to max speed).</summary>
        public float StepTime => Mathf.Lerp(T.stepTime.x, T.stepTime.y, Speed / T.maxSpeed);
        public int HoldingCount => (Legs[0].Holding ? 1 : 0) + (Legs[1].Holding ? 1 : 0);

        float _pending, _hopVelocity, _launchVelocity, _form, _hopBias;
        Spring _head, _headFwd;   // the head angle; where the keys have taken it, coming back with no key down
        double _keyUpTime;        // sim time the last key down came up
        double _launchTime, _landTime;
        float _windupLeft, _windupTotal, _jumpLeft, _jumpTotal, _hitLeft;

        public RunnerSim(RunnerTuning tuning)
        {
            T = tuning;
            Reset();
        }

        public void Reset()
        {
            Time = 0.0;
            Speed = Distance = HopY = _hopVelocity = _pending = _form = _hopBias = 0f;
            _windupLeft = _jumpLeft = _hitLeft = 0f;
            Energy = T.energyMax;
            Presses = 0;
            LastLeg = -1;
            LastPressTime = -10.0;
            LastResult = PressResult.None;
            LastEfficiency = 0f;
            _launchTime = -1.0;
            _landTime = -10.0;
            LeanSpring.Reset();
            _head.Reset();
            Strain = 0f;
            _headFwd.Reset();
            _keyUpTime = -1000.0;
            PoseLean = 0f;
            Legs[0].Reset();
            Legs[1].Reset();
        }

        public void Advance(float dt)
        {
            while (dt > 1e-6f)
            {
                float h = Mathf.Min(SubStep, dt);
                Step(h);
                dt -= h;
            }
        }

        public void Press(int leg)
        {
            var l = Legs[leg];
            if (l.Pressed) return;

            float sinceLast = (float)(Time - LastPressTime);
            bool launchPending = _launchTime >= 0.0;
            // The other foot pressed only just before: the two push together, not one into the other's stance.
            // No special case beyond that — it is judged like any press.
            bool together = LastLeg >= 0 && LastLeg != leg && sinceLast < T.togetherWindow;
            PressResult result;
            float eff;
            bool nearLanding = false;
            float earliness = 0f;   // how far before the landing the press comes, in step times (0 on the ground and in a hop)
            bool repeat = LastLeg == leg && sinceLast < 1f;
            if (Speed < 0.3f && !Airborne && !launchPending)
            {
                result = PressResult.Start;
                eff = 1f;
            }
            else if (repeat && (Airborne || launchPending))
            {
                // A hop has no stride to land from: the press in flight is on time (its rate is limited by hopFullGap below).
                result = PressResult.Good;
                eff = 1f;
            }
            else if (Airborne || (launchPending && !together))
            {
                float toLand = TimeToLand();
                if (launchPending) toLand += Mathf.Max(0f, (float)(_launchTime - Time)) + 2f * _launchVelocity / Mathf.Max(0.01f, T.gravity);
                earliness = Mathf.Clamp01(toLand / Mathf.Max(0.05f, StepTime));
                nearLanding = !launchPending && toLand < T.earlySoftTime;
                eff = nearLanding
                    ? Mathf.Lerp(1f, T.earlyEfficiency, Mathf.SmoothStep(0f, 1f, toLand / Mathf.Max(0.001f, T.earlySoftTime)))
                    : T.earlyEfficiency;
                result = eff > 0.9f ? PressResult.Good : PressResult.Early;
            }
            else
            {
                float late = (float)(Time - _landTime) - T.lateGrace;
                float span = Mathf.Max(0.01f, T.lateFloorTime - T.lateGrace);
                float lateCurve = Mathf.Lerp(1f, T.lateFloor, Mathf.SmoothStep(0f, 1f, late / span));
                eff = Mathf.Lerp(1f, lateCurve, Mathf.Clamp01(Speed / Mathf.Max(0.01f, T.lateSpeed)));
                result = eff > 0.9f ? PressResult.Good : PressResult.Late;
            }

            // The pressed leg must be back under the body to push again.
            float ready = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(T.legReadyExt.x, T.legReadyExt.y, l.Ext.Value));
            // A hop (the same leg again) never left the ground, so "back under the body" does not apply to it by the hop bias
            // it is building (sameLegFactor is its penalty): no speed cliff at the gap where Ext just stays high.
            if (repeat) ready = Mathf.Lerp(ready, 1f, _hopBias + (1f - _hopBias) * T.hopBiasFollow);
            eff *= ready;
            // The head's kick follows the timing without this: a hop (same leg again) is not a wasted press, the head must not be thrown.
            float kickTiming = Mathf.Clamp01(eff);
            if (repeat) eff *= Mathf.Lerp(T.sameLegStartFactor, T.sameLegFactor, Mathf.SmoothStep(0f, 1f, Speed / Mathf.Max(0.1f, T.sameLegStartSpeed)));
            // Tapping one key faster than hopFullGap gains nothing: the push per second stays what it is at that gap.
            float pushScale = Mathf.Min(1f, sinceLast / Mathf.Max(0.01f, repeat ? T.hopFullGap : T.pressFullGap));
            if (together) pushScale = 1f;   // a pair's second key is judged by togetherPush
            // The press's own quality, before the body's state (energy, head) scales the push: it is what the head's
            // first kick and Form follow, so a press on time keeps the head low and a wasted one throws it forward.
            float timing = Mathf.Clamp01(eff);
            // The second key of a pair is part of the first's push, not a push of its own.
            bool pairSecond = together && launchPending;
            float share = pairSecond ? T.togetherPush : 1f;
            eff *= EnergyFactor();
            // A head that is down spoils the push: it comes back in full only as the head comes up.
            eff *= HeadUpright;
            _head.Velocity += T.headNod * share;
            // Wasted presses pile strain up a little each (a press cut short by the tapping speed less); on-time ones wear it off, as far
            // as the runner has been pressing on time lately (Form) - mashing hits the beat by chance every other press (no threshold).
            Strain = Mathf.Clamp01(Strain + (T.strainGain * pushScale * pushScale * Wasted(earliness) - T.strainRelief * Mathf.Pow(kickTiming, 8f) * Mathf.Pow(_form, T.strainReliefFormPower)) * share);
            l.Kick = T.headKick * Mathf.Lerp(T.goodKickShare, T.wasteKickShare, 1f - kickTiming) * share;

            float gain = T.pushGain * eff * share * pushScale;
            // Below a floor speed a tap always pushes a little (slow, limping), whatever its timing or the head: never stuck.
            if (!pairSecond && Speed + _pending < T.tapFloorSpeed)
                gain = Mathf.Max(gain, Mathf.Min(T.tapFloorGain, T.tapFloorSpeed - Speed - _pending));
            _pending += gain;

            LeanSpring.Velocity += T.leanKick * (1f + T.stumbleKick * (1f - eff)) * share;
            if (!pairSecond) _form += (timing - _form) * T.formFollow;
            // The same leg again raises the hop bias, the other leg (a pair's second key too) lowers it: a continuous state, no mode.
            _hopBias += ((LastLeg == leg ? 1f : 0f) - _hopBias) * T.hopBiasFollow;

            if (result == PressResult.Start || result == PressResult.Good || result == PressResult.Late || nearLanding)
            {
                _launchTime = Time + (nearLanding ? TimeToLand() : 0f) + T.stanceTime;
                // Flight = StepTime − stance whatever the press was worth: it comes down as the next press is due, so
                // the rhythm depends on the speed only and a bad press costs speed, not the beat.
                _launchVelocity = 0.5f * T.gravity * Mathf.Max(0f, StepTime - T.stanceTime);
            }

            Energy = Mathf.Max(0f, Energy - T.drainPerPush);
            l.Pressed = true;
            l.PressTime = Time;
            LastLeg = leg;
            LastPressTime = Time;
            // A pair is one step: the feedback keeps what the first key got.
            if (!pairSecond)
            {
                LastResult = result;
                LastEfficiency = eff;
            }
            Presses++;
        }

        /// <summary>A jump: the wind-up on the ground, then the air. Asked again during one it does nothing.</summary>
        public void Jump()
        {
            if (InJump) return;
            _launchTime = -1.0;
            HopY = _hopVelocity = 0f;
            _windupLeft = _windupTotal = Mathf.Lerp(T.jumpWindup.x, T.jumpWindup.y, Speed / T.maxSpeed);
            _jumpLeft = _jumpTotal = Mathf.Lerp(T.jumpTime.x, T.jumpTime.y, Speed / T.maxSpeed);
            JumpLeg = LastLeg >= 0 ? 1 - LastLeg : 1;
        }

        /// <summary>The runner went through a hurdle: it loses speed and stumbles, staying on its feet.</summary>
        public void HitHurdle()
        {
            Speed *= 1f - T.hitLoss;
            _hitLeft = T.hitTime;
        }

        /// <summary>Time (s) until the body is back on the ground; 0 on the ground.</summary>
        float TimeToLand()
        {
            if (JumpInAir) return _jumpLeft;
            if (_windupLeft > 0f) return 0f;
            if (HopY <= 0f && _hopVelocity <= 0f) return 0f;
            return (_hopVelocity + Mathf.Sqrt(_hopVelocity * _hopVelocity + 2f * T.gravity * HopY)) / T.gravity;
        }

        public void Release(int leg)
        {
            var l = Legs[leg];
            if (!l.Pressed) return;
            l.Pressed = false;
            LeanSpring.Velocity -= T.releaseKick * l.HoldValue;
        }

        /// <summary>
        /// The head angle: while a key is down it goes forward — the press's kick at once (within ~0.1 s), then on at
        /// holdPush × the key's hold weight — and does not come back; with no key down it comes back to
        /// HeadRest through headReturnSpring and past it through the softer headSettleSpring. A tap is a short
        /// hold; presses before it is back pile it up. The head follows through its own spring.
        /// </summary>
        void StepHead(float h)
        {
            float push = 0f;
            bool down = false;
            foreach (var l in Legs)
            {
                var ws = l.Pressed ? T.holdIn : T.holdOut;
                l.Weight.Step(l.Pressed ? 1f : 0f, ws.frequency, ws.damping, h);
                if (!l.Pressed) continue;
                down = true;
                float t = (float)(Time - l.PressTime), t0 = Mathf.Max(0f, t - h);
                push += l.Kick * (Mathf.Exp(-t0 / PressBurst) - Mathf.Exp(-t / PressBurst)) + T.holdPush * l.WeightValue * h;
            }
            if (down)
            {
                _headFwd.Velocity *= Mathf.Exp(-HeldStop * h);   // a key down stops the way back
                _keyUpTime = Time;
            }
            else
            {
                // The way back builds up from the moment the last key came up.
                float ramp = Mathf.SmoothStep(0f, 1f, (float)(Time - _keyUpTime) / Mathf.Max(0.001f, T.headReturnDelay));
                float back = HeadRest;
                var s = _headFwd.Value > back ? T.headReturnSpring : T.headSettleSpring;
                _headFwd.Step(back, s.frequency * ramp, s.damping, h);
            }
            _headFwd.Value += push;
            if (_headFwd.Value > T.headForwardMax)
            {
                _headFwd.Value = T.headForwardMax;
                _headFwd.Velocity = Mathf.Min(0f, _headFwd.Velocity);
            }
            // Strain: a slow build from wasted presses, worn off by on-time ones and by rest; it adds to where the head is led.
            float quiet = Mathf.SmoothStep(0f, 1f, ((float)(Time - LastPressTime) - 0.15f) / 0.35f);
            Strain = Mathf.Max(0f, Strain - T.strainLeak * (1f + T.strainRest * quiet) * h);
            float lead = Mathf.Min(T.headForwardMax, _headFwd.Value + Strain * T.strainHead * T.headForwardMax);
            _head.Step(lead, T.headSpring.frequency, T.headSpring.damping, h);
        }

        /// <summary>How much of a press's strain it adds by how early it comes (0..1, rising smoothly): a little early costs almost nothing, far too early all of it.</summary>
        float Wasted(float earliness)
        {
            float a = Mathf.Pow(earliness, T.strainWastePower), h = Mathf.Pow(Mathf.Max(0.01f, T.strainWasteHalf), T.strainWastePower);
            return a / (a + h);
        }

        float EnergyFactor() =>
            Energy >= T.lowEnergy ? 1f : Mathf.Lerp(T.lowEnergyFactor, 1f, Energy / Mathf.Max(0.01f, T.lowEnergy));

        void Step(float h)
        {
            Time += h;
            _hitLeft = Mathf.Max(0f, _hitLeft - h);
            if (_windupLeft > 0f)
            {
                _windupLeft -= h;
                if (_windupLeft <= 0f)
                {
                    _launchTime = -1.0;
                    HopY = _hopVelocity = 0f;
                }
            }
            else if (_jumpLeft > 0f)
            {
                _jumpLeft -= h;
                if (_jumpLeft <= 0f) _landTime = Time;
            }

            // Take-off at the end of the stance, however long the finger stays down: the beat must not depend on it.
            if (_launchTime >= 0.0 && Time >= _launchTime)
            {
                _launchTime = -1.0;
                if (_launchVelocity > 0.05f) _hopVelocity = _launchVelocity;
                else _landTime = Time;
            }
            if (HopY > 0f || _hopVelocity > 0f)
            {
                _hopVelocity -= T.gravity * h;
                HopY += _hopVelocity * h;
                if (HopY <= 0f)
                {
                    HopY = 0f;
                    _hopVelocity = 0f;
                    _landTime = Time;
                }
            }

            // Legs.
            double thrustStart = double.MaxValue;
            float maxHold = 0f;
            foreach (var l in Legs)
            {
                float held = (float)(Time - l.PressTime);
                l.Holding = l.Pressed && held >= T.minStepTime;
                if (l.Holding) thrustStart = System.Math.Min(thrustStart, l.PressTime + T.minStepTime);

                float extTarget = l.Pressed || held < T.minStepTime ? 1f : 0f;
                var es = extTarget > 0.5f ? T.extRise : T.extFall;
                l.Ext.Step(extTarget, es.frequency, es.damping, h);
                l.Swinging = extTarget < 0.5f && l.Ext.Value > 0.02f;

                float holdTarget = l.Holding ? 1f : 0f;
                var hs = holdTarget > l.Hold.Value ? T.holdRise : T.holdFall;
                l.Hold.Step(holdTarget, hs.frequency, hs.damping, h);
                maxHold = Mathf.Max(maxHold, l.HoldValue);
            }

            // A press that has become a hold brings the body down: the hold pose has no flight.
            if (HoldingCount > 0 && (HopY > 0f || _hopVelocity > 0f))
            {
                HopY = _hopVelocity = 0f;
                _landTime = Time;
            }

            StepHead(h);
            if (Time - LastPressTime > T.formHoldTime) _form *= Mathf.Exp(-T.formDecay * h);
            _hopBias *= Mathf.Exp(-T.hopBiasDecay * h);

            // Speed. Every held foot pushes: both held push twice, no special case.
            float applied = _pending * (1f - Mathf.Exp(-3f * h / Mathf.Max(0.001f, T.pushTime)));
            _pending -= applied;
            float thrust = 0f;
            int holding = HoldingCount;
            if (holding > 0 && Time - thrustStart < T.holdThrustTime)
                thrust = T.holdThrust * holding * EnergyFactor();
            // Stumbling is lean beyond what holding asks for (every held foot leans on purpose).
            float poseLean = PoseLean = T.lungeLean * (Legs[0].HoldValue + Legs[1].HoldValue);
            // No key down for a while: the runner has stopped running and brakes (constant, like friction).
            float idle = (float)(Time - _keyUpTime) - T.stopDelay;
            float brake = T.stopBrake * Mathf.SmoothStep(0f, 1f, idle / Mathf.Max(0.001f, T.stopRamp));
            float drag = T.dragConstant + T.dragLinear * Speed + T.dragQuadratic * Speed * Speed + brake + T.holdDrag * maxHold
                         + T.headDrag * Mathf.Clamp01(HeadAngle / Mathf.Max(1f, T.headForwardMax));
            if (JumpInAir) drag = 0f;
            Speed = Mathf.Clamp(Speed + applied + (thrust - drag) * h, 0f, T.maxSpeed);
            Distance += Speed * h;

            // Lean.
            float leanTarget = T.runLean * Speed / T.maxSpeed + poseLean;
            LeanSpring.Step(leanTarget, T.lean.frequency, T.lean.damping, h);
            if (LeanSpring.Value > T.maxForwardLean || LeanSpring.Value < -T.maxBackLean)
            {
                LeanSpring.Value = Mathf.Clamp(LeanSpring.Value, -T.maxBackLean, T.maxForwardLean);
                LeanSpring.Velocity = 0f;
            }

            // Energy.
            if (holding > 0) Energy -= T.drainHoldPerSecond * h;
            else if (Speed < 0.1f && !Legs[0].Pressed && !Legs[1].Pressed) Energy += T.regenPerSecond * h;
            Energy = Mathf.Clamp(Energy, 0f, T.energyMax);
        }
    }
}
