"""Keeps the repo's copy of the Blender sources (Blender/) in step with the user's working folder.

The user works in Blender on a Google Drive folder synced across their devices; the repo keeps a copy
so the sources survive without the Drive (the user, 2026-11-11: everything must be on the fork).
Blender/ sits OUTSIDE Assets/, so Unity never imports it and nothing of it reaches a build.

    python Tools/blend_sync.py              # Drive -> repo: copy new/changed files (size or mtime differ)
    python Tools/blend_sync.py --to-drive   # repo -> a fresh working folder (a new machine, no Drive)
    python Tools/blend_sync.py --source DIR # another working folder

Blender's own backups (*.blend1, *.blend2...) are skipped. Nothing is ever deleted.
"""
import argparse
import os
import re
import shutil
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGET = os.path.join(REPO, "Blender")
DEFAULT_SOURCE = os.environ.get("BLEND_SOURCE", r"H:\Другие компьютеры\Ноутбук\blender projects\Buildings3D")
BACKUP = re.compile(r"\.blend\d+$", re.IGNORECASE)


def same(a, b):
    sa, sb = os.stat(a), os.stat(b)
    return sa.st_size == sb.st_size and abs(sa.st_mtime - sb.st_mtime) < 2.0


def sync(src, dst, label):
    copied = 0
    for root, _, files in os.walk(src):
        for f in files:
            if BACKUP.search(f) or f.startswith("~$"):
                continue
            s = os.path.join(root, f)
            d = os.path.join(dst, os.path.relpath(s, src))
            if os.path.exists(d) and same(s, d):
                continue
            os.makedirs(os.path.dirname(d), exist_ok=True)
            shutil.copy2(s, d)
            copied += 1
            print(f"{label}: {os.path.relpath(s, src)}")
    print(f"{copied} file(s) copied")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--to-drive", action="store_true")
    ap.add_argument("--source", default=DEFAULT_SOURCE)
    a = ap.parse_args()
    if a.to_drive:
        sync(TARGET, a.source, "repo -> working folder")
    else:
        if not os.path.isdir(a.source):
            print(f"no working folder at {a.source} (pass --source or set BLEND_SOURCE)")
            return 1
        sync(a.source, TARGET, "working folder -> repo")


if __name__ == "__main__":
    sys.exit(main())
