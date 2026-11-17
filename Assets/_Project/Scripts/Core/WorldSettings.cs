using System;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// «Свобода песочницы» (card Design/features/sandbox-freedom.md, the user 2026-11-17): a sandbox world's own
    /// settings, chosen on the sandbox screen before the start and partly changeable in the game; saved with the world.
    /// Changing any of them needs the one-time purchase (Store.Freedom); the defaults are today's balance. Every factor
    /// behind a level is the sheet's (BalanceConfig.Freedom*).
    ///
    /// Every system reads <see cref="Current"/>; outside a sandbox world it is the defaults (GameSaveController sets it
    /// for every game the scene starts or loads).
    /// </summary>
    [Serializable]
    public class WorldSettings
    {
        public const int RaidLevels = 5, QuietLevels = 4, GoodsLevels = 3, SpeedLevels = 3;
        public const int DefaultRaids = 2, DefaultQuiet = 1, DefaultGoods = 1, DefaultSpeed = 0;

        /// <summary>0 off, 1 light, 2 normal, 3 hard, 4 nightmare.</summary>
        public int raids = DefaultRaids;
        /// <summary>The neighbour kingdom never goes to war (and cannot be warred on).</summary>
        public bool neighbourPeace;
        /// <summary>The quiet start's length, an index into the sheet's minutes (0 / 7 / 15 / 30).</summary>
        public int quietStart = DefaultQuiet;
        public bool fires = true;
        public bool storms = true;
        public bool plague = true;
        /// <summary>0 few, 1 normal, 2 many: the start chain's reward x the sheet's factor.</summary>
        public int startGoods = DefaultGoods;
        /// <summary>0 x1, 1 x2, 2 x4 (the sheet's factors).</summary>
        public int buildSpeed = DefaultSpeed;
        public int researchSpeed = DefaultSpeed;
        public bool fog = true;

        /// <summary>The world the game is in now (the defaults outside a sandbox world).</summary>
        public static WorldSettings Current { get; set; } = new WorldSettings();

        /// <summary>Raised when the settings of the running world change (the in-game scroll).</summary>
        public static event Action Changed;

        /// <summary>The settings the sandbox screen holds for the next new world (kept while the game runs).</summary>
        public static WorldSettings Draft { get; set; } = new WorldSettings();

        /// <summary>The settings that can only be chosen before the world is made: they are spent or laid out at the start.</summary>
        public static bool SetAtStartOnly(Setting s) => s == Setting.QuietStart || s == Setting.StartGoods || s == Setting.Fog;

        public enum Setting { Raids, Neighbour, QuietStart, Fires, Storms, Plague, StartGoods, BuildSpeed, ResearchSpeed, Fog }

        public static readonly Setting[] All =
        {
            Setting.Raids, Setting.Neighbour, Setting.QuietStart, Setting.Fires, Setting.Storms, Setting.Plague,
            Setting.StartGoods, Setting.BuildSpeed, Setting.ResearchSpeed, Setting.Fog,
        };

        public static int Options(Setting s)
        {
            switch (s)
            {
                case Setting.Raids: return RaidLevels;
                case Setting.QuietStart: return QuietLevels;
                case Setting.StartGoods: return GoodsLevels;
                case Setting.BuildSpeed:
                case Setting.ResearchSpeed: return SpeedLevels;
                default: return 2;
            }
        }

        /// <summary>The option picked, 0-based (a yes/no setting: 0 = the first option shown).</summary>
        public int Get(Setting s)
        {
            switch (s)
            {
                case Setting.Raids: return raids;
                case Setting.Neighbour: return neighbourPeace ? 0 : 1;      // «Всегда мир» / «Как обычно»
                case Setting.QuietStart: return quietStart;
                case Setting.Fires: return fires ? 1 : 0;                   // «Выкл» / «Вкл»
                case Setting.Storms: return storms ? 1 : 0;
                case Setting.Plague: return plague ? 1 : 0;
                case Setting.StartGoods: return startGoods;
                case Setting.BuildSpeed: return buildSpeed;
                case Setting.ResearchSpeed: return researchSpeed;
                default: return fog ? 1 : 0;                                // «Нет» / «Есть»
            }
        }

        public void Set(Setting s, int option)
        {
            option = Mathf.Clamp(option, 0, Options(s) - 1);
            switch (s)
            {
                case Setting.Raids: raids = option; break;
                case Setting.Neighbour: neighbourPeace = option == 0; break;
                case Setting.QuietStart: quietStart = option; break;
                case Setting.Fires: fires = option == 1; break;
                case Setting.Storms: storms = option == 1; break;
                case Setting.Plague: plague = option == 1; break;
                case Setting.StartGoods: startGoods = option; break;
                case Setting.BuildSpeed: buildSpeed = option; break;
                case Setting.ResearchSpeed: researchSpeed = option; break;
                default: fog = option == 1; break;
            }
        }

        public static int DefaultOf(Setting s) => new WorldSettings().Get(s);

        /// <summary>This setting's choice is easier than the default (the piggy's tasks stop counting).</summary>
        public bool EasierAt(Setting s)
        {
            switch (s)
            {
                case Setting.Raids: return raids < DefaultRaids;
                case Setting.Neighbour: return neighbourPeace;
                case Setting.QuietStart: return quietStart > DefaultQuiet;
                case Setting.Fires: return !fires;
                case Setting.Storms: return !storms;
                case Setting.Plague: return !plague;
                case Setting.StartGoods: return startGoods > DefaultGoods;
                case Setting.BuildSpeed: return buildSpeed > DefaultSpeed;
                case Setting.ResearchSpeed: return researchSpeed > DefaultSpeed;
                default: return !fog;
            }
        }

        /// <summary>Any setting easier than the default (the user, 2026-11-17: then the piggy's tasks do not count here).</summary>
        public bool Easier
        {
            get
            {
                foreach (var s in All) if (EasierAt(s)) return true;
                return false;
            }
        }

        public bool IsDefault
        {
            get
            {
                foreach (var s in All) if (Get(s) != DefaultOf(s)) return false;
                return true;
            }
        }

        public WorldSettings Clone() => (WorldSettings)MemberwiseClone();

        public void CopyFrom(WorldSettings other)
        {
            if (other == null) other = new WorldSettings();
            foreach (var s in All) Set(s, other.Get(s));
        }

        // ---- what the systems read (the factors are the sheet's)

        public float RaidSizeFactor => Config == null ? 1f : Config.FreedomRaidSize(raids);
        public float RaidIntervalFactor => Config == null ? 1f : Config.FreedomRaidInterval(raids);
        public bool RaidsOff => raids <= 0;
        public float QuietSeconds => Config == null ? 420f : Config.FreedomQuietMinutes(quietStart) * 60f;
        public float GoodsFactor => Config == null ? 1f : Config.FreedomGoods(startGoods);
        public float BuildFactor => Config == null ? 1f : Config.FreedomSpeed(buildSpeed);
        public float ResearchFactor => Config == null ? 1f : Config.FreedomSpeed(researchSpeed);

        private static BalanceConfig Config => BalanceConfig.Instance;

        /// <summary>The running world's settings changed in the game: tells the systems that act at once.</summary>
        public static void RaiseChanged() => Changed?.Invoke();
    }
}
