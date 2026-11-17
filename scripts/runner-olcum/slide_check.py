"""Foot slide while running: how far a foot that is on the ground moves through the world.

Usage: python -I slide_check.py <probe dir> [<probe dir after>]

A foot is on the ground when its ball (toe bone) or its ankle-foot point is within 1.5 cm of its floor (the lowest 1% of its
heights: the ball on the ground, the ankle of a flat foot). Moving faster than 0.3 m/s along the run there is sliding. Only running
counts: frames with no key lately (d_idleStock, the glide after the last tap) and holds are left out, and the first second.
Per scenario: metres slid per second of running, the fastest slide, and the leg phases it happened in.
"""
import collections
import csv
import os
import statistics as st
import sys

SCENARIOS = [("fastRun", 0.20), ("longTapRun", 0.27), ("roughRun", 0.27), ("rhythm032", 0.32), ("slowRun", 0.45),
             ("walkStart", 0.60), ("start", 0.0), ("stop", 0.27), ("slowStop", 0.60)]
CONTACT = 0.015
SLIDE = 0.3


def measure(folder, name):
    path = os.path.join(folder, f"trans_{name}.csv")
    if not os.path.exists(path):
        return None
    with open(path, newline="") as f:
        rows = list(csv.DictReader(f))
    ground = {}
    for s in "LR":
        # The floor: the lowest the bone ever is (the ball on the ground; the ankle of a flat foot). The median while planted was
        # the heel-up height, 3 cm over the floor, and counted a foot held up in the air as sliding.
        toe = sorted(float(x[f"{s}_toeH"]) for x in rows if float(x["t"]) >= 1.0)
        foot = sorted(float(x[f"{s}_footH"]) for x in rows if float(x["t"]) >= 1.0)
        ground[s] = {"toe": toe[len(toe) // 100], "foot": foot[len(foot) // 100]}
    total, worst, seconds = 0.0, 0.0, 0.0
    phases = collections.Counter()
    for i in range(1, len(rows)):
        x, p = rows[i], rows[i - 1]
        dt = float(x["t"]) - float(p["t"])
        if dt <= 0 or float(x["t"]) < 1.0 or x["d_idleStock"] == "1" or float(x["d_heldNow"]) > 0.1:
            continue
        seconds += dt
        for s in "LR":
            for part in ("toe", "foot"):
                base = ground[s][part] + CONTACT
                if float(p[f"{s}_{part}H"]) <= base and float(x[f"{s}_{part}H"]) <= base:
                    v = abs(float(x[f"{s}_{part}Along"]) - float(p[f"{s}_{part}Along"])) / dt
                    if v > SLIDE:
                        key = x[f"{s}_phase"] + ("/g" if x[f"{s}_grounded"] == "1" else "") + ("/pin" if x[f"{s}_pinned"] == "1" else "")
                        phases[key] += v * dt
                        total += v * dt
                        worst = max(worst, v)
                    break
    speed = st.mean(float(x["speed"]) for x in rows if float(x["t"]) > 1.5)
    return total / max(seconds, 1e-6), worst, speed, phases


def geometry(folder, name):
    """Touchdown and toe-off of planted feet after 2 s: ankle z from the hip (m, + ahead), knee, hip height; carry = the difference."""
    path = os.path.join(folder, f"trans_{name}.csv")
    if not os.path.exists(path):
        return None
    with open(path, newline="") as f:
        rows = list(csv.DictReader(f))
    down, up = [], []
    for s in "LR":
        prev = None
        for x in rows:
            if float(x["t"]) >= 2.0 and prev is not None:
                g, pg = x[f"{s}_grounded"] == "1", prev[f"{s}_grounded"] == "1"
                if g and not pg:
                    down.append((float(x[f"{s}_ankleZ"]), float(x[f"{s}_knee"]), float(x["hipY"])))
                if pg and not g:
                    up.append((float(prev[f"{s}_ankleZ"]), float(prev[f"{s}_knee"]), float(prev["hipY"])))
            prev = x
    if not down or not up:
        return None
    mean = lambda rows_, i: sum(r[i] for r in rows_) / len(rows_)
    return mean(down, 0), mean(down, 1), mean(down, 2), mean(up, 0), mean(up, 2)


def jolts(folder, name):
    """The drawn ankle's speed change in one frame (m/s), 99th percentile and largest, running frames only: a teleport shows here."""
    path = os.path.join(folder, f"trans_{name}.csv")
    if not os.path.exists(path):
        return None
    with open(path, newline="") as f:
        rows = list(csv.DictReader(f))
    out = []
    for s in "LR":
        vel = None
        for i in range(1, len(rows)):
            x, p = rows[i], rows[i - 1]
            dt = float(x["t"]) - float(p["t"])
            if dt <= 0 or float(x["t"]) < 1.0 or x["d_idleStock"] == "1":
                vel = None
                continue
            v = ((float(x[f"{s}_footAlong"]) - float(p[f"{s}_footAlong"])) / dt, (float(x[f"{s}_footH"]) - float(p[f"{s}_footH"])) / dt)
            if vel is not None:
                out.append(((v[0] - vel[0]) ** 2 + (v[1] - vel[1]) ** 2) ** 0.5)
            vel = v
    out.sort()
    return out[int(0.99 * (len(out) - 1))], out[-1]


def main():
    folders = sys.argv[1:3]
    print(f"{'senaryo':<11}{'ritim':>6}{'hiz':>6} | " + " | ".join(f"{os.path.basename(f.rstrip('/'))}: m/sn  en m/sn  fazlar" for f in folders))
    for name, gap in SCENARIOS:
        cells, speed = [], 0.0
        for folder in folders:
            r = measure(folder, name)
            if r is None:
                cells.append("-")
                continue
            rate, worst, speed, phases = r
            top = ", ".join(f"{k} {v:.2f}" for k, v in phases.most_common(3))
            cells.append(f"{rate:5.2f} {worst:5.1f}  {top}")
        print(f"{name:<11}{gap:6.2f}{speed:6.1f} | " + " | ".join(cells))
    print()
    print(f"{'senaryo':<11} | " + " | ".join(f"{os.path.basename(f.rstrip('/'))}: inis z / diz / kalca -> kalkis z / kalca = tasinan" for f in folders))
    for name, _ in SCENARIOS:
        cells = []
        for folder in folders:
            g = geometry(folder, name)
            cells.append("-" if g is None else f"{g[0]:+.2f} / {g[1]:3.0f} / {g[2]:.2f} -> {g[3]:+.2f} / {g[4]:.2f} = {g[0] - g[3]:.2f}")
        print(f"{name:<11} | " + " | ".join(cells))
    print()
    print(f"{'senaryo':<11} | " + " | ".join(f"{os.path.basename(f.rstrip('/'))}: ayak bir karede hiz degisimi m/s (%99 / en buyuk)" for f in folders))
    for name, _ in SCENARIOS:
        cells = []
        for folder in folders:
            j = jolts(folder, name)
            cells.append("-" if j is None else f"{j[0]:5.1f} / {j[1]:5.1f}")
        print(f"{name:<11} | " + " | ".join(cells))


if __name__ == "__main__":
    main()
