"""
Sends a built APK to Telegram, so a fresh build lands on the phone that is going to test it
without anyone copying files around.

Builds go to the "GameBuilds" topic of the "Depigo Games" supergroup, which is where the user
looks for them. It uses TELEGRAM_BOT_TOKEN. Sending builds is the ONLY thing the bot does now --
the old telegram_bridge.py, which ran Claude Code tasks from incoming chat messages, has been removed
on purpose (see Tools/telegram_bridge_disabled.md). Nothing here reads incoming messages.

Note what this is NOT: a bot cannot post into Saved Messages ("Избранное") -- that chat belongs to
the user alone and no bot can reach it. A forum topic in a group the bot belongs to is the closest
equivalent, and posting into one needs message_thread_id alongside chat_id, or the file quietly
lands in the group's General topic instead.

Usage:  python send_build_to_telegram.py <path-to-apk> [extra caption text]
"""

import os
import subprocess
import sys
from datetime import datetime

import requests

# The Bot API refuses documents past 50 MB. Worth checking before a two-minute upload fails.
MAX_UPLOAD_BYTES = 50 * 1024 * 1024
UPLOAD_TIMEOUT_SECONDS = 600

# A document's caption is capped at 1024 characters; a plain message gets 4096. Notes longer than
# this go out as a separate message right after the file rather than being cut off mid-sentence.
MAX_CAPTION_CHARS = 900
MAX_MESSAGE_CHARS = 4000

# Written before each build, in Russian: what changed and what to look at. Kept in a file rather
# than passed on the command line so the notes travel with the build automatically instead of
# depending on somebody remembering to type them.
NOTES_FILENAME = "build_notes.txt"

# The "GameBuilds" topic in the "Depigo Games" supergroup. Hard-coded rather than left to the
# environment because neither value is a secret -- a chat id is useless without the bot token --
# and a build should not silently go somewhere else just because a variable was never set on a
# given machine. Both can still be overridden by TELEGRAM_BUILD_CHAT_ID / TELEGRAM_BUILD_THREAD_ID.
DEFAULT_CHAT_ID = "-1003453984361"
DEFAULT_THREAD_ID = "175"


def read_token() -> str:
    """
    The token, preferring the one stored for the user over the one this process inherited.

    A process started before the variable was last changed carries the OLD value for its whole
    life -- which on 2026-08-19 meant a freshly issued token sitting in the registry while every
    call still failed with 401 against the revoked one. Reading the registry first makes a token
    change take effect without restarting anything.
    """
    try:
        import winreg
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as key:
            stored = winreg.QueryValueEx(key, "TELEGRAM_BOT_TOKEN")[0]
            if stored:
                return stored
    except Exception:
        pass
    return os.environ.get("TELEGRAM_BOT_TOKEN", "")


def git_description(project_dir: str) -> str:
    """The commit the build came from -- otherwise three APKs in a chat are indistinguishable."""
    try:
        commit = subprocess.run(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=project_dir, capture_output=True, text=True, timeout=15,
        ).stdout.strip()
        subject = subprocess.run(
            ["git", "log", "-1", "--pretty=%s"],
            cwd=project_dir, capture_output=True, text=True, timeout=15,
        ).stdout.strip()
        return f"{commit} — {subject}" if commit else ""
    except Exception:
        return ""


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: send_build_to_telegram.py <path-to-apk> [extra caption text]")
        return 2

    apk_path = sys.argv[1]
    extra_caption = " ".join(sys.argv[2:]).strip()

    token = read_token()
    chat_id = os.environ.get("TELEGRAM_BUILD_CHAT_ID") or DEFAULT_CHAT_ID
    thread_id = os.environ.get("TELEGRAM_BUILD_THREAD_ID") or DEFAULT_THREAD_ID
    if not token:
        print("No bot token -- set TELEGRAM_BOT_TOKEN and try again.")
        return 1

    if not os.path.isfile(apk_path):
        print(f"No such file: {apk_path}")
        return 1

    size = os.path.getsize(apk_path)
    if size > MAX_UPLOAD_BYTES:
        print(f"{apk_path} is {size / 1024 / 1024:.1f} MB, over the Bot API's 50 MB limit -- not sending.")
        return 1

    project_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    caption_lines = [
        f"🤖 {os.path.basename(apk_path)} — {size / 1024 / 1024:.1f} MB",
        datetime.now().strftime("%Y-%m-%d %H:%M"),
    ]
    described = git_description(project_dir)
    if described:
        caption_lines.append(described)
    if extra_caption:
        caption_lines.append(extra_caption)

    # Short notes ride along with the file card; long ones follow as their own message.
    notes = read_notes(project_dir)
    if notes and len("\n".join(caption_lines)) + len(notes) < MAX_CAPTION_CHARS:
        caption_lines.append("")
        caption_lines.append(notes)
        notes = ""

    payload = {"chat_id": chat_id, "caption": "\n".join(caption_lines)[:1000]}
    # Without this the file lands in the group's General topic instead of GameBuilds.
    if thread_id:
        payload["message_thread_id"] = thread_id

    with open(apk_path, "rb") as handle:
        response = requests.post(
            f"https://api.telegram.org/bot{token}/sendDocument",
            data=payload,
            files={"document": (os.path.basename(apk_path), handle)},
            timeout=UPLOAD_TIMEOUT_SECONDS,
        )

    if response.status_code != 200:
        print(f"Telegram refused the upload: {response.status_code} {response.text[:300]}")
        return 1

    print(f"Sent {os.path.basename(apk_path)} ({size / 1024 / 1024:.1f} MB) to Telegram.")

    if notes:
        send_message(token, chat_id, thread_id, notes[:MAX_MESSAGE_CHARS])
    return 0


def read_notes(project_dir: str) -> str:
    """The hand-written Russian notes for this build, if there are any."""
    path = os.path.join(project_dir, "Tools", "research", NOTES_FILENAME)
    if not os.path.isfile(path):
        return ""
    with open(path, encoding="utf-8") as handle:
        return handle.read().strip()


def send_message(token: str, chat_id: str, thread_id: str, text: str) -> None:
    """Posts the notes as their own message, so the file card stays readable and nothing is truncated."""
    payload = {"chat_id": chat_id, "text": text, "disable_notification": True}
    if thread_id:
        payload["message_thread_id"] = thread_id

    response = requests.post(f"https://api.telegram.org/bot{token}/sendMessage", data=payload, timeout=60)
    if response.status_code != 200:
        print(f"The APK went through but the notes did not: {response.status_code} {response.text[:200]}")
    else:
        print("Sent the build notes.")


if __name__ == "__main__":
    sys.exit(main())
