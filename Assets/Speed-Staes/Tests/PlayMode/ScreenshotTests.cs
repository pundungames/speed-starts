using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
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
    /// Evidence shots taken in Play mode: Home and Race with injected views, live numbers and the menu character posed,
    /// UI included. Only runs when the command line carries -shotDir <folder>; a normal test run ignores it and writes
    /// nothing. Overlay canvases do not reach a camera's RenderTexture, so for the shot they are switched to Screen Space -
    /// Camera and put back afterwards. The save is restored like in the button smoke tests.
    /// Run: Unity -batchmode (graphics on) -runTests -testPlatform PlayMode -testFilter Shots_HomeAndRace -shotDir &lt;folder&gt;
    /// </summary>
    public sealed class ScreenshotTests
    {
        private const int Width = 2400;
        private const int Height = 1080;
        private const float SettleSeconds = 1f;

        [UnityTest]
        public IEnumerator Shots_HomeAndRace()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: screenshot test only runs when asked for.");
            Directory.CreateDirectory(dir);

            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            bool captured = false;
            int entryTickets = 0, timedTickets = 0;
            long regenDue = 0, timedExpires = 0;
            RaceEventId selected = default;
            try
            {
                yield return LoadScene(HomeScene);

                DiContainer container = ProjectContext.Instance.Container;
                PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
                entryTickets = progress.EntryTickets;
                timedTickets = progress.TimedTickets;
                regenDue = progress.EntryRegenDueUtcTicks;
                timedExpires = progress.TimedAccessExpiresUtcTicks;
                selected = container.Resolve<IRaceSelection>().Selected;
                captured = true;

                HomeScreenView home = Require<HomeScreenView>();
                yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
                yield return Settle();
                yield return Capture(dir, "Home_play");

                // Home screens: open, shoot, close (same buttons and close path as the smoke tests).
                yield return ShootScreen(dir, home, "eventSelectButton", "eventSelectScreen", "Home_eventselect");
                yield return ShootScreen(dir, home, "environmentButton", "themeSelectScreen", "Home_themeselect");
                yield return ShootScreen(dir, home, "editRacerButton", "cosmeticsScreen", "Home_costume");
                yield return ShootScreen(dir, home, "highScoresButton", "highScoresScreen", "Home_leaderboard");
                yield return ShootScreen(dir, home, "emoteButton", "emoteScreen", "Home_emote");

                // Settings is a popup over Home (Home stays open behind its dimscreen).
                UiPopup settings = RequireField<UiPopup>(home, "settingsPopup");
                yield return Tap(RequireField<Button>(home, "settingsButton"), "settingsButton");
                yield return WaitOpen(settings, "settings popup");
                yield return Settle();
                yield return Capture(dir, "Home_settings");
                yield return Tap(CloseButtonOf(settings), "settings X");
                yield return WaitClosed(settings, "settings popup");
                yield return Settle();

                // Entry gate: Play with no ticket opens it.
                SelectTicketEvent(container);
                progress.EntryTickets = 0;
                progress.EntryRegenDueUtcTicks = 0;
                progress.TimedAccessExpiresUtcTicks = 0;
                container.Resolve<IEntryTickets>().Refresh();
                EntryGateModalView emptyGate = RequireField<EntryGateModalView>(home, "entryGateModal");
                yield return Tap(RequireField<Button>(home, "playButton"), "Play button (no ticket)");
                yield return WaitOpen(emptyGate, "entry gate");
                yield return Settle();
                yield return Capture(dir, "Home_entrygate");
                yield return Tap(CloseButtonOf(emptyGate), "entry gate Close");
                yield return WaitClosed(emptyGate, "entry gate");

                // Race: a ticketed event, exactly one ticket, then Use Entry Ticket.
                SelectTicketEvent(container);
                progress.EntryTickets = 1;
                progress.EntryRegenDueUtcTicks = 0;
                progress.TimedAccessExpiresUtcTicks = 0;
                container.Resolve<IEntryTickets>().Refresh();

                EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
                yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
                yield return WaitOpen(gate, "entry gate");
                yield return Tap(RequireField<Button>(gate, "useTicketButton"), "Use Entry Ticket");
                yield return WaitForScene(RaceScene);

                // Countdown: the SET word (or, on a very slow load, whatever is up by then), taken before the race moves on.
                IRaceStartSignal startSignal = null;
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                    if (behaviour is IRaceStartSignal found) { startSignal = found; break; }
                if (startSignal != null)
                {
                    yield return WaitUntil(() => startSignal.StartPhase == RaceStartPhase.Set || startSignal.StartPhase == RaceStartPhase.Go, "SET to show");
                    yield return null;
                    yield return null;
                    yield return Capture(dir, "Race_countdown");
                }

                yield return Settle();
                yield return Capture(dir, "Race_play");

                // Results: finish through the adapter's dev hook (dev builds only; reached by reflection, this assembly
                // cannot reference the Bridge), then shoot the results screen.
                IRaceSession session = null;
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                    if (behaviour is IRaceSession found) { session = found; break; }
                System.Reflection.MethodInfo finish = session?.GetType().GetMethod("DevForceFinish");
                if (finish != null)
                {
                    finish.Invoke(session, null);
                    ResultsScreenView results = UnityEngine.Object.FindObjectOfType<ResultsScreenView>(true);
                    yield return WaitUntil(() => results != null && results.gameObject.activeInHierarchy, "results screen");
                    yield return Settle();
                    yield return Capture(dir, "Race_results");

                    // Analysis: Stats on the results screen opens it (a force-finish at ~0 m shows the empty state, not a crash).
                    AnalysisScreenView analysis = RequireField<AnalysisScreenView>(results, "analysisScreen");
                    yield return Tap(RequireField<Button>(results, "statsButton"), "Results Stats");
                    yield return WaitOpen(analysis, "analysis screen");
                    yield return Settle();
                    yield return Capture(dir, "Race_analysis");
                }
            }
            finally
            {
                if (ProjectContext.HasInstance)
                {
                    DiContainer container = ProjectContext.Instance.Container;
                    container.Resolve<IRaceEntryGate>().TryTakePending(out _);
                    if (captured)
                    {
                        PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
                        progress.EntryTickets = entryTickets;
                        progress.TimedTickets = timedTickets;
                        progress.EntryRegenDueUtcTicks = regenDue;
                        progress.TimedAccessExpiresUtcTicks = timedExpires;
                        if (!selected.IsEmpty) container.Resolve<IRaceSelection>().Select(selected);
                    }
                }
                saveFiles.Restore();
            }
        }

        private static IEnumerator ShootScreen(string dir, HomeScreenView home, string buttonField, string screenField, string shotName)
        {
            UiScreen screen = RequireField<UiScreen>(home, screenField);
            yield return Tap(RequireField<Button>(home, buttonField), buttonField);
            yield return WaitOpen(screen, screenField);
            yield return WaitClosed(home, "HomeScreen to step aside");
            yield return Settle();
            yield return Capture(dir, shotName);

            yield return Tap(CloseButtonOf(screen), $"{screenField} Back");
            yield return WaitClosed(screen, screenField);
            yield return WaitOpen(home, "HomeScreen to come back");
            yield return Settle();
        }

        private static IEnumerator Settle()
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < SettleSeconds) yield return null;
        }

        private static IEnumerator Capture(string dir, string name)
        {
            // Not WaitForEndOfFrame: it never fires in batchmode (no player loop render), and the camera is rendered by hand below.
            yield return null;
            Camera camera = Camera.main;
            Assert.IsNotNull(camera, $"{name}: no main camera in the scene.");

            var restore = new List<(Canvas canvas, RenderMode mode, Camera cam, float plane)>();
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                restore.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.1f + canvas.sortingOrder * 0.001f;
            }
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(Width, Height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                string file = Path.Combine(dir, name + ".png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                Assert.IsTrue(File.Exists(file) && new FileInfo(file).Length > 0, $"{name}: shot file was not written.");
                Debug.Log($"SHOT_OK {file}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                foreach (var entry in restore)
                {
                    if (entry.canvas == null) continue;
                    entry.canvas.renderMode = entry.mode;
                    entry.canvas.worldCamera = entry.cam;
                    entry.canvas.planeDistance = entry.plane;
                }
                rt.Release();
                UnityEngine.Object.Destroy(rt);
                UnityEngine.Object.Destroy(tex);
            }
        }

        private static void SelectTicketEvent(DiContainer container)
        {
            RaceEventCatalog catalog = container.Resolve<RaceEventCatalog>();
            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                if (!definition.IsPlayable || !definition.RequiresTicket) continue;
                container.Resolve<IRaceSelection>().Select(definition.Id);
                return;
            }
            Assert.Ignore("The event catalog has no playable event that requires a ticket; the Race shot cannot be taken.");
        }

        private static string ArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
