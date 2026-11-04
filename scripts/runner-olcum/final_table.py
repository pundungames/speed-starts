"""Side by side of the run scenarios from two probe folders, with hip jumps computed from the frames."""
import csv
import os
import sys

a, b = sys.argv[1], sys.argv[2]
names = ["perfect", "rhythm026", "late050", "mash012", "both030", "same030", "same025", "same020", "tap012", "tap016", "human", "holdOne", "holdBoth"]
cols = ["speedAvg", "trunkLeanAvg", "holdFrames", "speedRipple", "firstSecondSpeedSteps", "kneeYMin", "passKnee", "hipsYawRange", "armL_swingMax"]


def summary(folder):
    path = os.path.join(folder, "form_summary.csv")
    with open(path, newline="") as f:
        return {r["scenario"]: r for r in csv.DictReader(f)}


def jumps(folder, name):
    path = os.path.join(folder, f"form_{name}.csv")
    if not os.path.exists(path):
        return None
    with open(path, newline="") as f:
        rows = [x for x in csv.DictReader(f) if float(x["t"]) >= 3.0]
    h = [float(x["hipY"]) for x in rows]
    d = [h[i] - h[i - 1] for i in range(1, len(h))]
    return -min(d) * 100, max(d) * 100


sa, sb = summary(a), summary(b)
print(f"{'':<11}" + "".join(f"{c[:11]:>12}" for c in cols) + f"{'fall cm':>9}{'rise cm':>9}")
for n in names:
    for tag, s, folder in (("", sa, a), ("  ->", sb, b)):
        if n not in s:
            continue
        r = s[n]
        cells = ""
        for c in cols:
            v = r.get(c, "")
            try:
                cells += f"{float(v):>12.2f}"
            except (TypeError, ValueError):
                cells += f"{'-':>12}"
        j = jumps(folder, n)
        print(f"{(tag or n):<11}" + cells + (f"{j[0]:>9.1f}{j[1]:>9.1f}" if j else ""))
