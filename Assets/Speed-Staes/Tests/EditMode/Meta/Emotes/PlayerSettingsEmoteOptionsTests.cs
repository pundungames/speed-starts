using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Emotes;
using SpeedStars.Tests.Meta.Regression;

namespace SpeedStars.Tests.Meta.Emotes
{
    /// <summary>The emote options follow the player's settings live and pass their change event on (T-089 / T-088).</summary>
    public sealed class PlayerSettingsEmoteOptionsTests
    {
        private RegressionHarness _h;

        [SetUp]
        public void SetUp() => _h = new RegressionHarness();

        [TearDown]
        public void TearDown() => _h.Dispose();

        [Test]
        public void Switches_FollowTheSettings_AndChangedIsRaised()
        {
            IPlayerSettings settings = _h.Boot().Settings;
            IEmoteRaceOptions options = new PlayerSettingsEmoteOptions(settings);
            int raised = 0;
            options.Changed += () => raised++;

            settings.SetShowEmoteButton(false);
            Assert.IsFalse(options.ShowEmoteButton);
            settings.SetShowEmoteButton(true);
            Assert.IsTrue(options.ShowEmoteButton);

            settings.SetAutoEmoteSelf(!settings.AutoEmoteSelf);
            Assert.AreEqual(settings.AutoEmoteSelf, options.AutoEmoteSelf);
            settings.SetAutoEmoteCompetitors(!settings.AutoEmoteCompetitors);
            Assert.AreEqual(settings.AutoEmoteCompetitors, options.AutoEmoteCompetitors);

            Assert.Greater(raised, 0, "a settings change reaches the listeners of the options");
        }

        [Test]
        public void Unsubscribing_StopsTheNotifications()
        {
            IPlayerSettings settings = _h.Boot().Settings;
            IEmoteRaceOptions options = new PlayerSettingsEmoteOptions(settings);
            int raised = 0;
            System.Action listener = () => raised++;
            options.Changed += listener;
            options.Changed -= listener;

            settings.SetShowEmoteButton(!settings.ShowEmoteButton);
            Assert.AreEqual(0, raised);
        }
    }
}
