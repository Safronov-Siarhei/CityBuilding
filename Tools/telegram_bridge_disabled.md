# Мост Telegram → Claude Code отключён

Раньше здесь лежал `telegram_bridge.py`: он опрашивал Telegram и на каждое
сообщение от разрешённого пользователя запускал `claude -p "<текст>"
--permission-mode acceptEdits` в этом проекте, то есть позволял выполнять
любые задачи на этом ПК с телефона.

Этот режим выключен намеренно. У бота остаётся ровно одна функция —
`Tools/research/send_build_to_telegram.py`: отправка собранного APK в топик
«GameBuilds» группы «Depigo Games». Она не читает входящие сообщения вообще,
только отправляет файл, поэтому написать боту что-то и получить выполнение
команды больше нельзя.

Что ещё нужно сделать вручную (требует прав администратора, из elevated
PowerShell), иначе задача продолжит пытаться запускать удалённый скрипт:

    schtasks /Change /TN "TelegramClaudeBridge" /DISABLE

или совсем удалить её:

    schtasks /Delete /TN "TelegramClaudeBridge" /F

Переменные окружения `TELEGRAM_BOT_TOKEN` и `TELEGRAM_BUILD_CHAT_ID` /
`TELEGRAM_BUILD_THREAD_ID` нужны отправке билдов — их удалять нельзя.
`TELEGRAM_ALLOWED_USER_ID` больше не используется ничем.
