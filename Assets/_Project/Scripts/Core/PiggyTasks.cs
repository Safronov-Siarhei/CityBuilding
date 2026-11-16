using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// The piggy bank's tasks (the user, 2026-11-16: 40 approved, the list in the sheet tab «Копилка: задания» and
    /// Design/store/Piggy_Tasks.md). Every piggy_tasks_reset_hours (8: at 00:00, 08:00 and 16:00 local time) a new set of
    /// piggy_tasks_per_slot (3) is drawn -- one easy, one medium, one any -- from the tasks whose mode the player has
    /// opened (a dungeon's after the first dungeon, the archipelago's after the first archipelago map, the campaign
    /// record once a record exists); unfinished ones go. A finished task puts its reward in the piggy by itself (no claim
    /// button); a full piggy loses it.
    ///
    /// The game reports what happens through Report (a count: orcs killed, wood gathered...) and ReportMax (a best value:
    /// the wave reached, the biggest squad...), with a `kind` word the tab's rows name. Sandbox tasks count only in the
    /// sandbox (not in a run, the campaign or the tutorial); the others are reported only where they can happen.
    /// </summary>
    public static class PiggyTasks
    {
        // the kinds the game reports (the tab's `kind` column)
        public const string InvasionRun = "invasion_run", InvasionWave = "invasion_wave", KingKill = "king_kill";
        public const string OrcKill = "orc_kill", OrcBuilding = "orc_building", OrcCampKind = "orc_camp", Cage = "cage", Raid = "raid";
        public const string Build = "build", Upgrade = "upgrade", BuildingLevel = "building_level", Road = "road", Decor = "decor";
        public const string Wood = "wood", Stone = "stone", Food = "food", Bar = "bar", Coins = "coins";
        public const string Hire = "hire", Machine = "machine", Squad = "squad", HeroLevel = "hero_level";
        public const string Repair = "repair", Citizen = "citizen", Research = "research", SandboxMinute = "sandbox_minute";
        public const string Ship = "ship", Island = "island";
        public const string CampaignWin = "campaign_win", CampaignRecord = "campaign_record";
        public const string DungeonCrystal = "dungeon_crystal", DungeonChest = "dungeon_chest", DungeonPotion = "dungeon_potion";
        public const string DungeonMonster = "dungeon_monster", DungeonBoss = "dungeon_boss", DungeonDeeper = "dungeon_deeper";

        public const string ModeAny = "any", ModeSandbox = "sandbox", ModeInvasion = "invasion", ModeCampaign = "campaign";
        public const string ModeDungeon = "dungeon", ModeArchipelago = "archipelago";

        private const string StateKey = "piggy.tasks";
        private const string SeenPrefix = "piggy.seen.";
        private const string IslandsPrefix = "piggy.isl.";
        private const string LostKey = "piggy.task_lost";
        /// <summary>Mission 1's record (Campaign/MissionOne BestKey): the campaign-record task drops only once one exists.</summary>
        private const string CampaignBestKey = "m1_best";

        [Serializable]
        private class State
        {
            public long slot = long.MinValue;
            public List<string> ids = new List<string>();
            public List<int> progress = new List<int>();
            public List<bool> done = new List<bool>();
        }

        private static State _state;
        private static bool _dirty;

        /// <summary>A task advanced, finished, or a new set was drawn.</summary>
        public static event Action Changed;

        /// <summary>A task finished: it, and the crystals that went into the piggy (less than its reward when the piggy filled up).</summary>
        public static event Action<BalanceConfig.PiggyTask, int> Completed;

        private static BalanceConfig Config => BalanceConfig.Sandbox;

        private static float Hours => Config != null ? Mathf.Max(1f, Config.PiggyTasksResetHours) : 8f;

        private static readonly DateTime Origin = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

        /// <summary>The number of the 8-hour slot now (local time, counted from midnight: slots start at 00, 08, 16).</summary>
        public static long CurrentSlot => (long)Math.Floor((DateTime.Now - Origin).TotalHours / Hours);

        /// <summary>Time left until the next set is drawn.</summary>
        public static TimeSpan UntilNext => Origin.AddHours((CurrentSlot + 1) * Hours) - DateTime.Now;

        private static State S
        {
            get
            {
                if (_state == null) Load();
                if (_state.slot != CurrentSlot) Draw(CurrentSlot);
                return _state;
            }
        }

        public static int Count => S.ids.Count;

        public static BalanceConfig.PiggyTask Task(int i) => i >= 0 && i < Count && Config != null ? Config.FindPiggyTask(S.ids[i]) : null;

        public static int Progress(int i) => i >= 0 && i < Count ? S.progress[i] : 0;

        public static bool Done(int i) => i >= 0 && i < Count && S.done[i];

        /// <summary>The task's name as the window shows it.</summary>
        public static string Name(BalanceConfig.PiggyTask task) => task == null ? "" : Localization.Get("#piggy_task_" + task.id);

        /// <summary>A task finished while the piggy was full (its crystals lost): the main menu opens the piggy once.</summary>
        public static bool LostWhileFull
        {
            get => PlayerPrefs.GetInt(LostKey, 0) == 1;
            set => PlayerPrefs.SetInt(LostKey, value ? 1 : 0);
        }

        // ------------------------------------------------------------ reporting

        /// <summary>`amount` more of `kind` happened (an orc killed, 12 wood gathered...).</summary>
        public static void Report(string kind, int amount = 1)
        {
            if (amount <= 0 || Config == null) return;
            var s = S;
            for (var i = 0; i < s.ids.Count; i++)
            {
                if (s.done[i]) continue;
                var task = Config.FindPiggyTask(s.ids[i]);
                if (task == null || task.kind != kind || !CountsHere(task.mode)) continue;
                s.progress[i] = Mathf.Min(task.target, s.progress[i] + amount);
                _dirty = true;
                Check(i, task);
                Changed?.Invoke();
            }
        }

        /// <summary>A best value of `kind` reached (the wave of a run, the biggest squad, a building's level).</summary>
        public static void ReportMax(string kind, int value)
        {
            if (value <= 0 || Config == null) return;
            var s = S;
            for (var i = 0; i < s.ids.Count; i++)
            {
                if (s.done[i]) continue;
                var task = Config.FindPiggyTask(s.ids[i]);
                if (task == null || task.kind != kind || !CountsHere(task.mode) || value <= s.progress[i]) continue;
                s.progress[i] = Mathf.Min(task.target, value);
                _dirty = true;
                Check(i, task);
                Changed?.Invoke();
            }
        }

        /// <summary>The army stepped ashore on island `island` of the map now: a NEW island (never landed on here) counts.</summary>
        public static void Landed(string mapId, int island) => Visit(mapId, island, true);

        /// <summary>Remembers an island as visited (a shipyard's own island: sailing home is no new island); `report` = it counts.</summary>
        public static void Visit(string mapId, int island, bool report)
        {
            if (island < 0) return;
            var key = IslandsPrefix + (mapId ?? "");
            var seen = PlayerPrefs.GetString(key, "");
            var mark = "," + island + ",";
            if (("," + seen + ",").Contains(mark)) return;
            PlayerPrefs.SetString(key, string.IsNullOrEmpty(seen) ? island.ToString() : seen + "," + island);
            if (report) Report(Island);
        }

        /// <summary>A building of the player's stands finished (placed with no timer, or its scaffolding came down):
        /// a decoration counts as decor, roads and walls not at all, anything else as a building.</summary>
        public static void Built(Buildings.BuildingData data)
        {
            if (data == null || data.isRoad) return;
            if (Citizens.DecorBonus.IsDecor(data)) { Report(Decor); return; }
            if (Buildings.WallWalk.Concerns(data.buildingName)) return;
            Report(Build);
        }

        /// <summary>The player has been in this mode (dungeon, archipelago): its tasks may drop from the next set.</summary>
        public static void MarkSeen(string mode)
        {
            if (string.IsNullOrEmpty(mode) || PlayerPrefs.GetInt(SeenPrefix + mode, 0) == 1) return;
            PlayerPrefs.SetInt(SeenPrefix + mode, 1);
            PlayerPrefs.Save();
        }

        private static void Check(int i, BalanceConfig.PiggyTask task)
        {
            if (_state.progress[i] < task.target) return;
            _state.done[i] = true;
            var credited = PiggyBank.Add(task.reward);
            if (credited < task.reward) LostWhileFull = true;
            Flush();
            if (credited > 0) EventLogManager.Instance?.Log(Localization.Format("#piggy_task_done", Name(task), credited));
            Completed?.Invoke(task, credited);
        }

        /// <summary>Sandbox (and archipelago) tasks count only in the sandbox; the others are reported only where they happen.</summary>
        private static bool CountsHere(string mode)
        {
            if (mode != ModeSandbox && mode != ModeArchipelago) return true;
            return InSandbox;
        }

        /// <summary>A sandbox game is on: not a run, the campaign or the tutorial.</summary>
        public static bool InSandbox =>
            !Invasion.InvasionMode.IsActive && !Saving.GameSessionIntent.NewGameIsInvasion
            && !Campaign.CityDefenceMode.IsActive && !Tutorial.TutorialMode.IsActive;

        // ------------------------------------------------------------ the draw

        private static bool Opened(BalanceConfig.PiggyTask task)
        {
            if (task.kind == CampaignRecord) return PlayerPrefs.GetFloat(CampaignBestKey, 0f) > 0f;
            switch (task.mode)
            {
                case ModeDungeon:
                case ModeArchipelago:
                    return PlayerPrefs.GetInt(SeenPrefix + task.mode, 0) == 1;
                default:
                    return true;
            }
        }

        /// <summary>A new set for `slot`: one easy, one medium, then any, different tasks of different kinds where possible.</summary>
        private static void Draw(long slot)
        {
            _state = new State { slot = slot };
            var config = Config;
            if (config == null) return;
            var pool = new List<BalanceConfig.PiggyTask>();
            foreach (var t in config.PiggyTasks) if (t != null && Opened(t)) pool.Add(t);
            var rnd = new System.Random(unchecked((int)(slot * 7919L) ^ 0x5BD1E995));
            var want = Mathf.Max(1, config.PiggyTasksPerSlot);
            var order = new[] { "easy", "medium" };
            for (var n = 0; n < want && pool.Count > 0; n++)
            {
                var difficulty = n < order.Length ? order[n] : null;
                var pick = Pick(pool, rnd, difficulty, true) ?? Pick(pool, rnd, difficulty, false) ?? Pick(pool, rnd, null, true) ?? Pick(pool, rnd, null, false);
                if (pick == null) break;
                pool.Remove(pick);
                _state.ids.Add(pick.id);
                _state.progress.Add(0);
                _state.done.Add(false);
            }
            _dirty = true;
            Flush();
            Changed?.Invoke();
        }

        private static BalanceConfig.PiggyTask Pick(List<BalanceConfig.PiggyTask> pool, System.Random rnd, string difficulty, bool newKind)
        {
            var fit = new List<BalanceConfig.PiggyTask>();
            foreach (var t in pool)
            {
                if (difficulty != null && t.difficulty != difficulty) continue;
                if (newKind && KindTaken(t.kind)) continue;
                fit.Add(t);
            }
            return fit.Count == 0 ? null : fit[rnd.Next(fit.Count)];
        }

        private static bool KindTaken(string kind)
        {
            foreach (var id in _state.ids)
            {
                var t = Config.FindPiggyTask(id);
                if (t != null && t.kind == kind) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ keeping

        private static void Load()
        {
            var json = PlayerPrefs.GetString(StateKey, "");
            _state = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { _state = JsonUtility.FromJson<State>(json); } catch (ArgumentException) { _state = null; }
            }
            if (_state == null || _state.ids == null || _state.progress == null || _state.done
                == null || _state.progress.Count != _state.ids.Count || _state.done.Count != _state.ids.Count)
                _state = new State();
        }

        /// <summary>Writes the progress out when it changed (PiggyRunner calls it every few seconds and on pause).</summary>
        public static void Flush()
        {
            if (!_dirty || _state == null) return;
            _dirty = false;
            PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(_state));
        }

        /// <summary>Cheats: a new set now (the same slot redrawn) / the task finished.</summary>
        public static void CheatRedraw()
        {
            if (_state == null) Load();
            Draw(CurrentSlot + UnityEngine.Random.Range(1000, 100000));
            _state.slot = CurrentSlot;
            _dirty = true;
            Flush();
        }

        public static void CheatFinish(int i)
        {
            var task = Task(i);
            if (task == null || S.done[i]) return;
            S.progress[i] = task.target;
            _dirty = true;
            Check(i, task);
            Changed?.Invoke();
        }

        /// <summary>Tests: forget everything (the next read draws again).</summary>
        public static void ResetForTests()
        {
            _state = null;
            _dirty = false;
            PlayerPrefs.DeleteKey(StateKey);
        }
    }
}
