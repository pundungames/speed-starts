"""Transition check (T-139): where does a runner stutter or tremble, and which phase change causes it.

Reads trans_*.csv written by RunnerTransitions_WriteFrameStrips (one row per 60 fps frame).
Usage: python -I transition_check.py <dir> [<dir2>]
With a second directory the metrics are printed before (dir) and after (dir2) side by side.
Standard library only. Units: degrees, centimetres, seconds. d = change in one frame, dd = change of that change (jerk).
"""
import csv
import glob
import os
import sys

# Angle channels wrap at 360; the others are metres turned into centimetres.
CHANNELS = [  # key, column, scale, is_angle, short name
    ("tL", "L_thigh", 1, False, "L thigh"), ("tR", "R_thigh", 1, False, "R thigh"),
    ("kL", "L_knee", 1, False, "L knee"), ("kR", "R_knee", 1, False, "R knee"),
    ("hipH", "hipBoneH", 100, False, "hip H cm"), ("hipP", "hips_pitch", 1, True, "hip pitch"),
    ("chP", "chest_pitch", 1, True, "chest pitch"), ("headP", "headPitch", 1, True, "head pitch"),
    ("aL", "armL_swing", 1, False, "L arm"), ("aR", "armR_swing", 1, False, "R arm"),
]
MAIN = ["tL", "tR", "kL", "kR", "hipH", "hipP", "chP"]       # the channels in the worst-change table
TREMBLE = {"hipH": 0.15, "hipP": 0.15, "chP": 0.15}           # per-frame speed (cm or deg) a flip side must exceed
TREMBLE_NAMES = {"hipH": "hipH", "hipP": "hipP", "chP": "chestP"}
SHORT_HOLD_SECONDS = 0.2
TOP_EVENTS = 12
CLUSTER_FRAMES = 3                                             # jerk peaks of one channel closer than this are one event
JERK_FLOOR = {"hipH": 0.05}                                     # smallest jerk (deg, or cm for hipH) that ranks a channel by its own p90
SKIP_FRAMES = 3                                                # the first frames are the rig settling
PH = {"Rest": "Rst", "Push": "Psh", "Recover": "Rcv", "Mid": "Mid", "Reach": "Rch", "Front": "Frt", "Hold": "Hld", "Land": "Lnd"}


def wrap(a):
    a %= 360.0
    return a - 360.0 if a > 180.0 else a


def num(row, col, scale=1.0):
    try:
        return float(row[col]) * scale
    except (KeyError, ValueError):
        return float("nan")


