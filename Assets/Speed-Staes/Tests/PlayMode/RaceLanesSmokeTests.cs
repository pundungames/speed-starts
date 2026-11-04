using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// Where the runners stand in the real Race scene: everybody on the road, the field around its middle, the player's lane changing from
    /// race to race; and the opening camera, in front of the runner at READY and the race camera's own place once it has swung round.
    /// </summary>
    public sealed class RaceLanesSmokeTests
    {
        private const float RoadHalfWidth = 5f;   // the road mesh is 11.2 m wide, curbs included
        private const int RaceAgainTries = 12;    // six lanes: twelve draws on the same one would be one chance in 6^11
        private const float SwungRoundSeconds = 1.5f;

        private SaveFolderSnapshot _save;
        private IRaceSelection _selection;
        private RaceEventId _selectedBefore;

        [SetUp]
        public void SetUp()
        {
            _save = SaveFolderSnapshot.Take();
            DiContainer container = ProjectContext.Instance.Container;
            _selection = container.Resolve<IRaceSelection>();
            _selectedBefore = _selection.Selected;
            foreach (RaceEventDefinition definition in container.Resolve<RaceEventCatalog>().GetEvents())
                if (definition.IsPlayable && definition.HasOpponents && definition.HurdleCount == 0 && !definition.IsRelay)
                {
                    _selection.Select(definition.Id);
                    break;
                }
        }

        [TearDown]
        public void TearDown()
        {
            if (!_selectedBefore.IsEmpty) _selection.Select(_selectedBefore);
            _save.Restore();
        }

        private static MonoBehaviour Adapter()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour is IRaceSession) return behaviour;
            return null;
        }

        private static MonoBehaviour Named(string typeName)
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour.GetType().Name == typeName) return behaviour;
            return null;
        }

        [UnityTest]
        public IEnumerator TheField_StaysOnTheRoad_AndThePlayersLaneChanges()
        {
            yield return LoadScene(RaceScene);
            MonoBehaviour adapter = Adapter();
            Assert.IsNotNull(adapter, "No IRaceSession in the Race scene.");
            var session = (IRaceSession)adapter;
            var signal = (IRaceStartSignal)adapter;
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready || signal.StartPhase == RaceStartPhase.Set, "the countdown", 15f);
            yield return null;

            MonoBehaviour bots = Named("RaceBots");
            Assert.IsNotNull(bots, "No RaceBots in the Race scene.");
            var roots = (IReadOnlyList<Transform>)bots.GetType().GetProperty("RunnerRoots").GetValue(bots);
            Component player = (Component)adapter.GetType().GetProperty("PlayerBody").GetValue(adapter);
            float middle = ((Vector3)bots.GetType().GetField("_basePosition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bots)).x;

            var seen = new HashSet<float>();
            for (int i = 0; i < RaceAgainTries; i++)
            {
                session.RequestStart(new RaceStartRequest(_selection.Selected, session.TargetDistance));
                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready, "READY");
                yield return null;
                float x = player.transform.position.x;
                seen.Add(Mathf.Round(x * 100f));
                Assert.Less(Mathf.Abs(x - middle), RoadHalfWidth, "the player must stay on the road");

                float sum = x - middle;
                int active = 1;
                var taken = new HashSet<float> { Mathf.Round(x * 100f) };
                foreach (Transform root in roots)
                {
                    if (!root.gameObject.activeInHierarchy) continue;
                    float botX = root.position.x;
                    Assert.Less(Mathf.Abs(botX - middle), RoadHalfWidth, $"{root.name} must stay on the road");
                    Assert.IsTrue(taken.Add(Mathf.Round(botX * 100f)), $"{root.name} shares a lane");
                    sum += botX - middle;
                    active++;
                }
                Assert.Greater(active, 1, "this race has a field");
                Assert.AreEqual(0f, sum / active, 0.01f, "the field stands around the road's middle");
            }
            Assert.Greater(seen.Count, 1, "the player's lane must not always be the same one");
        }

        [UnityTest]
        public IEnumerator TheRace_OpensInFrontOfTheRunner_ThenSwingsToTheRaceCamera()
        {
            yield return LoadScene(RaceScene);
            MonoBehaviour adapter = Adapter();
            Assert.IsNotNull(adapter, "No IRaceSession in the Race scene.");
            var session = (IRaceSession)adapter;
            var signal = (IRaceStartSignal)adapter;
            Component player = (Component)adapter.GetType().GetProperty("PlayerBody").GetValue(adapter);
            Camera camera = Camera.main;
            Assert.IsNotNull(camera);

            session.RequestStart(new RaceStartRequest(_selection.Selected, session.TargetDistance));
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready, "READY");
            yield return null;
            yield return null;
            Vector3 opening = camera.transform.position - player.transform.position;
            Assert.Greater(Vector3.Dot(opening, player.transform.forward), 1f, "it opens ahead of the runner");
            Assert.Greater(Vector3.Dot(opening, player.transform.right), 1f, "on the runner's right");

            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO", 15f);
            yield return new WaitForSecondsRealtime(SwungRoundSeconds);
            Vector3 race = camera.transform.position - player.transform.position;
            Assert.Greater(Vector3.Dot(race, player.transform.right), Mathf.Abs(Vector3.Dot(race, player.transform.forward)),
                "after the swing it is the race camera, at the runner's side");
        }
    }
}
