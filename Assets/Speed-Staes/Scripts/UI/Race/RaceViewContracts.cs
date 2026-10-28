using System;
using SpeedStars.Core.Race;
using SpeedStars.UI.Framework;

namespace SpeedStars.UI.Race
{
    /// <summary>Everything the Results screen shows for one finished race. PB / WR texts stay "-" until Faz 6 brings the records.</summary>
    public readonly struct RaceResult
    {
        public readonly string EventTitle;
        public readonly string TimeText;
        public readonly string PersonalBestText;
        public readonly string WorldRecordText;
        public readonly bool IsPersonalBest;

        public RaceResult(string eventTitle, string timeText, string personalBestText, string worldRecordText, bool isPersonalBest)
        {
            EventTitle = eventTitle ?? string.Empty;
            TimeText = timeText ?? string.Empty;
            PersonalBestText = personalBestText ?? string.Empty;
            WorldRecordText = worldRecordText ?? string.Empty;
            IsPersonalBest = isPersonalBest;
        }
    }

    /// <summary>The Results screen as the flow sees it. The view implements it; tests use a fake.</summary>
    public interface IResultsView : IUiView
    {
        /// <summary>Fills the screen. Called before it is shown.</summary>
        void Present(RaceResult result);

        /// <summary>"Race again" was pressed.</summary>
        event Action RaceAgainRequested;

        /// <summary>"Main menu" was pressed.</summary>
        event Action MainMenuRequested;

        /// <summary>Visible refusal (e.g. no ticket): the Race again button shakes.</summary>
        void NotifyDenied();
    }

    /// <summary>Opens the Entry Gate modal of the Race scene for an event (the player may get a ticket there). Optional.</summary>
    public interface IEntryGatePrompt
    {
        void Open(RaceEventId id);
    }
}
