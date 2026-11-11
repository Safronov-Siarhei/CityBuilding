Перенос проекта CityBuilding на другое устройство или другой ИИ (обновлено 2026-11-11)

Вся информация проекта лежит в репозитории:
- правила работы с пользователем: Design/WORKING_RULES.md (коротко: CLAUDE.md; для других ИИ: AGENTS.md);
- карточки фич: Design/features/README.md; отложенные идеи: Design/features/ideas.md;
- память ИИ (текущее состояние, списки с прохождений, решения, ссылки): .claude/memory/MEMORY.md.

На новом устройстве с Claude Code:
1. Склонировать проект (лучше в тот же путь, например G:\Fork\CityBuilding).
2. python Tools/memory_sync.py --to-local
   (кладёт память в C:\Users\<имя>\.claude\projects\<папка проекта>\memory\; имя папки = путь проекта,
   где ':' и '\' заменены на '-', например G--Fork-CityBuilding).
3. Поставить Unity 6000.5.4f1 (с Android Build Support), Blender 5.2.2, Python 3.
4. Дать доступ к Google-таблице баланса (id в Design/WORKING_RULES.md) и к Google Drive с .blend-файлами
   (Design/features/art-pipeline.md, раздел Files and tools).
5. Токен Telegram для отправки сборок: переменная окружения TELEGRAM_BOT_TOKEN (в файлах его нет и быть не должно).

После изменений памяти: python Tools/memory_sync.py, затем закоммитить .claude/memory/.
