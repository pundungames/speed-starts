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
        /// <summary>Second finger of a two-finger press (dive).</summary>
        Both,
    }

    /// <summary>All numbers of the runner's reduced physics. Defaults are tuned to the reference videos.</summary>
    [System.Serializable]
    public sealed class RunnerTuning
    {
        [Header("Speed (m/s)")]
        public float maxSpeed = 10.5f;
        [Tooltip("Speed gained by a full push at 0 m/s; shrinks toward maxSpeed.")]
        public float pushGain = 1.4f;
        [Tooltip("Time over which a push's gain is applied (s).")]
        public float pushTime = 0.12f;
        [Tooltip("Constant loss (m/s²).")]
        public float dragConstant = 0.64f;
        [Tooltip("Loss growing with speed (per second).")]
        public float dragLinear = 0.05f;

        [Header("Stride")]
        [Tooltip("Press → take-off: how long the foot pushes on the ground (s).")]
        public float stanceTime = 0.12f;
        [Tooltip("Take-off vertical speed of a full push at max speed (m/s). Flight time = 2 × this / gravity.")]
        public float hopVelocity = 1f;
        [Tooltip("Share of hopVelocity at 0 m/s.")]
        [Range(0f, 1f)] public float hopAtRest = 0.8f;
        public float gravity = 9.81f;

        [Header("Push efficiency (read from the physical state, no timing curve)")]
        [Tooltip("Two presses closer than this (s) count as one two-finger press.")]
        public float simultaneousWindow = 0.06f;
        [Tooltip("Efficiency of a press in the air or while the other foot is still pushing.")]
        [Range(0f, 1f)] public float earlyEfficiency = 0.15f;
        [Tooltip("After landing, presses within this time (s) are full.")]
        public float lateGrace = 0.08f;
        [Tooltip("Time after landing (s) where a late press reaches lateFloor.")]
        public float lateFloorTime = 0.45f;
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
        [Tooltip("Forward push while held (m/s²), for holdThrustTime after the hold starts.")]
        public float holdThrust = 1f;
        public float holdThrustTime = 0.6f;
        [Tooltip("Both feet held push this many times harder.")]
        public float bothMultiplier = 1.5f;
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
        public float lungeLean = 40f;
        public float diveLean = 75f;
        [Tooltip("Lean at max speed.")]
        public float runLean = 8f;
        [Tooltip("Forward lean kick of a push (deg/s).")]
        public float leanKick = 40f;
        [Tooltip("A wasted press kicks harder: × (1 + this × (1 − efficiency)).")]
        public float stumbleKick = 3f;
        [Tooltip("Backward kick when a hold is let go (deg/s × hold weight): the bent support leg straightens.")]
        public float releaseKick = 140f;
        [Tooltip("Lean past this (deg) is stumbling: extra loss.")]
        public float stumbleLean = 30f;
        [Tooltip("Loss per degree past stumbleLean (m/s²).")]
        public float stumbleDrag = 0.2f;
        public float maxForwardLean = 85f;
        public float maxBackLean = 45f;

        [Header("Energy")]
        public float energyMax = 100f;
        public float drainPerPush = 0.25f;
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

        public float ExtValue => Mathf.Clamp01(Ext.Value);
        public float HoldValue => Mathf.Clamp01(Hold.Value);

        public void Reset()
        {
            Pressed = Holding = Swinging = false;
            PressTime = -10.0;
            Ext.Reset();
            Hold.Reset();
        }
    }

    /// <summary>
    /// The runner as a handful of physical quantities: forward speed (mass + impulses + losses), one
    /// extension and one hold spring per leg, a whole-body lean spring and a vertical hop under gravity.
    /// How good a press is comes only from this state: is the body on the ground, how long since it
    /// landed, is the pressed leg back under the body. Mashing therefore wastes presses and their lean
    /// kicks pile up until the runner stumbles; a release lets the lean spring swing back past upright.
    /// Pure C#: no scene, no input, steps itself in small fixed sub-steps.
    /// </summary>
    public sealed class RunnerSim
    {
        const float SubStep = 1f / 240f;

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
        /// <summary>The part of the lean the hold poses ask for (deg); Lean − PoseLean beyond stumbleLean is stumbling.</summary>
        public float PoseLean { get; private set; }
        public bool Airborne => HopY > 0f;
        public int HoldingCount => (Legs[0].Holding ? 1 : 0) + (Legs[1].Holding ? 1 : 0);

        float _pending, _hopVelocity, _launchVelocity;
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
            PressResult result;
            float eff;
            if (LastLeg >= 0 && LastLeg != leg && sinceLast < T.simultaneousWindow)
            {
                result = PressResult.Both;
                eff = LastEfficiency;
            }
            else if (Speed < 0.3f && !Airborne && !launchPending)
            {
                result = PressResult.Start;
                eff = 1f;
            }
            else if (Airborne || launchPending)
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

            if (result != PressResult.Both)
            {
                // The pressed leg must be back under the body to push again.
                eff *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(T.legReadyExt.x, T.legReadyExt.y, l.Ext.Value));
                if (LastLeg == leg && sinceLast < 1f) eff *= T.sameLegFactor;
            }
            eff *= EnergyFactor();

            float room = Mathf.Max(0f, 1f - Speed / T.maxSpeed);
            if (result == PressResult.Both)
                _pending += T.pushGain * eff * room * (T.bothMultiplier - 1f);
            else
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

            // Speed.
            float applied = _pending * (1f - Mathf.Exp(-3f * h / Mathf.Max(0.001f, T.pushTime)));
            _pending -= applied;
            float thrust = 0f;
            int holding = HoldingCount;
            if (holding > 0 && Time - thrustStart < T.holdThrustTime)
                thrust = T.holdThrust * (holding == 2 ? T.bothMultiplier : 1f)
                         * Mathf.Max(0f, 1f - Speed / T.maxSpeed) * EnergyFactor();
            // Stumbling is lean beyond what the pose asks for (a hold pose leans on purpose).
            float hl = Legs[0].HoldValue, hr = Legs[1].HoldValue;
            float both = hl * hr;
            float one = hl * (1f - hr) + hr * (1f - hl);
            float poseLean = PoseLean = T.lungeLean * one + T.diveLean * both;
            float drag = T.dragConstant + T.dragLinear * Speed + T.holdDrag * maxHold
                         + Mathf.Max(0f, Lean - poseLean - T.stumbleLean) * T.stumbleDrag;
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
