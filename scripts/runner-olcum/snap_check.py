"""Snap check: how far each limb jumps in one frame, and how much its speed changes in one frame.
A real limb cannot do either at once. Usage: python -I snap_check.py <probe folder> [scenario ...]"""
import csv
import glob
import os
import sys

folder = sys.argv[1]
names = sys.argv[2:] or sorted(os.path.basename(p)[5:-4] for p in glob.glob(os.path.join(folder, "form_*.csv")) if "summary" not in p)
# column, scale: degrees as they are, metres to cm
LIMBS = [("armL_swing", 1), ("armR_swing", 1), ("armL_elbow", 1), ("armR_elbow", 1), ("L_thighWorld", 1), ("R_thighWorld", 1),
         ("L_knee", 1), ("R_knee", 1), ("trunkLean", 1), ("hipY", 100), ("L_ankleZ", 100), ("R_ankleZ", 100), ("L_ankleY", 100), ("R_ankleY", 100)]
SHORT = ["kolL", "kolR", "dirL", "dirR", "uylL", "uylR", "dizL", "dizR", "govde", "kalca", "bilZ_L", "bilZ_R", "bilY_L", "bilY_R"]

print("Bir karede en buyuk sicrama (derece ya da cm) / hiz degisimi")
print(f"{'':<12}" + "".join(f"{s:>12}" for s in SHORT))
for name in names:
    path = os.path.join(folder, f"form_{name}.csv")
    if not os.path.exists(path):
        continue
    with open(path, newline="") as f:
        rows = [r for r in csv.DictReader(f) if float(r["t"]) >= 0.5]
    cells = ""
    for col, scale in LIMBS:
        if col not in rows[0]:
            cells += f"{'-':>12}"
            continue
        v = [float(r[col]) * scale for r in rows]
        d = [b - a for a, b in zip(v, v[1:])]
        dd = [b - a for a, b in zip(d, d[1:])]
        cells += f"{max(abs(x) for x in d):7.0f}/{max(abs(x) for x in dd):<4.0f}"
    # Skim: a foot within 6 cm of the ground that moves more than 2 cm in the world in one frame: it looks like sliding.
    skim = {}
    for s in "LR":
        toe, z = [float(r[s + "_toeY"]) for r in rows], [float(r[s + "_footWorldZ"]) for r in rows]
        skim[s] = sum(1 for i in range(1, len(rows)) if toe[i] < 0.06 and abs(z[i] - z[i - 1]) > 0.02)
    print(f"{name:<12}" + cells + f"  skim L{skim['L']} R{skim['R']} /{len(rows)}")
