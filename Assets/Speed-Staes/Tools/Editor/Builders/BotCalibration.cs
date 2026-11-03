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
    /// Runs Malik's RunnerSim headless (no scene, no Play mode) with the bots' key schedules to measure what time a given bot
    /// skill really produces over each race event, and writes the skill -> time tables the bot planner needs (T-074, T-086).
    /// The schedule is fed to the sim exactly the way RunnerBody.Update does it: advance to the event's time, then Press / Release.
    /// In a hurdle event the bot also jumps (the same <see cref="SpeedStars.Bridge.RaceBots.WantsJump"/> rule as in the race) and a
    /// hurdle passed on the ground slows it (the same judging as RunnerHurdles). Times are measured from GO, the reaction included
    /// (what RaceController and RaceBots measure). Distance and hurdle layout come from the event assets.
    /// <code>
    /// Unity.exe -batchmode -nographics -quit -projectPath &lt;copy&gt; -executeMethod SpeedStars.Tools.Builders.BotCalibration.Run
    ///     -out &lt;csv&gt; [-events all|100m,110mh,200m,400m,60mh,400mh] [-seeds 20] [-step 0.1] [-horizon 120]
    ///     [-config &lt;asset path&gt;] [-prefab &lt;prefab path&gt;] [-apply]
    /// </code>
    /// <c>-apply</c> writes the tables into the config asset: the 100m row goes to <c>BotRaceConfig.calibration</c> (the base table),
    /// every other event becomes a <c>BotEventProfile</c> (otherwise only the csv and the table text next to it are written).
    /// <c>-events 100m</c> alone is the old 100 m run. The horizon grows with the distance (0.6 s per metre) so a 400 m run can finish.
    /// </summary>
    public static class BotCalibration
    {
        private const string Source = nameof(BotCalibration);
        private const string DefaultPrefab = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        private const int DefaultSeeds = 20;
        private const float DefaultStep = 0.1f;
        private const float DefaultDistance = 100f;
        private const double DefaultHorizon = 120.0;
        private const double HorizonSecondsPerMeter = 0.6; // slowest bot over 400 m is ~100 s; this leaves room for knocked hurdles
        private const string BaseEventId = "100m"; // its table is BotRaceConfig.calibration, not a profile
        private static readonly string[] AllEventIds = { BaseEventId, "110mh", "200m", "400m", "60mh", "400mh" };
        private const float MinGapBetweenRows = 0.01f; // seconds; keeps the table strictly falling so it can be inverted
        private const int SearchSeeds = 10;
        private const float SearchPeriodMin = 0.20f, SearchHoldMin = 0.06f, SearchStep = 0.02f;
        private const int SearchPeriodSteps = 10, SearchHoldSteps = 5; // period up to 0.40 s, hold up to 0.16 s
        private const double SearchHorizon = 60.0;

        [MenuItem("Tools/Speed Stars/Run bot calibration")]
        public static void Run() => Calibrate(HasArg("-apply"));

        /// <summary>Same run, writing the measured table into the config asset.</summary>
        [MenuItem("Tools/Speed Stars/Run bot calibration and apply")]
        public static void RunAndApply() => Calibrate(true);

        /// <summary>
        /// The fastest driver the physics allows: every press period x hold in the search range, with the best bot's jitter and mistakes,
        /// over a few seeds. The result goes to BotRaceConfig periodBest / holdBest by hand, then the table is measured again.
        /// </summary>
        [MenuItem("Tools/Speed Stars/Search bot driver")]
        public static void Search()
        {
            string outPath = ArgValue("-out") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "bot-driver-search.csv");
            int seeds = Mathf.Max(1, ArgInt("-seeds", SearchSeeds));
            var config = AssetDatabase.LoadAssetAtPath<BotRaceConfig>(BotRaceConfigSeed.Path)
                ?? throw new InvalidOperationException($"BotRaceConfig not found at '{BotRaceConfigSeed.Path}'.");
            RunnerTuning tuning = PrefabTuning(DefaultPrefab);
            BotPlan best = BotSimDriver.PlanForSkill(config, 1f);

            var csv = new StringBuilder("period,hold,finished,mean_seconds_from_first_press,mean_seconds_from_go,worst_seconds_from_first_press\n");
            for (int p = 0; p <= SearchPeriodSteps; p++)
            {
                for (int h = 0; h <= SearchHoldSteps; h++)
                {
                    float period = SearchPeriodMin + p * SearchStep, hold = SearchHoldMin + h * SearchStep;
                    var plan = new BotPlan("search", string.Empty, best.TargetSeconds, 1f, best.ReactionSeconds, period, hold,
                        best.JitterSeconds, best.MistakeChance, best.MistakeShiftRatio);
                    double sumPress = 0.0, sumGo = 0.0, worst = 0.0;
                    int finished = 0;
                    for (int seed = 0; seed < seeds; seed++)
                    {
                        var schedule = new BotPressSchedule(plan, new System.Random(unchecked(p * 7919 + h * 104729 + seed * 1299709 + 17)), SearchHorizon);
                        BotSimDriver.Result r = BotSimDriver.Run(tuning, schedule, DefaultDistance, SearchHorizon);
                        if (!r.Finished) continue;
                        finished++;
                        sumPress += r.SecondsFromFirstPress;
                        sumGo += r.SecondsFromGo;
                        worst = Math.Max(worst, r.SecondsFromFirstPress);
                    }
                    double meanPress = finished > 0 ? sumPress / finished : double.NaN;
                    double meanGo = finished > 0 ? sumGo / finished : double.NaN;
                    csv.Append(Format(period)).Append(',').Append(Format(hold)).Append(',').Append(finished).Append(',')
                       .Append(Format(meanPress)).Append(',').Append(Format(meanGo)).Append(',').Append(Format(worst)).Append('\n');
                    UnityEngine.Debug.Log($"[{Source}] SEARCH_ROW period {period:0.00} hold {hold:0.00}: {meanPress:0.00} s, finished {finished}/{seeds}");
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
            File.WriteAllText(outPath, csv.ToString(), new UTF8Encoding(false));
            UnityEngine.Debug.Log($"[{Source}] SEARCH_OK {outPath}");
        }

        private static RunnerTuning PrefabTuning(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)
                ?? throw new InvalidOperationException($"Runner prefab not found at '{prefabPath}'.");
            var body = prefab.GetComponentInChildren<RunnerBody>(true)
                ?? throw new InvalidOperationException($"'{prefabPath}' has no RunnerBody; its tuning is the sim's numbers.");
            return body.tuning;
        }

        /// <summary>One event to measure: its length and hurdle layout (from the event asset).</summary>
        private sealed class EventCase
        {
            public string Id;
            public float Distance;
            public int HurdleCount;
            public float FirstHurdle;
            public float HurdleSpacing;
            public bool IsBase => Id == BaseEventId;
        }

        private sealed class Measured
        {
            public EventCase Case;
            public List<BotCalibrationPoint> Raw;
            public List<BotCalibrationPoint> Table;
            public float MinEnergyLeft = float.MaxValue;
            public float WorstSkillEnergyLeft;
            public int Unfinished;
        }

        private static void Calibrate(bool apply)
        {
            string outPath = ArgValue("-out") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "bot-calibration.csv");
            int seeds = Mathf.Max(1, ArgInt("-seeds", DefaultSeeds));
            float step = Mathf.Max(0.01f, ArgFloat("-step", DefaultStep));
            double horizonArg = Math.Max(10.0, ArgFloat("-horizon", (float)DefaultHorizon));
            string configPath = ArgValue("-config") ?? BotRaceConfigSeed.Path;
            string prefabPath = ArgValue("-prefab") ?? DefaultPrefab;

            var config = AssetDatabase.LoadAssetAtPath<BotRaceConfig>(configPath)
                ?? throw new InvalidOperationException($"BotRaceConfig not found at '{configPath}'; seed the configs first (ConfigAssetSeeder).");
            RunnerTuning tuning = PrefabTuning(prefabPath);

            var csv = new StringBuilder("event,skill,seed,finished,hurdles_knocked,seconds_from_first_press,seconds_from_go,energy_left\n");
            var results = new List<Measured>();
            foreach (string id in ParseEvents(ArgValue("-events")))
            {
                EventCase ev = LoadCase(id);
                results.Add(Measure(config, tuning, ev, seeds, step, Math.Max(horizonArg, ev.Distance * HorizonSecondsPerMeter), csv));
            }

            // The fallback reference keeps the 100 m relation: the skill the 100 m fallback time stands for, read off the event's own table.
            IReadOnlyList<BotCalibrationPoint> baseTable = results.Find(r => r.Case.IsBase)?.Table ?? config.Calibration;
            float fallbackSkill = BotSkillCurve.SkillForSeconds(baseTable, config.FallbackReferenceSeconds);

            string tableText = TableText(results, fallbackSkill);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
            File.WriteAllText(outPath, csv.ToString(), new UTF8Encoding(false));
            File.WriteAllText(outPath + ".table.txt", tableText, new UTF8Encoding(false));
            UnityEngine.Debug.Log($"[{Source}] CALIBRATION_OK {outPath}\n{tableText}");

            if (!apply) return;
            foreach (Measured m in results)
            {
                if (m.Case.IsBase) config.SetCalibration(m.Table);
                else config.SetProfile(new BotEventProfile(m.Case.Id, FallbackFor(m.Table, fallbackSkill), m.Table));
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[{Source}] Tables written into '{configPath}' ({results.Count} events).");
        }

        private static Measured Measure(BotRaceConfig config, RunnerTuning tuning, EventCase ev, int seeds, float step, double horizon, StringBuilder csv)
        {
            var points = new List<BotCalibrationPoint>();
            var m = new Measured { Case = ev };
            int rows = Mathf.RoundToInt(1f / step);
            for (int row = 0; row <= rows; row++)
            {
                float skill = Mathf.Clamp01(row * step);
                BotPlan plan = BotSimDriver.PlanForSkill(config, skill);
                double sum = 0.0, energy = 0.0, knocked = 0.0;
                int finished = 0;
                for (int seed = 0; seed < seeds; seed++)
                {
                    var random = new System.Random(unchecked(row * 7919 + seed * 104729 + 17));
                    var schedule = new BotPressSchedule(plan, random, horizon);
                    BotSimDriver.HurdleRun hurdles = ev.HurdleCount > 0
                        ? new BotSimDriver.HurdleRun(ev.HurdleCount, ev.FirstHurdle, ev.HurdleSpacing, plan,
                            new BotJumpDriver(new System.Random(unchecked(row * 7919 + seed * 104729 + 31))))
                        : null;
                    BotSimDriver.Result r = BotSimDriver.Run(tuning, schedule, ev.Distance, horizon, hurdles);
                    csv.Append(ev.Id).Append(',').Append(skill.ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append(seed).Append(',')
                       .Append(r.Finished ? 1 : 0).Append(',').Append(r.Knocked).Append(',')
                       .Append(Format(r.Finished ? r.SecondsFromFirstPress : double.NaN)).Append(',')
                       .Append(Format(r.Finished ? r.SecondsFromGo : double.NaN)).Append(',')
                       .Append(Format(r.Finished ? r.EnergyLeft : double.NaN)).Append('\n');
                    // A run that never finished counts as the horizon: the table stays usable and the row says it in the log.
                    sum += r.Finished ? r.SecondsFromGo : horizon;
                    knocked += r.Knocked;
                    if (!r.Finished) continue;
                    finished++;
                    energy += r.EnergyLeft;
                    m.MinEnergyLeft = Mathf.Min(m.MinEnergyLeft, r.EnergyLeft);
                }
                float mean = (float)(sum / seeds);
                points.Add(new BotCalibrationPoint(skill, mean));
                m.Unfinished += seeds - finished;
                double energyMean = finished > 0 ? energy / finished : double.NaN;
                if (row == 0) m.WorstSkillEnergyLeft = (float)energyMean;
                UnityEngine.Debug.Log($"[{Source}] CALIBRATION_ROW {ev.Id} skill {skill:0.00}: mean {mean:0.00} s, finished {finished}/{seeds}, " +
                                      $"knocked {knocked / seeds:0.0}, energy left {energyMean:0.0}");
                if (finished < seeds)
                    UnityEngine.Debug.LogWarning($"[{Source}] {ev.Id} skill {skill:0.00}: {seeds - finished}/{seeds} runs did not finish {ev.Distance:0} m within {horizon:0} s.");
            }
            m.Raw = points;
            m.Table = MakeStrictlyFalling(points);
            return m;
        }

        /// <summary>The event ids of an <c>-events</c> argument ("all" or a comma list, default all).</summary>
        private static List<string> ParseEvents(string arg)
        {
            var ids = new List<string>();
            if (string.IsNullOrWhiteSpace(arg) || arg.Trim() == "all") ids.AddRange(AllEventIds);
            else
                foreach (string part in arg.Split(','))
                    if (!string.IsNullOrWhiteSpace(part) && !ids.Contains(part.Trim())) ids.Add(part.Trim());
            return ids;
        }

        /// <summary>Distance and hurdle layout of an event from its asset. The 100 m base falls back to a flat 100 m without an asset.</summary>
        private static EventCase LoadCase(string id)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RaceEventDefinition", new[] { ConfigAssetSeeder.EventsFolder }))
            {
                var def = AssetDatabase.LoadAssetAtPath<SpeedStars.Meta.Config.RaceEventDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null || def.Id.Value != id) continue;
                if (def.DistanceMeters <= 0f) throw new InvalidOperationException($"Event '{id}' has no race length.");
                return new EventCase
                {
                    Id = id, Distance = def.DistanceMeters,
                    HurdleCount = def.HurdleCount, FirstHurdle = def.FirstHurdleMeters, HurdleSpacing = def.HurdleSpacingMeters,
                };
            }
            if (id == BaseEventId) return new EventCase { Id = id, Distance = DefaultDistance };
            throw new InvalidOperationException($"No race event '{id}' under '{ConfigAssetSeeder.EventsFolder}'.");
        }

        private static float FallbackFor(IReadOnlyList<BotCalibrationPoint> table, float fallbackSkill)
            => Mathf.Round(BotSkillCurve.SecondsForSkill(table, fallbackSkill) * 100f) / 100f;

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

        private static string TableText(List<Measured> results, float fallbackSkill)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Measured mean time per skill (from GO, hurdles and jumps included). Run with -apply to write the config asset;");
            sb.AppendLine("// the same rows go into the BotRaceConfig code defaults (calibration / eventProfiles) so asset and code agree.");
            foreach (Measured m in results)
            {
                sb.AppendLine();
                sb.AppendLine($"// ---- {m.Case.Id}: {m.Case.Distance:0} m, {m.Case.HurdleCount} hurdles; energy left at the line: slowest bot " +
                              $"{Format(m.WorstSkillEnergyLeft)}, lowest of all runs {Format(m.MinEnergyLeft)}; unfinished runs {m.Unfinished}");
                if (!m.Case.IsBase)
                {
                    sb.AppendLine($"new BotEventProfile(\"{m.Case.Id}\", {Format(FallbackFor(m.Table, fallbackSkill))}f, new[]");
                    sb.AppendLine("{");
                }
                for (int i = 0; i < m.Table.Count; i++)
                {
                    string note = Mathf.Abs(m.Table[i].seconds - m.Raw[i].seconds) > 0.0005f
                        ? $"   // measured {Format(m.Raw[i].seconds)}, flattened to stay falling" : string.Empty;
                    sb.Append(m.Case.IsBase ? "new BotCalibrationPoint(" : "    new BotCalibrationPoint(")
                      .Append(Format(m.Table[i].skill)).Append("f, ").Append(Format(m.Table[i].seconds)).Append("f),").AppendLine(note);
                }
                if (!m.Case.IsBase) sb.AppendLine("}),");
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
            public readonly int Knocked;
            public readonly float EnergyLeft;

            public Result(bool finished, double fromGo, double fromFirstPress, int knocked = 0, float energyLeft = 0f)
            {
                Finished = finished;
                SecondsFromGo = fromGo;
                SecondsFromFirstPress = fromFirstPress;
                Knocked = knocked;
                EnergyLeft = energyLeft;
            }
        }

        /// <summary>
        /// The hurdles of one headless run: the bot's jump rule (<see cref="SpeedStars.Bridge.RaceBots.WantsJump"/>) before each step and
        /// the judging of <c>RunnerHurdles</c> after it: in the air at the hurdle = cleared, on the ground = knocked (the sim loses speed).
        /// </summary>
        public sealed class HurdleRun
        {
            private readonly int _count;
            private readonly float _first, _spacing;
            private readonly BotPlan _plan;
            private readonly BotJumpDriver _jumper;
            private int _next;

            public int Knocked { get; private set; }

            public HurdleRun(int count, float first, float spacing, BotPlan plan, BotJumpDriver jumper)
            {
                _count = count;
                _first = first;
                _spacing = spacing;
                _plan = plan;
                _jumper = jumper;
            }

            public void Drive(RunnerSim sim)
            {
                if (_next >= _count) return;
                if (SpeedStars.Bridge.RaceBots.WantsJump(sim, _jumper, _plan, _first + _spacing * _next, _next)) sim.Jump();
            }

            public void Judge(RunnerSim sim)
            {
                while (_next < _count && sim.Distance >= _first + _spacing * _next)
                {
                    if (!sim.JumpInAir)
                    {
                        Knocked++;
                        sim.HitHurdle();
                    }
                    _next++;
                }
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
        public static Result Run(RunnerTuning tuning, BotPressSchedule schedule, float distance, double horizonSeconds, HurdleRun hurdles = null)
        {
            var sim = new RunnerSim(tuning);
            double firstPress = -1.0;
            foreach (BotPressEvent e in schedule.Events)
            {
                if (e.Time >= horizonSeconds) break;
                if (AdvanceTo(sim, e.Time, distance, hurdles, out double crossed)) return Done(crossed, firstPress, sim, hurdles);
                if (e.IsPress)
                {
                    sim.Press(e.Leg);
                    if (firstPress < 0.0) firstPress = sim.LastPressTime;
                }
                else sim.Release(e.Leg);
            }
            if (AdvanceTo(sim, horizonSeconds, distance, hurdles, out double late)) return Done(late, firstPress, sim, hurdles);
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

        private static Result Done(double crossed, double firstPress, RunnerSim sim, HurdleRun hurdles) =>
            new Result(true, crossed, firstPress >= 0.0 ? crossed - firstPress : crossed, hurdles?.Knocked ?? 0, sim.Energy);

        /// <summary>Advances the sim to <paramref name="to"/> in small steps so the crossing time can be interpolated; true = it crossed.</summary>
        private static bool AdvanceTo(RunnerSim sim, double to, float distance, HurdleRun hurdles, out double crossed)
        {
            const double Step = 1.0 / 120.0;
            crossed = 0.0;
            // RunnerSim.Advance ignores dt <= 1e-6, so a remainder below the margin is dropped instead of looping on it.
            while (to - sim.Time > MinAdvance)
            {
                double before = sim.Time;
                float distanceBefore = sim.Distance;
                hurdles?.Drive(sim);
                sim.Advance((float)Math.Min(Step, to - sim.Time));
                if (sim.Time <= before) return false; // no progress: never spin
                hurdles?.Judge(sim);
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
