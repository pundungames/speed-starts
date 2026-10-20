using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Ties Input → Timing → Movement and owns the two logical leg cycles.
    /// Left key drives the left foot, right key the right foot: press steps front → back,
    /// holding keeps the foot back, release returns it to front (a quick press-release still
    /// completes the step). Every press takes a step from wherever that foot is; its efficiency
    /// comes only from the timing graph (error against the moment the foot is back on its
    /// front point) and feeds Movement.
    /// Step and return durations are fixed, tuned by hand. The rig visual only reads this.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RunnerStepController : MonoBehaviour
    {
        public enum TapResult { None, Start, Perfect, Good, Late, Early, SameLeg }

        public struct TapRecord
        {
            public int Leg;
            public float Error;
            public float Efficiency;
            public TapResult Result;
        }

        public RunnerInput input;
        public RunnerMovement movement;
        public TimingEvaluator timing = new TimingEvaluator();

        [Header("Leg cycle (fixed, seconds)")]
        [Tooltip("Front → back, started by the tap.")]
        public float stepDuration = 0.12f;
        [Tooltip("Back → front, automatic.")]
        public float returnDuration = 0.25f;

        [Header("Rules")]
        [Tooltip("Efficiency multiplier when the same foot steps twice in a row.")]
        [Range(0f, 1f)] public float sameLegFactor = 0.3f;

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

        int _lastSteppedLeg = -1;
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
                else Legs[tap.Leg].Release(tap.Time);
            }

            movement.Tick(dt);
            Legs[0].Tick(now, stepDuration, returnDuration);
            Legs[1].Tick(now, stepDuration, returnDuration);

            if (!Started) return;
            RaceTime += dt;

            if (movement.Idle && IsResting(Legs[0]) && IsResting(Legs[1]))
                Stop(now);
        }

        static bool IsResting(LegCycle c) => c.State == LegState.Ready || c.State == LegState.Grounded;

        void Tap(int leg, double t, double now)
        {
            Taps++;
            var c = Legs[leg];

            if (Started)
            {
                float interval = (float)(t - _lastTapTime);
                _avgInterval = float.IsInfinity(_avgInterval) ? interval : Mathf.Lerp(_avgInterval, interval, tapRateSmoothing);
            }
            _lastTapTime = t;

            if (!Started)
            {
                Started = true;
                c.StartStep(now);
                movement.Push(1f);
                _lastSteppedLeg = leg;
                Record(leg, 0f, 1f, TapResult.Start);
                return;
            }

            float error = c.State == LegState.Grounded ? 0f : (float)(t - c.ReadyTime(stepDuration, returnDuration));
            c.StartStep(now);

            float eff = timing.Evaluate(error);
            TapResult result = eff >= 0.85f ? TapResult.Perfect : eff >= 0.5f ? TapResult.Good
                : error < 0f ? TapResult.Early : TapResult.Late;
            if (_lastSteppedLeg == leg)
            {
                eff *= sameLegFactor;
                result = TapResult.SameLeg;
            }
            _lastSteppedLeg = leg;
            movement.Push(eff);
            Record(leg, error, eff, result);
        }

        void Record(int leg, float error, float eff, TapResult result)
        {
            Last = new TapRecord { Leg = leg, Error = error, Efficiency = eff, Result = result };
            for (int i = HistorySize - 1; i > 0; i--) History[i] = History[i - 1];
            History[0] = Last;
            HistoryCount = Mathf.Min(HistoryCount + 1, HistorySize);
        }

        void Stop(double now)
        {
            Started = false;
            Legs[0].SetGrounded(now);
            Legs[1].SetGrounded(now);
            _lastSteppedLeg = -1;
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
