"""Synthesises one strike of a village church bell (2026-11-11): no CC0 church bell was found, so the
campaign film's bell is our own. Additive partials of a minor-third bell (hum, prime, tierce, quint,
nominal and upper partials), each with its own decay and a slight beat, plus a short clang.

    python bell.py <out.wav> [fundamental Hz]
"""
import sys
import wave

import numpy as np

RATE = 44100


def strike(f0=196.0, seconds=4.5):
    t = np.arange(int(RATE * seconds)) / RATE
    # (ratio to the prime, level, decay seconds)
    partials = [(0.5, 0.55, 3.8), (1.0, 0.8, 2.8), (1.2, 0.5, 2.0), (1.5, 0.3, 1.5), (2.0, 0.6, 1.6),
                (2.5, 0.25, 0.9), (3.0, 0.22, 0.7), (4.2, 0.14, 0.45), (5.4, 0.09, 0.3), (6.8, 0.06, 0.2)]
    s = np.zeros_like(t)
    rng = np.random.default_rng(7)
    for ratio, level, decay in partials:
        f = f0 * ratio
        beat = 1.0 + 0.04 * np.sin(2 * np.pi * rng.uniform(0.6, 1.8) * t)
        s += level * beat * np.exp(-t / decay) * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28))
    clang = rng.standard_normal(len(t)) * np.exp(-t / 0.012) * 0.35
    s += np.convolve(clang, np.ones(6) / 6, mode="same")
    s *= np.minimum(1.0, t / 0.002)
    return s / np.abs(s).max() * 0.9


def main():
    out = sys.argv[1]
    f0 = float(sys.argv[2]) if len(sys.argv) > 2 else 196.0
    s = strike(f0)
    pcm = (np.stack([s, s], 1) * 32767).astype(np.int16)
    with wave.open(out, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    print("BELL", out)


main()
