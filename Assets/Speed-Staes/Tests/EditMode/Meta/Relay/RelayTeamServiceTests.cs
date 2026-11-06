using NUnit.Framework;
using SpeedStars.Meta.Relay;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Cosmetics;

namespace SpeedStars.Tests.Meta.Relay
{
    public sealed class RelayTeamServiceTests
    {
        private const int Legs = 4;
        private CosmeticTestKit _kit;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        private RelayTeamService Team(int legs = Legs)
        {
            _kit = _kit ?? new CosmeticTestKit(0);
            return new RelayTeamService(_kit.Store, _kit.Service, legs);
        }

        [Test]
        public void Default_LegWearsThePresetWithItsOwnNumber_Wrapping()
        {
            var team = Team();
            int presets = _kit.Service.PresetCount;

            for (int leg = 0; leg < Legs; leg++)
                Assert.AreEqual(leg % presets, team.GetPreset(leg), $"leg {leg}");
            Assert.AreEqual(Legs, team.LegCount);
            Assert.AreEqual(0, team.SelectedLeg);
        }

        [Test]
        public void SetPreset_ChangesOnlyThatLeg_AndRaisesChangedOnce()
        {
            var team = Team();
            int changed = 0;
            team.Changed += () => changed++;
            int before1 = team.GetPreset(1);
            int target = (team.GetPreset(2) + 1) % _kit.Service.PresetCount;

            Assert.IsTrue(team.SetPreset(2, target));

            Assert.AreEqual(target, team.GetPreset(2));
            Assert.AreEqual(before1, team.GetPreset(1));
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void TwoLegsMayWearTheSamePreset()
        {
            var team = Team();

            team.SetPreset(0, 1);
            team.SetPreset(3, 1);

            Assert.AreEqual(team.GetPreset(0), team.GetPreset(3));
        }

        [Test]
        public void SetPreset_OutOfRange_ChangesNothing()
        {
            var team = Team();
            int changed = 0;
            team.Changed += () => changed++;
            int before = team.GetPreset(1);

            Assert.IsFalse(team.SetPreset(1, _kit.Service.PresetCount));
            Assert.IsFalse(team.SetPreset(1, -1));
            Assert.IsFalse(team.SetPreset(Legs, 0));
            Assert.IsFalse(team.SetPreset(-1, 0));

            Assert.AreEqual(before, team.GetPreset(1));
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void SelectLeg_RangeAndEvent()
        {
            var team = Team();
            int changed = 0;
            team.Changed += () => changed++;

            Assert.IsTrue(team.SelectLeg(2));
            Assert.AreEqual(2, team.SelectedLeg);
            Assert.AreEqual(1, changed);

            Assert.IsTrue(team.SelectLeg(2), "the same leg is accepted");
            Assert.AreEqual(1, changed, "but nothing changed");

            Assert.IsFalse(team.SelectLeg(Legs));
            Assert.IsFalse(team.SelectLeg(-1));
            Assert.AreEqual(2, team.SelectedLeg);
        }

        [Test]
        public void SavedChoices_SurviveReopening()
        {
            var team = Team();
            team.SelectLeg(3);
            team.SetPreset(1, 2);
            team.SetPreset(3, 0);

            var reopened = new RelayTeamService(_kit.Store, _kit.Service, Legs);

            Assert.AreEqual(3, reopened.SelectedLeg);
            Assert.AreEqual(2, reopened.GetPreset(1));
            Assert.AreEqual(0, reopened.GetPreset(3));
            Assert.AreEqual(team.GetPreset(0), reopened.GetPreset(0), "an untouched leg keeps its default");
        }

        [Test]
        public void FewerPresets_NeverBreakTheTeam_AndTheChoiceReturnsWhenTheyGrowBack()
        {
            var team = Team();
            team.SetPreset(3, 2);
            int original = _kit.Service.PresetCount;

            ConfigFixture.SetInt(_kit.Catalog, "presetCount", 1);
            for (int leg = 0; leg < Legs; leg++)
                Assert.AreEqual(0, team.GetPreset(leg), $"leg {leg} with one preset");

            ConfigFixture.SetInt(_kit.Catalog, "presetCount", original);
            Assert.AreEqual(2, team.GetPreset(3));
        }

        [Test]
        public void BrokenOrShortSave_ReadsAsDefaults()
        {
            _kit = new CosmeticTestKit(0);
            _kit.Store.Write(RelayTeamService.SaveKey, "{ not json");
            var broken = new RelayTeamService(_kit.Store, _kit.Service, Legs);
            Assert.AreEqual(0, broken.SelectedLeg);
            Assert.AreEqual(0, broken.GetPreset(0));

            _kit.Store.Write(RelayTeamService.SaveKey, "{\"selectedLeg\":9,\"presets\":[2]}");
            var shortSave = new RelayTeamService(_kit.Store, _kit.Service, Legs);
            Assert.AreEqual(0, shortSave.SelectedLeg, "a leg outside the team is dropped");
            Assert.AreEqual(2, shortSave.GetPreset(0));
            Assert.AreEqual(1 % _kit.Service.PresetCount, shortSave.GetPreset(1), "legs the file does not know take the default");
        }

        [Test]
        public void NoRelay_NoLegs_NothingBreaks()
        {
            var team = Team(0);

            Assert.AreEqual(0, team.LegCount);
            Assert.IsFalse(team.SelectLeg(0));
            Assert.IsFalse(team.SetPreset(0, 0));
            Assert.AreEqual(0, team.GetPreset(0));
        }
    }
}
