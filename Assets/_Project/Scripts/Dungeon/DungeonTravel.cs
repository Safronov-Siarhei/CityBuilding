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
        /// <summary>The one dungeon so far (Tools/blender/dungeon1.py).</summary>
        public const string FirstDungeonId = "Dungeon1";

        /// <summary>A sandbox town may send its heroes down; a run and a dungeon may not.</summary>
        public static bool CanEnter => !Invasion.InvasionMode.IsActive && !DungeonMode.IsActive && !LoadingScreen.Busy;

        public static void Enter(string dungeonId, int level = 1)
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
                    // a hero who fell below comes up hurt, not dead (the Town Hall raises him -- later)
                    entry.currentHealth = hero.IsFallen || hero.Unit == null ? 1 : hero.Unit.CurrentHealth;
                }
            }
            // what the heroes earned below: the coins into the shared treasury, the slain bosses into the record
            var monsters = DungeonMonsters.Instance;
            if (monsters != null)
            {
                if (monsters.CoinsEarned > 0)
                {
                    var entry = town.resources.Find(r => r.type == HeroRoster.Money);
                    if (entry == null) town.resources.Add(entry = new ResourceEntry { type = HeroRoster.Money });
                    entry.amount += monsters.CoinsEarned;
                }
                foreach (var id in monsters.BossesSlainNow)
                    if (!town.dungeonBossesSlain.Contains(id)) town.dungeonBossesSlain.Add(id);
            }
            SaveSystem.Save(SaveSystem.HiddenTownSlot, town);
            GameSessionIntent.SaveNameToLoad = SaveSystem.HiddenTownSlot;
            GameSessionIntent.SaveNameAfterLoad = mode.ReturnName ?? string.Empty;
            LoadingScreen.IntoGame(SceneManager.GetActiveScene().name);
        }
    }
}
