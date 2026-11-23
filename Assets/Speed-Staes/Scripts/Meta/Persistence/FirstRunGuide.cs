using System;
using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Persistence
{
    public enum FirstRunStage
    {
        None,
        Welcome,
        FirstRace,
        FirstReward,
        SecondRace,
        ModeGuide,
        ModePlay,
        ResourceReveal,
        Complete
    }

    /// <summary>One saved step per onboarding milestone. Old profiles have no active guide.</summary>
    public sealed class FirstRunGuide
    {
        public static readonly RaceEventId Sprint100 = new RaceEventId("100m");
        public static readonly RaceEventId Sprint200 = new RaceEventId("200m");

        private readonly ProgressRepository _progress;

        public FirstRunGuide(ProgressRepository progress) => _progress = progress;

        public FirstRunStage Stage => _progress.CanSave ? _progress.Current.FirstRunStage : FirstRunStage.None;
        public bool IsActive => Stage != FirstRunStage.None && Stage != FirstRunStage.Complete;
        public bool RequiresHomeAfterFinish => Stage == FirstRunStage.FirstReward || Stage == FirstRunStage.ModeGuide;

        public event Action Changed;

        public void AcceptWelcome() => Advance(FirstRunStage.Welcome, FirstRunStage.FirstRace);

        public void RecordFinish(RaceOutcome outcome)
        {
            if (outcome.EventId != Sprint100 || outcome.Rank < 1 || !(outcome.TimeSeconds > 0d)) return;
            if (Stage == FirstRunStage.FirstRace) Advance(FirstRunStage.FirstRace, FirstRunStage.FirstReward);
            else Advance(FirstRunStage.SecondRace, FirstRunStage.ModeGuide);
        }

        public void CheckFirstReward()
        {
            if (Stage == FirstRunStage.FirstReward && _progress.Current.SevenDay.ClaimedCount > 0)
                Advance(FirstRunStage.FirstReward, FirstRunStage.SecondRace);
        }

        public void SelectMode(RaceEventId race)
        {
            if (race == Sprint200) Advance(FirstRunStage.ModeGuide, FirstRunStage.ModePlay);
        }

        public void RaceStarted(RaceEventId race)
        {
            if (race == Sprint200) Advance(FirstRunStage.ModePlay, FirstRunStage.ResourceReveal);
        }

        public void FinishResourceReveal() => Advance(FirstRunStage.ResourceReveal, FirstRunStage.Complete);

        private void Advance(FirstRunStage from, FirstRunStage to)
        {
            if (Stage != from) return;
            _progress.Current.FirstRunStage = to;
            _progress.Save();
            Changed?.Invoke();
        }
    }
}