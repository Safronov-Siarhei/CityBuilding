"""The ten boulders' layouts (Stone1..Stone10, smallest first), shared by stones1.py, which
builds the gatherable boulders, and terrain.py, whose decorative pebbles and crags are drawn
from the same shapes (the user, 2026-11-01: the old box rocks replaced by the new ones)."""

# (x, y, radius x, radius y, height, colour, crown, seed, rotation, taper) per crag, smallest first.
STONES = {
    "Stone1": [(-0.1, -0.03, 0.16, 0.13, 0.2, 'stone_mid', None, 1, 10, 0.6),
               (0.13, 0.05, 0.13, 0.11, 0.17, 'stone_light', None, 4, 40, 0.6),
               (-0.02, 0.15, 0.09, 0.08, 0.12, 'stone_dark', None, 7, 0, 0.55)],
    "Stone2": [(-0.05, 0.0, 0.2, 0.15, 0.2, 'stone_mid', None, 2, 15, 0.6),
               (0.17, -0.07, 0.11, 0.09, 0.13, 'stone_dark', None, 5, 50, 0.55)],
    "Stone3": [(0.0, 0.0, 0.22, 0.17, 0.2, 'stone_light', None, 3, 25, 0.6),
               (-0.2, 0.14, 0.07, 0.06, 0.08, 'stone_mid', None, 8, 0, 0.5)],
    "Stone4": [(-0.04, 0.02, 0.2, 0.18, 0.32, 'stone_mid', None, 6, 5, 0.55),
               (0.2, -0.12, 0.1, 0.09, 0.15, 'stone_dark', None, 9, 35, 0.5)],
    "Stone5": [(0.0, 0.0, 0.27, 0.24, 0.42, 'stone_dark', None, 0, 20, 0.5),
               (-0.22, -0.17, 0.08, 0.07, 0.1, 'stone_light', None, 11, 0, 0.5)],
    "Stone6": [(-0.08, 0.05, 0.25, 0.23, 0.52, 'stone_mid', 'grass_dark', 10, 0, 0.5),
               (0.2, -0.12, 0.15, 0.13, 0.28, 'stone_light', None, 3, 30, 0.55)],
    "Stone7": [(0.0, 0.03, 0.3, 0.28, 0.62, 'stone_light', 'grass', 12, 12, 0.6),
               (-0.24, -0.2, 0.11, 0.1, 0.18, 'stone_mid', None, 5, 0, 0.5),
               (0.25, -0.18, 0.09, 0.08, 0.12, 'stone_dark', None, 1, 45, 0.5)],
    "Stone8": [(-0.1, 0.06, 0.29, 0.27, 0.74, 'stone_mid', 'grass_dark', 4, 8, 0.62),
               (0.2, -0.1, 0.2, 0.18, 0.5, 'stone_dark', None, 7, 32, 0.5)],
    "Stone9": [(0.0, 0.06, 0.36, 0.33, 0.9, 'stone_dark', 'grass', 9, 18, 0.62),
               (-0.26, -0.22, 0.13, 0.12, 0.3, 'stone_light', None, 2, 0, 0.5),
               (0.28, -0.22, 0.12, 0.1, 0.22, 'stone_mid', None, 6, 40, 0.5)],
    "Stone10": [(-0.05, 0.1, 0.36, 0.32, 1.1, 'stone_mid', 'grass', 11, 6, 0.6),
                (0.22, -0.06, 0.22, 0.24, 0.78, 'stone_dark', 'grass_dark', 3, 28, 0.6),
                (-0.22, -0.24, 0.17, 0.15, 0.45, 'stone_light', None, 8, 15, 0.5),
                (0.24, -0.32, 0.1, 0.09, 0.16, 'stone_mid', None, 0, 0, 0.5)],
}
