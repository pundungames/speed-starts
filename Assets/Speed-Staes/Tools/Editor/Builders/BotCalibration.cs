using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SpeedStars.Core.Race.Bots;
using SpeedStars.Physical;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Runs Malik's RunnerSim headless (no scene, no Play mode) with the bots' key schedules to measure what 100 m time a given bot
    /// skill really produces, and writes the skill -> time table the bot planner needs (BotRaceConfig.calibration, T-074).
    /// The schedule is fed to the sim exactly the way RunnerBody.Update does it: advance to the event's time, then Press / Release.
    /// Times are measured from the first press (what RaceController and RaceBots measure), the CSV also holds the time from GO.
    /// <code>
    /// Unity.exe -batchmode -nographics -quit -projectPath &lt;copy&gt; -executeMethod SpeedStars.Tools.Builders.BotCalibration.Run
    ///     -out &lt;csv&gt; [-seeds 20] [-step 0.1] [-distance 100] [-horizon 120] [-config &lt;asset path&gt;] [-prefab &lt;prefab path&gt;] [-apply]
    /// </code>
    /// <c>-apply</c> writes the table into the config asset (otherwise only the csv and the table text next to it are written).
    /// </summary>
    public static class BotCalibration
    {
        private const string Source = nameof(BotCalibration);
        private const string DefaultPrefab = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        private const int DefaultSeeds = 20;
        private const float DefaultStep = 0.1f;
        private const float DefaultDistance = 100f;
        private const double DefaultHorizon = 120.0;
        private const float MinGapBetweenRows = 0.01f; // seconds; keeps the table strictly falling so it can be inverted

        [MenuItem("Tools/Speed Stars/Run bot calibration")]
        public static void Run()
        {
            string outPath = ArgValue("-out") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "bot-calibration.csv");
            int seeds = Mathf.Max(1, ArgInt("-seeds", DefaultSeeds));
            float step = Mathf.Max(0.01f, ArgFloat("-step", DefaultStep));
            float distance = Mathf.Max(1f, ArgFloat("-distance", DefaultDistance));
            double horizon = Math.Max(10.0, ArgFloat("-horizon", (float)DefaultHorizon));
            string configPath = ArgValue("-config") ?? BotRaceConfigSeed.Path;
            string prefabPath = ArgValue("-prefab") ?? DefaultPrefab;

            var config = AssetDatabase.LoadAssetAtPath<BotRaceConfig>(configPath)
                ?? throw new InvalidOperationException($"BotRaceConfig not found at '{configPath}'; seed the configs first (ConfigAssetSeeder).");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)
                ?? throw new InvalidOperationException($"Runner prefab not found at '{prefabPath}'.");
            var body = prefab.GetComponentInChildren<RunnerBody>(true)
                ?? throw new InvalidOperationException($"'{prefabPath}' has no RunnerBody; its tuning is the sim's numbers.");
            RunnerTuning tuning = body.tuning;

            var csv = new StringBuilder("skill,seed,finished,seconds_from_first_press,seconds_from_go\n");
            var points = new List<BotCalibrationPoint>();
            int rows = Mathf.RoundToInt(1f / step);
            for (int row = 0; row <= rows; row++)
            {
                float skill = Mathf.Clamp01(row * step);
                BotPlan plan = BotSimDriver.PlanForSkill(config, skill);
                double sum = 0.0;
                int finished = 0;
                for (int seed = 0; seed < seeds; seed++)
                {
                    var random = new System.Random(unchecked(row * 7919 + seed * 104729 + 17));
                    var schedule = new BotPressSchedule(plan, random, horizon);
                    BotSimDriver.Result r = BotSimDriver.Run(tuning, schedule, distance, horizon);
                    csv.Append(skill.ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append(seed).Append(',')
                       .Append(r.Finished ? 1 : 0).Append(',')
                       .Append(Format(r.Finished ? r.SecondsFromFirstPress : double.NaN)).Append(',')
                       .Append(Format(r.Finished ? r.SecondsFromGo : double.NaN)).Append('\n');
                    // A run that never finished counts as the horizon: the table stays usable and the row says it in the log.
                    sum += r.Finished ? r.SecondsFromFirstPress : horizon;
                    if (r.Finished) finished++;
                }
                float mean = (float)(sum / seeds);
                points.Add(new BotCalibrationPoint(skill, mean));
                UnityEngine.Debug.Log($"[{Source}] CALIBRATION_ROW skill {skill:0.00}: mean {mean:0.00} s, finished {finished}/{seeds}");
                if (finished < seeds)
                    UnityEngine.Debug.LogWarning($"[{Source}] skill {skill:0.00}: {seeds - finished}/{seeds} runs did not finish {distance:0} m within {horizon:0} s.");
            }

            List<BotCalibrationPoint> table = MakeStrictlyFalling(points);
            string tableText = TableText(points, table);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
            File.WriteAllText(outPath, csv.ToString(), new UTF8Encoding(false));
            File.WriteAllText(outPath + ".table.txt", tableText, new UTF8Encoding(false));
            UnityEngine.Debug.Log($"[{Source}] CALIBRATION_OK {outPath}\n{tableText}");

            if (HasArg("-apply"))
            {
                config.SetCalibration(table);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log($"[{Source}] Table written into '{configPath}' ({table.Count} rows).");
            }
        }

        /// <summary>Skill must give a strictly shorter time; a measured bump (more skill, slower) is flattened so the planner can invert the table.</summary>
        private static List<BotCalibrationPoint> MakeStrictlyFalling(List<BotCalibrationPoint> measured)
        {
            var result = new List<BotCalibrationPoint>(measured.Count);
            float previous = float.MaxValue;
            foreach (BotCalibrationPoint p in measured)
            {
                float seconds = Mathf.Min(p.seconds, previous - MinGapBetweenRows);
                result.Add(new BotCalibrationPoint(p.skill, Mathf.Max(seconds, MinGapBetweenRows)));
                previous = seconds;
            }
            return result;
        }

        private static string TableText(List<BotCalibrationPoint> measured, List<BotCalibrationPoint> table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Measured mean 100 m time per skill (from the first press). Paste into BotRaceConfig.calibration or run with -apply.");
            for (int i = 0; i < table.Count; i++)
            {
                string note = Mathf.Abs(table[i].seconds - measured[i].seconds) > 0.0005f
                    ? $"   // measured {Format(measured[i].seconds)}, flattened to stay falling" : string.Empty;
                sb.Append("new BotCalibrationPoint(").Append(Format(table[i].skill)).Append("f, ").Append(Format(table[i].seconds)).Append("f),").AppendLine(note);
            }
            return sb.ToString();
        }

        private static string Format(double v) => double.IsNaN(v) ? string.Empty : v.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        private static string ArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static int ArgInt(string name, int fallback) =>
            int.TryParse(ArgValue(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        private static float ArgFloat(string name, float fallback) =>
            float.TryParse(ArgValue(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
    }

    /// <summary>Headless runs of the runner sim with a bot schedule. Shared by the calibration and the shot tool.</summary>
    internal static class BotSimDriver
    {
        public readonly struct Result
        {
            public readonly bool Finished;
            public readonly double SecondsFromGo;
            public readonly double SecondsFromFirstPress;

            public Result(bool finished, double fromGo, double fromFirstPress)
            {
                Finished = finished;
                SecondsFromGo = fromGo;
                SecondsFromFirstPress = fromFirstPress;
            }
        }

        /// <summary>
        /// A plan for a given skill, the way BotFieldPlanner derives the driver numbers from the skill (same lerps, kept in step by hand:
        /// if BotFieldPlanner.Plan changes how it maps skill to reaction / period / hold / jitter / mistakes, change it here too).
        /// </summary>
        public static BotPlan PlanForSkill(BotRaceConfig c, float skill)
        {
            float s = Mathf.Clamp01(skill);
            return new BotPlan("calibration", string.Empty, BotSkillCurve.SecondsForSkill(c.Calibration, s), s,
                reactionSeconds: Mathf.Lerp(c.ReactionWorstSeconds, c.ReactionBestSeconds, s),
                pressPeriodSeconds: Mathf.Lerp(c.PeriodWorstSeconds, c.PeriodBestSeconds, s),
                holdSeconds: Mathf.Lerp(c.HoldWorstSeconds, c.HoldBestSeconds, s),
                jitterSeconds: Mathf.Lerp(c.JitterWorstMs, c.JitterBestMs, s) * 0.001f,
                mistakeChance: Mathf.Lerp(c.MistakeWorstChance, c.MistakeBestChance, s),
                mistakeShiftRatio: c.MistakeShiftRatio);
        }

        /// <summary>Runs the schedule until the distance is reached (time interpolated inside the step) or the horizon ends.</summary>
        public static Result Run(RunnerTuning tuning, BotPressSchedule schedule, float distance, double horizonSeconds)
        {
            var sim = new RunnerSim(tuning);
            double firstPress = -1.0;
            foreach (BotPressEvent e in schedule.Events)
            {
                if (e.Time >= horizonSeconds) break;
                if (AdvanceTo(sim, e.Time, distance, out double crossed)) return Done(crossed, firstPress);
                if (e.IsPress)
                {
                    sim.Press(e.Leg);
                    if (firstPress < 0.0) firstPress = sim.LastPressTime;
                }
                else sim.Release(e.Leg);
            }
            if (AdvanceTo(sim, horizonSeconds, distance, out double late)) return Done(late, firstPress);
            return new Result(false, double.NaN, double.NaN);
        }

        /// <summary>Distance covered <paramref name="seconds"/> after GO (for the shot tool).</summary>
        public static float DistanceAt(RunnerTuning tuning, BotPressSchedule schedule, double seconds)
        {
            var sim = new RunnerSim(tuning);
            foreach (BotPressEvent e in schedule.Events)
            {
                if (e.Time >= seconds) break;
                if (e.Time - sim.Time > MinAdvance) sim.Advance((float)(e.Time - sim.Time));
                if (e.IsPress) sim.Press(e.Leg);
                else sim.Release(e.Leg);
            }
            if (seconds - sim.Time > MinAdvance) sim.Advance((float)(seconds - sim.Time));
            return sim.Distance;
        }

        /// <summary>Smallest step worth giving the sim (it ignores dt at or below 1e-6 s).</summary>
        private const double MinAdvance = 1e-5;

        private static Result Done(double crossed, double firstPress) =>
            new Result(true, crossed, firstPress >= 0.0 ? crossed - firstPress : crossed);

        /// <summary>Advances the sim to <paramref name="to"/> in small steps so the crossing time can be interpolated; true = it crossed.</summary>
        private static bool AdvanceTo(RunnerSim sim, double to, float distance, out double crossed)
        {
            const double Step = 1.0 / 120.0;
            crossed = 0.0;
            // RunnerSim.Advance ignores dt <= 1e-6, so a remainder below the margin is dropped instead of looping on it.
            while (to - sim.Time > MinAdvance)
            {
                double before = sim.Time;
                float distanceBefore = sim.Distance;
                sim.Advance((float)Math.Min(Step, to - sim.Time));
                if (sim.Time <= before) return false; // no progress: never spin
                if (sim.Distance >= distance)
                {
                    float span = sim.Distance - distanceBefore;
                    double fraction = span > 0f ? (distance - distanceBefore) / span : 1.0;
                    crossed = before + fraction * (sim.Time - before);
                    return true;
                }
            }
            return false;
        }
    }
}
