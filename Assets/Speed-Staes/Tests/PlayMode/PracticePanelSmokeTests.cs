using System.Collections;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Practice;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// The Practice panel on the real Race scene (T-090), pressed the way a player does (real raycast taps). Practice shows the panel,
    /// the Start dropdown moves the finish to the chosen distance, Hurdles saves its choice, and the energy / standing start rows stay
    /// grey while the mechanics have no API. A normal event shows no panel. Distances come from the config, not from this file.
    /// The save folder is restored byte for byte.
    /// </summary>
    public sealed class PracticePanelSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;
        private RaceEventId _selected;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            yield return LoadScene(HomeScene);
            _selected = Container.Resolve<IRaceSelection>().Selected;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
            {
                SceneFlow flow = Container.TryResolve<SceneFlow>();
                if (flow != null) yield return WaitUntil(() => !flow.IsTransitioning, "scene move to finish", 15f);
                if (!_selected.IsEmpty) Container.Resolve<IRaceSelection>().Select(_selected);
            }
            _saveFiles.Restore();
        }

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnityTest]
        public IEnumerator Practice_ShowsThePanel_And_StartDropdownMovesTheFinish()
        {
            yield return EnterFreeRace(withOpponents: false);
            PracticePanelView panel = Require<PracticePanelView>();
            yield return WaitUntil(() => RequireField<GameObject>(panel, "content").activeInHierarchy, "the Practice panel to show");

            var control = RequireField<IPracticeControl>(panel, "_control");
            var choices = control.Options.DistanceChoices;
            if (choices.Count < 2) Assert.Ignore("The Practice config lists one distance only; the dropdown has nothing to change.");
            int target = (control.Options.DistanceIndex + 1) % choices.Count;
            IRaceSession session = Object.FindObjectOfType<SceneContext>().Container.Resolve<IRaceSession>();

            yield return Tap(RequireField<Button>(panel, "distanceButton"), "Start dropdown");
            Assert.IsTrue(RequireField<GameObject>(panel, "distanceList").activeSelf, "the list must open on the Start button");
            yield return Tap(RequireField<Button[]>(panel, "distanceOptions")[target], "distance option");

            yield return WaitUntil(() => Mathf.Approximately(session.TargetDistance, choices[target]), "the finish to move to the chosen distance");
            Assert.IsFalse(RequireField<GameObject>(panel, "distanceList").activeSelf, "the list must close after a pick");
            Assert.AreEqual(target, control.Options.DistanceIndex);
        }

        [UnityTest]
        public IEnumerator Practice_HurdlesBox_TogglesAndRemembersTheChoice()
        {
            yield return EnterFreeRace(withOpponents: false);
            PracticePanelView panel = Require<PracticePanelView>();
            yield return WaitUntil(() => RequireField<GameObject>(panel, "content").activeInHierarchy, "the Practice panel to show");

            var control = RequireField<IPracticeControl>(panel, "_control");
            object row = RequireField<object>(panel, "hurdles");
            bool before = control.Options.Hurdles;

            yield return Tap(RequireField<Button>(row, "button"), "Hurdles row");
            Assert.AreNotEqual(before, control.Options.Hurdles, "the box must flip");
            Assert.AreEqual(control.Options.Hurdles, RequireField<Image>(row, "check").enabled, "the check mark follows the box");
        }

        [UnityTest]
        public IEnumerator Practice_EnergyAndStandingRows_AreGreyExactlyWhileTheMechanicsLackThem()
        {
            yield return EnterFreeRace(withOpponents: false);
            PracticePanelView panel = Require<PracticePanelView>();
            yield return WaitUntil(() => RequireField<GameObject>(panel, "content").activeInHierarchy, "the Practice panel to show");

            var control = RequireField<IPracticeControl>(panel, "_control");
            Assert.AreEqual(control.CanLockEnergy, RequireField<Button>(RequireField<object>(panel, "lockEnergy"), "button").interactable);
            Assert.AreEqual(control.CanStandingStart, RequireField<Button>(RequireField<object>(panel, "standingStart"), "button").interactable);
            Assert.AreEqual(control.CanResetEnergy, RequireField<Button>(panel, "resetEnergyButton").interactable);
        }

        [UnityTest]
        public IEnumerator Practice_Finish_OpensNoResults_HudStays_RestartRunsAgain()
        {
            yield return EnterFreeRace(withOpponents: false);
            PracticePanelView panel = Require<PracticePanelView>();
            yield return WaitUntil(() => RequireField<GameObject>(panel, "content").activeInHierarchy, "the Practice panel to show");

            IRaceSession session = Object.FindObjectOfType<SceneContext>().Container.Resolve<IRaceSession>();
            System.Reflection.MethodInfo finish = session.GetType().GetMethod("DevForceFinish");
            if (finish == null) Assert.Ignore("RaceSessionAdapter.DevForceFinish is not compiled in (non-dev build): no way to finish the race from a test.");
            ResultsScreenView results = Require<ResultsScreenView>();
            RaceHudView hud = Require<RaceHudView>();

            finish.Invoke(session, null);
            yield return WaitUntil(() => session.State == RaceSessionState.Finished, "the session to report the finish");
            for (int i = 0; i < 10; i++) yield return null; // Results would have opened by now

            Assert.IsFalse(results.gameObject.activeInHierarchy, "Practice ends without a Results screen");
            Assert.IsTrue(hud.gameObject.activeInHierarchy, "the HUD stays up after a Practice finish");

            yield return Tap(RequireField<Button>(hud, "restartButton"), "HUD Restart");
            yield return WaitUntil(() => session.State != RaceSessionState.Finished, "Restart to begin a new run");
            Assert.IsFalse(results.gameObject.activeInHierarchy, "Restart must not open Results either");
        }

        [UnityTest]
        public IEnumerator NormalEvent_HasNoPracticePanel()
        {
            yield return EnterFreeRace(withOpponents: true);
            PracticePanelView panel = Require<PracticePanelView>();
            yield return null;
            yield return null;

            Assert.IsFalse(RequireField<GameObject>(panel, "content").activeInHierarchy, "the panel is for Practice only");
        }

        private IEnumerator EnterFreeRace(bool withOpponents)
        {
            var catalog = Container.Resolve<RaceEventCatalog>();
            RaceEventDefinition chosen = null;
            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                if (!definition.IsPlayable || definition.RequiresTicket || definition.HasOpponents != withOpponents) continue;
                chosen = definition;
                break;
            }
            if (chosen == null) Assert.Ignore("The event catalog has no free playable event of this kind.");
            Container.Resolve<IRaceSelection>().Select(chosen.Id);

            HomeScreenView home = Require<HomeScreenView>();
            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitForScene(RaceScene);
        }
    }
}
