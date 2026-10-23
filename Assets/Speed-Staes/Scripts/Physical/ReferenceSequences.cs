using System.Collections.Generic;

namespace SpeedStars.Physical
{
    public struct SequenceEvent
    {
        /// <summary>Seconds from the start of the sequence.</summary>
        public float Time;
        public int Leg;
        public bool Pressed;
    }

    public sealed class ReferenceSequence
    {
        public string Name;
        /// <summary>What the reference video shows for this input (tuning target).</summary>
        public string Expect;
        public SequenceEvent[] Events;
    }

    /// <summary>
    /// Inputs measured from the reference videos of the original game (touch markers on screen),
    /// for replaying into the runner and tuning against the speeds the videos show.
    /// </summary>
    public static class ReferenceSequences
    {
        public const float TapLength = 0.08f;

        public static readonly ReferenceSequence[] All =
        {
            new ReferenceSequence
            {
                Name = "Slow taps (0.6 s)",
                Expect = "~3.4 m/s (7.5 mph)",
                Events = Build(l => Taps(l, 0f, 14, 0.6f)),
            },
            new ReferenceSequence
            {
                Name = "Rhythm (0.32 s)",
                Expect = "~7.8 m/s (17.5 mph) after ~5 s",
                Events = Build(l => Taps(l, 0f, 45, 0.32f)),
            },
            new ReferenceSequence
            {
                Name = "Rhythm, then mash (0.1 s)",
                Expect = "up to ~7 m/s, then falls to ~2.7 m/s while mashing",
                Events = Build(l => { Taps(l, 0f, 30, 0.32f); Taps(l, 30 * 0.32f, 70, 0.1f, 0.04f); }),
            },
            new ReferenceSequence
            {
                Name = "Holds (left, right, both)",
                Expect = "one foot ~1.4 m/s peak, both ~2.2 m/s; swing back past upright on release",
                Events = Build(l =>
                {
                    Hold(l, 0, 0f, 4f);
                    Hold(l, 1, 7f, 4f);
                    Hold(l, 0, 14f, 4f);
                    Hold(l, 1, 14.02f, 4f);
                }),
            },
        };

        static SequenceEvent[] Build(System.Action<List<SequenceEvent>> fill)
        {
            var list = new List<SequenceEvent>();
            fill(list);
            list.Sort((a, b) => a.Time.CompareTo(b.Time));
            return list.ToArray();
        }

        /// <summary>Alternating quick taps, left first.</summary>
        static void Taps(List<SequenceEvent> list, float start, int count, float interval, float length = TapLength)
        {
            for (int i = 0; i < count; i++)
                Hold(list, i % 2, start + i * interval, length);
        }

        static void Hold(List<SequenceEvent> list, int leg, float at, float length)
        {
            list.Add(new SequenceEvent { Time = at, Leg = leg, Pressed = true });
            list.Add(new SequenceEvent { Time = at + length, Leg = leg, Pressed = false });
        }
    }
}
