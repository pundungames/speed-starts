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
        [Tooltip("Top speed (m/s): pushes and the hold thrust give less and less toward it, nothing at it.")]
        public float maxSpeed = 10.5f;
        [Tooltip("Speed gained by a full push at 0 m/s; shrinks toward maxSpeed.")]
        public float pushGain = 1.2f;
        [Tooltip("Time over which a push's gain is applied (s).")]
        public float pushTime = 0.12f;
        [Tooltip("Constant loss (m/s²).")]
        public float dragConstant = 0.64f;
        [Tooltip("Loss growing with speed (per second).")]
        public float dragLinear = 0.05f;

        [Header("Stride")]
        [Tooltip("Press → take-off: how long the foot pushes on the ground (s).")]
        public float stanceTime = 0.08f;
        [Tooltip("Take-off vertical speed of a full push at max speed (m/s). Flight time = 2 × this / gravity. With " +
                 "stanceTime it sets the rhythm: 0.08 s push + ~0.1 s flight = a press every ~0.2 s lands on time.")]
        public float hopVelocity = 0.35f;
        [Tooltip("Share of hopVelocity at 0 m/s.")]
        [Range(0f, 1f)] public float hopAtRest = 0.8f;
        [Tooltip("Gravity (m/s²): the hop between strides here, and the body's fall in a dive (RunnerPoseDriver).")]
        public float gravity = 7f;

        [Header("Push efficiency (read from the physical state, no timing curve)")]
        [Tooltip("A press this soon (s) after the other foot's is not early: the two push together. Later in " +
                 "the other foot's stance it is early. (Fast runs in the videos press 0.05–0.08 s apart.)")]
        public float togetherWindow = 0.08f;
        [Tooltip("Efficiency of a press in the air or while the other foot is still pushing.")]
        [Range(0f, 1f)] public float earlyEfficiency = 0.15f;
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
        [Tooltip("Same leg twice in a row within a second.")]
        [Range(0f, 1f)] public float sameLegFactor = 0.4f;

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
        [Tooltip("How far the first moment of a key down takes the head forward (deg, all of it within ~0.1 s), " +
                 "whatever the press, from wherever the head is.")]
        public float headKick = 12f;
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

        [Header("Head → speed: the further forward the head, the less a press gives and the more the runner loses")]
        [Tooltip("Head angle (deg) at which a press gives nothing; upright or back a press gives all. In between it " +
                 "falls off smoothly (no threshold): 1 − (head / headSpoil)^headSpoilPower.")]
        public float headSpoil = 90f;
        [Tooltip("Shape of that fall-off: 1 = straight; above 1 a slightly forward head costs less, a far forward one as much.")]
        public float headSpoilPower = 1.1f;
        [Tooltip("Extra loss with the head all the way forward (m/s² at headForwardMax, in proportion below).")]
        public float headDrag = 1.6f;

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

        public void Reset()
        {
            Pressed = Holding = Swinging = false;
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
    /// the head comes up again. No threshold anywhere.
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
        /// <summary>Where the head rests with no key down (deg): headBack while running, upright standing.</summary>
        public float HeadRest => T.headBack * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(T.headBackSpeed.x, T.headBackSpeed.y, Speed));
        /// <summary>How much of its push a press gets at this head angle (1 upright or back, 0 at headSpoil).</summary>
        public float HeadUpright => 1f - Mathf.Pow(Mathf.Clamp01(HeadAngle / Mathf.Max(1f, T.headSpoil)), T.headSpoilPower);
        /// <summary>The part of the lean the hold poses ask for (deg); Lean − PoseLean beyond stumbleLean is stumbling.</summary>
        public float PoseLean { get; private set; }
        public bool Airborne => HopY > 0f;
        public int HoldingCount => (Legs[0].Holding ? 1 : 0) + (Legs[1].Holding ? 1 : 0);

        float _pending, _hopVelocity, _launchVelocity;
        Spring _head, _headFwd;   // the head angle; where the keys have taken it, coming back with no key down
        double _keyUpTime;        // sim time the last key down came up
        double _launchTime, _landTime;

        public RunnerSim(RunnerTuning tuning)
        {
            T = tuning;
            Reset();
        }

        public void Reset()
        {
            Time = 0.0;
            Speed = Distance = HopY = _hopVelocity = _pending = 0f;
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
            if (Speed < 0.3f && !Airborne && !launchPending)
            {
                result = PressResult.Start;
                eff = 1f;
            }
            else if (Airborne || (launchPending && !together))
            {
                result = PressResult.Early;
                eff = T.earlyEfficiency;
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
            eff *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(T.legReadyExt.x, T.legReadyExt.y, l.Ext.Value));
            if (LastLeg == leg && sinceLast < 1f) eff *= T.sameLegFactor;
            eff *= EnergyFactor();
            // A head that is down spoils the push: it comes back in full only as the head comes up.
            eff *= HeadUpright;
            _head.Velocity += T.headNod;

            float room = Mathf.Max(0f, 1f - Speed / T.maxSpeed);
            _pending += T.pushGain * eff * room;

            LeanSpring.Velocity += T.leanKick * (1f + T.stumbleKick * (1f - eff));

            if (result == PressResult.Start || result == PressResult.Good || result == PressResult.Late)
            {
                _launchTime = Time + T.stanceTime;
                _launchVelocity = T.hopVelocity * eff * Mathf.Lerp(T.hopAtRest, 1f, Speed / T.maxSpeed);
            }

            Energy = Mathf.Max(0f, Energy - T.drainPerPush);
            l.Pressed = true;
            l.PressTime = Time;
            LastLeg = leg;
            LastPressTime = Time;
            LastResult = result;
            LastEfficiency = eff;
            Presses++;
        }

        public void Release(int leg)
        {
            var l = Legs[leg];
            if (!l.Pressed) return;
            l.Pressed = false;
            LeanSpring.Velocity -= T.releaseKick * l.HoldValue;
        }

        /// <summary>
        /// The head angle: while a key is down it goes forward — headKick at once (within ~0.1 s), then on at
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
                push += T.headKick * (Mathf.Exp(-t0 / PressBurst) - Mathf.Exp(-t / PressBurst)) + T.holdPush * l.WeightValue * h;
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
            _head.Step(_headFwd.Value, T.headSpring.frequency, T.headSpring.damping, h);
        }

        float EnergyFactor() =>
            Energy >= T.lowEnergy ? 1f : Mathf.Lerp(T.lowEnergyFactor, 1f, Energy / Mathf.Max(0.01f, T.lowEnergy));

        void Step(float h)
        {
            Time += h;

            // Take-off at the end of the stance; a held foot stays down (hold pose, no flight).
            if (_launchTime >= 0.0 && Time >= _launchTime)
            {
                _launchTime = -1.0;
                bool held = LastLeg >= 0 && Legs[LastLeg].Pressed;
                if (!held && _launchVelocity > 0.05f) _hopVelocity = _launchVelocity;
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

            StepHead(h);

            // Speed. Every held foot pushes: both held push twice, no special case.
            float applied = _pending * (1f - Mathf.Exp(-3f * h / Mathf.Max(0.001f, T.pushTime)));
            _pending -= applied;
            float thrust = 0f;
            int holding = HoldingCount;
            if (holding > 0 && Time - thrustStart < T.holdThrustTime)
                thrust = T.holdThrust * holding * Mathf.Max(0f, 1f - Speed / T.maxSpeed) * EnergyFactor();
            // Stumbling is lean beyond what holding asks for (every held foot leans on purpose).
            float poseLean = PoseLean = T.lungeLean * (Legs[0].HoldValue + Legs[1].HoldValue);
            float drag = T.dragConstant + T.dragLinear * Speed + T.holdDrag * maxHold
                         + T.headDrag * Mathf.Clamp01(HeadAngle / Mathf.Max(1f, T.headForwardMax));
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
