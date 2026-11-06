using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Relay;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The relay team on the Home stage: while a relay event is selected and the Home screen is on top, the other legs stand beside
    /// the main character (<see cref="MenuCharacter"/>, which wears the edited leg through the selected preset). Slots are scene
    /// transforms the designer moves; the runners are copies of the runner prefab, stopped the same way as the main character
    /// (<see cref="MenuCharacter.Freeze"/>) and dressed with the preset of their leg. They slide in from the main character and
    /// slide back, no snapping; the main character pulses when the edited leg changes. Nothing is built before the first relay.
    /// Reads the services only; the Home card (leg arrows, preset arrows) is what changes them.
    /// </summary>
    public sealed class MenuRelayTeam : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField, Tooltip("The runner prefab (HumanCharacterDummy_F), copied once per slot the first time a relay needs it.")]
        GameObject runnerPrefab;

        [SerializeField, Tooltip("Where the other legs stand (scene transforms, move them freely). A relay of four legs fills three; extra slots stay empty.")]
        Transform[] slots = new Transform[0];

        [SerializeField, Tooltip("The main character: the helpers slide out of and back into its position, and it pulses when the edited leg changes. Empty = they slide from their own slot.")]
        Transform mainRunner;

        [Header("Motion")]
        [SerializeField, Min(0f), Tooltip("Seconds a helper takes to slide in or out. 0 = no slide.")]
        float slideSeconds = 0.25f;

        [SerializeField, Tooltip("Easing of the slide.")]
        Ease slideEase = Ease.OutCubic;

        [SerializeField, Min(0f), Tooltip("Seconds between helpers sliding in or out (0 = together).")]
        float slideStagger = 0.05f;

        [SerializeField, Min(0f), Tooltip("Feedback when the edited leg changes: the main character pulses by this fraction of its scale. 0 = off.")]
        float switchPulse = 0.08f;

        [SerializeField, Min(0f), Tooltip("Seconds of the pulse.")]
        float switchPulseSeconds = 0.25f;

        sealed class Helper
        {
            public GameObject Root;
            public CosmeticApplier Look;
            public Transform Slot;
        }

        readonly List<Helper> _helpers = new List<Helper>();
        ScreenNavigator _navigator;
        IRaceSelection _selection;
        RaceEventCatalog _events;
        IRaceOfTheDay _raceOfTheDay;
        IRelayTeam _team;
        ICosmeticService _cosmetics;
        CosmeticCatalog _catalog;
        bool _subscribed;
        bool _shown;
        int _lastLeg = -1;

        // Optional so a scene without the relay services still opens: the stage then never shows anyone.
        [Inject]
        void Construct([InjectOptional] ScreenNavigator navigator, [InjectOptional] IRaceSelection selection, [InjectOptional] RaceEventCatalog events,
            [InjectOptional] IRaceOfTheDay raceOfTheDay)
        {
            _navigator = navigator;
            _selection = selection;
            _events = events;
            _raceOfTheDay = raceOfTheDay;
        }

        [Inject]
        void ConstructTeam([InjectOptional] IRelayTeam team, [InjectOptional] ICosmeticService cosmetics, [InjectOptional] CosmeticCatalog catalog)
        {
            _team = team;
            _cosmetics = cosmetics;
            _catalog = catalog;
        }

        void OnEnable() => Subscribe();

        void Start()
        {
            Subscribe(); // injection may land after OnEnable in a runtime-instantiated menu
            if (_team == null) Debug.LogWarning("[MenuRelayTeam] IRelayTeam is not bound; the relay team is not shown.", this);
            if (_team != null) _lastLeg = _team.SelectedLeg;
            Refresh();
        }

        void OnDisable()
        {
            if (_subscribed)
            {
                if (_navigator != null) _navigator.CurrentScreenChanged -= OnScreenChanged;
                if (_selection != null) _selection.Changed -= Refresh;
                if (_team != null) _team.Changed -= OnTeamChanged;
                if (_cosmetics != null) _cosmetics.Changed -= OnOutfitsChanged;
            }
            _subscribed = false;
            KillTweens();
        }

        void OnDestroy() => KillTweens();

        void Subscribe()
        {
            if (_subscribed || _team == null || _selection == null) return;
            if (_navigator != null) _navigator.CurrentScreenChanged += OnScreenChanged;
            _selection.Changed += Refresh;
            _team.Changed += OnTeamChanged;
            if (_cosmetics != null) _cosmetics.Changed += OnOutfitsChanged;
            _subscribed = true;
        }

        void OnScreenChanged(IUiView _) => Refresh();

        void OnOutfitsChanged()
        {
            if (_shown) Dress();
        }

        void OnTeamChanged()
        {
            if (_team == null) return;
            bool legChanged = _team.SelectedLeg != _lastLeg;
            _lastLeg = _team.SelectedLeg;
            Refresh();
            if (_shown) Dress(); // the same legs, another preset on one of them
            if (legChanged && _shown) Pulse();
        }

        /// <summary>The Home screen is on top and a relay (not the day's race) is selected.</summary>
        bool WantsTeam()
        {
            if (_team == null || _selection == null || _events == null || !runnerPrefab) return false;
            if (_navigator == null || !(_navigator.CurrentScreen is HomeScreenView)) return false;
            bool relay = _events.TryGet(_selection.Selected, out RaceEventDefinition definition) && definition.IsRelay;
            bool daily = _raceOfTheDay != null && _raceOfTheDay.IsRaceOfTheDay(_selection.Selected);
            return RelayCardRules.ShowsTeam(relay, daily, _team.LegCount);
        }

        void Refresh()
        {
            bool want = WantsTeam();
            if (want == _shown) return;
            _shown = want;
            if (want)
            {
                if (_helpers.Count == 0) Build();
                Dress();
            }
            Slide(want);
        }

        /// <summary>The helper copies, made once, asleep under the slots, stopped like the main character.</summary>
        void Build()
        {
            var holder = new GameObject("Relay Stage Holder");
            holder.SetActive(false); // copies stay asleep until they are placed and frozen
            foreach (Transform slot in slots)
            {
                if (!slot) continue;
                GameObject root = Instantiate(runnerPrefab, holder.transform);
                root.name = "Relay Helper " + (_helpers.Count + 1);
                MenuCharacter.Freeze(root);
                root.transform.SetPositionAndRotation(slot.position, slot.rotation);
                root.transform.SetParent(slot, true);
                root.SetActive(false);
                var look = root.GetComponent<CosmeticApplier>() ?? root.AddComponent<CosmeticApplier>();
                _helpers.Add(new Helper { Root = root, Look = look, Slot = slot });
            }
            Destroy(holder);
        }

        /// <summary>Every helper wears the preset of its leg: the legs other than the edited one, in leg order.</summary>
        void Dress()
        {
            if (_team == null || _cosmetics == null || _catalog == null) return;
            int[] legs = RelayTeamRules.StageLegs(_team.LegCount, _team.SelectedLeg);
            for (int i = 0; i < _helpers.Count && i < legs.Length; i++)
            {
                _helpers[i].Look.Initialize();
                _helpers[i].Look.Apply(_cosmetics.GetOutfit(_team.GetPreset(legs[i])), _catalog);
            }
        }

        /// <summary>Helpers with a leg to show slide out of the main character; going away they slide back and sleep.</summary>
        void Slide(bool show)
        {
            int legs = _team != null ? RelayTeamRules.StageLegs(_team.LegCount, _team.SelectedLeg).Length : 0;
            for (int i = 0; i < _helpers.Count; i++)
            {
                Helper helper = _helpers[i];
                Transform t = helper.Root.transform;
                t.DOKill();
                if (show && i >= legs) continue; // no leg for this slot

                Vector3 home = helper.Slot.position;
                Vector3 away = mainRunner ? mainRunner.position : home;
                float delay = i * slideStagger;
                if (show)
                {
                    if (!helper.Root.activeSelf) t.position = away;
                    helper.Root.SetActive(true);
                    t.DOMove(home, slideSeconds).SetDelay(delay).SetEase(slideEase).SetUpdate(true).SetTarget(t);
                }
                else if (helper.Root.activeSelf)
                {
                    GameObject root = helper.Root;
                    t.DOMove(away, slideSeconds).SetDelay(delay).SetEase(slideEase).SetUpdate(true).SetTarget(t)
                        .OnComplete(() => { if (root) root.SetActive(false); });
                }
            }
        }

        /// <summary>The edited leg changed: the outfit already switched, the main character pulses once.</summary>
        void Pulse()
        {
            if (!mainRunner || switchPulse <= 0f || switchPulseSeconds <= 0f) return;
            mainRunner.DOKill(true); // finishes a running pulse so the scale is back at the designed one
            mainRunner.DOPunchScale(mainRunner.localScale * switchPulse, switchPulseSeconds, 1, 0f).SetUpdate(true).SetTarget(mainRunner);
        }

        void KillTweens()
        {
            foreach (Helper helper in _helpers)
                if (helper.Root) helper.Root.transform.DOKill();
            if (mainRunner) mainRunner.DOKill(true);
        }
    }
}
