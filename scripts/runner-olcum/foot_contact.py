"""How high the feet ride above their lowest value, and how fast a foot near the ground moves through the world."""
import csv
import os
import sys

out = sys.argv[1]
for name in ["perfect", "rhythm026", "late050", "same030", "both030"]:
    with open(os.path.join(out, f"form_{name}.csv"), newline="") as f:
        rows = list(csv.DictReader(f))
    all_rows = rows
    rows = [x for x in rows if float(x["t"]) >= 3.0]
    line = [f"{name:<10}"]
    for tol in (0.02, 0.035, 0.05, 0.08):
        low = {s: min(float(x[f"{s}_footY"]) for x in all_rows) for s in "LR"}
        none = 0
        slide, n = 0.0, 0
        for i, x in enumerate(rows):
            touch = [s for s in "LR" if float(x[f"{s}_footY"]) <= low[s] + tol]
            if not touch:
                none += 1
            if i:
                for s in touch:
                    slide += abs(float(x[f"{s}_footWorldZ"]) - float(rows[i - 1][f"{s}_footWorldZ"])) * 60
                    n += 1
        line.append(f"tol {tol:.3f}: air {none / len(rows):.2f} slide {slide / max(1, n):5.1f}")
    speed = sum(float(x["speed"]) for x in rows) / len(rows)
    print(" | ".join(line) + f" | speed {speed:.1f}")
