"""Generates the placeholder UI sound effects (T-052) as 44.1 kHz mono 16-bit WAV files.

Standard library only (wave + math), no randomness: running it again writes byte-identical files.
Run from the repository root:  python -I scripts/sfx-uret.py
Output: Assets/Speed-Staes/Audio/Sfx/<SfxId>.wav (file names match the SfxId enum names, which the config seed uses).
Soft and short on purpose: peak -6 dBFS, 5 ms fade in / fade out. Real sound design replaces these later.
"""
import math
import os
import wave

RATE = 44100
PEAK = 10 ** (-6.0 / 20.0)  # -6 dBFS
RAMP_SECONDS = 0.005
OUT_DIR = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "Assets", "Speed-Staes", "Audio", "Sfx",
)


def tone(freq, seconds, decay=0.0, harmonic=0.0):
    """One sine note; decay > 0 adds an exponential fade (per second), harmonic mixes in the octave above."""
    count = int(round(seconds * RATE))
    out = []
    for i in range(count):
        t = i / RATE
        value = math.sin(2.0 * math.pi * freq * t)
        if harmonic:
            value = (value + harmonic * math.sin(4.0 * math.pi * freq * t)) / (1.0 + harmonic)
        out.append(value * math.exp(-decay * t))
    return out


def silence(seconds):
    return [0.0] * int(round(seconds * RATE))


def finish(samples):
    """Normalises to the peak, then applies the fade in / fade out ramps."""
    top = max(abs(s) for s in samples) or 1.0
    ramp = int(round(RAMP_SECONDS * RATE))
    total = len(samples)
    result = []
    for i, s in enumerate(samples):
        gain = PEAK / top
        if i < ramp:
            gain *= i / ramp
        if i >= total - ramp:
            gain *= (total - 1 - i) / ramp
        result.append(s * gain)
    return result


def notes(sequence, note_seconds, decay):
    samples = []
    for freq in sequence:
        samples += tone(freq, note_seconds, decay, harmonic=0.25)
    return samples


SOUNDS = {
    # ~30 ms soft click
    "Tap": lambda: tone(1800.0, 0.030, decay=90.0),
    # ~120 ms low double blip
    "Denied": lambda: tone(220.0, 0.055, decay=14.0) + silence(0.010) + tone(165.0, 0.055, decay=14.0),
    # ~350 ms rising three notes (C5 E5 G5)
    "Claim": lambda: notes([523.25, 659.25, 783.99], 0.1167, 9.0),
    # ~350 ms rising four notes (G4 C5 E5 G5)
    "Reward": lambda: notes([392.00, 523.25, 659.25, 783.99], 0.0875, 10.0),
    # ~250 ms two-note "coin" (E5 then A5)
    "Purchase": lambda: notes([659.25, 880.00], 0.125, 9.0),
    # ~150 ms beeps; Set is a step above Ready
    "CountdownReady": lambda: tone(440.0, 0.150, decay=6.0, harmonic=0.2),
    "CountdownSet": lambda: tone(523.25, 0.150, decay=6.0, harmonic=0.2),
    # ~400 ms higher, longer beep
    "CountdownGo": lambda: tone(880.0, 0.400, decay=4.0, harmonic=0.3),
}


def write_wav(path, samples):
    frames = bytearray()
    for s in samples:
        value = int(round(max(-1.0, min(1.0, s)) * 32767))
        frames += value.to_bytes(2, "little", signed=True)
    with wave.open(path, "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(bytes(frames))


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name in sorted(SOUNDS):
        samples = finish(SOUNDS[name]())
        path = os.path.join(OUT_DIR, name + ".wav")
        write_wav(path, samples)
        print("%s.wav  %d ms" % (name, round(len(samples) * 1000 / RATE)))


if __name__ == "__main__":
    main()
