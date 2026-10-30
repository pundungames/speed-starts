using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Emotes;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Plays the procedural emotes on the menu character. The one blend point: it runs right after <see cref="MenuIdlePose"/>
    /// in LateUpdate and turns bones by (emote rotation x weight) on top of whatever the idle stance and the rig left there.
    /// All timing and maths are in <c>EmotePlayback</c> / <c>EmoteSampler</c> (pure, tested); this class only finds the bones,
    /// listens to <see cref="IEmotePreview"/> and writes rotations. A bone nobody else resets each frame (head, hands...)
    /// is put back to its captured value before the next write, and once more when the emote ends, so nothing accumulates.
    /// Malik's code and the prefab are untouched.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public sealed class MenuEmotePlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("The runner root (HumanCharacterDummy_F instance). Empty = this object.")]
        Transform runner;

        // Bone names of the runner rig, in EmoteBone order (parents before children).
        static readonly string[] BoneNames =
        {
            "B-spine", "B-chest", "B-neck", "B-head",
            "B-shoulder.L", "B-upperArm.L", "B-forearm.L", "B-hand.L",
            "B-shoulder.R", "B-upperArm.R", "B-forearm.R", "B-hand.R"
        };

        sealed class BoneState
        {
            public Transform Bone;
            public Quaternion Base;        // local rotation without the emote
            public Quaternion Written;     // local rotation as this component left it last frame
            public bool Dirty;             // Written differs from what the rig / idle pose would have put there
        }

        readonly EmotePlayback _playback = new EmotePlayback();
        BoneState[] _bones;
        Transform _root;
        IEmotePreview _preview;
        EmoteCatalog _catalog;
        bool _subscribed;

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
                Debug.LogWarning("[MenuEmotePlayer] IEmotePreview / EmoteCatalog are not bound; emotes will not play.", this);
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
            Stop();
            Apply(); // puts the bones back before this component goes quiet
        }

        void Subscribe()
        {
            if (_subscribed || _preview == null) return;
            _preview.PlayRequested += Play;
            _preview.StopRequested += Stop;
            _subscribed = true;
        }

        /// <summary>Idempotent: finds the bones once (also callable where AddComponent skips Awake).</summary>
        public void Initialize()
        {
            if (_bones != null) return;
            _root = runner ? runner : transform;
            var all = _root.GetComponentsInChildren<Transform>(true);
            _bones = new BoneState[BoneNames.Length];
            for (int i = 0; i < BoneNames.Length; i++)
            {
                Transform found = null;
                foreach (Transform t in all)
                    if (t.name == BoneNames[i]) { found = t; break; }
                _bones[i] = new BoneState { Bone = found };
            }
        }

        /// <summary>Starts an emote by catalog id (an unknown id does nothing).</summary>
        public void Play(string emoteId)
        {
            Initialize();
            if (_catalog == null || !_catalog.TryGet(emoteId, out EmoteDefinition emote)) return;
            _playback.Play(emote);
        }

        /// <summary>Ends the running emote; the bones go back in the next LateUpdate.</summary>
        public void Stop() => _playback.Stop();

        void LateUpdate()
        {
            _playback.Tick(Time.deltaTime);
            Apply();
        }

        void Apply()
        {
            if (_bones == null) return;
            float weight = _playback.Weight;
            Quaternion rootRot = _root.rotation;
            Quaternion inverse = Quaternion.Inverse(rootRot);

            for (int i = 0; i < _bones.Length; i++)
            {
                BoneState state = _bones[i];
                if (state.Bone == null) continue;
                bool active = _playback.IsPlaying && weight > 0f;
                if (!active && !state.Dirty) continue;

                // Someone else wrote the bone since our last write (idle pose, rig): that value is the new base.
                // Untouched since: our own earlier write stands, so the base is still the captured one.
                Quaternion current = state.Bone.localRotation;
                bool untouched = state.Dirty && Quaternion.Angle(current, state.Written) < 0.001f;
                Quaternion baseRotation = untouched ? state.Base : current;
                state.Base = baseRotation;
                state.Bone.localRotation = baseRotation;

                if (active)
                {
                    Quaternion turn = Quaternion.Euler(_playback.Euler((EmoteBone)i) * weight);
                    // Runner-space rotation as a world rotation, on the bone's own world rotation (children follow).
                    state.Bone.rotation = rootRot * turn * inverse * state.Bone.rotation;
                }
                state.Written = state.Bone.localRotation;
                state.Dirty = active;
            }
        }
    }
}
