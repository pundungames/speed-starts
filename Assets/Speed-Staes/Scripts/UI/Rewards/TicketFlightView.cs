using System;
using DG.Tweening;
using SpeedStars.Meta.Config;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using Zenject;

namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// Daily gift ticket burst (prefab RewardFlight, a scene object on the overlay canvas, starts
    /// inactive). <see cref="Play"/> waits for the popup to close, shows three red tickets in the middle of the screen, bends them
    /// along a slight arc to the Entry Ticket box of the resource bar and punches that box on arrival. While it runs a
    /// transparent blocker keeps the menu from taking taps, so the menu is free again when the ticket has landed. The flight
    /// only draws: the ticket was added and saved before it started, so a missing target or a killed tween never loses it.
    /// The existing ticket art is reused for all three positions.
    /// </summary>
    public sealed class TicketFlightView : MonoBehaviour
    {
        [Tooltip("Flight numbers (duration, easing, arc, sizes).")]
        [SerializeField] private RewardUiConfig config;

        [Tooltip("Icon: the flying ticket picture (starts hidden, moved by this view).")]
        [SerializeField] private RectTransform icon;

        [Tooltip("The resource bar whose Entry Ticket box is the target. Scene-bound.")]
        [SerializeField] private ResourceBarView resourceBar;

        private UiMotionConfig _motion;
        private Sequence _flight;
        private RectTransform[] _icons;
        private Vector3 _iconScale;
        private Tween _punch;
        private Action<int> _reveal;
        private Transform _punched;
        private Vector3 _punchedBaseScale;

        [Inject]
        public void ConstructFlight([InjectOptional] UiMotionConfig motion)
        {
            _motion = motion;
        }

        /// <summary>True from <see cref="Play"/> until the ticket has landed.</summary>
        public bool IsFlying => _flight != null;

        /// <summary>
        /// Starts the flight after <paramref name="delay"/> seconds (the popup's closing time). Without a target the widget
        /// stays off: the reward is already in the wallet and the resource bar shows it.
        /// </summary>
        public void Play(float delay, int previousBalance)
        {
            Stop();
            if (resourceBar == null || resourceBar.EntryTicketBox == null || icon == null || config == null)
            {
                Debug.LogWarning($"{name}: the flight has no target or config bound; nothing flies.", this);
                return;
            }

            int granted = resourceBar.Balance(SevenDayRewardKind.EntryTicket) - previousBalance;
            if (granted > 0) _reveal = resourceBar.HoldReward(SevenDayRewardKind.EntryTicket, granted);
            gameObject.SetActive(true);
            if (_icons == null)
            {
                _iconScale = icon.localScale;
                _icons = new[] { Instantiate(icon, icon.parent), icon, Instantiate(icon, icon.parent) };
            }
            foreach (RectTransform ticket in _icons) ticket.gameObject.SetActive(false);

            RectTransform area = (RectTransform)transform;
            RectTransform target = resourceBar.EntryTicketBox;
            Vector3 start = area.TransformPoint(area.rect.center);
            Vector3 end = target.TransformPoint(target.rect.center);
            Vector3 arc = Vector3.up * (config.ArcHeight * area.lossyScale.y);
            Vector3 spread = area.TransformVector(Vector3.right * (icon.rect.width * 0.85f));
            float progress = 0f;

            Sequence flight = DOTween.Sequence();
            flight.AppendInterval(Mathf.Max(0f, delay));
            flight.AppendCallback(() =>
            {
                foreach (RectTransform ticket in _icons) ticket.gameObject.SetActive(true);
                Place(start, end, arc, spread, 0f);
            });
            flight.AppendInterval(0.12f);
            flight.Append(DOTween.To(() => progress, p =>
                {
                    progress = p;
                    Place(start, end, arc, spread, p);
                }, 1f, config.FlightDuration * 1.55f).SetEase(config.FlightEase));
            flight.SetUpdate(true);
            flight.SetLink(gameObject);
            flight.OnComplete(() => Land(target));
            _flight = flight;
        }

        private void Place(Vector3 start, Vector3 end, Vector3 arc, Vector3 spread, float p)
        {
            Vector3 center = Vector3.Lerp(start, end, p) + arc * Mathf.Sin(p * Mathf.PI);
            Vector3 scale = _iconScale * Mathf.Lerp(config.StartScale * 1.18f, config.EndScale, p);
            for (int i = 0; i < _icons.Length; i++)
            {
                _icons[i].position = center + spread * ((i - 1) * (1f - p));
                _icons[i].localScale = scale;
            }
        }

        /// <summary>Arrival: the ticket disappears, the box punches once, the widget (and its blocker) switches off.</summary>
        private void Land(RectTransform target)
        {
            _flight = null;
            RevealReward();
            resourceBar.SlideCounter(SevenDayRewardKind.EntryTicket);
            PunchTarget(target);
            gameObject.SetActive(false);
        }

        private void PunchTarget(Transform target)
        {
            if (_motion == null) return;
            if (_punch != null) StopPunch();
            _punched = target;
            _punchedBaseScale = target.localScale;
            Tween punch = target
                .DOPunchScale(_punchedBaseScale * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(target.gameObject);
            punch.OnKill(() =>
            {
                if (_punch == punch) _punch = null;
                if (target != null) target.localScale = _punchedBaseScale;
            });
            _punch = punch;
        }

        private void RevealReward()
        {
            _reveal?.Invoke(int.MaxValue);
            _reveal = null;
        }

        private void Stop()
        {
            if (_flight != null)
            {
                Sequence running = _flight;
                _flight = null;
                running.Kill();
            }
            RevealReward();
            StopPunch();
        }

        private void StopPunch()
        {
            if (_punch == null) return;
            Tween running = _punch;
            _punch = null;
            running.Kill();
        }

        private void OnDisable()
        {
            RevealReward();
            // A running punch belongs to the target and finishes on its own.
            if (_flight == null) return;
            Sequence running = _flight;
            _flight = null;
            running.Kill();
        }

        private void OnDestroy() => Stop();
    }
}
