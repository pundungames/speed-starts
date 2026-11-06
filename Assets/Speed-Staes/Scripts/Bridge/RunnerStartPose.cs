using SpeedStars.Core.Race;
using SpeedStars.Physical;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The start poses of the character FBX (READY, SET) laid over the driver's computed stance. Added at runtime to a runner copy
    /// (like <c>RelayBatonArms</c>); Malik's prefab is not touched. Both clips are sampled once into local rotations and positions of
    /// the bones they change, then every frame the pose is blended in by the driver's stance weight: Lerp(ready, set, setShare).
    /// It runs after <c>RunnerPoseDriver.LateUpdate</c>, so the rig and the driver have written the skeleton first. With the
    /// weight at 0 (running, braking, holding) it does nothing at all.
    /// </summary>
    [DefaultExecutionOrder(200)] // after RunnerPoseDriver (-50) and RelayBatonArms (100)
    public sealed class RunnerStartPose : MonoBehaviour
    {
        const float PositionEpsilon = 1e-4f;
        const float RotationDotEpsilon = 0.999999f; // ~0.16 deg

        [Tooltip("The runner's pose driver; its stance weight drives the blend. Set by Initialize.")]
        [SerializeField] RunnerPoseDriver driver;

        AnimationClip _ready, _set;
        GameObject _animatorGo;
        bool _sampled;

        // The bones the clips change (all but the animator's own object), with the READY and SET local poses.
        Transform[] _bones;
        Quaternion[] _readyRot, _setRot;
        Vector3[] _readyPos, _setPos;
        bool[] _movesPos;
        // Per bone: the pose nobody drives (the base), and what we wrote last. Equal to what we wrote = nobody else wrote it this frame.
        Quaternion[] _baseRot, _wroteRot;
        Vector3[] _basePos, _wrotePos;
        bool[] _hasWrote;
        bool _active;
        int _count;

        /// <summary>Idempotent: gives the driver and the clips; a missing clip uses the other one. Returns false when there is nothing to lay.</summary>
        public bool Initialize(RunnerPoseDriver poseDriver, AnimationClip ready, AnimationClip set)
        {
            if (!poseDriver || (!ready && !set)) return false;
            if (_sampled && driver == poseDriver && _ready == (ready ? ready : set) && _set == (set ? set : ready)) return true;
            driver = poseDriver;
            _ready = ready ? ready : set;
            _set = set ? set : ready;
            Animator animator = driver.GetComponentInChildren<Animator>(true);
            _animatorGo = animator ? animator.gameObject : null;
            if (_active) Release();
            _sampled = false;
            _count = 0;
            return _animatorGo;
        }

        /// <summary>Adds (once) and sets up the pose layer on this driver's runner from the config's clips; no clips = nothing is added.</summary>
        public static void Ensure(RunnerPoseDriver poseDriver, RaceStartConfig config)
        {
            if (!poseDriver || !config || (!config.ReadyPose && !config.SetPose)) return;
            RunnerStartPose layer = poseDriver.GetComponent<RunnerStartPose>();
            if (!layer) layer = poseDriver.gameObject.AddComponent<RunnerStartPose>();
            layer.Initialize(poseDriver, config.ReadyPose, config.SetPose);
        }

        void LateUpdate()
        {
            if (!driver) return;
            float w = driver.StanceWeight;
            if (w <= 0f)
            {
                if (_active) Release();
                return;
            }
            if (!_sampled) Sample();
            if (_count == 0) return;
            _active = true;

            float set = driver.StanceSetWeight;
            for (int i = 0; i < _count; i++)
            {
                Transform bone = _bones[i];
                Quaternion target = Quaternion.Slerp(_readyRot[i], _setRot[i], set);
                Quaternion cur = bone.localRotation;
                if (!_hasWrote[i] || !SameRot(cur, _wroteRot[i])) _baseRot[i] = cur; // the driver or the rig wrote it: that is the base
                Quaternion rot = Quaternion.Slerp(_baseRot[i], target, w);
                bone.localRotation = rot;
                _wroteRot[i] = rot;
                if (_movesPos[i])
                {
                    Vector3 curPos = bone.localPosition;
                    if (!_hasWrote[i] || curPos != _wrotePos[i]) _basePos[i] = curPos;
                    Vector3 pos = Vector3.Lerp(_basePos[i], Vector3.Lerp(_readyPos[i], _setPos[i], set), w);
                    bone.localPosition = pos;
                    _wrotePos[i] = pos;
                }
                _hasWrote[i] = true;
            }
        }

        void OnDisable()
        {
            if (_active) Release();
        }

        /// <summary>Weight is 0 (or we are off): bones nobody drives go back to their base, so the clip pose does not stay on them.</summary>
        void Release()
        {
            _active = false;
            for (int i = 0; i < _count; i++)
            {
                if (!_hasWrote[i]) continue;
                Transform bone = _bones[i];
                if (bone)
                {
                    if (SameRot(bone.localRotation, _wroteRot[i])) bone.localRotation = _baseRot[i];
                    if (_movesPos[i] && bone.localPosition == _wrotePos[i]) bone.localPosition = _basePos[i];
                }
                _hasWrote[i] = false;
            }
        }

        static bool SameRot(Quaternion a, Quaternion b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;

        /// <summary>
        /// Once: remember every bone's present pose, sample each clip at 0, keep the bones that differ, put everything back. The root
        /// (the animator's own object) is never written, so the runner does not move.
        /// </summary>
        void Sample()
        {
            _sampled = true;
            _count = 0;
            if (!_animatorGo || _ready == null || _set == null) return;

            Transform root = _animatorGo.transform;
            Transform[] all = _animatorGo.GetComponentsInChildren<Transform>(true);
            int n = all.Length;
            var savedRot = new Quaternion[n];
            var savedPos = new Vector3[n];
            Save(all, savedRot, savedPos);

            var rRot = new Quaternion[n]; var rPos = new Vector3[n];
            var sRot = new Quaternion[n]; var sPos = new Vector3[n];
            _ready.SampleAnimation(_animatorGo, 0f);
            Save(all, rRot, rPos);
            Restore(all, savedRot, savedPos);
            _set.SampleAnimation(_animatorGo, 0f);
            Save(all, sRot, sPos);
            Restore(all, savedRot, savedPos);

            int kept = 0;
            for (int i = 0; i < n; i++)
                if (all[i] != root && (Differs(savedRot[i], savedPos[i], rRot[i], rPos[i], out _) || Differs(savedRot[i], savedPos[i], sRot[i], sPos[i], out _)))
                    kept++;
            if (kept == 0)
            {
                Debug.LogWarning("RunnerStartPose: the start pose clips match no bone under " + root.name + " (wrong skeleton or clip paths); the computed stance stays.", this);
                return;
            }

            _bones = new Transform[kept];
            _readyRot = new Quaternion[kept]; _setRot = new Quaternion[kept];
            _readyPos = new Vector3[kept]; _setPos = new Vector3[kept];
            _movesPos = new bool[kept];
            _baseRot = new Quaternion[kept]; _wroteRot = new Quaternion[kept];
            _basePos = new Vector3[kept]; _wrotePos = new Vector3[kept];
            _hasWrote = new bool[kept];
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                if (all[i] == root) continue;
                bool movesR, movesS;
                bool dr = Differs(savedRot[i], savedPos[i], rRot[i], rPos[i], out movesR);
                bool ds = Differs(savedRot[i], savedPos[i], sRot[i], sPos[i], out movesS);
                if (!dr && !ds) continue;
                _bones[k] = all[i];
                _readyRot[k] = rRot[i]; _setRot[k] = sRot[i];
                _readyPos[k] = rPos[i]; _setPos[k] = sPos[i];
                _movesPos[k] = movesR || movesS;
                _baseRot[k] = savedRot[i]; _basePos[k] = savedPos[i];
                k++;
            }
            _count = kept;
        }

        static void Save(Transform[] all, Quaternion[] rot, Vector3[] pos)
        {
            for (int i = 0; i < all.Length; i++)
            {
                rot[i] = all[i].localRotation;
                pos[i] = all[i].localPosition;
            }
        }

        static void Restore(Transform[] all, Quaternion[] rot, Vector3[] pos)
        {
            for (int i = 0; i < all.Length; i++)
            {
                all[i].localRotation = rot[i];
                all[i].localPosition = pos[i];
            }
        }

        static bool Differs(Quaternion rot, Vector3 pos, Quaternion otherRot, Vector3 otherPos, out bool movesPos)
        {
            movesPos = (otherPos - pos).sqrMagnitude > PositionEpsilon * PositionEpsilon;
            return movesPos || Mathf.Abs(Quaternion.Dot(rot, otherRot)) < RotationDotEpsilon;
        }
    }
}
