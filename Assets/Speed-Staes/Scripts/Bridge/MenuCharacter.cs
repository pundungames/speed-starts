using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Keeps the runner prefab instance standing still in the main menu. Switches off everything that makes it run
    /// (input, sim, replay, tuning HUD) and the pose driver: with no driver the rig targets stay at the prefab's own
    /// rest, which is the upright stance; the driver's "Ready" is the starting-block crouch, wrong for a menu.
    /// Only components on the instance are toggled; Malik's code and the prefab are untouched. Cosmetic slots
    /// (Accessories/*) are not touched here.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MenuCharacter : MonoBehaviour
    {
        [SerializeField, Tooltip("The runner root (HumanCharacterDummy_F instance). Empty = this object.")]
        GameObject runner;

        bool _applied;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        /// <summary>Idempotent; also callable from tests or a builder (AddComponent skips Awake in EditMode).</summary>
        public void Initialize()
        {
            if (_applied) return;
            var root = runner ? runner : gameObject;

            // Awake of a disabled component still runs, so the driver binds the rig (arm IK off) before it is silenced.
            Disable<RunnerInput>(root);
            Disable<RunnerReplay>(root);
            Disable<RunnerBodyHUD>(root);
            Disable<RunnerPoseDriver>(root);
            var body = root.GetComponentInChildren<RunnerBody>(true);
            if (body)
            {
                body.acceptInput = false;
                body.enabled = false;
            }
            // Relaxed arms instead of the T-pose; added on the runner so no scene wiring is needed. Menu instance only.
            if (!root.GetComponent<MenuIdlePose>()) root.AddComponent<MenuIdlePose>().Initialize();
            _applied = true;
        }

        static void Disable<T>(GameObject root) where T : Behaviour
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
                if (c) c.enabled = false;
        }
    }
}
