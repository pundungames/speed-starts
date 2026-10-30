using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Marker: this child of a SafeAreaFitter (Children mode) keeps its authored anchors, so a background, dim or full-screen
    /// art reaches the screen edges behind a notch. Stretched children that draw something are recognised without it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FullBleed : MonoBehaviour
    {
    }
}
