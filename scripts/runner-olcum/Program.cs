using System;
using System.Globalization;
using System.Collections.Generic;
using SpeedStars.Physical;
using UnityEngine;

static class P
{
    // Prefab values of HumanCharacterDummy_F (2026-11-04), the ones the game runs with.
    public static float Waste = -1f; public static float Waste2 = -1f;
    public static float PushT = -1f;
    static RunnerTuning T() { var t = new RunnerTuning(); if (Waste >= 0f) t.wasteKickShare = Waste; if (PushT > 0f) t.pushTime = PushT; if (FlatKick) { t.goodKickShare = 1f; t.wasteKickShare = 1f; } t.maxSpeed = 13f; t.pushGain = 1.22f; t.dragQuadratic = 0.032f; t.gravity = 5f; t.headKick = 12f; if (Waste2 >= 0f) t.wasteKickShare = Waste2; t.headReturnDelay = 0.1f; Over(t); return t; }
    // Quick tuning trials: SIM_SET="strainGain=0.09;headKick=10" overrides float fields of RunnerTuning.
    static string Ovr; static void Over(RunnerTuning t) { var v = Ovr ?? Environment.GetEnvironmentVariable("SIM_SET"); if (string.IsNullOrEmpty(v)) return; foreach (var kv in v.Split(';')) { var a = kv.Split('='); var f = typeof(RunnerTuning).GetField(a[0]); if (f != null) f.SetValue(t, float.Parse(a[1], CultureInfo.InvariantCulture)); } }

    sealed class R { public string Name; public float T100 = -1, VEnd, VMax, HeadAvg, HeadMax, Eff; public int Good, Early, Late, N; }

    static R Run(string name, Func<RunnerSim, double, double, (int leg, double next, bool both)> next, float hold)
    {
        var s = new RunnerSim(T());
        var r = new R { Name = name };
        double nextPress = 0.2, relL = -1, relR = -1, dt = 1.0 / 240.0, t = 0;
        double headSum = 0; int headN = 0; double effSum = 0;
        while (t < 30 && s.Distance < 100f)
        {
            if (t >= nextPress)
            {
                var d = next(s, t, nextPress);
                if (d.both) { s.Press(0); s.Press(1); relL = relR = t + hold; }
                else { s.Press(d.leg); if (d.leg == 0) relL = t + hold; else relR = t + hold; }
                effSum += s.LastEfficiency; r.N++;
                if (s.LastResult == PressResult.Good) r.Good++; else if (s.LastResult == PressResult.Early) r.Early++; else if (s.LastResult == PressResult.Late) r.Late++;
                nextPress = d.next;
            }
            if (relL >= 0 && t >= relL) { s.Release(0); relL = -1; }
            if (relR >= 0 && t >= relR) { s.Release(1); relR = -1; }
            s.Advance((float)dt); t += dt;
            r.VMax = Math.Max(r.VMax, s.Speed); r.HeadMax = Math.Max(r.HeadMax, s.HeadAngle);
            if (t > 2) { headSum += s.HeadAngle; headN++; }
        }
        if (s.Distance >= 100f) r.T100 = (float)t;
        r.VEnd = s.Speed; r.HeadAvg = headN > 0 ? (float)(headSum / headN) : 0; r.Eff = r.N > 0 ? (float)(effSum / r.N) : 0;
        return r;
    }

