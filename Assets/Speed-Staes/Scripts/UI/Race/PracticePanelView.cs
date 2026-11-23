using SpeedStars.Meta.Practice;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The Practice panel of the race HUD (T-090): a Start dropdown (100m ... 400m), Reset energy, and the Lock energy / Hurdles /
    /// Standing start boxes. The prefab holds the widgets, this view only fills and reads them; the rules are in
    /// <see cref="PracticePanelModel"/>. The root stays active and only <c>content</c> is switched, so the view keeps hearing
    /// the control when Practice starts. A row the mechanics cannot do yet is shown grey and cannot be pressed.
    /// </summary>
    public sealed class PracticePanelView : MonoBehaviour
    {
        [System.Serializable]
        private sealed class CheckRow
        {
            [Tooltip("The whole row button.")]
            public Button button;
            [Tooltip("The check mark, shown while the box is on.")]
            public Image check;
        }

        [Tooltip("Everything the panel shows; switched on in Practice only. Must be a child, not the object that carries this view.")]
        [SerializeField] private GameObject content;

        [Tooltip("The Start dropdown button; its label shows the chosen distance.")]
        [SerializeField] private Button distanceButton;
        [SerializeField] private Text distanceLabel;

        [Tooltip("The open dropdown list; closed until the Start button is pressed.")]
        [SerializeField] private GameObject distanceList;

        [Tooltip("One button per dropdown entry, in order. Entries beyond the configured distances are hidden.")]
        [SerializeField] private Button[] distanceOptions = new Button[0];
        [SerializeField] private Text[] distanceOptionLabels = new Text[0];

        [Tooltip("Reset energy button.")]
        [SerializeField] private Button resetEnergyButton;

        [SerializeField] private CheckRow lockEnergy = new CheckRow();
        [SerializeField] private CheckRow hurdles = new CheckRow();
        [SerializeField] private CheckRow standingStart = new CheckRow();

        [Tooltip("Alpha of a row the mechanics cannot do yet.")]
        [SerializeField, Range(0f, 1f)] private float inactiveAlpha = 0.45f;

        private IPracticeControl _control;
        private PracticePanelModel _model;
        private IRaceHudActions _actions;
        private bool _wired;

        [Inject]
        public void InjectActions([InjectOptional] IRaceHudActions actions) => _actions = actions;

        private void Awake() => Initialize();
        private void OnEnable() => Initialize();

        private void OnDestroy()
        {
            if (_control != null) _control.Changed -= Refresh;
        }

        /// <summary>Idempotent: hooks the buttons once.</summary>
        public void Initialize()
        {
            if (_wired) return;
            _wired = true;
            if (distanceButton != null) distanceButton.onClick.AddListener(ToggleList);
            for (int i = 0; i < distanceOptions.Length; i++)
            {
                int index = i;
                if (distanceOptions[i] != null) distanceOptions[i].onClick.AddListener(() => PickDistance(index));
            }
            if (resetEnergyButton != null) resetEnergyButton.onClick.AddListener(OnResetEnergy);
            if (lockEnergy.button != null) lockEnergy.button.onClick.AddListener(OnLockEnergy);
            if (hurdles.button != null) hurdles.button.onClick.AddListener(OnHurdles);
            if (standingStart.button != null) standingStart.button.onClick.AddListener(OnStandingStart);
            if (distanceList != null) distanceList.SetActive(false);
            if (content != null) content.SetActive(false);
        }

        /// <summary>Connects the race side (called by the Bridge's PracticeSession). Binding again swaps the control.</summary>
        public void Bind(IPracticeControl control)
        {
            Initialize();
            if (_control != null) _control.Changed -= Refresh;
            _control = control;
            _model = new PracticePanelModel(control);
            if (_control != null) _control.Changed += Refresh;
            Refresh();
        }

        /// <summary>Copies the model onto the widgets.</summary>
        public void Refresh()
        {
            if (_model == null) return;
            bool visible = _model.Visible;
            if (content != null && content.activeSelf != visible) content.SetActive(visible);
            if (!visible)
            {
                if (distanceList != null) distanceList.SetActive(false);
                return;
            }

            if (distanceLabel != null) distanceLabel.text = PracticePanelModel.DistanceLabel(_control.Options.DistanceMeters);
            var distances = _model.Distances;
            for (int i = 0; i < distanceOptions.Length; i++)
            {
                if (distanceOptions[i] == null) continue;
                bool used = i < distances.Count;
                distanceOptions[i].gameObject.SetActive(used);
                if (used && i < distanceOptionLabels.Length && distanceOptionLabels[i] != null)
                    distanceOptionLabels[i].text = PracticePanelModel.DistanceLabel(distances[i]);
            }

            SetRow(lockEnergy, _model.LockEnergyOn, _model.LockEnergyEnabled);
            SetRow(hurdles, _model.HurdlesOn, true);
            SetRow(standingStart, _model.StandingStartOn, _model.StandingStartEnabled);
            SetEnabled(resetEnergyButton, _model.ResetEnergyEnabled);
        }

        private void ToggleList()
        {
            if (distanceList != null) distanceList.SetActive(!distanceList.activeSelf);
        }

        private void PickDistance(int index)
        {
            if (distanceList != null) distanceList.SetActive(false);
            if (_control == null) return;
            if (_control.Options.SetDistanceIndex(index)) Changed(PracticeChange.Distance);
        }

        private void OnResetEnergy() => _control?.ResetEnergy();

        private void OnLockEnergy()
        {
            if (_control != null && _control.CanLockEnergy && _control.Options.SetLockEnergy(!_control.Options.LockEnergy))
                Changed(PracticeChange.LockEnergy);
        }

        private void OnHurdles()
        {
            if (_control != null && _control.Options.SetHurdles(!_control.Options.Hurdles)) Changed(PracticeChange.Hurdles);
        }

        private void OnStandingStart()
        {
            if (_control != null && _control.CanStandingStart && _control.Options.SetStandingStart(!_control.Options.StandingStart))
                Changed(PracticeChange.StandingStart);
        }

        /// <summary>A choice that shapes the start restarts the attempt through the race flow (Practice is free).</summary>
        private void Changed(PracticeChange change)
        {
            Refresh();
            if (PracticePanelModel.RestartsAttempt(change)) _actions?.RequestRestart();
        }

        private void SetRow(CheckRow row, bool on, bool enabled)
        {
            if (row.check != null) row.check.enabled = on;
            SetEnabled(row.button, enabled);
        }

        private void SetEnabled(Button button, bool enabled)
        {
            if (button == null) return;
            button.interactable = enabled;
            var group = button.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = enabled ? 1f : inactiveAlpha;
        }
    }
}
