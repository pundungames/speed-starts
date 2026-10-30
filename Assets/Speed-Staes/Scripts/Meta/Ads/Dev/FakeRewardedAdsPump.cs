#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;

namespace SpeedStars.Meta.Ads
{
    /// <summary>Hidden helper of <see cref="FakeRewardedAds"/>: waits in real time, calls back once, removes itself.</summary>
    public sealed class FakeRewardedAdsPump : MonoBehaviour
    {
        public void Run(float seconds, Action finished)
        {
            StartCoroutine(Wait(seconds, finished));
        }

        private IEnumerator Wait(float seconds, Action finished)
        {
            yield return new WaitForSecondsRealtime(seconds);
            try
            {
                finished();
            }
            finally
            {
                Destroy(gameObject);
            }
        }
    }
}
#endif
