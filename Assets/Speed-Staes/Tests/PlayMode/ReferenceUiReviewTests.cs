using System.Collections;
using System.IO;
using System.Linq;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using NUnit.Framework;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using SpeedStars.UI.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    public sealed class ReferenceUiReviewTests
    {
        SaveFolderSnapshot _saves;
        RaceEventId _selected;
        IPlayerSettings _settings;
        bool _music;
        string _flag;
        [UnitySetUp] public IEnumerator Setup()
        {
            _saves=SaveFolderSnapshot.Take();
            yield return LoadScene(HomeScene);
            _selected=ProjectContext.Instance.Container.Resolve<IRaceSelection>().Selected;
            _settings=ProjectContext.Instance.Container.Resolve<IPlayerSettings>();
            _music=_settings.MusicOn;_flag=_settings.FlagCode;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ProjectContext.Instance.Container.Resolve<IRaceSelection>().Select(_selected);
            if(_settings!=null){_settings.SetMusic(_music);_settings.SetFlag(_flag);}
            _saves.Restore();yield break;
        }

        [UnityTest]
        public IEnumerator ReferenceHud_HasLiveTelemetryAndEdgeControls()
        {
            var container=ProjectContext.Instance.Container;
            var race=container.Resolve<RaceEventCatalog>().GetEvents().First(e=>e.IsPlayable&&e.HasOpponents&&!e.IsRelay);
            container.Resolve<IRaceSelection>().Select(race.Id);
            yield return LoadScene(RaceScene);
            yield return new WaitForSecondsRealtime(5f);
            var hud=Require<RaceHudView>();
            Assert.IsTrue(RequireField<TMP_Text>(hud,"speedText").text.EndsWith("mph"));
            Assert.IsNotEmpty(RequireField<TMP_Text>(hud,"energyValueText").text);
            Assert.IsNotEmpty(RequireField<TMP_Text>(hud,"cadenceValueText").text);
            var energy = RequireField<Image>(hud,"energyFill");
            var cadence = RequireField<Image>(hud,"cadenceFill");
            Assert.AreEqual(281f,energy.sprite.rect.width,"Only the painted bar belongs to the fill image.");
            Assert.AreEqual(17f,energy.sprite.rect.height);
            Assert.AreEqual(energy.sprite.rect.size,cadence.sprite.rect.size);
            Assert.That(energy.fillAmount,Is.EqualTo(Mathf.Clamp01(RequireField<IRaceTelemetry>(hud,"_telemetry").Energy01)).Within(.001f));
            Assert.AreEqual(container.Resolve<IWallet>().PurpleGems.ToString(),RequireField<TMP_Text>(hud,"cashText").text);
            var exit=RequireField<Button>(hud,"exitButton");
            Assert.AreEqual(new Vector2(0,1),((RectTransform)exit.transform).anchorMin);
            var emote=RequireField<Button>(hud,"emoteButton");
            var rect=(RectTransform)emote.transform;
            Assert.AreEqual(rect.rect.width,rect.rect.height,.01f,"Emote is square.");
            Assert.AreEqual("emote_icon",emote.transform.Find("Icon").GetComponent<Image>().sprite.name);
            Assert.AreEqual("Emote",emote.transform.Find("Label").GetComponent<TMP_Text>().text);
            yield return Capture("race-hud");
        }

        [UnityTest]
        public IEnumerator Settings_ReferenceControlsAndBackCloseOnlyTheOpenList()
        {
            yield return LoadScene(HomeScene);
            var home=Require<HomeScreenView>();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(RequireField<TMP_Text>(home,"racerNameText").gameObject.activeInHierarchy);
            Assert.IsTrue(RequireField<Button>(home,"previousRacerButton").gameObject.activeInHierarchy);
            yield return Capture("home");
            yield return Tap(RequireField<Button>(home,"settingsButton"),"Settings");
            var popup=(SettingsPopupView)RequireField<UiPopup>(home,"settingsPopup");
            yield return WaitOpen(popup,"Settings");
            yield return new WaitForSecondsRealtime(.6f);
            var input=RequireField<InputField>(popup,"nameInput");
            Assert.IsNotNull(input.transform.Find("Pencil").GetComponent<CanvasRenderer>());
            var flag=RequireField<Image>(popup,"flagValueImage");
            Assert.IsTrue(flag.preserveAspect);
            var square=(RectTransform)flag.transform.parent;
            Assert.AreEqual(square.rect.width,square.rect.height,.01f);
            yield return Capture("settings");
            yield return Tap(RequireField<Button>(popup,"flagButton"),"Flag picker");
            Assert.IsTrue(RequireField<GameObject>(popup,"flagWindow").activeSelf);
            Assert.IsTrue(RequireField<ScreenNavigator>(home,"_navigator").Back());
            Assert.IsFalse(RequireField<GameObject>(popup,"flagWindow").activeSelf);
            Assert.IsTrue(popup.IsVisible);
            var toggle=RequireField<SettingsToggleView>(popup,"musicToggle");
            yield return Tap(RequireField<Button>(toggle,"button"),"Music switch");
            Assert.AreEqual(!_music,_settings.MusicOn);
            yield return Tap(RequireField<Button>(toggle,"button"),"Music switch restore");
            Assert.AreEqual(_music,_settings.MusicOn);
            yield return Tap(RequireField<Button>(popup,"flagButton"),"Flag picker again");
            string language=_settings.LanguageCode;
            var cell=RequireField<RectTransform>(popup,"flagContent").GetComponentsInChildren<FlagCellView>().First(c=>c.Code!=_flag);
            yield return ScrollIntoView(cell);
            yield return Tap(RequireField<Button>(cell,"button"),"Country selection");
            Assert.AreEqual(cell.Code,_settings.FlagCode);
            Assert.AreEqual(language,_settings.LanguageCode,"A flag selection must not change language.");
            yield return WaitUntil(()=>!RequireField<GameObject>(popup,"flagWindow").activeSelf,"Flag list closes on selection");
            yield return Tap(RequireField<Button>(popup,"doneButton"),"Done");
            yield return WaitClosed(popup,"Settings");
        }

        static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            Directory.CreateDirectory("Temp/UIReferenceReview");
            File.WriteAllBytes("Temp/UIReferenceReview/"+name+".png",texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}
