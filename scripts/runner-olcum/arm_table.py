# How an arm moves through a run, from trans_<scenario>.csv. Usage: python -I arm_table.py <folder> [<folder2>] [scenario,scenario]
# range: swing's low..high. maxd: largest one-frame move (deg). flat: share of frames moving under 0.5 deg/frame (an arm
# that jumps and then waits has a high flat share). fund: how much of the movement is the slowest wave (1 = a clean sine).
import csv
import math
import os
import sys

DEFAULT = ['slowRun', 'rhythm032', 'fastRun', 'roughRun']
SKIP_FRAMES = 120   # the start's first two seconds


def stats(path):
    rows = list(csv.DictReader(open(path, newline='')))[SKIP_FRAMES:]
    out = []
    for arm in ('armL_swing', 'armR_swing'):
        v = [float(r[arm]) for r in rows]
        d = [abs(v[i + 1] - v[i]) for i in range(len(v) - 1)]
        path_len = sum(d)
        out.append((min(v), max(v), max(d), sum(1 for x in d if x < 0.5) / max(1, len(d)),
                    2.0 * (max(v) - min(v)) * crossings(v) / max(1e-3, path_len)))
    return [sum(o[i] for o in out) / len(out) for i in range(5)]


def crossings(v):
    # The number of half waves: each one covers the full range once if the arm does not stop and turn on the way.
    mid = 0.5 * (min(v) + max(v))
    return max(1, sum(1 for i in range(len(v) - 1) if (v[i] - mid) * (v[i + 1] - mid) < 0)) * 0.5


args = sys.argv[1:]
scenarios = DEFAULT
if args and not os.path.isdir(args[-1]):
    scenarios = args.pop().split(',')
print('%-22s %-12s %-6s %-6s %-6s' % ('folder scenario', 'range', 'maxd', 'flat', 'fund'))
for folder in args:
    for s in scenarios:
        path = os.path.join(folder, 'trans_%s.csv' % s)
        if not os.path.exists(path):
            continue
        low, high, maxd, flat, fund = stats(path)
        print('%-22s %5.0f..%-5.0f %-6.1f %-6.2f %-6.2f' % (os.path.basename(folder.rstrip('/\\')) + ' ' + s, low, high, maxd, flat, fund))
