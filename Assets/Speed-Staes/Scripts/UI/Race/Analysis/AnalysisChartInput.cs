using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The chart's invisible touch surface (a transparent Image over the plot): a press or a drag reports the pointer, the
    /// Analysis view turns it into the nearest sample. No slider, no scrubbing UI.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class AnalysisChartInput : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        /// <summary>Screen position and the event camera (null on an overlay canvas).</summary>
        public event Action<Vector2, Camera> Pointed;

        public void OnPointerDown(PointerEventData eventData) => Pointed?.Invoke(eventData.position, eventData.pressEventCamera);

        public void OnDrag(PointerEventData eventData) => Pointed?.Invoke(eventData.position, eventData.pressEventCamera);
    }
}
