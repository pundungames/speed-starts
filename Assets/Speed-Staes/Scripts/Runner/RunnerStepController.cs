using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Ties Input → Timing → Movement and owns the two logical leg cycles.
    /// Left key drives the left foot, right key the right foot, and the feet move as a pair:
    /// a press steps that foot back while the other comes forward, arriving first.
    /// Holding keeps the pressed foot back and the other at front. On release the back foot
    /// returns to Mid while the front one waits; then the front one comes to Mid too (feet together
    /// on the ground, waiting). Holding both keeps both back.
    /// Every press takes a step from wherever that foot is; its push power comes only from where
    /// that foot is at the tap (front = full, Mid = part, back = none; the same timers the rig visual
    /// reads, so what you see is what you get). Both pressed together push twice.
    /// Move durations are fixed, tuned by hand. The rig visual only reads this.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RunnerStepController : MonoBehaviour
    {
        public enum TapResult { None, Start, Perfect, Good, Early }

        public struct TapRecord
        {
            public int Leg;
            /// <summary>Foot position at the tap: 0 = back point, 0.5 = Mid, 1 = front point.</summary>
            public float Frontness;
            public float Efficiency;
            public TapResult Result;
        }

        public RunnerInput input;
        public RunnerMovement movement;
        public PushEvaluator push = new PushEvaluator();

        [Header("Leg cycle (fixed, seconds)")]
        [Tooltip("Pressed foot → back, started by the tap.")]
        public float stepDuration = 0.12f;
        [Tooltip("Other foot → front, started by the same tap. Shorter than the step: it arrives first.")]
        public float reachDuration = 0.08f;
        [Tooltip("Back foot → Mid after release.")]
        public float returnDuration = 0.25f;
        [Tooltip("Front foot → Mid, starting once the back foot has reached Mid.")]
        public float frontReturnDuration = 0.15f;

        public LegTimings Timings => new LegTimings
        {
            Step = stepDuration,
            Reach = reachDuration,
            Return = returnDuration,
            FrontReturn = frontReturnDuration,
        };

        [Header("Tap rate")]
        [Tooltip("Smoothing of the measured tap rate (0..1, higher = faster response).")]
        [Range(0.05f, 1f)] public float tapRateSmoothing = 0.3f;

        public readonly LegCycle[] Legs = { new LegCycle(), new LegCycle() };

        public bool Started { get; private set; }
        public float RaceTime { get; private set; }
        public int Taps { get; private set; }
        public TapRecord Last { get; private set; }

        public const int HistorySize = 12;
        public readonly TapRecord[] History = new TapRecord[HistorySize];
        public int HistoryCount { get; private set; }

        double _lastTapTime;
        float _avgInterval = float.PositiveInfinity;

        /// <summary>Taps per second; decays when tapping stops.</summary>
        public float TapRate
        {
            get
            {
                if (!Started || float.IsInfinity(_avgInterval)) return 0f;
                float interval = Mathf.Max(_avgInterval, (float)(Time.timeAsDouble - _lastTapTime));
                return 1f / Mathf.Max(0.01f, interval);
            }
        }

        void Start() => ResetRun();

        void Update()
        {
            float dt = Time.deltaTime;
            double now = Time.timeAsDouble;

            while (input.TryDequeue(out var tap))
            {
                if (tap.Pressed) Tap(tap.Leg, tap.Time, now);
                else Release(tap.Leg, tap.Time);
            }

            movement.Tick(dt);
            TickLegs(now);

            if (!Started) return;
            RaceTime += dt;

            if (movement.Idle && IsResting(Legs[0]) && IsResting(Legs[1]))
                Stop(now);
        }

        static bool IsResting(LegCycle c) => c.State == LegState.Ready || c.State == LegState.Grounded;

        void TickLegs(double time)
        {
            var d = Timings;
            // Twice: the front foot's wait ends on the other foot reaching Mid in the same tick.
            for (int pass = 0; pass < 2; pass++)
            {
                Legs[0].Tick(time, d, Legs[1]);
                Legs[1].Tick(time, d, Legs[0]);
            }
        }

        void Release(int leg, double t)
        {
            TickLegs(t);
            Legs[leg].Release(t, Timings);
        }

        void Tap(int leg, double t, double now)
        {
            Taps++;
            var c = Legs[leg];
            var other = Legs[1 - leg];

            if (Started)
            {
                float interval = (float)(t - _lastTapTime);
                _avgInterval = float.IsInfinity(_avgInterval) ? interval : Mathf.Lerp(_avgInterval, interval, tapRateSmoothing);
            }
            _lastTapTime = t;

            // Bring the legs up to the tap moment so a move that ended earlier this frame counts.
            TickLegs(t);
            var d = Timings;
            float frontness = c.Frontness(t, d);
            c.Press(t, d);
            // The other foot comes forward, unless it is held back too (both back).
            if (!other.Holding) other.StartReach(t, d);

            if (!Started)
            {
                Started = true;
                movement.Push(1f);
                Record(leg, 1f, 1f, TapResult.Start);
                return;
            }

            float eff = push.Evaluate(frontness);
            TapResult result = frontness >= 1f ? TapResult.Perfect : eff >= 0.5f ? TapResult.Good : TapResult.Early;
            movement.Push(eff);
            Record(leg, frontness, eff, result);
        }

        void Record(int leg, float frontness, float eff, TapResult result)
        {
            Last = new TapRecord { Leg = leg, Frontness = frontness, Efficiency = eff, Result = result };
            for (int i = HistorySize - 1; i > 0; i--) History[i] = History[i - 1];
            History[0] = Last;
            HistoryCount = Mathf.Min(HistoryCount + 1, HistorySize);
        }

        void Stop(double now)
        {
            Started = false;
            Legs[0].SetGrounded(now);
            Legs[1].SetGrounded(now);
            _avgInterval = float.PositiveInfinity;
        }

        /// <summary>Stand still at the current spot and clear stats.</summary>
        public void ResetRun()
        {
            movement.ResetMotion();
            input.Clear();
            Stop(Time.timeAsDouble);
            RaceTime = 0f;
            Taps = 0;
            HistoryCount = 0;
            Last = default;
        }
    }
}
