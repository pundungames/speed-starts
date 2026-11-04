using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;

namespace SpeedStars.UI.Race
{
    /// <summary>The Results screen as the flow sees it. The view implements it; tests use a fake.</summary>
    public interface IResultsView : IUiView
    {
        /// <summary>Fills the screen. Called before it is shown.</summary>
        void Present(ResultsData data);

        /// <summary>"Race again" was pressed.</summary>
        event Action RaceAgainRequested;

        /// <summary>"Main menu" was pressed.</summary>
        event Action MainMenuRequested;

        /// <summary>Visible refusal (e.g. no ticket): the Race again button shakes.</summary>
        void NotifyDenied();
    }

    /// <summary>
    /// Optional side of the Results screen: the Practice button. The flow tells whether a Practice event exists and hears the press.
    /// A separate interface so a view without the button (or a test fake) needs nothing.
    /// </summary>
    public interface IResultsPracticeView
    {
        /// <summary>"Practice" was pressed.</summary>
        event Action PracticeRequested;

        /// <summary>The button works only while the catalog has an event to practise on.</summary>
        void SetPracticeAvailable(bool available);
    }

    /// <summary>Opens the Entry Gate modal of the Race scene for an event (the player may get a ticket there). Optional.</summary>
    public interface IEntryGatePrompt
    {
        void Open(RaceEventId id);
    }

    /// <summary>
    /// Optional side of the Results screen: the Cash the race paid, split into lines. RaceFlow calls it before the screen
    /// opens, once per race; an empty reward (free event, nothing paid) means "show no Cash block" and clears a previous race's.
    /// </summary>
    public interface IRaceCashRewardView
    {
        void ShowCashReward(RaceCashReward reward);
    }
}
