# WARGAME — ЗАДАЧА 04: ПРОВЕРКА В UNITY И ИСПРАВЛЕНИЕ ОШИБОК (ЗАВЕРШЕНИЕ PHASE 1)

Запускать, когда проект открыт в Unity на моём ПК и (желательно) подключён Unity MCP.

## ТВОЯ РОЛЬ
Ты — QA Engineer и Senior Unity Developer игры WARGAME.

## КОНТЕКСТ — ОБЯЗАТЕЛЬНО
1. Подключи репозиторий `gamerfashpro-sudo/wargame` (доступ push), клонируй: `git clone --depth 1 https://github.com/gamerfashpro-sudo/wargame /home/claude/wargame`.
2. Прочитай `Documentation/DEV_CONTEXT.md`, `README.md` и весь код `Assets/_Project/Scripts/`.
3. Проверь, есть ли инструменты Unity MCP (поиск инструментов по слову «Unity», например `Unity_ReadConsole`, `Unity_ManageScene`). Если есть — работай через них. Если нет — попроси меня скопировать сюда текст ошибок из Console Unity (все красные строки).

## ПРАВИЛА
- Коротко, по делу. Протокол: факты вместо предположений; не чини то, что не доказано сломанным; воспроизведи ошибку; сформулируй гипотезу до правки; изолируй причину; подтверждай стектрейсом/логом.
- Исправления делай в репозитории (облако), затем commit + push. Мне — что сделать в Unity, чтобы подтянуть изменения (GitHub Desktop → Fetch/Pull).
- Не утверждай, что работает, пока я или Unity MCP это не подтвердили.
- Git: `git pull --rebase origin main` перед push, без force push/reset --hard, коммиты на русском, в конце `Co-Authored-By: Claude <noreply@anthropic.com>`.

## ЗАДАЧА
1. Ошибки компиляции: прочитать Console, исправить все до нуля. Повторять до чистой компиляции.
2. Setup Project: убедиться, что URP-ассеты, материалы, сцены, Build Settings, ModelRegistry созданы, TMP Essentials импортированы.
3. Проверка FBX: MBT_Main и остальные модели — поворот корня (0,0,0), масштаб 1, перед смотрит в +Z, материалы WG_* назначены. Если нет — исправить импорт/экспорт.
4. Play Mode в MainMenu: меню, фон, все кнопки, настройки (каждая опция применяется и сохраняется после перезапуска), язык RU/EN, новая игра → Gameplay: мир, камера, выбор объектов, HUD, мини-карта, пауза, выход в меню. Ошибки и предупреждения в Console — исправить.
5. Производительность: окно Stats/Profiler — FPS, draw calls, batches; при проблемах — оптимизировать.
6. Тестовая сборка Windows (File > Build Profiles > Windows > Build) в папку `Builds/WARGAME/` (не коммитить), запуск EXE.

## В КОНЦЕ
Обнови статус Phase 1 в DEV_CONTEXT.md и README.md (✅ только то, что подтверждено в Unity), commit + push, короткий отчёт и следующий шаг (Phase 2).
