"""Prints form_summary.csv as a table; with two folders, before / after side by side."""
import csv
import os
import sys

COLUMNS = ["speedAvg", "trunkLeanAvg", "splitMax", "thighMaxDegPerSec", "holdShare", "swapShare", "corrHipSplit", "hipAtWide", "hipAtPass",
           "passKnee", "flightShare", "footSlide", "steppingAvg", "kneeYMin", "hipsYawRange", "chestYawRange", "armL_swingMin", "armL_swingMax"]


def load(folder):
    with open(os.path.join(folder, "form_summary.csv"), newline="") as f:
        return {row["scenario"]: row for row in csv.DictReader(f)}


def cell(row, col):
    value = row.get(col, "")
    if value in ("", None):
        return "-"
    try:
        return f"{float(value):.2f}"
    except ValueError:
        return value


folders = sys.argv[1:]
tables = [load(f) for f in folders]
names = list(tables[0].keys())
print(f"{'':<20}" + "".join(f"{n:>11}" for n in names))
for col in COLUMNS:
    for i, table in enumerate(tables):
        tag = col if i == 0 else "  -> sonra"
        print(f"{tag:<20}" + "".join(f"{cell(table.get(n, {}), col):>11}" for n in names))
