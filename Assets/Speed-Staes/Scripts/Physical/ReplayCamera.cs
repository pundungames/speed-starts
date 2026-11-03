using System;
using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// The replay's cameras, one shot at a time, switched by a button (a cut, never automatic: the reference game changes shot only when
    /// asked). Every shot sits in the followed runner's own frame (x = his right, y = up, z = ahead, from the root) and looks at a point
    /// on him, so lane, side and direction come from the runner and the shots work on any track. A shot may swing from its start place
    /// to its run place over the first metres (the default broadcast shot: from in front at the blocks to the side once running).
    /// Driven by <see cref="RacePlayback"/>: it takes the scene camera while a replay plays and gives it back after.
    /// </summary>
    public sealed class ReplayCamera : MonoBehaviour
    {
        [Serializable]
        public sealed class Shot
        {
            [Tooltip("Name, for the Inspector and the debug log.")]
            public string name = "Shot";
            [Tooltip("Camera place at the start line, in the runner's frame (m): x = his right, y = up, z = ahead.")]
            public Vector3 startOffset = new Vector3(6f, 2f, 0f);
            [Tooltip("Camera place once running (m, same frame). Equal to startOffset for a shot that never swings.")]
            public Vector3 runOffset = new Vector3(6f, 2f, 0f);
            [Tooltip("Distance run (m) over which the camera swings from startOffset to runOffset (eased).")]
            public float swingDistance = 0f;
            [Tooltip("Height on the runner the camera looks at (m above the root).")]
            public float lookHeight = 0.9f;
            [Tooltip("How far ahead of the runner the camera looks (m); negative looks behind him.")]
            public float lookAhead = 0f;
            [Tooltip("Vertical field of view (deg). Small = long lens, flat and far.")]
            public float fieldOfView = 40f;
            [Tooltip("How tightly the camera follows (per second). Higher = stuck to the runner, lower = lags behind and catches up.")]
            public float followSharpness = 10f;
        }

        [Tooltip("The shots the camera button steps through, in order. The first is the one a replay opens with.")]
        public Shot[] shots = DefaultShots();

        public int Index { get; private set; }
        public Shot Current => shots.Length > 0 ? shots[Mathf.Clamp(Index, 0, shots.Length - 1)] : null;

        Camera _camera;
        Vector3 _position;
        bool _snap;
        float _savedFov;

        /// <summary>Takes this camera over (its FOV is given back by Release).</summary>
        public void Take(Camera camera)
        {
            _camera = camera;
            if (_camera) _savedFov = _camera.fieldOfView;
            _snap = true;
        }

        public void Release()
        {
            if (_camera) _camera.fieldOfView = _savedFov;
            _camera = null;
        }

        /// <summary>Next (+1) or previous (-1) shot; a cut.</summary>
        public void Step(int step)
        {
            if (shots.Length == 0) return;
            Index = ((Index + step) % shots.Length + shots.Length) % shots.Length;
            _snap = true;
        }

        public void ResetShot()
        {
            Index = 0;
            _snap = true;
        }

        /// <summary>A jump in the replay clock or another runner: no catching up through the field.</summary>
        public void Snap() => _snap = true;

        /// <summary>Frames the runner at this root (position and facing) having run this far (m). Called after the replay has posed him.</summary>
        public void Frame(Transform runner, float distance, float deltaTime)
        {
            Shot shot = Current;
            if (!_camera || !runner || shot == null) return;

            float swing = shot.swingDistance > 0f ? Mathf.SmoothStep(0f, 1f, distance / shot.swingDistance) : 1f;
            Vector3 offset = Vector3.Lerp(shot.startOffset, shot.runOffset, swing);
            Vector3 wanted = runner.position + runner.right * offset.x + Vector3.up * offset.y + runner.forward * offset.z;
            float t = _snap ? 1f : 1f - Mathf.Exp(-shot.followSharpness * deltaTime);
            _position = Vector3.Lerp(_position, wanted, t);
            _snap = false;

            Vector3 look = runner.position + Vector3.up * shot.lookHeight + runner.forward * shot.lookAhead;
            Transform cam = _camera.transform;
            cam.position = _position;
            Vector3 dir = look - _position;
            if (dir.sqrMagnitude > 1e-6f) cam.rotation = Quaternion.LookRotation(dir, Vector3.up);
            _camera.fieldOfView = shot.fieldOfView;
        }

        /// <summary>The eight shots seen in the reference game's replay (Nov 2026 phone capture), in its order where it was seen.</summary>
        public static Shot[] DefaultShots() => new[]
        {
            new Shot { name = "Broadcast", startOffset = new Vector3(5f, 3.5f, 7f), runOffset = new Vector3(8f, 3f, 1.5f), swingDistance = 20f,
                lookHeight = 0.8f, fieldOfView = 40f, followSharpness = 4f },
            new Shot { name = "Wide side", startOffset = new Vector3(18f, 5f, 2f), runOffset = new Vector3(18f, 5f, 2f),
                lookHeight = 0.9f, lookAhead = 1f, fieldOfView = 35f, followSharpness = 6f },
            new Shot { name = "Close low side", startOffset = new Vector3(3f, 0.5f, 0.6f), runOffset = new Vector3(3f, 0.5f, 0.6f),
                lookHeight = 0.9f, lookAhead = 0.4f, fieldOfView = 55f, followSharpness = 12f },
            new Shot { name = "Low behind", startOffset = new Vector3(0.9f, 1.3f, -3f), runOffset = new Vector3(0.9f, 1.3f, -3f),
                lookHeight = 1f, lookAhead = 6f, fieldOfView = 55f, followSharpness = 10f },
            new Shot { name = "High crane", startOffset = new Vector3(8f, 10f, -8f), runOffset = new Vector3(8f, 10f, -8f),
                lookHeight = 0.5f, lookAhead = 2f, fieldOfView = 40f, followSharpness = 5f },
            new Shot { name = "Front", startOffset = new Vector3(1.5f, 2.5f, 7f), runOffset = new Vector3(1.5f, 2.5f, 7f),
                lookHeight = 0.9f, fieldOfView = 45f, followSharpness = 8f },
            new Shot { name = "Overhead", startOffset = new Vector3(2f, 24f, -3f), runOffset = new Vector3(2f, 24f, -3f),
                lookHeight = 0f, lookAhead = 1f, fieldOfView = 40f, followSharpness = 5f },
            new Shot { name = "Far tele", startOffset = new Vector3(1f, 6f, -40f), runOffset = new Vector3(1f, 6f, -40f),
                lookHeight = 1f, lookAhead = 4f, fieldOfView = 10f, followSharpness = 6f },
        };
    }
}
