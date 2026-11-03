using System.Collections.Generic;
using System.Globalization;

namespace SpeedStars.Meta.Practice
{
    /// <summary>
    /// What the Practice panel shows, as plain values (T-090). The view only copies these onto its widgets; the rules
    /// (which row is grey, what a distance is called, which change restarts the attempt) are here so they are tested.
    /// </summary>
    public sealed class PracticePanelModel
    {
        private readonly IPracticeControl _control;

        public PracticePanelModel(IPracticeControl control)
        {
            _control = control;
        }

        public bool Visible => _control != null && _control.IsPractice;

        /// <summary>"100m": the dropdown caption and its entries.</summary>
        public static string DistanceLabel(float meters) => meters.ToString("0", CultureInfo.InvariantCulture) + "m";

        public string SelectedDistanceLabel => _control == null ? string.Empty : DistanceLabel(_control.Options.DistanceMeters);

        public IReadOnlyList<float> Distances => _control?.Options == null ? new float[0] : _control.Options.DistanceChoices;

        public int SelectedIndex => _control == null ? 0 : _control.Options.DistanceIndex;

        /// <summary>Hurdles are a plain flag: the layout comes from the distance.</summary>
        public bool HurdlesOn => _control != null && _control.Options.Hurdles;

        /// <summary>A row without a mechanic reads off and cannot be pressed.</summary>
        public bool LockEnergyOn => _control != null && _control.CanLockEnergy && _control.Options.LockEnergy;
        public bool LockEnergyEnabled => _control != null && _control.CanLockEnergy;
        public bool StandingStartOn => _control != null && _control.CanStandingStart && _control.Options.StandingStart;
        public bool StandingStartEnabled => _control != null && _control.CanStandingStart;
        public bool ResetEnergyEnabled => _control != null && _control.CanResetEnergy;

        /// <summary>
        /// Distance, hurdles and standing start shape the start, so changing them restarts the attempt; lock energy and
        /// reset energy act on the running one.
        /// </summary>
        public static bool RestartsAttempt(PracticeChange change) =>
            change == PracticeChange.Distance || change == PracticeChange.Hurdles || change == PracticeChange.StandingStart;
    }

    public enum PracticeChange
    {
        Distance,
        Hurdles,
        LockEnergy,
        StandingStart
    }
}
