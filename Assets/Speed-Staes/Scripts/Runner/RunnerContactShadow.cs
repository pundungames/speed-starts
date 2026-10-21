using UnityEngine;

namespace SpeedStars.Runner
{
    // This race track is flat. Ground height is independent of the runner's step bob.
    [ExecuteAlways]
    public sealed class RunnerContactShadow : MonoBehaviour
    {
        public Transform target;
        public float groundHeight = 0.012f;
        public Vector2 size = new Vector2(0.65f, 1.05f);

        void LateUpdate()
        {
            if (!target) return;
            var position = target.position;
            position.y = groundHeight;
            transform.SetPositionAndRotation(position, Quaternion.Euler(90, target.eulerAngles.y, 0));
            transform.localScale = new Vector3(size.x, size.y, 1);
        }
    }
}
