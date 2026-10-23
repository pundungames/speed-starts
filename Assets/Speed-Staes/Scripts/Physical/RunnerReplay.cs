using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Plays a reference input sequence (measured from the videos) into RunnerInput with exact
    /// timestamps, as if a player pressed it. Keys 1–4 start one (from a reset run), Esc stops.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class RunnerReplay : MonoBehaviour
    {
        public RunnerInput input;
        public RaceController race;

        public ReferenceSequence Playing { get; private set; }
        public float Elapsed => Playing == null ? 0f : (float)(Time.timeAsDouble - _start);

        double _start;
        int _next;

        void Awake()
        {
            if (!input) input = GetComponent<RunnerInput>();
            if (!race) race = FindObjectOfType<RaceController>();
        }

        public void Play(int index)
        {
            if (index < 0 || index >= ReferenceSequences.All.Length) return;
            if (race) race.ResetRace();
            Playing = ReferenceSequences.All[index];
            _start = Time.timeAsDouble + 0.5; // a moment to settle after the reset
            _next = 0;
        }

        public void Stop()
        {
            // Let go of anything still held.
            if (Playing != null && input)
                for (int leg = 0; leg < 2; leg++) input.Inject(leg, false, Time.timeAsDouble);
            Playing = null;
        }

        void Update()
        {
            for (int i = 0; i < ReferenceSequences.All.Length; i++)
                if (DebugKeys.Down(KeyCode.Alpha1 + i)) Play(i);
            if (DebugKeys.Down(KeyCode.Escape)) Stop();

            if (Playing == null || !input) return;
            double now = Time.timeAsDouble;
            var events = Playing.Events;
            while (_next < events.Length && _start + events[_next].Time <= now)
            {
                var e = events[_next++];
                input.Inject(e.Leg, e.Pressed, _start + e.Time);
            }
            if (_next >= events.Length) Playing = null;
        }
    }
}