    static R Rhythm(float gap, float hold = 0.07f) { int l = 0; return Run($"ritim {gap:0.00}", (s, t, n) => { int use = l; l = 1 - l; return (use, n + gap, false); }, hold); }
    static R Same(float gap) => Run($"ayni ayak {gap:0.00}", (s, t, n) => (1, n + gap, false), 0.07f);
    static R Both(float gap) => Run($"iki tus {gap:0.00}", (s, t, n) => (0, n + gap, true), 0.07f);
    // The best player: presses the moment the body has landed, plus an offset (s); checks every sim step.
    static R Perfect(float off)
    {
        int l = 0; double landedAt = -1;
        var s0 = new RunnerSim(T());
        var s = s0; var r = new R { Name = off == 0 ? "en iyi (inise tam)" : $"inisten {off:0.00} sonra" };
        double t = 0, rel = -1, dt = 1.0 / 240.0, headSum = 0, effSum = 0; int headN = 0, relLeg = 0; bool first = true;
        while (t < 30 && s.Distance < 100f)
        {
            bool grounded = !s.Airborne && s.Time > 0.01;
            if (first && t > 0.2) { s.Press(l); relLeg = l; rel = t + 0.07; l = 1 - l; first = false; r.N++; effSum += s.LastEfficiency; landedAt = -1; }
            else if (!first)
            {
                bool pendingUp = (float)(t - s.LastPressTime) < s.T.stanceTime + 0.01f;
                if (!pendingUp && grounded && landedAt < 0) landedAt = t;
                if (landedAt >= 0 && t >= landedAt + off)
                {
                    s.Press(l); relLeg = l; rel = t + 0.07; l = 1 - l; r.N++; effSum += s.LastEfficiency; landedAt = -1;
                    if (s.LastResult == PressResult.Good) r.Good++; else if (s.LastResult == PressResult.Early) r.Early++; else if (s.LastResult == PressResult.Late) r.Late++;
                }
            }
            if (rel >= 0 && t >= rel) { s.Release(relLeg); rel = -1; }
            s.Advance((float)dt); t += dt;
            r.VMax = Math.Max(r.VMax, s.Speed); r.HeadMax = Math.Max(r.HeadMax, s.HeadAngle);
            if (t > 2) { headSum += s.HeadAngle; headN++; }
        }
        if (s.Distance >= 100f) r.T100 = (float)t;
        r.VEnd = s.Speed; r.HeadAvg = headN > 0 ? (float)(headSum / headN) : 0; r.Eff = r.N > 0 ? (float)(effSum / r.N) : 0;
        return r;
    }

    static void CliffSweep()
    {
        var one = "tek tus (son hiz m/s, 100m sn):";
        foreach (var g in new[] { 0.16f, 0.18f, 0.20f, 0.21f, 0.22f, 0.23f, 0.24f, 0.25f, 0.28f, 0.30f, 0.35f, 0.40f }) { var r = Same(g); one += $" {g:0.00}={r.VEnd:0.0}/{(r.T100 < 0 ? ">30" : r.T100.ToString("0.0"))}"; }
        Console.WriteLine(one);
        foreach (var flat in new[] { false, true })
        {
            FlatKick = flat;
            var line = flat ? "tus suresi, eski duz bas vurusu, ritim 0.28:" : "tus suresi, simdiki, ritim 0.28:";
            foreach (var tap in new[] { 0.05f, 0.07f, 0.09f, 0.11f, 0.13f, 0.15f, 0.17f, 0.20f }) { var r = Rhythm(0.28f, tap); line += $" {tap:0.00}={(r.T100 < 0 ? ">30" : r.T100.ToString("0.0"))}/b{r.HeadAvg:0}"; }
            Console.WriteLine(line);
        }
    }

    public static bool FlatKick;

