using System;
using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// Bones an emote may move. Order matters: parents come before their children, the player applies them in this order.
    /// </summary>
    public enum EmoteBone
    {
        Spine = 0,
        Chest = 1,
        Neck = 2,
        Head = 3,
        ShoulderL = 4,
        UpperArmL = 5,
        ForearmL = 6,
        HandL = 7,
        ShoulderR = 8,
        UpperArmR = 9,
        ForearmR = 10,
        HandR = 11
    }

    /// <summary>How a key is approached from the previous key of the same bone.</summary>
    public enum EmoteEase
    {
        Linear = 0,
        Smooth = 1,
        EaseOut = 2,
        EaseIn = 3
    }

    /// <summary>
    /// One key: at <c>time</c> the bone is turned by <c>euler</c> degrees, as a rotation in RUNNER space (x = right,
    /// y = up, z = forward) applied on top of the stance the menu is in. Negative x tips the bone forward / the arm forward;
    /// positive z swings a right-side limb outward and up (left-side limbs use negative z).
    /// </summary>
    [Serializable]
    public sealed class EmoteKey
    {
        [Tooltip("Seconds from the start of the emote.")]
        [SerializeField, Min(0f)] private float time;

        [Tooltip("The bone this key moves.")]
        [SerializeField] private EmoteBone bone;

        [Tooltip("Rotation in degrees, runner space (x right, y up, z forward), on top of the menu stance.")]
        [SerializeField] private Vector3 euler;

        [Tooltip("How the bone gets here from its previous key.")]
        [SerializeField] private EmoteEase ease = EmoteEase.Smooth;

        public EmoteKey() { }

        public EmoteKey(float time, EmoteBone bone, Vector3 euler, EmoteEase ease = EmoteEase.Smooth)
        {
            this.time = time;
            this.bone = bone;
            this.euler = euler;
            this.ease = ease;
        }

        public float Time => time;
        public EmoteBone Bone => bone;
        public Vector3 Euler => euler;
        public EmoteEase Ease => ease;
    }

    /// <summary>One emote: an id, a name, a Cash price (0 = free) and its keys. No code lists emotes; the catalog does.</summary>
    [Serializable]
    public sealed class EmoteDefinition
    {
        [Tooltip("Stable id written to saves (owned and equipped emotes). Never change it once players have saves.")]
        [SerializeField] private string emoteId;

        [Tooltip("Name shown to the player (English).")]
        [SerializeField] private string displayName;

        [Tooltip("Price in Cash. 0 = free: every player owns the emote.")]
        [SerializeField, Min(0)] private int cashPrice;

        [Tooltip("Length of the emote in seconds; the pose has faded back to the menu stance by then.")]
        [SerializeField, Min(0.1f)] private float duration = 2f;

        [Tooltip("Seconds to fade from the menu stance into the emote.")]
        [SerializeField, Min(0f)] private float blendIn = 0.2f;

        [Tooltip("Seconds before the end in which the emote fades back to the menu stance.")]
        [SerializeField, Min(0f)] private float blendOut = 0.35f;

        [Tooltip("Key frames of all bones (any order). A bone holds its first key before it and its last key after it.")]
        [SerializeField] private EmoteKey[] keys = new EmoteKey[0];

        public string Id => emoteId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public int CashPrice => cashPrice;
        public float Duration => duration;
        public float BlendIn => blendIn;
        public float BlendOut => blendOut;
        public EmoteKey[] Keys => keys ?? new EmoteKey[0];
    }
}
