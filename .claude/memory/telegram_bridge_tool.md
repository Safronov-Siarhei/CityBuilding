---
name: telegram-bridge-tool
description: "Telegram-бот проекта отправляет ТОЛЬКО APK-билды; удалённый запуск задач через чат (telegram_bridge.py) отключён и удалён 2026-08-20."
metadata:
  node_type: memory
  type: project
  originSessionId: 04bdd1d4-f9be-4484-b721-4f059c90b34c
  modified: 2026-08-20T00:00:00.000Z
---

Телеграм-бот проекта CityBuilding теперь умеет ровно одно: отправлять собранный APK
(`Tools/research/send_build_to_telegram.py`, вызывается из `Tools/research/build_android.ps1`) в
топик «GameBuilds» группы «Depigo Games». Он **не читает входящие сообщения вообще**.

**Что убрано (2026-08-20, по просьбе пользователя)**: `Tools/telegram_bridge.py` — long-polling
Telegram + запуск `claude -p "<сообщение>" --permission-mode acceptEdits` в директории проекта,
то есть выполнение произвольных задач на ПК с телефона. Файл удалён, вместо него
`Tools/telegram_bridge_disabled.md` с объяснением. Не восстанавливать и не предлагать «поднять
мост обратно» без явной просьбы.

**Why**: пользователь решил, что удалённое общение с ботом больше не нужно; из функционала должна
остаться только доставка билдов. Побочно это закрывает и дыру: любой, кто получил бы токен, через
мост получал полный доступ к инструментам на машине.

**How to apply**:
- Задача планировщика **"TelegramClaudeBridge"** (запуск при входе в систему) ещё существует и на
  момент правки была в состоянии Ready — отключить её не удалось, `schtasks /Change /DISABLE` и
  `Disable-ScheduledTask` возвращают Access is denied без прав администратора. Скрипта, который она
  запускает, больше нет, так что вреда она не приносит, но окончательно убрать её можно только из
  elevated PowerShell: `schtasks /Delete /TN "TelegramClaudeBridge" /F`.
- `TELEGRAM_BOT_TOKEN` и `TELEGRAM_BUILD_CHAT_ID` / `TELEGRAM_BUILD_THREAD_ID` нужны отправке
  билдов — не трогать. `TELEGRAM_ALLOWED_USER_ID` больше не используется ничем.
- Детали доставки билдов (лимит 50 МБ, чтение токена из реестра, build_notes.txt) — см.
  [[feedback-build-to-telegram]].