    static void PushSweep()
    {
        foreach (var pt in new[] { 0.12f, 0.2f, 0.3f, 0.4f, 0.5f, 0.7f })
        {
            PushT = pt;
            var line = $"pushTime {pt:0.00}: en iyi {Perfect(0f).T100:0.00}";
            foreach (var g in new[] { 0.24f, 0.26f, 0.30f, 0.50f }) line += $" | {g:0.00}={Rhythm(g).T100:0.00}";
            Console.WriteLine(line);
            var s = new RunnerSim(T());
            double t = 0; int leg = 0; double next = 0, rel = -1; int relLeg = 0;
            float best01 = 0f, last = 0f; var hist = new System.Collections.Generic.Queue<float>();
            float rMin = 99f, rMax = 0f, rSum = 0f; int rN = 0; double winEnd = 3.0;
            var stair = "  ilk 1.6 sn hiz:";
            int frame = 0;
            while (t < 8)
            {
                if (t >= next) { s.Press(leg); relLeg = leg; rel = t + 0.07; leg = 1 - leg; next += 0.26; }
                if (rel >= 0 && t >= rel) { s.Release(relLeg); rel = -1; }
                s.Advance(1f / 60f); t += 1.0 / 60.0; frame++;
                hist.Enqueue(s.Speed); if (hist.Count > 6) { best01 = Math.Max(best01, s.Speed - hist.Dequeue()); }
                if (t < 1.6 && frame % 6 == 0) stair += $" {s.Speed:0.0}";
                if (t >= 3.0) { rMin = Math.Min(rMin, s.Speed); rMax = Math.Max(rMax, s.Speed); if (t >= winEnd) { rSum += rMax - rMin; rN++; rMin = 99f; rMax = 0f; winEnd = t + 0.52; } }
                last = s.Speed;
            }
            Console.WriteLine(stair);
            Console.WriteLine($"  0.1 sn'de en buyuk hiz artisi {best01:0.00} m/s | 3-8 sn iki adimlik dalga {(rN > 0 ? rSum / rN : 0):0.00} m/s | son hiz {last:0.0}");
        }
    }


    static void Phased()
    {
        var s = new RunnerSim(T());
        double t = 0, dt = 1.0 / 240.0, next = 0.2, rel = -1; int leg = 0, relLeg = 0, n = 0; double rep = 0;
        string line = "";
        while (t < 17)
        {
            float gap = t < 5 ? 0.26f : t < 13 ? 0.11f : 0.28f, tap = t < 5 || t >= 13 ? 0.07f : 0.04f;
            if (t >= next) { s.Press(leg); relLeg = leg; rel = t + tap; leg = 1 - leg; next += gap; }
            if (rel >= 0 && t >= rel) { s.Release(relLeg); rel = -1; }
            s.Advance((float)dt); t += dt;
            if (t >= rep) { line += $" {t:0.0}:{s.Speed:0.0}/{s.HeadAngle:0}/{s.Strain:0.00}/{s.LastEfficiency:0.00}"; rep += 0.5; if (rep % 2.0 < 0.01) { Console.WriteLine(line); line = ""; } }
        }
        Console.WriteLine(line);
    }
    static void Table()
    {
        foreach (var tap in new[] { 0.04f, 0.07f })
        {
            var line = $"tap {tap:0.00}:";
            foreach (var g in new[] { 0.10f, 0.11f, 0.12f, 0.14f, 0.16f, 0.18f, 0.20f }) { var r = Rhythm(g, tap); line += $" {g:0.00}={(r.T100 < 0 ? ">30" : r.T100.ToString("0.0"))}"; }
            Console.WriteLine(line);
        }
    }


    static void Start3()
    {
        foreach (var one in new[] { true, false })
        foreach (var gap in new[] { 0.20f, 0.25f, 0.30f })
        {
            var s = new RunnerSim(T()); double t = 0, next = 0.2, rel = -1; int leg = 0, rl = 0; string o = $"{(one ? "tek" : "sirali")} {gap:0.00}:";
            double rep = 1;
            while (t < 3.01)
            {
                if (t >= next) { s.Press(leg); rl = leg; rel = t + 0.07; if (!one) leg = 1 - leg; next += gap; }
                if (rel >= 0 && t >= rel) { s.Release(rl); rel = -1; }
                s.Advance(1f / 240f); t += 1.0 / 240;
                if (t >= rep) { o += $" {rep:0}s={s.Speed:0.0}"; rep += 1; }
            }
            Console.WriteLine(o);
        }
    }

