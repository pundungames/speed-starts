# Per-swing table of a leg's thigh and knee from trans_<scenario>.csv.
# Usage: python -I swing_table.py <folder> [<folder2>] [scenario,scenario]
# A swing = the frames from Push's end to the next Push (phases Recover, Mid). Shin = thigh - (180 - knee):
# the shin's angle from straight down, + = ahead of the knee.
import csv
import os
import sys

DEFAULT = ['slowRun', 'rhythm032', 'fastRun', 'roughRun']
MIN_FRAMES = 12   # shorter 'swings' are a stance cut short, not a swing; they pull the average down


def swings(path):
    rows = list(csv.DictReader(open(path, newline='')))
    out = []
    for side in 'LR':
        cur = None
        for r in rows:
            phase = r[side + '_phase']
            if phase in ('Recover', 'Mid'):
                if cur is None:
                    cur = []
                cur.append(r)
            elif cur is not None:
                if len(cur) >= MIN_FRAMES:
                    out.append((side, cur))
                cur = None
    return sorted(out, key=lambda s: float(s[1][0]['t']))


def row_of(side, frames):
    thigh = [float(r[side + '_thigh']) for r in frames]
    world = [float(r[side + '_thighWorld']) for r in frames]
    knee = [float(r[side + '_knee']) for r in frames]
    peak = max(range(len(thigh)), key=lambda i: thigh[i])
    shin = [thigh[i] - (180.0 - knee[i]) for i in range(len(thigh))]
    return dict(
        t=float(frames[0]['t']),
        speed=float(frames[0]['speed']),
        frames=len(frames),
        thigh_peak=thigh[peak],
        world_peak=max(world),
        knee_at_peak=knee[peak],
        knee_min=min(knee),
        peak_share=peak / max(1, len(frames) - 1),
        shin_after=max(shin[peak:]),
        knee_end=knee[-1],
        thigh_end=thigh[-1],
    )


def table(folder, scenario):
    path = os.path.join(folder, 'trans_%s.csv' % scenario)
    if not os.path.exists(path):
        print('  (no %s)' % path)
        return
    rows = [row_of(s, f) for s, f in swings(path)]
    if not rows:
        print('  (no swings)')
        return
    keys = ['speed', 'frames', 'thigh_peak', 'world_peak', 'knee_at_peak', 'knee_min', 'peak_share', 'shin_after', 'knee_end', 'thigh_end']
    print('  ' + ' '.join('%-12s' % k for k in ['t'] + keys))
    for r in rows:
        print('  ' + ' '.join('%-12.2f' % r[k] for k in ['t'] + keys))
    tail = rows[len(rows) // 2:]
    print('  avg(last half) ' + ' '.join('%s=%.1f' % (k, sum(r[k] for r in tail) / len(tail)) for k in keys))


args = sys.argv[1:]
scenarios = DEFAULT
if args and not os.path.isdir(args[-1]):
    scenarios = args.pop().split(',')
for folder in args:
    print('=== ' + folder)
    for s in scenarios:
        print('-- ' + s)
        table(folder, s)
