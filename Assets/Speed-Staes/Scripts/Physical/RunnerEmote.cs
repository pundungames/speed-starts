using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Plays an upper-body animation over the run. It is only laid on the bones at the end of the frame, after
    /// the pose driver: physics, legs, lean and arms keep running underneath, so the run comes back by itself.
    /// The clock never stops: both keys down (the dive) takes the weight away until the body is up again, and
    /// whatever time is left then plays on.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class RunnerEmote : MonoBehaviour
    {
        [Tooltip("The runner whose keys and head angle cut the animation.")]
        public RunnerBody body;
        [Tooltip("Where the emote key comes from.")]
        public RunnerInput input;
        [Tooltip("Animations in play order: each press takes the next one.")]
        public RunnerEmoteData[] clips;
        [Tooltip("Seconds the body takes to flow into and out of the animation.")]
        public float fadeTime = 0.25f;
        [Tooltip("After both keys were down the animation stays away until the head angle is below this (deg).")]
        public float uprightAngle = 30f;
        [Tooltip("Shows the playing animation's name on screen.")]
        public bool showName = true;

        RunnerEmoteData _clip;
        Transform[] _bones;
        Quaternion[] _base, _written;
        int _next;
        float _clock, _weight;
        bool _playing, _cut;

        void Awake()
        {
            if (!body) body = GetComponent<RunnerBody>();
            if (!input) input = GetComponent<RunnerInput>();
        }

        void OnEnable()
        {
            if (input) input.EmotePressed += Play;
            if (body) body.RunReset += Stop;
        }

        void OnDisable()
        {
            if (input) input.EmotePressed -= Play;
            if (body) body.RunReset -= Stop;
            Stop();
        }

        /// <summary>Starts the next animation; ignored while one is still playing.</summary>
        public void Play()
        {
            if (_clip || clips == null || clips.Length == 0) return;
            var clip = clips[_next];
            _next = (_next + 1) % clips.Length;
            if (!clip || clip.FrameCount == 0) return;
            Bind(clip);
            _clock = 0f;
            _playing = true;
        }

        void Stop()
        {
            if (_clip)
                for (int i = 0; i < _bones.Length; i++)
                    if (_bones[i]) _bones[i].localRotation = _base[i];
            _playing = false;
            _weight = 0f;
            _clip = null;
        }

        void Bind(RunnerEmoteData clip)
        {
            _clip = clip;
            _bones = new Transform[clip.bones.Length];
            _base = new Quaternion[clip.bones.Length];
            _written = new Quaternion[clip.bones.Length];
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < _bones.Length; i++)
            {
                string bone = clip.bones[i];
                _bones[i] = System.Array.Find(all, t => t.name == bone);
                if (_bones[i]) _base[i] = _bones[i].localRotation;
            }
        }

        void Update()
        {
            if (!_clip || body == null || body.Sim == null) return;
            var sim = body.Sim;
            bool both = sim.Legs[0].Pressed && sim.Legs[1].Pressed;
            _cut = both || (_cut && sim.HeadAngle > uprightAngle);

            if (_playing)
            {
                _clock += Time.deltaTime;
                if (_clock >= _clip.Length) _playing = false;
            }
            // Out in time: the fade ends with the clip, it never freezes on the last frame.
            bool wanted = _playing && !_cut && _clip.Length - _clock > fadeTime;
            _weight = Mathf.MoveTowards(_weight, wanted ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, fadeTime));
        }

        void LateUpdate()
        {
            if (!_clip) return;
            float f = Mathf.Clamp(_clock * _clip.frameRate, 0f, _clip.FrameCount - 1);
            int a = Mathf.Min((int)f, _clip.FrameCount - 1), b = Mathf.Min(a + 1, _clip.FrameCount - 1);
            int count = _clip.bones.Length;
            float w = Mathf.SmoothStep(0f, 1f, _weight);

            for (int i = 0; i < count; i++)
            {
                var bone = _bones[i];
                if (!bone) continue;
                // A bone nobody wrote this frame (the hands, the fingers) still holds our value: its own stays as it was.
                Quaternion now = bone.localRotation;
                if (!Same(now, _written[i])) _base[i] = now;
                Quaternion pose = Quaternion.Slerp(_clip.frames[a * count + i], _clip.frames[b * count + i], f - a);
                bone.localRotation = Quaternion.Slerp(_base[i], pose, w);
                _written[i] = bone.localRotation;
            }
            if (!_playing && _weight <= 0f) _clip = null;
        }

        static bool Same(Quaternion a, Quaternion b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;

        void OnGUI()
        {
            if (!showName || !_clip || !_playing) return;
            GUI.Label(new Rect(Screen.width * 0.5f - 150f, 10f, 300f, 24f), $"{_clip.name}  {_clock:0.0} / {_clip.Length:0.0}");
        }
    }
}
