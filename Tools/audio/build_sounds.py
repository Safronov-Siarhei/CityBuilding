"""Synthesises the building sounds (the user, 2026-11-17: «когда игрок ставит какое-то здание на карте - должен быть
звук, при окончании его улучшения на карте тоже»; every mode). Our own, like bell.py: no download.

    place     a building set down: a soft low thump and two wooden knocks (a mallet on a beam), ~0.4 s
    upgraded  an upgrade (or a construction) finished: a bright rising three-note chime, ~1.3 s

    python Tools/audio/build_sounds.py   ->  Assets/_Project/Audio/Resources/Build/{place,upgraded}.wav
"""
import os
import wave

import numpy as np

RATE = 22050
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', '_Project', 'Audio', 'Resources', 'Build')


def t_of(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def knock(seconds, f, level, rng):
    """A wooden knock: a short decaying body tone with two overtones and a click of filtered noise."""
    t = t_of(seconds)
    body = (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.71 * t) + 0.25 * np.sin(2 * np.pi * f * 4.1 * t))
    body *= np.exp(-t / 0.045)
    click = rng.standard_normal(len(t)) * np.exp(-t / 0.006)
    click = np.convolve(click, np.ones(4) / 4, mode='same')
    return level * (body + 0.6 * click)


def place():
    rng = np.random.default_rng(3)
    s = np.zeros(int(RATE * 0.45))
    t = t_of(0.45)
    # the thump: a low sine falling in pitch
    f = 110 * np.exp(-t / 0.15) + 55
    phase = 2 * np.pi * np.cumsum(f) / RATE
    s += 0.9 * np.sin(phase) * np.exp(-t / 0.09)
    for at, freq, level in ((0.0, 420, 0.7), (0.11, 470, 0.55)):
        k = knock(0.25, freq, level, rng)
        i = int(at * RATE)
        s[i:i + len(k)] += k[:len(s) - i]
    s *= np.minimum(1.0, t / 0.002)
    return s


def chime_note(f, seconds, level):
    t = t_of(seconds)
    s = np.zeros_like(t)
    for ratio, lv, decay in ((1.0, 1.0, 0.9), (2.0, 0.35, 0.5), (3.0, 0.15, 0.3), (4.2, 0.08, 0.2)):
        s += lv * np.exp(-t / decay) * np.sin(2 * np.pi * f * ratio * t)
    return level * s * np.minimum(1.0, t / 0.003)


def upgraded():
    s = np.zeros(int(RATE * 1.4))
    for at, f, level in ((0.0, 784.0, 0.7), (0.11, 988.0, 0.75), (0.22, 1175.0, 0.85), (0.22, 587.0, 0.35)):
        n = chime_note(f, 1.4 - at, level)
        i = int(at * RATE)
        n = n[:len(s) - i]
        s[i:i + len(n)] += n
    return s


# ---- round 2 (the user, 2026-11-17: «мне не нравится звук размещения здания» + a sound for the build's finish)

def noise_burst(seconds, decay, smooth, rng):
    t = t_of(seconds)
    n = rng.standard_normal(len(t))
    n = np.convolve(n, np.ones(smooth) / smooth, mode='same')
    return n * np.exp(-t / decay)


def place_pop():
    """B: a soft round «plop» like a piece set on a board game: a falling sine pop + a tiny high tick."""
    t = t_of(0.3)
    f = 520 * np.exp(-t / 0.05) + 180
    s = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.07)
    s += 0.25 * np.sin(2 * np.pi * 1900 * t) * np.exp(-t / 0.012)
    return s * np.minimum(1.0, t / 0.002)


def place_thud():
    """C: something heavy set on soft ground: a deep thud and a short puff of dust."""
    rng = np.random.default_rng(11)
    t = t_of(0.5)
    f = 90 * np.exp(-t / 0.08) + 45
    s = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.12)
    s += 0.35 * noise_burst(0.5, 0.06, 18, rng)
    return s * np.minimum(1.0, t / 0.003)


def place_chunk():
    """D: a wooden block clicked into place: one dry mid knock, a hint of thump under it."""
    rng = np.random.default_rng(5)
    t = t_of(0.28)
    s = knock(0.28, 300, 0.9, rng)
    s += 0.5 * np.sin(2 * np.pi * 120 * t) * np.exp(-t / 0.05)
    return s


def built_poof():
    """The build done under the dust (BuildFinishEffect): a soft rising whoosh of dust, then a warm two-note «ding-dong»."""
    rng = np.random.default_rng(21)
    total = 1.5
    s = np.zeros(int(RATE * total))
    t = t_of(0.45)
    swell = rng.standard_normal(len(t))
    swell = np.convolve(swell, np.ones(10) / 10, mode='same') * np.sin(np.pi * t / 0.45) ** 2 * 0.35
    s[:len(swell)] += swell
    for at, f, level in ((0.3, 659.0, 0.8), (0.45, 988.0, 0.9)):
        n = chime_note(f, total - at, level)
        i = int(at * RATE)
        n = n[:len(s) - i]
        s[i:i + len(n)] += n
    return s


def write(name, s):
    s = s / np.abs(s).max() * 0.85
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.wav')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((s * 32767).astype(np.int16).tobytes())
    print('written', os.path.normpath(path))


if __name__ == '__main__':
    # the variants to listen to (Design/audio/, not in the game)
    if len(os.sys.argv) > 1 and os.sys.argv[1] == 'variants':
        OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Design', 'audio')
        write('place_A_knock', place())
        write('place_B_pop', place_pop())
        write('place_C_thud', place_thud())
        write('place_D_block', place_chunk())
        write('built_poof', built_poof())
        write('upgraded_chime', upgraded())
    else:
        write('place', place_thud())     # the user's pick of the four, 2026-11-17 (C)
        write('built', built_poof())
        write('upgraded', upgraded())