class Scenario:
    def __init__(self, path):
        self.name = os.path.basename(path)[6:-4]
        with open(path, newline="") as f:
            self.rows = list(csv.DictReader(f))[SKIP_FRAMES:]
        rows = self.rows
        self.n = len(rows)
        self.t = [num(r, "t") for r in rows]
        gaps = sorted(b - a for a, b in zip(self.t, self.t[1:]) if b > a)
        self.dt = gaps[len(gaps) // 2] if gaps else 1 / 60
        # A frame pair is valid when the time step is about one frame; a run reset breaks the series.
        self.ok2 = [False] + [0.5 * self.dt < self.t[i] - self.t[i - 1] < 1.6 * self.dt for i in range(1, self.n)]
        self.ok3 = [False, False] + [self.ok2[i] and self.ok2[i - 1] for i in range(2, self.n)]
        self.v, self.d, self.dd = {}, {}, {}
        for key, col, scale, is_angle, _ in CHANNELS:
            v = [num(r, col, scale) for r in rows]
            d = [float("nan")] * self.n
            dd = [float("nan")] * self.n
            for i in range(1, self.n):
                if self.ok2[i]:
                    d[i] = wrap(v[i] - v[i - 1]) if is_angle else v[i] - v[i - 1]
            for i in range(2, self.n):
                if self.ok3[i]:
                    dd[i] = d[i] - d[i - 1]   # centred on frame i-1
            self.v[key], self.d[key], self.dd[key] = v, d, dd
        self.events = [(self.t[i], r.get("events", "")) for i, r in enumerate(rows) if r.get("events", "") not in ("", "-")]

    def phase(self, leg, i):
        i = max(0, min(self.n - 1, i))
        return self.rows[i].get(leg + "_phase", "?")

    def flag(self, leg, name, i):
        i = max(0, min(self.n - 1, i))
        return int(num(self.rows[i], f"{leg}_{name}") > 0.5)


def worst(series, t, shift=0):
    best, at = 0.0, 0.0
    for i, x in enumerate(series):
        if x == x and abs(x) > best:
            best, at = abs(x), t[max(0, i + shift)]
    return best, at


def percentile(values, q):
    vals = sorted(abs(x) for x in values if x == x)
    return vals[min(len(vals) - 1, int(q * len(vals)))] if vals else 0.0


def tremble(sc, key):
    """Velocity sign flips per second: (between significant speeds, between adjacent frames)."""
    thr = TREMBLE[key]
    d = sc.d[key]
    flips = adj = 0
    last = 0
    for i in range(1, sc.n):
        x = d[i]
        if x != x:
            last = 0
            continue
        if abs(x) > thr:
            s = 1 if x > 0 else -1
            if last and s != last:
                flips += 1
            last = s
        if i > 1 and d[i - 1] == d[i - 1] and abs(x) > thr and abs(d[i - 1]) > thr and (x > 0) != (d[i - 1] > 0):
            adj += 1
    secs = max(sc.n * sc.dt, 1e-6)
    return flips / secs, adj / secs


def ground(sc):
    """Worst world slide per frame (cm) while grounded or pinned, and the lowest toe and foot height (cm)."""
    out = {}
    for leg in "LR":
        along = [num(r, f"{leg}_footAlong", 100) for r in sc.rows]
        slide, at = 0.0, 0.0
        for i in range(1, sc.n):
            if not sc.ok2[i]:
                continue
            a = sc.flag(leg, "grounded", i) or sc.flag(leg, "pinned", i)
            b = sc.flag(leg, "grounded", i - 1) or sc.flag(leg, "pinned", i - 1)
            if a and b and abs(along[i] - along[i - 1]) > slide:
                slide, at = abs(along[i] - along[i - 1]), sc.t[i]
        toe = min((num(r, f"{leg}_toeH", 100) for r in sc.rows if num(r, f"{leg}_toeH") == num(r, f"{leg}_toeH")), default=float("nan"))
        foot = min((num(r, f"{leg}_footH", 100) for r in sc.rows if num(r, f"{leg}_footH") == num(r, f"{leg}_footH")), default=float("nan"))
        out[leg] = (slide, at, toe, foot)
    return out


def phase_changes(sc):
    counts, short, holds = {}, 0, 0
    for leg in "LR":
        start = None
        for i in range(1, sc.n):
            a, b = sc.phase(leg, i - 1), sc.phase(leg, i)
            if a != b and sc.ok2[i]:
                key = f"{PH.get(a, a)}>{PH.get(b, b)}"
                counts[key] = counts.get(key, 0) + 1
                if b == "Hold":
                    start = sc.t[i]
                if a == "Hold" and start is not None:
                    holds += 1
                    if sc.t[i] - start < SHORT_HOLD_SECONDS:
                        short += 1
                    start = None
    return counts, short, holds


def jerk_events(sc):
    """Top jerk peaks over all channels, ranked by size relative to the channel's own p90 so cm and degrees mix."""
    cand = []
    for key, _, _, _, name in CHANNELS:
        dd = sc.dd[key]
        p90 = max(percentile(dd, 0.9), JERK_FLOOR.get(key, 0.3))
        peaks = sorted(((abs(x), i) for i, x in enumerate(dd) if x == x), reverse=True)
        taken = []
        for size, i in peaks[:40]:
            if any(abs(i - j) < CLUSTER_FRAMES for j in taken):
                continue
            taken.append(i)
            cand.append((size / p90, size, i - 1, key, name))
    cand.sort(reverse=True)
    return cand[:TOP_EVENTS]


def nearest_event(sc, t):
    if not sc.events:
        return "-"
    et, ev = min(sc.events, key=lambda e: abs(e[0] - t))
    return f"{ev} {et - t:+.2f}s"


def describe(sc, c):
    """Phases before and after, carry and grounded changes, around centre frame c."""
    parts = []
    for leg in "LR":
        a, b = sc.phase(leg, c - 1), sc.phase(leg, c + 1)
        parts.append(f"{leg}:{PH.get(a, a)}>{PH.get(b, b)}")
    ch = []
    for leg in "LR":
        for name, tag in (("carry", "c"), ("grounded", "g")):
            a, b = sc.flag(leg, name, c - 1), sc.flag(leg, name, c + 1)
            if a != b:
                ch.append(f"{leg}{tag}{a}{b}")
    return " ".join(parts), (",".join(ch) if ch else "-")


def metrics(sc):
    """Everything a before/after comparison needs, as ordered (label, text) pairs."""
    out = []
    for key in MAIN:
        name = [c[4] for c in CHANNELS if c[0] == key][0]
        d, dt = worst(sc.d[key], sc.t)
        dd, ddt = worst(sc.dd[key], sc.t, -1)
        out.append((f"{name} d", f"{d:.2f}@{dt:.2f}"))
        out.append((f"{name} jerk", f"{dd:.2f}@{ddt:.2f}"))
    for key in TREMBLE:
        f, a = tremble(sc, key)
        out.append((f"tremble {TREMBLE_NAMES[key]} /s", f"{f:.1f} (adj {a:.1f})"))
    g = ground(sc)
    for leg in "LR":
        slide, at, toe, foot = g[leg]
        out.append((f"slide {leg} cm/fr", f"{slide:.2f}@{at:.2f}"))
        out.append((f"low {leg} toe/foot cm", f"{toe:.1f}/{foot:.1f}"))
    counts, short, holds = phase_changes(sc)
    out.append(("phase changes", " ".join(f"{k} {v}" for k, v in sorted(counts.items(), key=lambda kv: -kv[1])) or "-"))
    out.append(("Hold phases / <0.2s", f"{holds} / {short}"))
    return out


def print_block(sc):
    print(f"== {sc.name}  frames={sc.n} dur={sc.n * sc.dt:.2f}s")
    for label, text in metrics(sc):
        print(f"  {label:<22}{text}")
    print("  top jerk: t, channel, size, phases before>after, carry/grounded changes, nearest key")
    for score, size, c, key, name in jerk_events(sc):
        if c < 0 or c >= sc.n:
            continue
        ph, ch = describe(sc, c)
        print(f"   {sc.t[c]:5.2f} {name:<11}{size:6.2f} {ph:<20} {ch:<10} {nearest_event(sc, sc.t[c])}")


def print_pair(a, b):
    print(f"== {a.name}  before: frames={a.n}  after: frames={b.n}")
    mb = dict(metrics(b))
    for label, text in metrics(a):
        print(f"  {label:<22}{text:<34}{mb.get(label, '-')}")


def load(folder):
    paths = sorted(glob.glob(os.path.join(folder, "trans_*.csv")))
    return {s.name: s for s in (Scenario(p) for p in paths) if s.n > 3}


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    first = load(sys.argv[1])
    if len(sys.argv) < 3:
        for sc in first.values():
            print_block(sc)
        return 0
    second = load(sys.argv[2])
    for name, sc in first.items():
        if name in second:
            print_pair(sc, second[name])
    return 0


if __name__ == "__main__":
    sys.exit(main())
