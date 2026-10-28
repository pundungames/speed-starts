using UnityEngine;
using UnityEngine.InputSystem;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Dev only: a key finishes the race with the current time, so Results can be seen without running 100 m.
    /// The class and its fields always compile (the scene serializes it, so release builds must not show a missing
    /// script); only the behaviour is dev-gated, in release it is an empty MonoBehaviour.
    /// </summary>
    public sealed class DevRaceFinish : MonoBehaviour
    {
        [SerializeField, Tooltip("The adapter to finish.")]
        RaceSessionAdapter adapter;
        [SerializeField, Tooltip("Key that triggers the finish.")]
        Key finishKey = Key.F;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            var keyboard = Keyboard.current;
            if (adapter && keyboard != null && keyboard[finishKey].wasPressedThisFrame)
                adapter.DevForceFinish();
        }
#endif
    }
}
