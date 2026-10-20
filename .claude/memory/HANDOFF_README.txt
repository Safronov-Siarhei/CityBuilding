Перенос контекста CityBuilding на другое устройство (2026-10-20)

1. memory/  ->  скопировать содержимое в
   C:\Users\<имя>\.claude\projects\<папка проекта>\memory\
   Имя папки проекта строится из пути к проекту: для D:\Fork\CityBuilding это D--Fork-CityBuilding.
   Проще всего склонировать проект тоже в D:\Fork\CityBuilding.

2. local-tools/  ->  скопировать в те же места внутри проекта (Tools/research, Tools/balance).
   Токена Telegram в файлах нет: скрипт берёт его из переменной окружения TELEGRAM_BOT_TOKEN.

3. Отдельно, вручную:
   - Blender-файлы из D:/blender projects/Buildings3D/ (Buildings-Size1*.blend, Map-4-TwoRivers.blend и др.);
   - Blender 5.2.2 и Unity 6000.5.4f1 с Android Build Support.
