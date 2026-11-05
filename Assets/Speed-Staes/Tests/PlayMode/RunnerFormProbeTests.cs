using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// T-107 measuring rig for the runner's form (not a pass/fail test): the real Race scene, the real prefab and pose driver, eleven
    /// scripted press scenarios fed through RunnerInput.Inject, every frame written to a csv and 12 frames of the steady part drawn
    /// from a close side camera into one contact sheet. Run it before and after a change and compare the files; it asserts nothing
    /// about balance, only that the scenarios ran and the files exist.
    ///
    /// Output in -shotDir (never Temp/, Unity empties it on exit), per scenario S:
    ///   form_S.csv (a row per frame), form_S_sheet.png (4x3, left to right and top to bottom, times in form_S_sheet.txt),
    ///   form_S_game.png (the game's own SideFollowCamera, same moment as cell 7), and form_summary.csv (one row per scenario).
    /// Scenarios: perfect rhythm26 late50 mash12 both30 same30 holdOne holdBoth tap012 tap016 human (see <see cref="Scenarios"/>).
    ///
    /// Run it in a verification copy WITHOUT -nographics (the shots need a renderer); unity-verify.ps1 only runs EditMode, so call
    /// Unity directly after kopya.ps1 -Al / -Senkron -HeadOnly (the file must be committed first):
    ///   $unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2-x86_64\Editor\Unity.exe'
    ///   Start-Process -Wait -FilePath $unity -ArgumentList '-batchmode','-projectPath','D:\Pundun\speed-starts-verify-1',
    ///     '-runTests','-testPlatform','PlayMode','-testFilter','RunnerForm_AllScenarios_WriteCsvAndFrames',
    ///     '-testResults','D:\Pundun\form-probe\result.xml','-logFile','D:\Pundun\form-probe\unity.log','-shotDir','D:\Pundun\form-probe\before'
    /// Optional: -probeSide left puts the probe camera on the runner's left (default right).
    /// </summary>
    public sealed class RunnerFormProbeTests
    {
        private const int Fps = 60;
        private const float Dt = 1f / Fps;
        private const int Cols = 4;
        private const int Rows = 3;
        private const int ShotCount = Cols * Rows;
        private const int CellW = 640;
        private const int CellH = 480;
        private const int GameW = 1600;
        private const int GameH = 720;
        private const int GameShotCell = 6;

        // Orthographic side view: 2.3 m tall, 3.07 m wide per cell, so the whole body and both strides fit whatever the runner's speed.
        private const float ProbeHalfHeight = 1.15f;
        private const float ProbeCenterHeight = 0.9f;
        private const float ProbeDistance = 2.5f;
        // Near plane at 1 m from the camera cuts everything between the camera and 1.5 m from the runner (lane fences, props).
        private const float ProbeNear = 1f;
        private const float ProbeFar = 6f;

        private const float FarRaceMeters = 2000f;
        private const int WarmupFrames = 18;
        private const float PerfectMinGap = 0.1f;
        private const float PerfectFallbackSeconds = 0.6f;
        private const float HoldSeconds = 2f;

        /// <summary>
        /// A "stutter" is a frame where a thigh's angular speed changes by more than this (deg/s in one frame), i.e. an angular
        /// acceleration spike of about 54000 deg/s^2 at 60 fps. A smooth swing (5 Hz, 40 deg amplitude) changes ~650 deg/s per
        /// frame; a phase hand-off that breaks the curve shows up well above. A first guess, tuned from the first baseline: change
        /// it here only together with a new baseline, since the counts depend on it.
        /// </summary>
        private const float StutterJumpDegPerSec = 900f;

        /// <summary>A thigh slower than this (deg/s) is "holding" its angle; faster than <see cref="SwapRateDegPerSec"/> it is in a swap.</summary>
        private const float HoldRateDegPerSec = 120f;
        private const float SwapRateDegPerSec = 600f;
        /// <summary>A leg whose world thigh is within this many degrees of straight down is passing the other one.</summary>
        private const float PassThighDeg = 15f;
        /// <summary>A foot touches the ground when its toe bone is this close (m) to its height at rest...</summary>
        private const float TouchToleranceMeters = 0.02f;
        /// <summary>...or its foot bone is this close (m) to its height at rest: a foot landing on the heel has its toe in the air.</summary>
        private const float FootTouchToleranceMeters = 0.035f;
        /// <summary>Below this largest split (deg) the hip-versus-split figures are left empty (the legs never open).</summary>
        private const float SplitMinDeg = 1f;
        /// <summary>The share of frames, by split size, that counts as "wide" (top) and "passing" (bottom).</summary>
        private const float SplitEdgeShare = 0.2f;

        /// <summary>The first-seconds speed step is the largest gain in any window of <see cref="StepWindowSeconds"/> within this many seconds.</summary>
        private const float FirstSecondsWindow = 2f;
        private const float StepWindowSeconds = 0.1f;

        private enum Mode { Alternate, SameLeg, Both, Perfect, HoldOne, HoldBoth, Swap }

        private sealed class Scenario
        {
            public string Name;
            public Mode Mode;
            public float Period;          // seconds between presses (not for Perfect / holds)
            public float Hold = 0.07f;    // key down time
            public float HoldMin, HoldMax; // both > 0: each press draws its own key down time from this range (seeded)
            public float PeriodJitter;    // gap between presses is Period +- this, drawn from the seeded generator
            public int Seed = 20261104;
            public float Duration = 6f;
            public float SummaryFrom = 3f;
            public float ShotFrom = 4f;
            public float ShotWindow;      // 0 = two steps of the scenario's own rhythm
        }

        private static Scenario[] Scenarios() => new[]
        {
            new Scenario { Name = "perfect", Mode = Mode.Perfect },
            new Scenario { Name = "rhythm026", Mode = Mode.Alternate, Period = 0.26f },
            new Scenario { Name = "late050", Mode = Mode.Alternate, Period = 0.50f },
            new Scenario { Name = "mash012", Mode = Mode.Alternate, Period = 0.125f, Hold = 0.04f },
            new Scenario { Name = "both030", Mode = Mode.Both, Period = 0.30f },
            new Scenario { Name = "same030", Mode = Mode.SameLeg, Period = 0.30f },
            new Scenario { Name = "same020", Mode = Mode.SameLeg, Period = 0.20f },
            new Scenario { Name = "same025", Mode = Mode.SameLeg, Period = 0.25f },
            new Scenario { Name = "holdOne", Mode = Mode.HoldOne, SummaryFrom = 0.5f, ShotFrom = 0.5f, ShotWindow = 3f },
            new Scenario { Name = "holdBoth", Mode = Mode.HoldBoth, SummaryFrom = 0.5f, ShotFrom = 0.5f, ShotWindow = 3f },
            // A person holds the key longer than 0.07 s and unevenly; PushTime (~0.11 s at speed) may be crossed by a normal tap.
            // Alternating legs: a key's next press is at least two gaps (0.46 s) away, always after its release (hold <= 0.16 s).
            new Scenario { Name = "tap012", Mode = Mode.Alternate, Period = 0.26f, Hold = 0.12f },
            new Scenario { Name = "tap016", Mode = Mode.Alternate, Period = 0.28f, Hold = 0.16f },
            new Scenario { Name = "rough", Mode = Mode.Alternate, Period = 0.30f, PeriodJitter = 0.10f, HoldMin = 0.05f, HoldMax = 0.18f, Seed = 7 },
            new Scenario { Name = "hopRough", Mode = Mode.SameLeg, Period = 0.27f, PeriodJitter = 0.08f, HoldMin = 0.05f, HoldMax = 0.18f, Seed = 11 },
            // One key for a few presses, then the keys in turn, and again: the legs change between a hop and a run with no mode.
            new Scenario { Name = "swap", Mode = Mode.Swap, Period = 0.27f, PeriodJitter = 0.05f, HoldMin = 0.05f, HoldMax = 0.16f, Seed = 13 },
            new Scenario { Name = "human", Mode = Mode.Alternate, Period = 0.27f, PeriodJitter = 0.04f, HoldMin = 0.06f, HoldMax = 0.16f },
        };

        private const string SummaryHeader =
            "scenario,presses,fallbackPresses,frames,speedAvg,speedPeak,trunkLeanAvg,trunkLeanMax," +
            "L_thighWorldMin,L_thighWorldMax,R_thighWorldMin,R_thighWorldMax,splitMax,thighMaxDegPerSec," +
            "stutters,stutterThresholdDegPerSec,hipYMin,hipYMax,kneeYMin," +
            "armL_swingMin,armL_swingMax,armR_swingMin,armR_swingMax," +
            "hipsYawRange,hipsRollRange,chestYawRange,chestRollRange,shots," +
            "holdShare,swapShare,corrHipSplit,hipAtWide,hipAtPass,passKnee,flightShare,footSlide,steppingAvg," +
            "holdFrames,speedRipple,firstSecondSpeedSteps";

        [UnityTest]
        [Explicit("Measuring rig: run on request with -shotDir (see the class summary).")]
        [Category("RunnerProbe")]
        public IEnumerator RunnerForm_AllScenarios_WriteCsvAndFrames()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: the measuring rig only runs when asked for.");
            Directory.CreateDirectory(dir);

            int previousFps = Time.captureFramerate;
            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            DiContainer container = ProjectContext.Instance.Container;
            IRaceSelection selection = container.Resolve<IRaceSelection>();
            RaceEventId selectedBefore = selection.Selected;
            var summary = new List<string> { SummaryHeader };
            try
            {
                // Fixed 60 fps game time: the same input timing gives the same frames on any machine.
                Time.captureFramerate = Fps;
                foreach (Scenario scenario in Scenarios())
                {
                    PickFlatEvent(container, selection);
                    yield return RunScenario(dir, scenario, summary);
                    File.WriteAllLines(Path.Combine(dir, "form_summary.csv"), summary);
                }
            }
            finally
            {
                Time.captureFramerate = previousFps;
                if (!selectedBefore.IsEmpty) selection.Select(selectedBefore);
                saveFiles.Restore();
            }

            Assert.AreEqual(Scenarios().Length + 1, summary.Count, "every scenario must add a summary row");
            Assert.IsTrue(File.Exists(Path.Combine(dir, "form_summary.csv")), "form_summary.csv was not written");
        }

        /// <summary>
        /// The player's runner in the nearest lane, then in the farthest one, from the race camera (a farther lane makes it back off).
        /// Writes form_lane_near_game.png and form_lane_far_game.png (plus the usual csv and sheet under the same names) and a CAMERA_OK line each.
        /// </summary>
        [UnityTest]
        [Explicit("Measuring rig: run on request with -shotDir (see the class summary).")]
        [Category("RunnerProbe")]
        public IEnumerator RaceCamera_FarLanes_WriteShots()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: the measuring rig only runs when asked for.");
            Directory.CreateDirectory(dir);

            int previousFps = Time.captureFramerate;
            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            DiContainer container = ProjectContext.Instance.Container;
            IRaceSelection selection = container.Resolve<IRaceSelection>();
            RaceEventId selectedBefore = selection.Selected;
            PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
            int laneBefore = progress.LanePreference;
            var summary = new List<string> { SummaryHeader };
            try
            {
                Time.captureFramerate = Fps;
                // 1 = nearest lane; a preference past the last lane is the last lane (BotLanes.PlayerSlot), so 99 is the farthest.
                foreach ((string name, int lane) in new[] { ("lane_near", 1), ("lane_far", 99) })
                {
                    PickFlatEvent(container, selection, true); // lanes only differ when there is a field around the runner
                    progress.LanePreference = lane;
                    var scenario = new Scenario { Name = name, Mode = Mode.Alternate, Period = 0.26f, Duration = 3f, SummaryFrom = 1.5f, ShotFrom = 2f };
                    yield return RunScenario(dir, scenario, summary);
                    Assert.IsTrue(File.Exists(Path.Combine(dir, $"form_{name}_game.png")), $"form_{name}_game.png was not written");
                }
            }
            finally
            {
                Time.captureFramerate = previousFps;
                progress.LanePreference = laneBefore;
                if (!selectedBefore.IsEmpty) selection.Select(selectedBefore);
                saveFiles.Restore();
            }
        }

        /// <summary>
        /// A flat, no-relay event (hurdles would change the run); one without opponents when there is one, so nothing stands in the shot
        /// (or, with <paramref name="withOpponents"/>, one that has them).
        /// </summary>
        private static void PickFlatEvent(DiContainer container, IRaceSelection selection, bool withOpponents = false)
        {
            RaceEventDefinition best = null;
            foreach (RaceEventDefinition definition in container.Resolve<RaceEventCatalog>().GetEvents())
            {
                if (!definition.IsPlayable || definition.HurdleCount != 0 || definition.IsRelay) continue;
                if (best == null) best = definition;
                else if (withOpponents ? (!best.HasOpponents && definition.HasOpponents) : (best.HasOpponents && !definition.HasOpponents)) best = definition;
            }
            if (best != null) selection.Select(best.Id);
        }

        private static IEnumerator RunScenario(string dir, Scenario scenario, List<string> summary)
        {
            // A fresh scene per scenario: no runner state (head, legs, speed) carries over.
            yield return LoadScene(RaceScene);
            MonoBehaviour adapter = null;
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour is IRaceSession) { adapter = behaviour; break; }
            Assert.IsNotNull(adapter, "No IRaceSession in the Race scene.");
            var session = (IRaceSession)adapter;
            var signal = (IRaceStartSignal)adapter;

            // A press before GO is a false start (FAIL), so everything waits for GO and the running state first.
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO", 60f);
            yield return WaitUntil(() => session.State == RaceSessionState.Running, "the race to run after GO", 20f);

            // The intro swing runs on unscaled time, so under captureFramerate it would still be turning during the scenario: end it, the
            // race camera underneath renders (OnDisable restores the field of view). Found by name: the test assembly cannot see it.
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour.GetType().Name == "RaceStartCamera") behaviour.enabled = false;

            Component race = RequireField<Component>(session, "race");
            FieldInfo length = race.GetType().GetField("raceDistance");
            Assert.IsNotNull(length, "RaceController.raceDistance is gone; the race cannot be lengthened.");
            length.SetValue(race, Mathf.Max((float)length.GetValue(race), FarRaceMeters));

            object bodyObject = adapter.GetType().GetProperty("PlayerBody").GetValue(adapter);
            var body = (Component)bodyObject;
            Type driverType = body.GetType().Assembly.GetType("SpeedStars.Physical.RunnerPoseDriver");
            Assert.IsNotNull(driverType, "SpeedStars.Physical.RunnerPoseDriver is gone.");
            Component driver = body.GetComponentInChildren(driverType);
            Assert.IsNotNull(driver, "the player's runner has no pose driver");
            object input = body.GetType().GetField("input").GetValue(body);
            MethodInfo inject = input.GetType().GetMethod("Inject");
            Assert.IsNotNull(inject, "RunnerInput.Inject is gone; the rig cannot press.");

            Camera gameCamera = Camera.main;
            Assert.IsNotNull(gameCamera, "no main camera in the Race scene");
            var probeObject = new GameObject("RunnerFormProbeCamera");
            Camera probe = probeObject.AddComponent<Camera>();
            probe.CopyFrom(gameCamera);
            probe.enabled = false; // rendered by hand only; untagged, so Camera.main stays the game's
            probe.orthographic = true;
            probe.orthographicSize = ProbeHalfHeight;
            probe.nearClipPlane = ProbeNear;
            probe.farClipPlane = ProbeFar;

            var others = new List<Renderer>();
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                if (behaviour.GetType().Name == "RunnerBody" && behaviour != (MonoBehaviour)body)
                    others.AddRange(behaviour.GetComponentsInChildren<Renderer>(true));

            var run = new Run(scenario, body, driver, bodyObject, input, inject, probe, gameCamera, others, ProbeSign());
            var hookObject = new GameObject("RunnerFormProbeHook");
            LateHook hook = hookObject.AddComponent<LateHook>();
            try
            {
                for (int i = 0; i < WarmupFrames; i++) yield return null;

                run.Begin();
                // Samples are taken in LateUpdate (after the rig and the driver's own LateUpdate); without the hook, right after the frame.
                if (hook != null) hook.OnLate = run.Sample;
                while (true)
                {
                    double t = Time.timeAsDouble - run.Start;
                    if (t >= scenario.Duration) break;
                    run.Tick(t);
                    yield return null;
                    if (hook == null) run.Sample();
                }
                run.End();
            }
            finally
            {
                run.RestoreOthers();
                if (hookObject != null) UnityEngine.Object.Destroy(hookObject);
                UnityEngine.Object.Destroy(probeObject);
            }

            run.WriteFiles(dir);
            summary.Add(run.SummaryRow());
            Assert.Greater(run.FrameCount, 0, $"{scenario.Name}: no frame was recorded");
            Assert.AreEqual(ShotCount, run.ShotsTaken, $"{scenario.Name}: the contact sheet is incomplete");
            UnityEngine.Debug.Log($"FORM_OK {scenario.Name}: {run.FrameCount} frames, {run.PressCount} presses");
        }

        private static float ProbeSign()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-probeSide") return args[i + 1] == "left" ? -1f : 1f;
            return 1f;
        }

        private static string ArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        /// <summary>Calls back after every LateUpdate, last in the order: the pose is final by then.</summary>
        [DefaultExecutionOrder(32000)]
        private sealed class LateHook : MonoBehaviour
        {
            public Action OnLate;
            private void LateUpdate() => OnLate?.Invoke();
        }

        private static string F(float v) => float.IsNaN(v) ? "nan" : v.ToString("0.####", CultureInfo.InvariantCulture);

        /// <summary>Like F, but a value that does not exist is an empty cell.</summary>
        private static string FE(float v) => float.IsNaN(v) ? string.Empty : v.ToString("0.####", CultureInfo.InvariantCulture);

        /// <summary>Yaw (angle in the ground plane against the runner's right) and roll (elevation out of that plane) of a bone's right vector.</summary>
        private static void YawRoll(Quaternion turn, out float yaw, out float roll)
        {
            Vector3 v = turn * Vector3.right; // runner frame: x right, y up, z forward
            yaw = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
            roll = Mathf.Asin(Mathf.Clamp(v.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static float Wrap(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Color32[] Grab(Camera camera, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                // Not WaitForEndOfFrame (never fires in batchmode): the camera is rendered by hand.
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                return tex.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(tex);
            }
        }

        private static void SavePng(string path, Color32[] pixels, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
            Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length > 0, $"{Path.GetFileName(path)} was not written");
        }

        private struct Fr
        {
            public float t, speed, trunk, thL, thR, twL, twR, hipY, kneeL, kneeR, armL, armR, hipsYaw, hipsRoll, chestYaw, chestRoll;
            public float kneeAngL, kneeAngR, toeL, toeR, footL, footR, fzL, fzR, stepping, heldL, heldR;
            public bool holdPhase; // either leg's phase is Hold
        }

        /// <summary>One scenario's state: scripted input, per-frame samples and the contact sheet cells.</summary>
        private sealed class Run
        {
            private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            private readonly Scenario _sc;
            private readonly Component _body, _driver;
            private readonly object _input, _sim;
            private readonly MethodInfo _inject;
            private readonly Camera _probe, _game;
            private readonly List<Renderer> _others;
            private readonly bool[] _othersWere;
            private readonly float _side;
            private readonly Transform _runner, _hipTarget, _hipsBone, _chestBone, _toeL, _toeR, _footL, _footR;
            private readonly FieldInfo _stepping;
            private float _restToeL, _restToeR, _restFootL, _restFootR;
            private readonly object _legL, _legR, _armL, _armR;
            private readonly System.Random _rng;
            private readonly FieldInfo _held;
            private readonly FieldInfo _phase, _angles, _thighWorld, _legKnee, _legAnkle, _angThigh, _angKnee, _swing, _elbow;
            private readonly PropertyInfo _speed, _head, _hop, _airborne, _lastResult, _lastEff;
            private readonly float _groundY;

            private readonly List<string> _csv = new List<string>();
            private readonly List<Fr> _frames = new List<Fr>();
            private readonly List<Color32[]> _cells = new List<Color32[]>();
            private readonly List<float> _cellTimes = new List<float>();
            private readonly List<(double time, int leg)> _releases = new List<(double, int)>();
            private readonly List<double> _pressTimes = new List<double>();
            private Quaternion _hipsRest, _chestRest;
            private bool _haveRest, _holdStarted, _airSincePress, _active;
            private double _nextPress, _lastPress;
            private int _frame = -1, _shotStart = -1, _shotStep, _fallbacks;

            public double Start { get; private set; }
            public int FrameCount => _frames.Count;
            public int ShotsTaken => _cells.Count;
            public int PressCount => _pressTimes.Count;

            public Run(Scenario sc, Component body, Component driver, object bodyObject, object input, MethodInfo inject,
                Camera probe, Camera game, List<Renderer> others, float side)
            {
                _sc = sc; _rng = new System.Random(sc.Seed); _body = body; _driver = driver; _input = input; _inject = inject;
                _probe = probe; _game = game; _others = others; _side = side;
                _othersWere = new bool[others.Count];
                _sim = bodyObject.GetType().GetProperty("Sim").GetValue(bodyObject);
                Type simType = _sim.GetType();
                _speed = simType.GetProperty("Speed"); _head = simType.GetProperty("HeadAngle"); _hop = simType.GetProperty("HopY");
                _airborne = simType.GetProperty("Airborne"); _lastResult = simType.GetProperty("LastResult");
                _lastEff = simType.GetProperty("LastEfficiency");

                Type d = driver.GetType();
                _legL = d.GetField("leftLeg").GetValue(driver); _legR = d.GetField("rightLeg").GetValue(driver);
                _armL = d.GetField("leftArm").GetValue(driver); _armR = d.GetField("rightArm").GetValue(driver);
                _hipTarget = (Transform)d.GetField("hipTarget").GetValue(driver);
                Type leg = _legL.GetType();
                _held = leg.GetField("held", Any);
                Assert.IsNotNull(_held, "RunnerPoseDriver leg field 'held' is gone; L_held/R_held cannot be read.");
                _phase = leg.GetField("phase"); _angles = leg.GetField("angles"); _thighWorld = leg.GetField("thighWorld");
                _legKnee = leg.GetField("knee"); _legAnkle = leg.GetField("ankle");
                Type ang = _angles.FieldType;
                _angThigh = ang.GetField("thigh"); _angKnee = ang.GetField("knee");
                Type arm = _armL.GetType();
                _swing = arm.GetField("swing"); _elbow = arm.GetField("elbow");

                _runner = driver.transform;
                _hipsBone = FindDeep(_runner, "B-hips");
                _chestBone = FindDeep(_runner, "B-chest");
                _toeL = FindDeep(_runner, "B-toe.L"); _toeR = FindDeep(_runner, "B-toe.R");
                _footL = FindDeep(_runner, "B-foot.L"); _footR = FindDeep(_runner, "B-foot.R");
                _stepping = d.GetField("_stepping", Any);
                Assert.IsNotNull(_toeL, "B-toe.L not found"); Assert.IsNotNull(_toeR, "B-toe.R not found");
                Assert.IsNotNull(_footL, "B-foot.L not found"); Assert.IsNotNull(_footR, "B-foot.R not found");
                Assert.IsNotNull(_hipTarget, "RunnerPoseDriver.hipTarget is not bound");
                Assert.IsNotNull(_hipsBone, "B-hips not found under the pose driver");
                Assert.IsNotNull(_chestBone, "B-chest not found under the pose driver");
                _groundY = body.transform.position.y;
            }

            public void Begin()
            {
                Start = Time.timeAsDouble;
                _active = true;
                // Rest heights, measured once while the runner still stands (before the first press).
                _restToeL = RelY(_toeL); _restToeR = RelY(_toeR); _restFootL = RelY(_footL); _restFootR = RelY(_footR);
                _csv.Add("t,speed,head,hopY,lastResult,lastEfficiency," + LegColumns("L") + "," + LegColumns("R") +
                         ",hipY,hipPitch,trunkLean,hips_yaw,hips_roll,hips_pitch,chest_yaw,chest_roll,chest_pitch," +
                         "armL_swing,armL_elbow,armR_swing,armR_elbow," +
                         "L_ankleZ,R_ankleZ,L_toeY,R_toeY,L_footY,R_footY,L_footWorldZ,R_footWorldZ,stepping,L_held,R_held");
            }

            /// <summary>World Y of a bone minus the runner root's Y.</summary>
            private float RelY(Transform bone) => bone.position.y - _runner.position.y;

            /// <summary>Leg.held as a number (a bool reads 1 or 0), NaN when it cannot be converted.</summary>
            private float ReadHeld(object leg)
            {
                object v = _held.GetValue(leg);
                if (v is bool b) return b ? 1f : 0f;
                try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
                catch (Exception) { return float.NaN; }
            }

            private float ReadStepping()
            {
                if (_stepping == null) return float.NaN;
                object v = _stepping.GetValue(_driver);
                if (v is bool b) return b ? 1f : 0f;
                try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
                catch (Exception) { return float.NaN; }
            }

            public void End()
            {
                _active = false;
                foreach (var release in _releases) Send(release.leg, false);
                _releases.Clear();
            }

            private static string LegColumns(string p) => $"{p}_phase,{p}_thigh,{p}_thighWorld,{p}_knee,{p}_ankleY,{p}_kneeY";

            // ---------------------------------------------------------------- scripted input

            private void Send(int leg, bool down) => _inject.Invoke(_input, new object[] { leg, down, Time.timeAsDouble });

            private void Press(double t, params int[] legs)
            {
                float hold = _sc.HoldMin > 0f && _sc.HoldMax > 0f
                    ? _sc.HoldMin + (float)_rng.NextDouble() * (_sc.HoldMax - _sc.HoldMin)
                    : _sc.Hold;
                foreach (int leg in legs)
                {
                    Send(leg, true);
                    _releases.Add((t + hold, leg));
                }
                _pressTimes.Add(t);
                _lastPress = t;
                _airSincePress = false;
            }

            /// <summary>Called once per frame, before the frame's pose is computed.</summary>
            public void Tick(double t)
            {
                for (int i = _releases.Count - 1; i >= 0; i--)
                {
                    if (t < _releases[i].time) continue;
                    Send(_releases[i].leg, false);
                    _releases.RemoveAt(i);
                }

                switch (_sc.Mode)
                {
                    case Mode.Alternate:
                    case Mode.SameLeg:
                    case Mode.Both:
                    case Mode.Swap:
                        while (t >= _nextPress)
                        {
                            int n = _pressTimes.Count;
                            if (_sc.Mode == Mode.Both) Press(t, 0, 1);
                            else if (_sc.Mode == Mode.Swap) Press(t, (n / 6) % 2 == 0 ? 0 : n % 2);
                            else Press(t, _sc.Mode == Mode.SameLeg ? 0 : n % 2);
                            _nextPress += _sc.Period + (_sc.PeriodJitter > 0f ? ((float)_rng.NextDouble() * 2f - 1f) * _sc.PeriodJitter : 0f);
                        }
                        break;

                    case Mode.Perfect:
                        // Press on the landing: the frame the sim stops being airborne after having been, the other leg each time.
                        // If it never lands (no hop), press anyway after a while so the scenario goes on; those are counted.
                        bool air = (bool)_airborne.GetValue(_sim);
                        if (air) _airSincePress = true;
                        if (_pressTimes.Count == 0) Press(t, 0);
                        else
                        {
                            bool landed = _airSincePress && !air && t - _lastPress >= PerfectMinGap;
                            bool overdue = t - _lastPress >= PerfectFallbackSeconds;
                            if (landed || overdue)
                            {
                                if (!landed) _fallbacks++;
                                Press(t, _pressTimes.Count % 2);
                            }
                        }
                        break;

                    case Mode.HoldOne:
                    case Mode.HoldBoth:
                        if (!_holdStarted)
                        {
                            _holdStarted = true;
                            _pressTimes.Add(t);
                            int[] legs = _sc.Mode == Mode.HoldBoth ? new[] { 0, 1 } : new[] { 0 };
                            foreach (int leg in legs)
                            {
                                Send(leg, true);
                                _releases.Add((t + HoldSeconds, leg));
                            }
                        }
                        break;
                }
            }

            // ---------------------------------------------------------------- sampling

            public void Sample()
            {
                if (!_active) return;
                _frame++;
                double t = Time.timeAsDouble - Start;
                float speed = (float)_speed.GetValue(_sim);

                Quaternion runnerInv = Quaternion.Inverse(_runner.rotation);
                Quaternion hipsRel = runnerInv * _hipsBone.rotation;
                Quaternion chestRel = runnerInv * _chestBone.rotation;
                if (!_haveRest)
                {
                    // Bone axes differ from the runner's: angles are reported as the turn since the first sample, in the runner's frame.
                    _haveRest = true;
                    _hipsRest = hipsRel;
                    _chestRest = chestRel;
                }
                Quaternion hipsTurn = hipsRel * Quaternion.Inverse(_hipsRest);
                Quaternion chestTurn = chestRel * Quaternion.Inverse(_chestRest);
                Vector3 hipsEuler = hipsTurn.eulerAngles;   // only the pitch column still comes from Euler
                Vector3 chestEuler = chestTurn.eulerAngles;
                // Yaw and roll from the turned right vector: a pure forward pitch leaves it alone, so both read 0 at any pitch.
                YawRoll(hipsTurn, out float hipsYaw, out float hipsRoll);
                YawRoll(chestTurn, out float chestYaw, out float chestRoll);
                Vector3 trunk = _runner.InverseTransformVector(_chestBone.position - _hipsBone.position);
                float trunkLean = Mathf.Atan2(trunk.z, trunk.y) * Mathf.Rad2Deg;
                Vector3 hipLocal = _runner.InverseTransformPoint(_hipTarget.position);
                float hipPitch = Wrap((runnerInv * _hipTarget.rotation).eulerAngles.x);

                string legL = ReadLeg(_legL, out float thL, out float twL, out float kneeL, out float kaL, out float azL, out float heldL, out bool holdL);
                string legR = ReadLeg(_legR, out float thR, out float twR, out float kneeR, out float kaR, out float azR, out float heldR, out bool holdR);
                Vector3 forward = _runner.forward;
                float toeLy = RelY(_toeL), toeRy = RelY(_toeR), footLy = RelY(_footL), footRy = RelY(_footR);
                float fzL = Vector3.Dot(_footL.position, forward), fzR = Vector3.Dot(_footR.position, forward);
                float stepping = ReadStepping();
                float aLs = (float)_swing.GetValue(_armL), aLe = (float)_elbow.GetValue(_armL);
                float aRs = (float)_swing.GetValue(_armR), aRe = (float)_elbow.GetValue(_armR);

                var fr = new Fr
                {
                    t = (float)t, speed = speed, trunk = trunkLean, thL = thL, thR = thR, twL = twL, twR = twR, hipY = hipLocal.y,
                    kneeL = kneeL, kneeR = kneeR, armL = aLs, armR = aRs,
                    hipsYaw = hipsYaw, hipsRoll = hipsRoll, chestYaw = chestYaw, chestRoll = chestRoll,
                    kneeAngL = kaL, kneeAngR = kaR, toeL = toeLy, toeR = toeRy, footL = footLy, footR = footRy,
                    fzL = fzL, fzR = fzR, stepping = stepping, heldL = heldL, heldR = heldR, holdPhase = holdL || holdR,
                };
                _frames.Add(fr);

                var sb = new StringBuilder(256);
                sb.Append(F((float)t)).Append(',').Append(F(speed)).Append(',').Append(F((float)_head.GetValue(_sim))).Append(',')
                  .Append(F((float)_hop.GetValue(_sim))).Append(',').Append(_lastResult.GetValue(_sim)).Append(',')
                  .Append(F((float)_lastEff.GetValue(_sim))).Append(',').Append(legL).Append(',').Append(legR).Append(',')
                  .Append(F(hipLocal.y)).Append(',').Append(F(hipPitch)).Append(',').Append(F(trunkLean)).Append(',')
                  .Append(F(hipsYaw)).Append(',').Append(F(hipsRoll)).Append(',').Append(F(Wrap(hipsEuler.x))).Append(',')
                  .Append(F(chestYaw)).Append(',').Append(F(chestRoll)).Append(',').Append(F(Wrap(chestEuler.x))).Append(',')
                  .Append(F(aLs)).Append(',').Append(F(aLe)).Append(',').Append(F(aRs)).Append(',').Append(F(aRe)).Append(',')
                  .Append(F(azL)).Append(',').Append(F(azR)).Append(',').Append(F(toeLy)).Append(',').Append(F(toeRy)).Append(',')
                  .Append(F(footLy)).Append(',').Append(F(footRy)).Append(',').Append(F(fzL)).Append(',').Append(F(fzR)).Append(',')
                  .Append(F(stepping)).Append(',').Append(F(heldL)).Append(',').Append(F(heldR));
                _csv.Add(sb.ToString());

                TakeShotIfDue((float)t);
            }

            private string ReadLeg(object leg, out float thigh, out float thighWorld, out float kneeY, out float kneeAngle, out float ankleZ,
                out float held, out bool inHold)
            {
                held = ReadHeld(leg);
                inHold = _phase.GetValue(leg).ToString() == "Hold";
                object angles = _angles.GetValue(leg);
                thigh = (float)_angThigh.GetValue(angles);
                kneeAngle = (float)_angKnee.GetValue(angles);
                thighWorld = (float)_thighWorld.GetValue(leg);
                Vector3 ankle = (Vector3)_legAnkle.GetValue(leg);
                float ankleY = ankle.y;
                ankleZ = ankle.z;
                kneeY = ((Vector3)_legKnee.GetValue(leg)).y;
                return $"{_phase.GetValue(leg)},{F(thigh)},{F(thighWorld)},{F(kneeAngle)},{F(ankleY)},{F(kneeY)}";
            }

            // ---------------------------------------------------------------- frames

            private float StepSeconds()
            {
                if (_sc.Mode != Mode.Perfect) return _sc.Period;
                int n = _pressTimes.Count;
                if (n < 3) return 0.6f;
                int from = Mathf.Max(0, n - 5);
                return (float)((_pressTimes[n - 1] - _pressTimes[from]) / (n - 1 - from));
            }

            private void TakeShotIfDue(float t)
            {
                if (t < _sc.ShotFrom || _cells.Count >= ShotCount) return;
                if (_shotStart < 0)
                {
                    // Twelve cells over two steps of this scenario's rhythm (or the scenario's fixed window), at least a frame apart.
                    float window = _sc.ShotWindow > 0f ? _sc.ShotWindow : 2f * StepSeconds();
                    _shotStep = Mathf.Max(1, Mathf.RoundToInt(window / ShotCount / Dt));
                    _shotStart = _frame;
                }
                if (_frame - _shotStart < _cells.Count * _shotStep) return;

                SetOthersVisible(false);
                Vector3 center = new Vector3(_body.transform.position.x, _groundY + ProbeCenterHeight, _body.transform.position.z);
                Vector3 side = _body.transform.right * _side;
                _probe.transform.position = center + side * ProbeDistance;
                _probe.transform.rotation = Quaternion.LookRotation(-side, Vector3.up);
                _cells.Add(Grab(_probe, CellW, CellH));
                SetOthersVisible(true);
                _cellTimes.Add(t);

                if (_cells.Count - 1 == GameShotCell)
                {
                    _gamePixels = Grab(_game, GameW, GameH);
                    LogCamera();
                }
            }

            /// <summary>The game camera's angles as numbers: yaw on the ground from the runner's right (+ toward its front), pitch downward.</summary>
            private void LogCamera()
            {
                Transform runner = _body.transform;
                Vector3 toCamera = Vector3.ProjectOnPlane(_game.transform.position - runner.position, Vector3.up);
                float yaw = Mathf.Atan2(Vector3.Dot(toCamera, runner.forward), Vector3.Dot(toCamera, runner.right)) * Mathf.Rad2Deg;
                float pitch = Mathf.Asin(Mathf.Clamp(-_game.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                UnityEngine.Debug.Log($"CAMERA_OK {_sc.Name} yawDeg={F(yaw)} pitchDeg={F(pitch)} fov={F(_game.fieldOfView)}");
            }

            private Color32[] _gamePixels;

            private void SetOthersVisible(bool visible)
            {
                for (int i = 0; i < _others.Count; i++)
                {
                    if (_others[i] == null) continue;
                    if (!visible) { _othersWere[i] = _others[i].enabled; _others[i].enabled = false; }
                    else _others[i].enabled = _othersWere[i];
                }
            }

            public void RestoreOthers() => SetOthersVisible(true);

            // ---------------------------------------------------------------- output

            public void WriteFiles(string dir)
            {
                File.WriteAllLines(Path.Combine(dir, $"form_{_sc.Name}.csv"), _csv);

                int sheetW = Cols * CellW, sheetH = Rows * CellH;
                var sheet = new Color32[sheetW * sheetH];
                for (int i = 0; i < _cells.Count; i++)
                {
                    int col = i % Cols, row = i / Cols;
                    int top = (Rows - 1 - row) * CellH; // pixel rows count from the bottom
                    for (int y = 0; y < CellH; y++)
                        Array.Copy(_cells[i], y * CellW, sheet, (top + y) * sheetW + col * CellW, CellW);
                }
                SavePng(Path.Combine(dir, $"form_{_sc.Name}_sheet.png"), sheet, sheetW, sheetH);

                var times = new List<string> { "cell,t" };
                for (int i = 0; i < _cellTimes.Count; i++) times.Add($"{i + 1},{F(_cellTimes[i])}");
                File.WriteAllLines(Path.Combine(dir, $"form_{_sc.Name}_sheet.txt"), times);

                if (_gamePixels != null) SavePng(Path.Combine(dir, $"form_{_sc.Name}_game.png"), _gamePixels, GameW, GameH);
                UnityEngine.Debug.Log($"SHOT_OK {Path.Combine(dir, $"form_{_sc.Name}_sheet.png")}");
            }

            public string SummaryRow()
            {
                var w = new List<Fr>();
                foreach (Fr f in _frames)
                    if (f.t >= _sc.SummaryFrom) w.Add(f);

                float speedSum = 0f, speedPeak = float.MinValue, trunkSum = 0f, trunkMax = float.MinValue;
                float lMin = float.MaxValue, lMax = float.MinValue, rMin = float.MaxValue, rMax = float.MinValue;
                float split = 0f, omegaMax = 0f;
                float hipMin = float.MaxValue, hipMax = float.MinValue, kneeMin = float.MaxValue;
                float alMin = float.MaxValue, alMax = float.MinValue, arMin = float.MaxValue, arMax = float.MinValue;
                float hyMin = float.MaxValue, hyMax = float.MinValue, hrMin = float.MaxValue, hrMax = float.MinValue;
                float cyMin = float.MaxValue, cyMax = float.MinValue, crMin = float.MaxValue, crMax = float.MinValue;
                int stutters = 0;
                for (int i = 0; i < w.Count; i++)
                {
                    Fr f = w[i];
                    speedSum += f.speed; speedPeak = Mathf.Max(speedPeak, f.speed);
                    trunkSum += f.trunk; trunkMax = Mathf.Max(trunkMax, f.trunk);
                    lMin = Mathf.Min(lMin, f.twL); lMax = Mathf.Max(lMax, f.twL);
                    rMin = Mathf.Min(rMin, f.twR); rMax = Mathf.Max(rMax, f.twR);
                    split = Mathf.Max(split, Mathf.Abs(f.twL - f.twR));
                    hipMin = Mathf.Min(hipMin, f.hipY); hipMax = Mathf.Max(hipMax, f.hipY);
                    kneeMin = Mathf.Min(kneeMin, Mathf.Min(f.kneeL, f.kneeR));
                    alMin = Mathf.Min(alMin, f.armL); alMax = Mathf.Max(alMax, f.armL);
                    arMin = Mathf.Min(arMin, f.armR); arMax = Mathf.Max(arMax, f.armR);
                    hyMin = Mathf.Min(hyMin, f.hipsYaw); hyMax = Mathf.Max(hyMax, f.hipsYaw);
                    hrMin = Mathf.Min(hrMin, f.hipsRoll); hrMax = Mathf.Max(hrMax, f.hipsRoll);
                    cyMin = Mathf.Min(cyMin, f.chestYaw); cyMax = Mathf.Max(cyMax, f.chestYaw);
                    crMin = Mathf.Min(crMin, f.chestRoll); crMax = Mathf.Max(crMax, f.chestRoll);

                    if (i == 0) continue;
                    float dt = Mathf.Max(1e-4f, f.t - w[i - 1].t);
                    float oL = (f.twL - w[i - 1].twL) / dt, oR = (f.twR - w[i - 1].twR) / dt;
                    omegaMax = Mathf.Max(omegaMax, Mathf.Max(Mathf.Abs(oL), Mathf.Abs(oR)));
                    if (i == 1) continue;
                    float pdt = Mathf.Max(1e-4f, w[i - 1].t - w[i - 2].t);
                    float pL = (w[i - 1].twL - w[i - 2].twL) / pdt, pR = (w[i - 1].twR - w[i - 2].twR) / pdt;
                    if (Mathf.Abs(oL - pL) > StutterJumpDegPerSec) stutters++;
                    if (Mathf.Abs(oR - pR) > StutterJumpDegPerSec) stutters++;
                }

                int n = Mathf.Max(1, w.Count);
                return string.Join(",", _sc.Name, PressCount, _fallbacks, w.Count, F(speedSum / n), F(speedPeak), F(trunkSum / n), F(trunkMax),
                    F(lMin), F(lMax), F(rMin), F(rMax), F(split), F(omegaMax), stutters, F(StutterJumpDegPerSec), F(hipMin), F(hipMax), F(kneeMin),
                    F(alMin), F(alMax), F(arMin), F(arMax), F(hyMax - hyMin), F(hrMax - hrMin), F(cyMax - cyMin), F(crMax - crMin), ShotsTaken)
                    + "," + ExtraMetrics(w) + "," + TimingMetrics(w);
            }

            /// <summary>holdFrames, speedRipple, firstSecondSpeedSteps (empty = not defined).</summary>
            private string TimingMetrics(List<Fr> w)
            {
                // Share of steady frames where either leg is in its Hold phase.
                int holdCount = 0;
                foreach (Fr f in w) if (f.holdPhase) holdCount++;
                float holdFrames = w.Count > 0 ? (float)holdCount / w.Count : float.NaN;

                // Mean of (max - min) of the speed inside consecutive windows of 2 presses, steady part only.
                var starts = new List<double>();
                foreach (double p in _pressTimes) if (p >= _sc.SummaryFrom) starts.Add(p);
                float rippleSum = 0f;
                int rippleN = 0;
                for (int k = 0; k + 2 < starts.Count; k += 2)
                {
                    float lo = float.MaxValue, hi = float.MinValue;
                    foreach (Fr f in w)
                    {
                        if (f.t < starts[k] || f.t >= starts[k + 2]) continue;
                        lo = Mathf.Min(lo, f.speed); hi = Mathf.Max(hi, f.speed);
                    }
                    if (hi < lo) continue;
                    rippleSum += hi - lo;
                    rippleN++;
                }
                float ripple = rippleN > 0 ? rippleSum / rippleN : float.NaN;

                // Largest speed gain between a frame and any later frame within 0.1 s, during the first 2 s.
                float step = 0f;
                for (int i = 0; i < _frames.Count && _frames[i].t <= FirstSecondsWindow; i++)
                    for (int j = i + 1; j < _frames.Count && _frames[j].t <= FirstSecondsWindow && _frames[j].t - _frames[i].t <= StepWindowSeconds + 1e-4f; j++)
                        step = Mathf.Max(step, _frames[j].speed - _frames[i].speed);

                return string.Join(",", FE(holdFrames), FE(ripple), F(step));
            }

            private static bool Touches(float toeY, float restToe, float footY, float restFoot) =>
                Mathf.Abs(toeY - restToe) <= TouchToleranceMeters || Mathf.Abs(footY - restFoot) <= FootTouchToleranceMeters;

            /// <summary>holdShare, swapShare, corrHipSplit, hipAtWide, hipAtPass, passKnee, flightShare, footSlide, steppingAvg (empty = not defined).</summary>
            private string ExtraMetrics(List<Fr> w)
            {
                int count = w.Count;
                int legFrames = 0, holds = 0, swaps = 0, passN = 0, flight = 0, slideN = 0, stepN = 0;
                float passKneeSum = 0f, stepSum = 0f, splitMax = 0f;
                double slideSum = 0.0;
                var split = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Fr f = w[i];
                    split[i] = Mathf.Abs(f.twL - f.twR);
                    splitMax = Mathf.Max(splitMax, split[i]);
                    bool touchL = Touches(f.toeL, _restToeL, f.footL, _restFootL);
                    bool touchR = Touches(f.toeR, _restToeR, f.footR, _restFootR);
                    if (!touchL && !touchR) flight++;
                    if (!float.IsNaN(f.stepping)) { stepSum += f.stepping; stepN++; }
                    if (i == 0) continue;

                    float dt = Mathf.Max(1e-4f, f.t - w[i - 1].t);
                    float oL = (f.twL - w[i - 1].twL) / dt, oR = (f.twR - w[i - 1].twR) / dt;
                    legFrames += 2;
                    if (Mathf.Abs(oL) < HoldRateDegPerSec) holds++;
                    if (Mathf.Abs(oR) < HoldRateDegPerSec) holds++;
                    if (Mathf.Abs(oL) > SwapRateDegPerSec) swaps++;
                    if (Mathf.Abs(oR) > SwapRateDegPerSec) swaps++;
                    if (oL > 0f && Mathf.Abs(f.twL) < PassThighDeg) { passKneeSum += f.kneeAngL; passN++; }
                    if (oR > 0f && Mathf.Abs(f.twR) < PassThighDeg) { passKneeSum += f.kneeAngR; passN++; }
                    if (touchL) { slideSum += Mathf.Abs(f.fzL - w[i - 1].fzL) / dt; slideN++; }
                    if (touchR) { slideSum += Mathf.Abs(f.fzR - w[i - 1].fzR) / dt; slideN++; }
                }

                float corr = float.NaN, atWide = float.NaN, atPass = float.NaN;
                if (count > 2 && splitMax >= SplitMinDeg)
                {
                    double mh = 0.0, ms = 0.0;
                    for (int i = 0; i < count; i++) { mh += w[i].hipY; ms += split[i]; }
                    mh /= count; ms /= count;
                    double cov = 0.0, vh = 0.0, vs = 0.0;
                    for (int i = 0; i < count; i++)
                    {
                        double a = w[i].hipY - mh, b = split[i] - ms;
                        cov += a * b; vh += a * a; vs += b * b;
                    }
                    if (vh > 1e-12 && vs > 1e-12) corr = (float)(cov / Math.Sqrt(vh * vs));

                    var order = new int[count];
                    for (int i = 0; i < count; i++) order[i] = i;
                    Array.Sort(order, (x, y) => split[x].CompareTo(split[y]));
                    int edge = Mathf.Max(1, Mathf.RoundToInt(count * SplitEdgeShare));
                    float lowSum = 0f, highSum = 0f;
                    for (int i = 0; i < edge; i++)
                    {
                        lowSum += w[order[i]].hipY;
                        highSum += w[order[count - 1 - i]].hipY;
                    }
                    atPass = lowSum / edge;
                    atWide = highSum / edge;
                }

                float holdShare = legFrames > 0 ? (float)holds / legFrames : float.NaN;
                float swapShare = legFrames > 0 ? (float)swaps / legFrames : float.NaN;
                float passKnee = passN > 0 ? passKneeSum / passN : float.NaN;
                float flightShare = count > 0 ? (float)flight / count : float.NaN;
                float slide = slideN > 0 ? (float)(slideSum / slideN) : float.NaN;
                float stepAvg = stepN > 0 ? stepSum / stepN : float.NaN;
                return string.Join(",", FE(holdShare), FE(swapShare), FE(corr), FE(atWide), FE(atPass), FE(passKnee), FE(flightShare),
                    FE(slide), FE(stepAvg));
            }
        }
    }
}
