using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Ties Input → Push power → Movement and owns the two logical leg cycles.
    /// Every press is one stride: the pressed leg drives back, the other reaches forward, and after
    /// strideDuration both feet are together under the body again, waiting with no penalty.
    /// Holding a key does nothing; only the press counts.
    /// Push power comes from readiness = the lower of
    ///   stride: how far the current stride has come (1 = feet together again), what you see;
    ///   charge: time since this key's own last press over two strides, so alternating at the stride
    ///           rhythm is full power and the same key twice in a row is not.
    /// Both keys pressed together (within simultaneousWindow) are one stride and push twice, each by
    /// its own charge. Stride duration is fixed, tuned by hand. The rig visual only reads this.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RunnerStepController : MonoBehaviour
    {
        public enum TapResult { None, Start, Perfect, Good, Early }

        public struct TapRecord
        {
            public int Leg;
            /// <summary>Readiness at the tap (0..1): the lower of stride progress and this key's charge.</summary>
            public float Readiness;
            public float Efficiency;
            public TapResult Result;
        }

        public RunnerInput input;
        public RunnerMovement movement;
        public PushEvaluator push = new PushEvaluator();

        [Header("Stride (fixed, seconds)")]
        [Tooltip("One press: feet together → apart → together again.")]
        public float strideDuration = 0.18f;
        [Tooltip("Presses of both keys closer than this count as pressed together (one stride, two pushes).")]
        public float simultaneousWindow = 0.05f;

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

        readonly double[] _lastPress = { double.NegativeInfinity, double.NegativeInfinity };
        int _strideLeg = -1;
        double _strideStart;
        float _strideBefore;
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

        /// <summary>0 = this key was just pressed, 1 = rested for two strides (full power).</summary>
        public float Charge(int leg, double time) =>
            Mathf.Clamp01((float)((time - _lastPress[leg]) / Mathf.Max(0.001f, 2f * strideDuration)));

        void Start() => ResetRun();

        void Update()
        {
            float dt = Time.deltaTime;
            double now = Time.timeAsDouble;

            while (input.TryDequeue(out var tap))
                if (tap.Pressed) Tap(tap.Leg, tap.Time);

            movement.Tick(dt);
            Legs[0].Tick(now, strideDuration);
            Legs[1].Tick(now, strideDuration);

            if (!Started) return;
            RaceTime += dt;

            if (movement.Idle && IsResting(Legs[0]) && IsResting(Legs[1]))
                Stop(now);
        }

        static bool IsResting(LegCycle c) => c.State == LegState.Ready || c.State == LegState.Grounded;

        void Tap(int leg, double t)
        {
            Taps++;

            if (Started)
            {
                float interval = (float)(t - _lastTapTime);
                _avgInterval = float.IsInfinity(_avgInterval) ? interval : Mathf.Lerp(_avgInterval, interval, tapRateSmoothing);
            }
            _lastTapTime = t;

            // Bring the stride up to the tap moment so one that ended earlier this frame counts.
            Legs[0].Tick(t, strideDuration);
            Legs[1].Tick(t, strideDuration);

            // The other key right after a press joins that stride and is judged as it stood before it.
            bool together = _strideLeg >= 0 && leg != _strideLeg && t - _strideStart <= simultaneousWindow;
            float stride = together ? _strideBefore : Legs[leg].Phase(t, strideDuration);
            float charge = Charge(leg, t);
            _lastPress[leg] = t;
            if (!together)
            {
                _strideBefore = stride;
                _strideStart = t;
                _strideLeg = leg;
                Legs[leg].Begin(LegState.Drive, t);
                Legs[1 - leg].Begin(LegState.Reach, t);
            }

            if (!Started)
            {
                Started = true;
                movement.Push(1f);
                Record(leg, 1f, 1f, TapResult.Start);
                return;
            }

            float readiness = Mathf.Min(stride, charge);
            float eff = push.Evaluate(readiness);
            TapResult result = readiness >= 1f ? TapResult.Perfect : eff >= 0.5f ? TapResult.Good : TapResult.Early;
            movement.Push(eff);
            Record(leg, readiness, eff, result);
        }

        void Record(int leg, float readiness, float eff, TapResult result)
        {
            Last = new TapRecord { Leg = leg, Readiness = readiness, Efficiency = eff, Result = result };
            for (int i = HistorySize - 1; i > 0; i--) History[i] = History[i - 1];
            History[0] = Last;
            HistoryCount = Mathf.Min(HistoryCount + 1, HistorySize);
        }

        void Stop(double now)
        {
            Started = false;
            Legs[0].SetGrounded(now);
            Legs[1].SetGrounded(now);
            _lastPress[0] = _lastPress[1] = double.NegativeInfinity;
            _strideLeg = -1;
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
