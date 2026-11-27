using System.Collections.Generic;

namespace WG.Core
{
    public enum Language { Russian, English }

    /// <summary>Локализация RU/EN. Ключ -> строка. L.T("menu.play").</summary>
    public static class L
    {
        public static Language Current { get; private set; } = Language.Russian;

        public static void Set(Language lang)
        {
            if (Current == lang) return;
            Current = lang;
            EventBus.Publish(new LanguageChanged());
        }

        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var pair)) return Current == Language.Russian ? pair.ru : pair.en;
            return key;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        static readonly Dictionary<string, (string ru, string en)> Table = new()
        {
            // --- Главное меню ---
            ["menu.tagline"] = ("СТРОЙ. РАЗВИВАЙСЯ. ПОБЕЖДАЙ.", "BUILD. GROW. PREVAIL."),
            ["menu.play"] = ("Играть", "Play"),
            ["menu.settings"] = ("Настройки", "Settings"),
            ["menu.about"] = ("Об игре", "About"),
            ["menu.credits"] = ("Авторы", "Credits"),
            ["menu.exit"] = ("Выход", "Exit"),
            ["menu.back"] = ("Назад", "Back"),
            ["menu.newgame"] = ("Новая игра", "New Game"),
            ["menu.start"] = ("Начать", "Start"),
            ["menu.exit_confirm"] = ("Выйти из игры?", "Quit the game?"),
            ["menu.yes"] = ("Да", "Yes"),
            ["menu.no"] = ("Нет", "No"),
            ["menu.version"] = ("Версия {0} · Phase 1", "Version {0} · Phase 1"),
            ["newgame.title"] = ("НОВАЯ ИГРА", "NEW GAME"),
            ["newgame.country"] = ("Название государства", "Country name"),
            ["newgame.color"] = ("Цвет государства", "Country color"),
            ["newgame.map"] = ("Карта", "Map"),
            ["newgame.seed"] = ("Вариант мира", "World variant"),
            ["newgame.default_country"] = ("Северная Республика", "Northern Republic"),
            ["about.title"] = ("ОБ ИГРЕ", "ABOUT"),
            ["about.text"] = (
                "WARGAME — военная стратегия-песочница. Развивайте островное государство, стройте города и промышленность, налаживайте логистику, создавайте армию и защищайте свои земли.\n\nТекущая версия — Phase 1: мир, камера, меню, настройки и базовый интерфейс. Строительство, экономика, армия, ИИ и песочница появятся в следующих фазах.",
                "WARGAME is a military sandbox strategy. Grow an island nation, build cities and industry, run logistics, raise an army and defend your land.\n\nThis build is Phase 1: world, camera, menus, settings and the base interface. Construction, economy, military, AI and sandbox arrive in later phases."),
            ["credits.title"] = ("АВТОРЫ", "CREDITS"),
            ["credits.text"] = (
                "Разработка: команда WARGAME\nКод, 3D-модели и текстуры: созданы для проекта\n\nШрифты (SIL Open Font License 1.1):\nRusso One — Jovanny Lemonad\nPT Sans — ParaType\n\nДвижок: Unity 6 (URP)",
                "Development: WARGAME team\nCode, 3D models and textures: made for this project\n\nFonts (SIL Open Font License 1.1):\nRusso One — Jovanny Lemonad\nPT Sans — ParaType\n\nEngine: Unity 6 (URP)"),

            // --- Настройки ---
            ["set.title"] = ("НАСТРОЙКИ", "SETTINGS"),
            ["set.tab.graphics"] = ("Графика", "Graphics"),
            ["set.tab.audio"] = ("Аудио", "Audio"),
            ["set.tab.controls"] = ("Управление", "Controls"),
            ["set.tab.gameplay"] = ("Геймплей", "Gameplay"),
            ["set.tab.interface"] = ("Интерфейс", "Interface"),
            ["set.apply"] = ("Применить", "Apply"),
            ["set.defaults"] = ("По умолчанию", "Defaults"),
            ["set.cancel"] = ("Отмена", "Cancel"),
            ["set.resolution"] = ("Разрешение", "Resolution"),
            ["set.windowmode"] = ("Режим окна", "Window mode"),
            ["set.wm.fullscreen"] = ("Полный экран", "Fullscreen"),
            ["set.wm.borderless"] = ("Без рамки", "Borderless"),
            ["set.wm.windowed"] = ("В окне", "Windowed"),
            ["set.vsync"] = ("Вертикальная синхронизация", "VSync"),
            ["set.fps"] = ("Ограничение FPS", "FPS limit"),
            ["set.fps.unlimited"] = ("Без ограничения", "Unlimited"),
            ["set.quality"] = ("Качество графики", "Graphics quality"),
            ["set.textures"] = ("Качество текстур", "Texture quality"),
            ["set.shadows"] = ("Тени", "Shadows"),
            ["set.aa"] = ("Сглаживание", "Anti-aliasing"),
            ["set.renderscale"] = ("Масштаб рендера", "Render scale"),
            ["set.viewdist"] = ("Дальность прорисовки", "View distance"),
            ["set.effects"] = ("Качество эффектов", "Effects quality"),
            ["set.postfx"] = ("Постобработка", "Post-processing"),
            ["set.lvl.off"] = ("Выкл", "Off"),
            ["set.lvl.low"] = ("Низкое", "Low"),
            ["set.lvl.medium"] = ("Среднее", "Medium"),
            ["set.lvl.high"] = ("Высокое", "High"),
            ["set.lvl.ultra"] = ("Ультра", "Ultra"),
            ["set.master"] = ("Общая громкость", "Master volume"),
            ["set.music"] = ("Музыка", "Music"),
            ["set.sfx"] = ("Эффекты", "Sound effects"),
            ["set.ui"] = ("Интерфейс", "Interface"),
            ["set.ambient"] = ("Окружение", "Ambient"),
            ["set.camspeed"] = ("Скорость камеры", "Camera speed"),
            ["set.camrot"] = ("Чувствительность поворота", "Rotation sensitivity"),
            ["set.zoomspeed"] = ("Скорость приближения", "Zoom speed"),
            ["set.edgepan"] = ("Движение у краёв экрана", "Edge panning"),
            ["set.invertzoom"] = ("Инвертировать колесо", "Invert scroll wheel"),
            ["set.invertrot"] = ("Инвертировать поворот мышью", "Invert mouse rotation"),
            ["set.smooth"] = ("Плавность камеры", "Camera smoothing"),
            ["set.keys"] = ("Назначение клавиш", "Key bindings"),
            ["set.press_key"] = ("Нажмите клавишу… (Esc — отмена)", "Press a key… (Esc to cancel)"),
            ["set.gamespeed"] = ("Скорость игры по умолчанию", "Default game speed"),
            ["set.autopause"] = ("Пауза при сворачивании", "Pause when unfocused"),
            ["set.hints"] = ("Подсказки", "Hints"),
            ["set.language"] = ("Язык", "Language"),
            ["set.uiscale"] = ("Масштаб интерфейса", "UI scale"),
            ["set.hud.minimap"] = ("Мини-карта", "Minimap"),
            ["set.hud.resources"] = ("Панель ресурсов", "Resource bar"),
            ["set.hud.clock"] = ("Время и скорость", "Clock and speed"),
            ["set.hud.notifications"] = ("Уведомления", "Notifications"),
            ["set.hud.fps"] = ("Счётчик FPS", "FPS counter"),
            ["set.on"] = ("Вкл", "On"),
            ["set.off"] = ("Выкл", "Off"),

            // --- Клавиши ---
            ["key.CamForward"] = ("Камера: вперёд", "Camera: forward"),
            ["key.CamBack"] = ("Камера: назад", "Camera: back"),
            ["key.CamLeft"] = ("Камера: влево", "Camera: left"),
            ["key.CamRight"] = ("Камера: вправо", "Camera: right"),
            ["key.CamRotLeft"] = ("Повернуть влево", "Rotate left"),
            ["key.CamRotRight"] = ("Повернуть вправо", "Rotate right"),
            ["key.CamTiltUp"] = ("Наклон вверх", "Tilt up"),
            ["key.CamTiltDown"] = ("Наклон вниз", "Tilt down"),
            ["key.CamFocus"] = ("Камера к выбранному", "Focus selected"),
            ["key.CamFast"] = ("Быстрое перемещение", "Fast move"),
            ["key.Pause"] = ("Пауза", "Pause"),
            ["key.Speed1"] = ("Скорость ×1", "Speed ×1"),
            ["key.Speed2"] = ("Скорость ×2", "Speed ×2"),
            ["key.Speed3"] = ("Скорость ×3", "Speed ×3"),

            // --- HUD ---
            ["hud.money"] = ("Казна", "Treasury"),
            ["hud.population"] = ("Население", "Population"),
            ["hud.power"] = ("Энергия", "Power"),
            ["hud.food"] = ("Продовольствие", "Food"),
            ["hud.steel"] = ("Сталь", "Steel"),
            ["hud.fuel"] = ("Топливо", "Fuel"),
            ["hud.day"] = ("День {0}", "Day {0}"),
            ["hud.paused"] = ("ПАУЗА", "PAUSED"),
            ["hud.speed"] = ("Скорость ×{0}", "Speed ×{0}"),
            ["hud.welcome"] = ("{0}: правительство приступило к работе.", "{0}: the government has taken office."),
            ["hud.controls_hint"] = ("WASD — камера · Q/E — поворот · колесо — масштаб · ЛКМ — выбор · F — к объекту · Пробел — пауза",
                                     "WASD — camera · Q/E — rotate · wheel — zoom · LMB — select · F — focus · Space — pause"),
            ["hud.owner"] = ("Владелец", "Owner"),
            ["hud.type"] = ("Тип", "Type"),
            ["hud.neutral"] = ("Нейтральная территория", "Neutral"),
            ["hud.region"] = ("Регион", "Region"),
            ["pause.title"] = ("ПАУЗА", "PAUSED"),
            ["pause.resume"] = ("Продолжить", "Resume"),
            ["pause.mainmenu"] = ("Главное меню", "Main menu"),
            ["pause.mainmenu_confirm"] = ("Выйти в главное меню? Несохранённый прогресс будет потерян (сохранения появятся в Phase 2).",
                                          "Return to main menu? Unsaved progress will be lost (saving arrives in Phase 2)."),

            // --- Объекты мира ---
            ["obj.House_Small"] = ("Жилой дом", "House"),
            ["obj.Apartment_Block"] = ("Многоквартирный дом", "Apartment block"),
            ["obj.Warehouse"] = ("Склад", "Warehouse"),
            ["obj.Factory"] = ("Завод", "Factory"),
            ["obj.Power_Plant"] = ("Электростанция", "Power plant"),
            ["obj.Barracks"] = ("Казармы", "Barracks"),
            ["obj.Hangar"] = ("Ангар", "Hangar"),
            ["obj.Control_Tower"] = ("Диспетчерская вышка", "Control tower"),
            ["obj.MBT_Main"] = ("Основной боевой танк", "Main battle tank"),
            ["obj.Truck_Military"] = ("Военный грузовик", "Military truck"),
            ["obj.Fighter_Jet"] = ("Истребитель", "Fighter jet"),
            ["obj.Frigate"] = ("Фрегат", "Frigate"),
            ["type.residential"] = ("Жильё", "Residential"),
            ["type.industry"] = ("Промышленность", "Industry"),
            ["type.military"] = ("Военный объект", "Military"),
            ["type.infrastructure"] = ("Инфраструктура", "Infrastructure"),
            ["type.unit"] = ("Техника", "Vehicle"),
            ["map.island"] = ("Остров Сторм", "Storm Island"),
            ["region.capital"] = ("Столица", "Capital"),
            ["region.port"] = ("Портовый город", "Port town"),
            ["region.industry"] = ("Промзона", "Industrial zone"),
            ["region.base"] = ("Военная база", "Military base"),
            ["region.airfield"] = ("Аэродром", "Airfield"),
            ["region.village"] = ("Посёлок", "Village"),
            ["region.wilds"] = ("Дикие земли", "Wilderness"),
        };
    }
}
