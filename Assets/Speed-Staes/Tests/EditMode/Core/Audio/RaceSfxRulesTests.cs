using System;
using NUnit.Framework;
using SpeedStars.Core.Audio;
using UnityEditor;

namespace SpeedStars.Tests.Audio
{
    /// <summary>
    /// Which sound a race event plays (T-120, RaceSfxRules) and that the shipped AudioConfig.asset has a clip for every sound id.
    /// No loudness or length is asserted: those are the asset's numbers.
    /// </summary>
    public sealed class RaceSfxRulesTests
    {
        private const string ConfigPath = "Assets/Speed-Staes/Data/Config/AudioConfig.asset";

        [Test]
        public void Step_LeftLegIsLeft_RightLegIsRight()
        {
            Assert.AreEqual(SfxId.StepLeft, RaceSfxRules.StepFor(0));
            Assert.AreEqual(SfxId.StepRight, RaceSfxRules.StepFor(1));
        }

        [Test]
        public void Finish_FirstWithOpponentsIsFirst_EverythingElseIsPlainFinish()
        {
            Assert.AreEqual(SfxId.FinishFirst, RaceSfxRules.FinishFor(1, 2));
            Assert.AreEqual(SfxId.FinishFirst, RaceSfxRules.FinishFor(1, 8));
            Assert.AreEqual(SfxId.FinishRace, RaceSfxRules.FinishFor(2, 8));
            Assert.AreEqual(SfxId.FinishRace, RaceSfxRules.FinishFor(8, 8));
            Assert.AreEqual(SfxId.FinishRace, RaceSfxRules.FinishFor(1, 1), "a solo run has no first place to celebrate");
        }

        [Test]
        public void StepAudible_OnlyAPressOfAnOpenRaceThatIsNotFalling()
        {
            Assert.IsTrue(RaceSfxRules.StepAudible(pressed: true, raceOpen: true, failing: false));
            Assert.IsFalse(RaceSfxRules.StepAudible(pressed: false, raceOpen: true, failing: false), "a release is silent");
            Assert.IsFalse(RaceSfxRules.StepAudible(pressed: true, raceOpen: false, failing: false), "countdown and the cool-down after the line are silent");
            Assert.IsFalse(RaceSfxRules.StepAudible(pressed: true, raceOpen: true, failing: true), "the feet of the false-start fall are silent");
        }

        [Test]
        public void SfxIds_AreOnlyAppended_SoSavedAssetsKeepTheirMeaning()
        {
            // The asset stores the id as an int: the first eight never move, the new ones follow them.
            Assert.AreEqual(0, (int)SfxId.Tap);
            Assert.AreEqual(7, (int)SfxId.Reward);
            Assert.AreEqual((int)SfxId.Reward + 1, (int)SfxId.StartGun);
        }

        [Test]
        public void ShippedConfig_EverySoundHasACueWithAClip()
        {
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            Assert.IsNotNull(config, ConfigPath);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                Assert.IsTrue(config.TryGetCue(id, out SfxCue cue), $"{id} has no cue in AudioConfig.asset");
                Assert.IsNotNull(cue.Clip, $"{id} has a cue without a clip");
            }
        }
    }
}