    static void Sweep()
    {
        foreach (var w in new[] { 1.6f, 2.5f, 3.5f, 5f, 7f })
        {
            Waste = w;
            Console.WriteLine($"--- wasteKickShare {w}");
            foreach (var tap in new[] { 0.04f, 0.06f, 0.08f })
            {
                var line = $"tap {tap:0.00}: ";
                foreach (var g in new[] { 0.10f, 0.11f, 0.125f, 0.14f, 0.16f, 0.18f, 0.20f })
                { var r = Rhythm(g, tap); line += $" {g:0.000}={(r.T100 < 0 ? ">30" : r.T100.ToString("0.0"))}/b{r.HeadAvg:0}"; }
                Console.WriteLine(line);
            }
            var ok = "iyi:"; foreach (var g in new[] { 0.24f, 0.26f, 0.30f, 0.50f }) { var r = Rhythm(g); ok += $" {g:0.00}={r.T100:0.00}"; }
            var best = Perfect(0f); Console.WriteLine(ok + $" | en iyi {best.T100:0.00}");
        }
    }

    struct Res { public float T100, V3, V6, V10, H10, Eff, Good, H30, H60, H80, Loss3; }

    // Mashing table: keys in turn at a fixed gap and key time; v3/v6/v10 = speed at that second, h10 = head at 10 s.
    static Res SpamRun(float gap, float hold)
    {
        var s = new RunnerSim(T());
        var r = new Res { T100 = -1f, H30 = -1f, H60 = -1f, H80 = -1f };
        double next = 0.2, rel = -1, t = 0, dt = 1.0 / 240.0, effSum = 0; int leg = 0, relLeg = 0, n = 0, good = 0;
        while (t < 30)
        {
            if (t >= next)
            {
                s.Press(leg); relLeg = leg; rel = t + hold; leg = 1 - leg; next += gap;
                n++; effSum += s.LastEfficiency; if (s.LastResult == PressResult.Good) good++;
            }
            if (rel >= 0 && t >= rel) { s.Release(relLeg); rel = -1; }
            s.Advance((float)dt); t += dt;
            if (r.T100 < 0f && s.Distance >= 100f) r.T100 = (float)t;
            if (r.V3 == 0f && t >= 3) r.V3 = s.Speed;
            if (r.V6 == 0f && t >= 6) r.V6 = s.Speed;
            if (r.V10 == 0f && t >= 10) { r.V10 = s.Speed; r.H10 = s.HeadAngle; }
            if (r.H30 < 0f && s.HeadAngle >= 30f) r.H30 = (float)t;
            if (r.H60 < 0f && s.HeadAngle >= 60f) r.H60 = (float)t;
            if (r.H80 < 0f && s.HeadAngle >= 79f) r.H80 = (float)t;
        }
        r.Eff = n > 0 ? (float)(effSum / n) : 0f;
        r.Good = n > 0 ? (float)good / n : 0f;
        return r;
    }

    static string Sec(float v) => v < 0 ? "  -" : v.ToString("0.0");

    static void SpamTable()
    {
        Console.WriteLine($"{"aralik",7}{"tus",6}{"100m",8}{"v3",6}{"v6",6}{"v10",6}{"bas10",7}{"verim",7}{"good%",7}{"b30",5}{"b60",5}{"b80",5}");
        foreach (var hold in new[] { 0.04f, 0.07f })
        foreach (var gap in new[] { 0.08f, 0.09f, 0.10f, 0.11f, 0.12f, 0.13f, 0.14f, 0.15f, 0.16f, 0.18f, 0.20f, 0.22f, 0.24f, 0.26f, 0.28f, 0.30f, 0.40f, 0.50f })
        {
            var r = SpamRun(gap, hold);
            Console.WriteLine($"{gap,7:0.00}{hold,6:0.00}{(r.T100 < 0 ? ">30" : r.T100.ToString("0.00")),8}{r.V3,6:0.0}{r.V6,6:0.0}{r.V10,6:0.0}{r.H10,7:0}{r.Eff,7:0.00}{r.Good * 100,7:0}{Sec(r.H30),5}{Sec(r.H60),5}{Sec(r.H80),5}");
        }
    }

