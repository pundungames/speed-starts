using SpeedStars.Physical;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Where a runner's root really is. RunnerBody writes start + forward x the sim's distance every frame and its start is private, so
    /// this comes right after it and before the pose driver reads the root: the lane is added on top (every runner starts on the road's
    /// middle line and goes to its lane from here, a new lane each race) and, after the line, the finish's own distance stands in for
    /// the sim's: the sim has no way to stop quickly, and a body sliding on planted feet is what that would look like. A replay writes the
    /// root itself and has the body off: nothing is added then. Malik's code is untouched.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class RunnerRootOffset : MonoBehaviour
    {
        RunnerBody _body;
        Vector3 _lane;
        bool _holdsDistance;
        float _distance;

        /// <summary>The offset component of this runner, added on first use.</summary>
        public static RunnerRootOffset Of(RunnerBody body)
        {
            RunnerRootOffset offset = body.GetComponent<RunnerRootOffset>();
            if (offset) return offset;
            offset = body.gameObject.AddComponent<RunnerRootOffset>();
            offset._body = body;
            body.RunReset += offset.OnRunReset;
            return offset;
        }

        void OnDestroy()
        {
            if (_body) _body.RunReset -= OnRunReset;
        }

        /// <summary>The lane, as an offset from the road's middle line. Takes effect now: whoever reads the root next finds it there.</summary>
        public void SetLane(Vector3 offset)
        {
            transform.position += offset - _lane;
            _lane = offset;
        }

        /// <summary>From now on the root is this far along the track, whatever the sim says, until the next reset.</summary>
        public void HoldDistance(float meters)
        {
            _holdsDistance = true;
            _distance = meters;
        }

        // RunnerBody has just put the root on its start line.
        void OnRunReset()
        {
            _holdsDistance = false;
            transform.position += _lane;
        }

        void Update()
        {
            if (!_body || !_body.enabled || _body.Sim == null) return;
            Vector3 position = transform.position + _lane; // RunnerBody wrote its own this frame (-100)
            if (_holdsDistance) position -= transform.forward * (_body.Sim.Distance - _distance);
            transform.position = position;
        }
    }
}
