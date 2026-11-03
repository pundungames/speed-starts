using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The baton exchange pose of one relay runner, laid over Malik's arm pose: <c>RunnerPoseDriver</c> writes the arms in its
    /// LateUpdate (order -50), this runs after it and turns the upper arm and forearm toward a reach direction for as long as a
    /// reach is asked, easing in and out. Giving: the baton arm reaches forward, the other arm swings back. Taking: the arm that will
    /// hold the baton reaches back. Only on relay runner copies, added at runtime; Malik's files and prefab are not touched, only
    /// his bone names are read (B-upperArm / B-forearm / B-hand .L / .R).
    /// </summary>
    [DefaultExecutionOrder(100)] // after RunnerPoseDriver (-50) has posed the arms
    public sealed class RelayBatonArms : MonoBehaviour
    {
        public enum Reach { None, Give, Take }

        [Tooltip("Seconds to ease into the reach pose and back out of it.")]
        [SerializeField, Min(0.01f)] float easeSeconds = 0.12f;
        [Tooltip("Giving arm: angle below straight forward (deg).")]
        [SerializeField] float giveDown = 25f;
        [Tooltip("Free arm while giving: angle below straight back (deg).")]
        [SerializeField] float giveOtherDown = 35f;
        [Tooltip("Taking arm: upper arm angle below straight back (deg).")]
        [SerializeField] float takeDown = 30f;
        [Tooltip("Taking arm: the forearm bends this much further down at the elbow (deg). Malik's play-mode pose (2026-11-03): 18.7.")]
        [SerializeField] float takeElbow = 18.7f;
        [Tooltip("Arms angled out sideways from the body while reaching (deg), so they clear the trunk.")]
        [SerializeField] float outward = 8f;

        struct Limb
        {
            public Transform Upper, Fore, Hand;
            public bool Valid => Upper && Fore && Hand;
        }

        Limb _left, _right;
        Reach _reach, _pose;
        bool _batonRight;
        float _weight;

        /// <summary>The hand prop bones (B-handProp.L / .R) the baton sits in; null if the rig has none.</summary>
        public Transform HandProp(bool right) => Find(right ? "B-handProp.R" : "B-handProp.L");

        void Awake()
        {
            _left = new Limb { Upper = Find("B-upperArm.L"), Fore = Find("B-forearm.L"), Hand = Find("B-hand.L") };
            _right = new Limb { Upper = Find("B-upperArm.R"), Fore = Find("B-forearm.R"), Hand = Find("B-hand.R") };
        }

        /// <summary>Starts (or ends, with <see cref="Reach.None"/>) a reach; <paramref name="batonRight"/> = the baton is (or will be) in the right hand.</summary>
        public void Set(Reach reach, bool batonRight)
        {
            _reach = reach;
            if (reach == Reach.None) return;
            _pose = reach; // kept while the arms ease back out
            _batonRight = batonRight;
        }

        /// <summary>Back to the plain running arms at once (a new race).</summary>
        public void Clear()
        {
            _reach = Reach.None;
            _weight = 0f;
        }

        void LateUpdate()
        {
            float target = _reach == Reach.None ? 0f : 1f;
            _weight = Mathf.MoveTowards(_weight, target, Time.deltaTime / easeSeconds);
            if (_weight <= 0f) return;

            Transform root = transform;
            Limb baton = _batonRight ? _right : _left;
            Limb other = _batonRight ? _left : _right;
            if (_pose == Reach.Take)
            {
                Point(baton, Back(root, takeDown, _batonRight), Back(root, takeDown + takeElbow, _batonRight), _weight);
                return;
            }
            Point(baton, Forward(root, giveDown, _batonRight), _weight);
            Point(other, Back(root, giveOtherDown, !_batonRight), _weight);
        }

        Vector3 Forward(Transform root, float down, bool right) =>
            Quaternion.AngleAxis(Side(right) * outward, root.forward) * (Quaternion.AngleAxis(down, root.right) * root.forward);

        Vector3 Back(Transform root, float down, bool right) =>
            Quaternion.AngleAxis(Side(right) * outward, root.forward) * (Quaternion.AngleAxis(-down, root.right) * -root.forward);

        static float Side(bool right) => right ? -1f : 1f;

        /// <summary>Turns the upper arm, then the forearm, so the arm points along <paramref name="dir"/> (straight), by <paramref name="w"/>.</summary>
        static void Point(Limb limb, Vector3 dir, float w) => Point(limb, dir, dir, w);

        /// <summary>Upper arm along <paramref name="upper"/>, forearm along <paramref name="fore"/> (a bent elbow), by <paramref name="w"/>.</summary>
        static void Point(Limb limb, Vector3 upper, Vector3 fore, float w)
        {
            if (!limb.Valid) return;
            Turn(limb.Upper, limb.Fore.position - limb.Upper.position, upper, w);
            Turn(limb.Fore, limb.Hand.position - limb.Fore.position, fore, w);
        }

        static void Turn(Transform bone, Vector3 from, Vector3 to, float w)
        {
            if (from.sqrMagnitude < 1e-8f) return;
            Quaternion full = Quaternion.FromToRotation(from, to) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, full, w);
        }

        Transform Find(string boneName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == boneName) return t;
            return null;
        }
    }
}
