"""Two-way sync of the AI's project memory between the repo and the local Claude folder.

The repo copy (.claude/memory/) is the durable one: it survives a deleted or unreachable Claude
folder and travels with the fork to another machine or another AI. The local copy
(%USERPROFILE%/.claude/projects/<project-folder>/memory/) is the one Claude Code loads by itself.

    python Tools/memory_sync.py            # sync both ways: for each file the newer copy wins
    python Tools/memory_sync.py --to-local # restore a fresh machine from the repo
    python Tools/memory_sync.py --to-repo  # push local notes into the repo

Nothing is ever deleted. The local folder's name is the project path with ':' '\\' '/' made '-'
(G:\\Fork\\CityBuilding -> G--Fork-CityBuilding); pass --local <dir> to override.
"""
import argparse
import os
import shutil
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO_MEMORY = os.path.join(REPO, ".claude", "memory")


def local_memory():
    name = REPO.replace(":", "-").replace("\\", "-").replace("/", "-")
    return os.path.join(os.path.expanduser("~"), ".claude", "projects", name, "memory")


def copy(src, dst, label):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copy2(src, dst)
    print(f"{label}: {os.path.basename(src)}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--to-local", action="store_true")
    ap.add_argument("--to-repo", action="store_true")
    ap.add_argument("--local")
    a = ap.parse_args()
    local = a.local or local_memory()
    os.makedirs(REPO_MEMORY, exist_ok=True)
    os.makedirs(local, exist_ok=True)
    names = set(os.listdir(REPO_MEMORY)) | set(os.listdir(local))
    changed = 0
    for n in sorted(names):
        r, l = os.path.join(REPO_MEMORY, n), os.path.join(local, n)
        if os.path.isdir(r) or os.path.isdir(l):
            continue
        has_r, has_l = os.path.exists(r), os.path.exists(l)
        if has_r and has_l:
            with open(r, "rb") as fr, open(l, "rb") as fl:
                if fr.read() == fl.read():
                    continue
        if a.to_local:
            if has_r:
                copy(r, l, "repo -> local"); changed += 1
        elif a.to_repo:
            if has_l:
                copy(l, r, "local -> repo"); changed += 1
        elif not has_l or (has_r and os.path.getmtime(r) > os.path.getmtime(l)):
            copy(r, l, "repo -> local"); changed += 1
        else:
            copy(l, r, "local -> repo"); changed += 1
    print(f"{changed} file(s) copied; repo {REPO_MEMORY}; local {local}")


if __name__ == "__main__":
    sys.exit(main())
