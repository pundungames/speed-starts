using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Menu-only relaxed stance: arms hang by the sides with a slight elbow bend instead of the prefab's T-pose.
    /// The runner pose driver is switched off in the menu, so this writes the arm bones itself in LateUpdate
    /// (after the rig has evaluated). Same recipe as the driver's PoseArm: the swing is built in runner space and
    /// applied on top of each bone's captured rest, so it works whatever the bone axes are. Malik's code is not touched.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class MenuIdlePose : MonoBehaviour
    {
        [SerializeField, Tooltip("The runner root (HumanCharacterDummy_F instance). Empty = this object.")]
        Transform runner;

        [SerializeField, Tooltip("How far each arm hangs away from the body side (deg). 0 = straight down.")]
        float armOutward = 8f;

        [SerializeField, Tooltip("Arm swing forward (+) / back (-) in the sagittal plane (deg).")]
        float armSwing = 0f;

        [SerializeField, Tooltip("Elbow bend forward (deg).")]
        float elbowBend = 14f;

        [SerializeField, Tooltip("Idle breathing: chest tilt amplitude (deg). 0 = static stance.")]
        float breathChest = 0.8f;

        [SerializeField, Tooltip("Idle breathing: arm lift amplitude (deg), the shoulders ride the chest.")]
        float breathArm = 0.8f;

        [SerializeField, Tooltip("Breaths per second.")]
        float breathRate = 0.22f;

        sealed class Arm
        {
            public Transform upper, fore, parent;
            public Quaternion restUpperLocal, restForeLocal;
            public Vector3 restDir; // upper arm -> forearm, runner space, at rest
            public float side;      // +1 right, -1 left
        }

        readonly Arm _left = new Arm { side = -1f };
        readonly Arm _right = new Arm { side = 1f };
        Transform _root, _chest;
        Quaternion _restChestLocal;
        bool _bound;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        /// <summary>Idempotent: captures the rest pose once (also callable where AddComponent skips Awake).</summary>
        public void Initialize()
        {
            if (_bound) return;
            _root = runner ? runner : transform;
            bool l = Bind(_left, "B-upperArm.L", "B-forearm.L");
            bool r = Bind(_right, "B-upperArm.R", "B-forearm.R");
            _chest = FindDeep(_root, "B-chest");
            if (_chest) _restChestLocal = _chest.localRotation;
            _bound = l || r;
        }

        bool Bind(Arm a, string upperName, string foreName)
        {
            a.upper = FindDeep(_root, upperName);
            a.fore = FindDeep(_root, foreName);
            if (!a.upper || !a.fore) return false;
            a.parent = a.upper.parent;
            a.restUpperLocal = a.upper.localRotation;
            a.restForeLocal = a.fore.localRotation;
            Quaternion toRunner = Quaternion.Inverse(_root.rotation);
            a.restDir = (toRunner * (a.fore.position - a.upper.position)).normalized;
            return true;
        }

        void LateUpdate()
        {
            if (!_bound) { Initialize(); if (!_bound) return; }
            float breath = Mathf.Sin(Time.time * breathRate * Mathf.PI * 2f);
            if (_chest) _chest.localRotation = _restChestLocal * Quaternion.Euler(breathChest * breath, 0f, 0f);
            Pose(_left, breath);
            Pose(_right, breath);
        }

        void Pose(Arm a, float breath)
        {
            if (a.upper == null || a.fore == null) return;
            float o = (armOutward + breathArm * breath) * Mathf.Deg2Rad;
            Quaternion hang = Quaternion.FromToRotation(a.restDir, new Vector3(a.side * Mathf.Sin(o), -Mathf.Cos(o), 0f));
            Quaternion rootRot = _root.rotation;
            Quaternion inv = Quaternion.Inverse(rootRot);

            // Runner-space rotation R as a world rotation: root * R * root^-1, applied on the bone's rest world rotation.
            Quaternion upperR = rootRot * Quaternion.Euler(-armSwing, 0f, 0f) * hang * inv;
            Quaternion foreR = rootRot * Quaternion.Euler(-elbowBend, 0f, 0f) * inv;

            a.upper.localRotation = a.restUpperLocal;
            a.upper.rotation = upperR * a.upper.rotation;
            a.fore.localRotation = a.restForeLocal;
            a.fore.rotation = foreR * (a.upper.rotation * a.restForeLocal);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
