using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Side-on race camera: sits to the runner's right and tracks along the run axis only,
    /// so step bob and pelvis sway never shake the frame.
    /// </summary>
    public sealed class SideFollowCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 4.5f;
        public float height = 1.0f;
        [Tooltip("Frame the runner slightly left so more track ahead is visible.")]
        public float leadAhead = 0.6f;
        public float lookHeight = 0.9f;
        public float followSharpness = 12f;

        void LateUpdate()
        {
            if (!target) return;
            Vector3 fwd = target.forward;
            Vector3 right = target.right;
            Vector3 focus = target.position + fwd * leadAhead;
            Vector3 desired = focus + right * distance + Vector3.up * height;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.rotation = Quaternion.LookRotation(-right, Vector3.up)
                * Quaternion.Euler(Mathf.Atan2(height - lookHeight, distance) * Mathf.Rad2Deg, 0f, 0f);
        }
    }
}