    // Neighbour gaps 0.01 apart, 0.08-0.30: v10, 100 m, and the v10 ratio to the previous gap (cliff = ratio > 1.6 either way).
    static void Scan()
    {
        foreach (var hold in new[] { 0.04f, 0.07f })
        {
            Console.WriteLine($"tus {hold:0.00}: aralik v10 [100m] oran");
            float prev = -1; var line = "";
            for (int i = 8; i <= 30; i++)
            {
                var r = SpamRun(i / 100f, hold);
                float ratio = prev > 0 ? Math.Max(r.V10, prev) / Math.Max(0.05f, Math.Min(r.V10, prev)) : 1f;
                line += $" {i / 100f:0.00}={r.V10:0.0}[{(r.T100 < 0 ? ">30" : r.T100.ToString("0.0"))}]{(ratio > 1.6f ? "!" + ratio.ToString("0.0") : "")}";
                prev = r.V10;
                if (i % 6 == 1 || i == 30) { Console.WriteLine(line); line = ""; }
            }
            Console.WriteLine(line);
        }
    }

    // Acceptance score of one override set (T-114): list of violated checks.
    static List<string> Score()
    {
        var bad = new List<string>();
        var v10 = new Dictionary<string, float>();
        foreach (var hold in new[] { 0.04f, 0.07f })
        {
            float prev = -1;
            for (int i = 8; i <= 24; i++)
            {
                var r = SpamRun(i / 100f, hold);
                string k = $"{i}/{hold:0.00}";
                if (prev > 0 && Math.Max(r.V10, prev) / Math.Max(0.05f, Math.Min(r.V10, prev)) > 1.6f) bad.Add($"cliff {k}");
                prev = r.V10;
                if (i <= 16)
                {
                    if (r.V10 < 2.5f || r.V10 > 5.2f) bad.Add($"v10 {k}={r.V10:0.0}");
                    if (r.T100 > 0 && r.T100 < 13f) bad.Add($"t100 {k}={r.T100:0.0}");
                }
                if (i >= 18 && i <= 22 && r.T100 > 0 && r.T100 <= 11.8f) bad.Add($"band {k}={r.T100:0.0}");
                if (i >= 10 && i <= 14 && hold == 0.04f)
                {
                    if (r.H30 < 0.5f || r.H30 > 3.5f) bad.Add($"b30 {k}={r.H30:0.0}");
                    if (r.H60 < 2.5f || r.H60 > 6.5f) bad.Add($"b60 {k}={r.H60:0.0}");
                    if (r.H80 < 4.5f || r.H80 > 8.5f) bad.Add($"b80 {k}={r.H80:0.0}");
                }
            }
        }
        return bad;
    }

    // GRID="strainGain=0.04,0.06;strainWastePower=1,2": every combination, sorted by violations.
    static void Grid()
    {
        var spec = Environment.GetEnvironmentVariable("GRID").Split(';');
        var names = new List<string>(); var vals = new List<string[]>();
        foreach (var sp in spec) { var a = sp.Split('='); names.Add(a[0]); vals.Add(a[1].Split(',')); }
        var res = new List<(string, List<string>)>();
        var idx = new int[names.Count];
        while (true)
        {
            var parts = new List<string>();
            for (int i = 0; i < names.Count; i++) parts.Add($"{names[i]}={vals[i][idx[i]]}");
            Ovr = string.Join(";", parts);
            res.Add((Ovr, Score()));
            int d = 0; while (d < idx.Length && ++idx[d] == vals[d].Length) idx[d++] = 0;
            if (d == idx.Length) break;
        }
        res.Sort((a, b) => a.Item2.Count.CompareTo(b.Item2.Count));
        for (int i = 0; i < Math.Min(12, res.Count); i++) Console.WriteLine($"{res[i].Item2.Count,3} {res[i].Item1} :: {string.Join(" ", res[i].Item2.GetRange(0, Math.Min(14, res[i].Item2.Count)))}");
    }

