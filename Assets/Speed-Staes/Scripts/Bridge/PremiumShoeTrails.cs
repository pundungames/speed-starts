using SpeedStars.Physical;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedStars.Bridge
{
    /// <summary>World-space ribbons on the feet, enabled only for an equipped premium shoe on a moving runner.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class PremiumShoeTrails : MonoBehaviour
    {
        readonly TrailRenderer[] _trails = new TrailRenderer[2];
        RunnerBody _body;
        Transform _motionRoot;
        Transform _shoe;
        Vector3 _previousPosition;
        bool _hasPosition;
        bool _configured;

        public void Refresh(Transform shoes)
        {
            Clear();
            _shoe = null;
            if (!shoes) return;
            foreach (Transform part in shoes)
                if (part.gameObject.activeSelf && (part.name == "PSH01" || part.name == "PSH02"))
                    _shoe = part;
            if (!_shoe) return;

            _body = GetComponentInParent<RunnerBody>();
            // Menu previews have no RunnerBody and must never emit while rotating.
            if (!_body) return;
            _motionRoot = _body.transform;
            if (!_configured)
            {
                Material material = Resources.Load<Material>("PremiumShoeTrail");
                if (!material) return;
                Transform[] bones = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < 2; i++)
                {
                    string boneName = i == 0 ? "B-foot.L" : "B-foot.R";
                    Transform foot = System.Array.Find(bones, bone => bone.name == boneName);
                    if (!foot) continue;
                    var anchor = new GameObject("Premium Shoe Trail");
                    anchor.layer = gameObject.layer;
                    anchor.transform.SetParent(foot, false);
                    var trail = anchor.AddComponent<TrailRenderer>();
                    trail.sharedMaterial = material;
                    trail.emitting = false;
                    trail.minVertexDistance = 0.035f;
                    trail.widthCurve = AnimationCurve.Linear(0f, 0.13f, 1f, 0f);
                    trail.numCapVertices = 4;
                    trail.numCornerVertices = 3;
                    trail.shadowCastingMode = ShadowCastingMode.Off;
                    trail.receiveShadows = false;
                    _trails[i] = trail;
                }
                _configured = true;
            }

            bool ice = _shoe.name == "PSH01";
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(ice ? new Color(0.55f, 1f, 1f) : new Color(1f, 0.93f, 0.4f), 0f),
                new GradientColorKey(ice ? new Color(0.05f, 0.4f, 1f) : new Color(1f, 0.18f, 0.02f), 1f)
            }, new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            foreach (var trail in _trails)
            {
                if (!trail) continue;
                trail.colorGradient = gradient;
                trail.time = ice ? 0.45f : 0.6f;
            }
        }

        void LateUpdate()
        {
            if (!_motionRoot || !_shoe || !_shoe.gameObject.activeInHierarchy)
            {
                Clear();
                return;
            }
            Vector3 position = _motionRoot.position;
            float distance = _hasPosition ? Vector3.Distance(position, _previousPosition) : 0f;
            // Reset/teleport must not draw a line across the track. Use actual displacement so a paused
            // or stationary runner cannot emit from foot animation alone.
            bool teleport = distance > Mathf.Max(2f, 30f * Time.deltaTime);
            bool moving = _hasPosition && Time.deltaTime > 0f && !teleport
                && distance > 0.2f * Time.deltaTime;
            if (teleport) Clear();
            foreach (var trail in _trails)
                if (trail) trail.emitting = moving;
            _previousPosition = position;
            _hasPosition = true;
        }

        void OnDisable() => Clear();

        void OnDestroy()
        {
            foreach (var trail in _trails)
                if (trail) Destroy(trail.gameObject);
        }

        void Clear()
        {
            _hasPosition = false;
            foreach (var trail in _trails)
            {
                if (!trail) continue;
                trail.emitting = false;
                trail.Clear();
            }
        }
    }
}
