using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Emotes;
using SpeedStars.Physical;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Plays the baked emotes (Malik's <c>RunnerEmoteData</c>, T-089) on the menu character as a loop, for the preview in the
    /// emote shop. The one blend point: it runs right after <see cref="MenuIdlePose"/> in LateUpdate and slerps every bone the
    /// clip drives from whatever the idle stance and the rig left there towards the clip's pose, by the fade weight. The clip is
    /// found by the catalog's clip name among the <c>RunnerEmote.clips</c> of the same runner prefab (the data is only read;
    /// the menu runner's own RunnerEmote never plays because its input is off). All timing is in <c>EmotePlayback</c> /
    /// <c>EmoteSampler</c> (pure, tested); this class only finds the bones, listens to <see cref="IEmotePreview"/> and writes
    /// rotations. A bone nobody else resets each frame (head, hands, fingers) keeps its captured base, and is put back when the
    /// clip fades out, so nothing accumulates. Malik's code and the prefab are untouched.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public sealed class MenuEmotePlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("The runner root (HumanCharacterDummy_F instance). Empty = this object.")]
        Transform runner;

        [SerializeField, Min(0f), Tooltip("Seconds the pose takes to flow into a clip and out of it (the same default as the race's RunnerEmote).")]
        float fadeTime = 0.25f;

        sealed class BoneState
        {
            public Transform Bone;
            public Quaternion Base;        // local rotation without the emote
            public Quaternion Written;     // local rotation as this component left it last frame
            public bool Dirty;             // Written differs from what the rig / idle pose would have put there
            public int ClipIndex = -1;     // bone index inside the playing clip; -1 = the clip does not drive it
        }

        readonly EmotePlayback _playback = new EmotePlayback();
        readonly Dictionary<string, BoneState> _byName = new Dictionary<string, BoneState>();
        readonly List<BoneState> _states = new List<BoneState>();
        Transform[] _transforms;
        Transform _root;
        RunnerEmote _source;
        RunnerEmoteData _clip;
        IEmotePreview _preview;
        EmoteCatalog _catalog;
        bool _subscribed;
        bool _warnedNoSource;

        // Optional so a scene without the emote bindings still shows the character; Start warns about it.
        [Inject]
        void Construct([InjectOptional] IEmotePreview preview, [InjectOptional] EmoteCatalog catalog)
        {
            _preview = preview;
            _catalog = catalog;
        }

        void Awake() => Initialize();

        void OnEnable()
        {
            Initialize();
            Subscribe();
        }

        void Start()
        {
            if (_preview == null || _catalog == null)
            {
                UnityEngine.Debug.LogWarning("[MenuEmotePlayer] IEmotePreview / EmoteCatalog are not bound; emotes will not play.", this);
                return;
            }
            Subscribe(); // injection may land after OnEnable in a runtime-instantiated menu
        }

        void OnDisable()
        {
            if (_subscribed && _preview != null)
            {
                _preview.PlayRequested -= Play;
                _preview.StopRequested -= Stop;
            }
            _subscribed = false;
            _playback.Stop();
            Apply(); // puts the bones back before this component goes quiet
        }

        void Subscribe()
        {
            if (_subscribed || _preview == null) return;
            _preview.PlayRequested += Play;
            _preview.StopRequested += Stop;
            _subscribed = true;
        }

        /// <summary>Idempotent: finds the runner's transforms and its clip list once (also callable where AddComponent skips Awake).</summary>
        public void Initialize()
        {
            if (_transforms != null) return;
            _root = runner ? runner : transform;
            _transforms = _root.GetComponentsInChildren<Transform>(true);
            _source = _root.GetComponentInChildren<RunnerEmote>(true);
        }

        /// <summary>Starts an emote by catalog id as a loop (unknown id: nothing; None: the running one fades out).</summary>
        public void Play(string emoteId)
        {
            Initialize();
            if (_catalog == null || !_catalog.TryGet(emoteId, out EmoteDefinition emote)) return;
            RunnerEmoteData clip = emote.HasClip ? FindClip(emote.ClipName) : null;
            if (clip == null)
            {
                _playback.Release(); // None (or a clip the prefab does not carry): the character goes back to its stance
                return;
            }
            Bind(clip);
            _playback.Play(clip.Length, fadeTime, fadeTime, loop: true);
        }

        /// <summary>Fades the running emote out; the bones are back when the fade is done.</summary>
        public void Stop() => _playback.Release();

        RunnerEmoteData FindClip(string clipName)
        {
            if (_source == null || _source.clips == null)
            {
                if (!_warnedNoSource)
                {
                    _warnedNoSource = true;
                    UnityEngine.Debug.LogWarning("[MenuEmotePlayer] The runner has no RunnerEmote with clips; the baked emotes cannot play.", this);
                }
                return null;
            }
            foreach (RunnerEmoteData candidate in _source.clips)
                if (candidate != null && string.Equals(candidate.name, clipName, StringComparison.Ordinal)) return candidate;
            return null;
        }

        void Bind(RunnerEmoteData clip)
        {
            _clip = clip;
            foreach (BoneState state in _states) state.ClipIndex = -1;
            for (int i = 0; i < clip.bones.Length; i++)
            {
                BoneState state = StateOf(clip.bones[i]);
                if (state != null) state.ClipIndex = i;
            }
        }

        BoneState StateOf(string boneName)
        {
            if (_byName.TryGetValue(boneName, out BoneState known)) return known;
            Transform found = null;
            foreach (Transform t in _transforms)
                if (t.name == boneName) { found = t; break; }
            BoneState state = found == null ? null : new BoneState { Bone = found };
            _byName[boneName] = state; // a missing bone is remembered too, so it is searched once
            if (state != null) _states.Add(state);
            return state;
        }

        void LateUpdate()
        {
            _playback.Tick(Time.deltaTime);
            Apply();
        }

        void Apply()
        {
            if (_states.Count == 0) return;
            bool playing = _playback.IsPlaying && _clip != null;
            float weight = playing ? _playback.Weight : 0f;
            int a = 0, b = 0;
            float blend = 0f;
            int count = 0;
            if (playing)
            {
                EmoteSampler.Frame(_playback.ClipTime, _clip.frameRate, _clip.FrameCount, out a, out b, out blend);
                count = _clip.bones.Length;
            }

            foreach (BoneState state in _states)
            {
                bool active = playing && weight > 0f && state.ClipIndex >= 0;
                if (!active && !state.Dirty) continue;

                // Someone else wrote the bone since our last write (idle pose, rig): that value is the new base.
                // Untouched since: our own earlier write stands, so the base is still the captured one.
                Quaternion current = state.Bone.localRotation;
                bool untouched = state.Dirty && Quaternion.Angle(current, state.Written) < 0.001f;
                Quaternion baseRotation = untouched ? state.Base : current;
                state.Base = baseRotation;

                if (active)
                {
                    Quaternion pose = Quaternion.Slerp(_clip.frames[a * count + state.ClipIndex], _clip.frames[b * count + state.ClipIndex], blend);
                    state.Bone.localRotation = Quaternion.Slerp(baseRotation, pose, weight);
                }
                else state.Bone.localRotation = baseRotation;
                state.Written = state.Bone.localRotation;
                state.Dirty = active;
            }
        }
    }
}