    // Trace one gap: time:speed/head/strain/form every 0.5 s (args: gap tap).
    static void Trace(float gap, float hold)
    {
        var s = new RunnerSim(T());
        double next = 0.2, rel = -1, t = 0, dt = 1.0 / 240.0, rep = 0; int leg = 0, relLeg = 0; string line = "";
        while (t < 12)
        {
            if (t >= next) { float st0 = s.Strain; s.Press(leg); if (Environment.GetEnvironmentVariable("PRESSLOG") != null && t < 3) Console.WriteLine($"  t={t:0.00} {s.LastResult} eff={s.LastEfficiency:0.00} v={s.Speed:0.0} air={s.Airborne} dStrain={s.Strain - st0:0.000}"); relLeg = leg; rel = t + hold; leg = 1 - leg; next += gap; }
            if (rel >= 0 && t >= rel) { s.Release(relLeg); rel = -1; }
            s.Advance((float)dt); t += dt;
            if (t >= rep) { line += $" {t:0.0}:{s.Speed:0.0}/{s.HeadAngle:0}/{s.Strain:0.00}/{s.Form:0.00}"; rep += 0.5; if (rep % 2.0 < 0.01) { Console.WriteLine(line); line = ""; } }
        }
        Console.WriteLine(line);
    }

    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "phased") { if (args.Length > 1) Waste2 = float.Parse(args[1]); Phased(); Table(); return; }
        if (args.Length > 0 && args[0] == "spam") { SpamTable(); return; }
        if (args.Length > 0 && args[0] == "scan") { Scan(); return; }
        if (args.Length > 0 && args[0] == "grid") { Grid(); return; }
        if (args.Length > 0 && args[0] == "trace") { Trace(float.Parse(args[1]), args.Length > 2 ? float.Parse(args[2]) : 0.04f); return; }
        if (args.Length > 0 && args[0] == "start") { Start3(); return; }
        if (args.Length > 0 && args[0] == "cliff") { CliffSweep(); return; }
        if (args.Length > 0 && args[0] == "push") { PushSweep(); return; }
        if (args.Length > 0 && args[0] == "push") { PushSweep(); return; }
        if (args.Length > 0) { Sweep(); return; }
        var all = new List<R> { Perfect(0f), Perfect(0.03f), Perfect(0.06f), Perfect(0.1f), Perfect(0.15f), Perfect(0.25f) };
        foreach (var g in new[] { 0.14f, 0.16f, 0.18f, 0.20f, 0.21f, 0.22f, 0.24f, 0.26f, 0.28f, 0.30f, 0.32f, 0.36f, 0.40f, 0.50f, 0.70f, 1.00f }) all.Add(Rhythm(g));
        foreach (var g in new[] { 0.08f, 0.10f, 0.125f }) all.Add(Rhythm(g, 0.04f));
        foreach (var g in new[] { 0.25f, 0.40f }) { all.Add(Same(g)); all.Add(Both(g)); }
        Console.WriteLine($"{"girdi",-22}{"100m sn",8}{"bitis m/s",10}{"tepe m/s",9}{"verim",7}{"bas ort",8}{"bas max",8}{"good",6}{"early",6}{"late",6}");
        foreach (var r in all)
            Console.WriteLine($"{r.Name,-22}{(r.T100 < 0 ? "  >30" : r.T100.ToString("0.00")),8}{r.VEnd,10:0.0}{r.VMax,9:0.0}{r.Eff,7:0.00}{r.HeadAvg,8:0}{r.HeadMax,8:0}{r.Good,6}{r.Early,6}{r.Late,6}");
    }
}
