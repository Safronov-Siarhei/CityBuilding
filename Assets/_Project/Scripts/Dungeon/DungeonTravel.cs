using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// Going down into a dungeon and coming back up (the user, 2026-10-30/31: the dungeon is a
    /// separate scene the heroes walk into, and the town is PAUSED meanwhile).
    ///
    /// Down: the town is written into a hidden slot (SaveSystem.HiddenTownSlot) -- and the
    /// autosave, so «Continue» still finds the town -- the heroes are noted as they are, and the
    /// game scene is loaded afresh on the dungeon's map (DungeonMode takes it from there). Nothing
    /// in the town moves while they are below, because the town is not running at all.
    /// Up: the heroes' levels, experience and health are written back into that slot and the town
    /// is loaded from it, under the name it had.
    /// </summary>
    public static class DungeonTravel
    {
        /// <summary>The approved first dungeon (Tools/blender/dungeon1.py on dungeon1-plan.json).</summary>
        public const string FirstDungeonId = "Dungeon1";

        /// <summary>
        /// The level-1 dungeons, so players do not see one labyrinth on every map (the user,
        /// 2026-11-03): the approved one and its variants -- other hall layouts and mazes
        /// (dungeon_plan.py layouts B-D, dungeon1.py variant=b..d), each its own map Dungeon-1&lt;V&gt;.
        /// </summary>
        public static readonly string[] LevelOneIds = { FirstDungeonId, "Dungeon1B", "Dungeon1C", "Dungeon1D" };

        /// <summary>
        /// The dungeon behind a map's `entrance`-th mouth (0 = level 1, nearest the Town Hall): from
        /// the map's seed, the mouths of one map all different. A version-2 map keeps Dungeon 1, as
        /// it was saved with it (bosses slain and crystals taken are kept per dungeon id).
        /// </summary>
        public static string LevelOneFor(int seed, int version, int entrance)
        {
            if (version <= 2) return FirstDungeonId;
            var first = (int)((uint)(seed * 2654435761u) % (uint)LevelOneIds.Length);
            return LevelOneIds[(first + entrance) % LevelOneIds.Length];
        }

        /// <summary>A sandbox town may send its heroes down; a run and a dungeon may not.</summary>
        public static bool CanEnter => !Invasion.InvasionMode.IsActive && !DungeonMode.IsActive && !LoadingScreen.Busy;

        /// <summary>
        /// Down with the heroes standing by `from` (the user, 2026-11-01: only those near the
        /// mouth go; with no mouth given -- a cheat -- every hero standing does).
        /// </summary>
        public static void Enter(string dungeonId, int level = 1, DungeonEntrance from = null)
        {
            if (!CanEnter) return;
            var save = Object.FindAnyObjectByType<GameSaveController>();
            if (save == null) return;
            save.Autosave();
            save.WriteSlot(SaveSystem.HiddenTownSlot);

            var heroes = new List<HeroEntry>();
            if (HeroRoster.Instance != null)
            {
                foreach (var hero in HeroRoster.Instance.Heroes)
                {
                    if (hero == null || hero.IsFallen || hero.Unit == null) continue;
                    if (from != null && !from.IsNear(hero)) continue;
                    heroes.Add(new HeroEntry
                    {
                        type = hero.Type,
                        level = hero.Level,
                        experience = hero.Experience,
                        currentHealth = hero.Unit.CurrentHealth,
                    });
                }
            }

            GameSessionIntent.DungeonId = dungeonId;
            GameSessionIntent.DungeonLevel = Mathf.Clamp(level, 1, 3);
            GameSessionIntent.DungeonHeroes = heroes;
            GameSessionIntent.DungeonReturnName = save.CurrentSaveName;
            // the camera comes back to this mouth, not to the Town Hall (the user, 2026-11-01)
            GameSessionIntent.DungeonMouth = from != null ? from.transform.position : (Vector3?)null;
            GameSessionIntent.NewGameMapId = dungeonId;
            GameSessionIntent.SaveNameToLoad = null;
            LoadingScreen.IntoGame(SceneManager.GetActiveScene().name);
        }

        /// <summary>Back to the town, the heroes as the dungeon left them.</summary>
        public static void Leave()
        {
            var mode = DungeonMode.Instance;
            if (mode == null || !DungeonMode.IsActive || LoadingScreen.Busy) return;
            var town = SaveSystem.Load(SaveSystem.HiddenTownSlot);
            if (town == null)
            {
                Debug.LogWarning("DungeonTravel.Leave: the town's slot is gone -- back to the menu.");
                LoadingScreen.OutToMenu("MainMenu");
                return;
            }
            if (town.heroes != null && HeroRoster.Instance != null)
            {
                foreach (var entry in town.heroes)
                {
                    var hero = HeroRoster.Instance.HeroOf(entry.type);
                    if (hero == null) continue;
                    entry.level = hero.Level;
                    entry.experience = hero.Experience;
                    // a hero who fell below rises at the Town Hall when his wait is out (the user, 2026-11-01)
                    entry.fallen = hero.IsFallen;
                    entry.respawnRemaining = hero.IsFallen ? Mathf.Max(0.1f, hero.RespawnRemaining) : 0f;
                    entry.currentHealth = hero.IsFallen || hero.Unit == null ? 1 : hero.Unit.CurrentHealth;
                }
            }
            // what the heroes earned below: the coins into the shared treasury, the slain bosses into the record
            // and what they paid out there (gnomes, potions)
            var monsters = DungeonMonsters.Instance;
            var coins = (monsters != null ? monsters.CoinsEarned : 0) - mode.CoinsSpent;
            if (coins != 0)
            {
                var entry = town.resources.Find(r => r.type == HeroRoster.Money);
                if (entry == null) town.resources.Add(entry = new ResourceEntry { type = HeroRoster.Money });
                entry.amount = Mathf.Max(0, entry.amount + coins);
            }
            if (monsters != null)
            {
                // a boss's resources into the town's stores
                foreach (var pair in monsters.ResourcesEarned)
                {
                    var entry = town.resources.Find(r => r.type == pair.Key);
                    if (entry == null) town.resources.Add(entry = new ResourceEntry { type = pair.Key });
                    entry.amount += pair.Value;
                }
                foreach (var id in monsters.BossesSlainNow)
                    if (!town.dungeonBossesSlain.Contains(id)) town.dungeonBossesSlain.Add(id);
            }
            var crystals = DungeonCrystals.Instance;
            if (crystals != null)
            {
                town.dungeonCrystalsTaken ??= new List<string>();
                foreach (var id in crystals.TakenNow)
                    if (!town.dungeonCrystalsTaken.Contains(id)) town.dungeonCrystalsTaken.Add(id);
            }
            // the gnomes stay below and wait at the way in for the next visit (the user, 2026-10-31)
            var party = DungeonHirelings.Instance;
            if (party != null)
            {
                town.dungeonHirelings ??= new List<HirelingEntry>();
                town.dungeonHirelings.RemoveAll(h => h == null || h.dungeonId == mode.DungeonId);
                town.dungeonHirelings.AddRange(party.Survivors());
            }
            // the potions not drunk wait below too, never in the town's inventory
            if (DungeonPotions.Instance != null) town.dungeonPotions = DungeonPotions.Instance.Carried();
            SaveSystem.Save(SaveSystem.HiddenTownSlot, town);
            GameSessionIntent.SaveNameToLoad = SaveSystem.HiddenTownSlot;
            GameSessionIntent.LookAtAfterLoad = GameSessionIntent.DungeonMouth;
            GameSessionIntent.SaveNameAfterLoad = mode.ReturnName ?? string.Empty;
            LoadingScreen.IntoGame(SceneManager.GetActiveScene().name);
        }
    }
}
