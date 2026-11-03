using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Emotes;

namespace SpeedStars.Tests.Meta.Emotes
{
    /// <summary>When the race emote button shows, when a press is taken, who plays by itself (T-089). Pure rules, no scene.</summary>
    public sealed class RaceEmoteRulesTests
    {
        [Test]
        public void Button_ShowsOnlyWhenWantedAndAnEmoteIsEquipped()
        {
            Assert.IsTrue(RaceEmoteRules.ButtonVisible(true, true));
            Assert.IsFalse(RaceEmoteRules.ButtonVisible(false, true), "the player turned the button off");
            Assert.IsFalse(RaceEmoteRules.ButtonVisible(true, false), "None has nothing to play");
            Assert.IsFalse(RaceEmoteRules.ButtonVisible(false, false));
        }

        [Test]
        public void Press_IsTakenFromGoOn_NotBefore()
        {
            Assert.IsFalse(RaceEmoteRules.CanPlay(true, RaceStartPhase.Idle, false));
            Assert.IsFalse(RaceEmoteRules.CanPlay(true, RaceStartPhase.Ready, false));
            Assert.IsFalse(RaceEmoteRules.CanPlay(true, RaceStartPhase.Set, false));
            Assert.IsTrue(RaceEmoteRules.CanPlay(true, RaceStartPhase.Go, false));
        }

        [Test]
        public void Press_IsRefusedWithoutAnEmote_OrWhileTheLastOneStillPlays()
        {
            Assert.IsFalse(RaceEmoteRules.CanPlay(false, RaceStartPhase.Go, false));
            Assert.IsFalse(RaceEmoteRules.CanPlay(true, RaceStartPhase.Go, true));
        }

        [Test]
        public void Press_WithoutAStartSignal_CountsAsStarted()
        {
            Assert.IsTrue(RaceEmoteRules.CanPlay(true, null, false));
            Assert.IsFalse(RaceEmoteRules.CanPlay(false, null, false));
        }

        [Test]
        public void AutoSelf_NeedsTheSwitchAndAnEmote()
        {
            Assert.IsTrue(RaceEmoteRules.AutoSelfWanted(true, true));
            Assert.IsFalse(RaceEmoteRules.AutoSelfWanted(false, true));
            Assert.IsFalse(RaceEmoteRules.AutoSelfWanted(true, false));
        }

        [Test]
        public void CrossedLine_IsAtOrPastTheTarget_AndNeverForAnOpenRace()
        {
            Assert.IsFalse(RaceEmoteRules.CrossedLine(99.9f, 100f));
            Assert.IsTrue(RaceEmoteRules.CrossedLine(100f, 100f));
            Assert.IsTrue(RaceEmoteRules.CrossedLine(130f, 100f));
            Assert.IsFalse(RaceEmoteRules.CrossedLine(50f, 0f), "no race length: nobody finishes");
        }

        [Test]
        public void BotClip_StaysInRange_SpreadsBotsAndMovesWithTheRound()
        {
            const int clips = 4;
            for (int bot = 0; bot < 10; bot++)
                for (int round = 0; round < 10; round++)
                {
                    int pick = RaceEmoteRules.BotClipIndex(bot, round, clips);
                    Assert.GreaterOrEqual(pick, 0);
                    Assert.Less(pick, clips);
                }

            Assert.AreNotEqual(RaceEmoteRules.BotClipIndex(0, 0, clips), RaceEmoteRules.BotClipIndex(1, 0, clips), "neighbouring bots differ");
            Assert.AreNotEqual(RaceEmoteRules.BotClipIndex(0, 0, clips), RaceEmoteRules.BotClipIndex(0, 1, clips), "the next round differs");
            Assert.AreEqual(-1, RaceEmoteRules.BotClipIndex(0, 0, 0), "no clips: nothing to pick");
        }

        [Test]
        public void DefaultOptions_AreAllOn_AndNeverChange()
        {
            IEmoteRaceOptions options = new DefaultEmoteRaceOptions();
            Assert.IsTrue(options.ShowEmoteButton);
            Assert.IsTrue(options.AutoEmoteSelf);
            Assert.IsTrue(options.AutoEmoteCompetitors);
            int raised = 0;
            options.Changed += () => raised++;
            Assert.AreEqual(0, raised);
        }
    }
}
