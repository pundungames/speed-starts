using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// One finished race, kept as pictures, not inputs: every runner's root and B-* bones as they were drawn, plus the numbers the HUD
    /// shows. A pose replay is exact at any speed or seek, and an old recording survives tuning changes (re-simulating the inputs would
    /// drift: RunnerSim and the pose springs step with the frame time). Time 0 is the start of the race (GO, or the first press).
    /// </summary>
    public sealed class RaceRecording
    {
        /// <summary>Race length (m) when it was recorded.</summary>
        public float RaceDistance;
        /// <summary>One track per runner, in registration order; the player is the one with IsPlayer.</summary>
        public readonly List<RunnerTrack> Tracks = new List<RunnerTrack>();

        /// <summary>Longest track (s).</summary>
        public float Duration
        {
            get
            {
                float d = 0f;
                for (int i = 0; i < Tracks.Count; i++) d = Mathf.Max(d, Tracks[i].Duration);
                return d;
            }
        }
    }

    /// <summary>
    /// One runner's samples. Bones are stored flat: sample s, bone b sits at s * BoneCount + b. Local position and rotation both,
    /// because the hips also move (body height, sitting back) and the playback writes the same locals back.
    /// </summary>
    public sealed class RunnerTrack
    {
        public readonly RunnerBody Body;
        public readonly bool IsPlayer;
        public readonly Transform[] Bones;

        public readonly List<float> Times = new List<float>();
        public readonly List<Vector3> RootPositions = new List<Vector3>();
        public readonly List<Quaternion> RootRotations = new List<Quaternion>();
        public readonly List<Vector3> BonePositions = new List<Vector3>();
        public readonly List<Quaternion> BoneRotations = new List<Quaternion>();
        public readonly List<float> Speeds = new List<float>();
        public readonly List<float> Energies = new List<float>();
        public readonly List<float> Distances = new List<float>();

        /// <summary>Crossing time (s from the race start), interpolated inside the frame; NaN if it never crossed.</summary>
        public float FinishTime = float.NaN;
        /// <summary>No more samples: done after the finish, reset, or the recording stopped.</summary>
        public bool Ended;

        public RunnerTrack(RunnerBody body, bool isPlayer, Transform[] bones)
        {
            Body = body;
            IsPlayer = isPlayer;
            Bones = bones;
        }

        public int BoneCount => Bones.Length;
        public int Count => Times.Count;
        public float Duration => Times.Count > 0 ? Times[Times.Count - 1] : 0f;
        public bool Finished => !float.IsNaN(FinishTime);

        /// <summary>The two samples around time t and how far between them (0..1); before the first or after the last it holds that one.</summary>
        public int IndexAt(float t, out float fraction)
        {
            fraction = 0f;
            int n = Times.Count;
            if (n == 0) return -1;
            if (t <= Times[0]) return 0;
            if (t >= Times[n - 1]) return n - 1;
            int lo = 0, hi = n - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (Times[mid] <= t) lo = mid;
                else hi = mid;
            }
            float span = Times[hi] - Times[lo];
            fraction = span > 0f ? (t - Times[lo]) / span : 0f;
            return lo;
        }

        /// <summary>Puts the root and every bone where they were drawn at time t (interpolated between samples).</summary>
        public void Apply(float t)
        {
            int i = IndexAt(t, out float f);
            if (i < 0 || !Body) return;
            int j = Mathf.Min(i + 1, Count - 1);
            Transform root = Body.transform;
            root.SetPositionAndRotation(Vector3.Lerp(RootPositions[i], RootPositions[j], f),
                Quaternion.Slerp(RootRotations[i], RootRotations[j], f));
            int a = i * BoneCount, b = j * BoneCount;
            for (int k = 0; k < BoneCount; k++)
            {
                if (!Bones[k]) continue;
                Bones[k].localPosition = Vector3.Lerp(BonePositions[a + k], BonePositions[b + k], f);
                Bones[k].localRotation = Quaternion.Slerp(BoneRotations[a + k], BoneRotations[b + k], f);
            }
        }

        /// <summary>The HUD numbers at time t: speed (m/s), energy, distance (m).</summary>
        public void ValuesAt(float t, out float speed, out float energy, out float distance)
        {
            int i = IndexAt(t, out float f);
            if (i < 0)
            {
                speed = energy = distance = 0f;
                return;
            }
            int j = Mathf.Min(i + 1, Count - 1);
            speed = Mathf.Lerp(Speeds[i], Speeds[j], f);
            energy = Mathf.Lerp(Energies[i], Energies[j], f);
            distance = Mathf.Lerp(Distances[i], Distances[j], f);
        }

        /// <summary>The runner's skeleton: every B-* transform under it (the order is fixed for the whole track).</summary>
        public static Transform[] FindBones(Transform root)
        {
            var bones = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("B-")) bones.Add(t);
            return bones.ToArray();
        }
    }
}
