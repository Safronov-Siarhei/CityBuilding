#if !CB_RELEASE
using System.Collections.Generic;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Invasion;
using CityBuilder.Maps;
using CityBuilder.Research;
using CityBuilder.Saving;
using CityBuilder.Ships;
using CityBuilder.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Cheats
{
    /// <summary>The cheat window's tabs for fighting and the world: army, heroes, orcs, world, service.</summary>
    public partial class CheatsPanel
    {
        private const string CheatSlot = "cheat";

        private static readonly (SoldierType type, string name)[] Tiers =
        {
            (SoldierType.Militia, "Ополчение"), (SoldierType.Spearman, "Копейщики"),
            (SoldierType.Archer, "Лучники"), (SoldierType.ManAtArms, "Латники"),
        };

        private static readonly (SoldierType type, string name)[] HeroNames =
        {
            (SoldierType.King, "Король"), (SoldierType.Queen, "Королева"), (SoldierType.Captain, "Капитан"),
        };

        private int _squadSize = 5;
        private int _raidSize = 5;
        private int _raidLevel = 1;
        private int _campIndex;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _triangles;
        private long _drawCallsSeen;
        private long _trianglesSeen;

        // -- Армия --------------------------------------------------------------------------

        private void DrawArmy()
        {
            SoldierUnit.InvulnerableCheat = Switch("Свои не получают урона (и герои)", SoldierUnit.InvulnerableCheat);

            Header("Отряд в центр экрана");
            GUILayout.BeginHorizontal();
            Label("Сколько:", 120f);
            foreach (var n in new[] { 1, 5, 10, 20 })
            {
                if (Button(n == _squadSize ? $"[{n}]" : n.ToString())) _squadSize = n;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            foreach (var (type, name) in Tiers)
            {
                if (Button(name)) SpawnSquad(type, _squadSize, ScreenCentreGround(), null);
            }
            GUILayout.EndHorizontal();
            Label("Уровень солдат — по исследованию ниже.");

            Header("Исследования армии");
            DrawTopics(ref _unitTopics, ResearchCatalog.UnitTopics);
        }

        /// <summary>`count` soldiers of `type` around `at`, sent at `target` when one is given.</summary>
        private void SpawnSquad(SoldierType type, int count, Vector3 at, IDamageTarget target)
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            var group = army.RestoreGroup(type, at, target != null ? TargetPriority.Structures : TargetPriority.Units);
            for (var i = 0; i < count; i++)
            {
                var row = i / 5;
                army.RestoreSoldier(group, at + new Vector3((i % 5 - 2) * 0.6f, 0f, -row * 0.6f), int.MaxValue);
            }
            if (target != null) group.OrderAttack(target);
            Say($"отряд: {count}");
        }

        // -- Герои --------------------------------------------------------------------------

        private void DrawHeroes()
        {
            HeroSkills.NoCooldownCheat = Switch("Умения без перезарядки", HeroSkills.NoCooldownCheat);
            var roster = HeroRoster.Instance;
            if (roster == null)
            {
                Label("Нет HeroRoster.");
                return;
            }
            var max = Mathf.Max(1, BalanceConfig.Instance.KingMaxLevel);
            foreach (var (type, name) in HeroNames)
            {
                var hero = roster.HeroOf(type);
                Header(hero == null ? name + ": нет" : $"{name}: ур. {hero.Level}/{max}{(hero.IsFallen ? ", пал" : "")}");
                GUILayout.BeginHorizontal();
                if (hero == null)
                {
                    if (Button("Выдать бесплатно") && roster.Spawn(type) != null) Say(name + " выдан");
                    GUILayout.EndHorizontal();
                    continue;
                }
                if (Button("Ур. −1", 110f)) SetHeroLevel(hero, hero.Level - 1);
                if (Button("Ур. +1", 110f)) SetHeroLevel(hero, hero.Level + 1);
                if (Button("Макс", 100f)) SetHeroLevel(hero, max);
                if (Button("Вылечить") && hero.Unit != null && !hero.IsFallen) hero.Unit.SetCurrentHealth(hero.Unit.MaxHealth);
                if (Button("Воскресить") && hero.IsFallen) hero.Rise();
                GUILayout.EndHorizontal();
            }
        }

        private void SetHeroLevel(Hero hero, int level)
        {
            if (hero.IsFallen) hero.Rise();
            hero.Restore(level, 0, int.MaxValue, false, 0f);
            Say($"герой: ур. {hero.Level}");
        }

        // -- Орки ---------------------------------------------------------------------------

        private void DrawOrcs()
        {
            var raids = OrcRaidManager.Instance;
            if (raids != null)
            {
                GUILayout.BeginHorizontal();
                raids.RaidsSuspended = Switch("Набеги на паузе", raids.RaidsSuspended);
                if (Button($"Ждать набега: {Mathf.CeilToInt(raids.SecondsUntilNextRaid)} с → 0")) { raids.EndRaidWait(); Say("набег идёт"); }
                GUILayout.EndHorizontal();

                Header("Набег сейчас");
                GUILayout.BeginHorizontal();
                Label($"Орков: {_raidSize}", 160f);
                if (Button("−", 60f)) _raidSize = Mathf.Max(1, _raidSize - 1);
                if (Button("+", 60f)) _raidSize++;
                if (Button("+5", 70f)) _raidSize += 5;
                Label($"Уровень: {_raidLevel}", 160f);
                if (Button("−", 60f)) _raidLevel = Mathf.Max(1, _raidLevel - 1);
                if (Button("+", 60f)) _raidLevel++;
                GUILayout.EndHorizontal();
                if (Button("Отправить набег"))
                {
                    Say(raids.RaidNow(_raidSize, _raidLevel) ? $"набег: {_raidSize} ур. {_raidLevel}" : "нет портала, откуда идти");
                }
            }
            if (Button($"Убить всех орков ({OrcUnit.All.Count})"))
            {
                var orcs = new List<OrcUnit>(OrcUnit.All);
                foreach (var orc in orcs) if (orc != null && orc.CurrentHealth > 0) orc.TakeDamage(orc.MaxHealth * 100);
                Say($"убито орков: {orcs.Count}");
            }

            if (InvasionMode.IsActive && InvasionMode.Instance.Waves != null)
            {
                Header("Нашествие");
                if (Button("Следующая волна сейчас")) { InvasionMode.Instance.Waves.SendNextWave(); Say("волна"); }
            }

            DrawCamps(raids);
        }

        private void DrawCamps(OrcRaidManager raids)
        {
            var camps = new List<OrcCamp>(OrcCamp.All);
            Header("Лагеря");
            if (camps.Count == 0)
            {
                Label("Лагерей нет: поставьте Ратушу.");
                return;
            }
            _campIndex = Mathf.Clamp(_campIndex, 0, camps.Count - 1);
            var camp = camps[_campIndex];

            GUILayout.BeginHorizontal();
            if (Button("◀", 70f)) { _campIndex = (_campIndex + camps.Count - 1) % camps.Count; Focus(camps[_campIndex]); }
            Label($"Лагерь {_campIndex + 1}/{camps.Count}: ур. {camp.Level} (макс {camp.MaxLevel}), " +
                  $"стража {camp.Guards.Count}{(camp.Cleared ? ", разграблен" : "")}");
            if (Button("▶", 70f)) { _campIndex = (_campIndex + 1) % camps.Count; Focus(camps[_campIndex]); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Button("К лагерю")) Focus(camp);
            for (var level = camp.IsMain ? OrcBaseLayout.MainLevel : 1; level <= camp.MaxLevel; level++)
            {
                if (Button($"Ур. {level}") && raids != null)
                {
                    camp.RebuildAt(level, raids);
                    Focus(camp);
                }
            }
            GUILayout.EndHorizontal();

            Label($"Отряд ({_squadSize}, размер — на вкладке «Армия») у ворот, в атаку на портал:");
            GUILayout.BeginHorizontal();
            foreach (var (type, name) in Tiers)
            {
                if (!Button(name)) continue;
                // The totem first while it stands: hitting a shielded portal does nothing.
                IDamageTarget target = camp.Portal;
                foreach (var building in camp.Buildings)
                {
                    if (building != null && building.Kind == OrcCampBuildingKind.Totem) target = building;
                }
                SpawnSquad(type, _squadSize, camp.InFrontOfGate(4f), target);
                Focus(camp);
            }
            GUILayout.EndHorizontal();
        }

        private static void Focus(OrcCamp camp)
        {
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null && camp != null) rig.FocusOn(camp.LocalToWorld(new Vector2(0f, camp.Layout.Bounds.yMin * 0.6f)));
        }

        // -- Мир ----------------------------------------------------------------------------

        private void DrawWorld()
        {
            Header("Скорость игры");
            GUILayout.BeginHorizontal();
            foreach (var speed in new[] { 0f, 1f, 3f, 6f, 10f })
            {
                var label = Mathf.Approximately(Time.timeScale, speed) ? $"[x{speed:0}]" : $"x{speed:0}";
                if (Button(label)) Time.timeScale = speed;
            }
            GUILayout.EndHorizontal();

            Header("Время и карта");
            var calendar = GameCalendar.Instance;
            GUILayout.BeginHorizontal();
            if (calendar != null && Button($"День {calendar.CurrentDay} → закончить")) { calendar.EndDayNow(); Say("день прошёл"); }
            var fog = FogOfWarManager.Instance;
            if (fog != null) fog.DisabledCheat = Switch("Туман выключен", fog.DisabledCheat);
            if (fog != null && Button("Снять туман"))
            {
                fog.RevealPermanent(new Vector2Int(0, 0), 20000);   // clamped to the grid; 20000² still fits an int
                Say("туман снят");
            }
            if (fog != null && Button("Вернуть туман")) { fog.ConcealAll(); Say("туман вернулся"); }
            GUILayout.EndHorizontal();
            if (Button("Маяки: готовы светить снова")) { LighthouseAim.ResetCooldowns(); Say("маяки готовы"); }

            Header("Подземелье");
            GUILayout.BeginHorizontal();
            if (Dungeon.DungeonMode.IsActive)
            {
                if (Button("Выйти из подземелья")) Dungeon.DungeonTravel.Leave();
            }
            else if (Dungeon.DungeonTravel.CanEnter && Button("Войти в подземелье 1 (герои города)"))
            {
                Dungeon.DungeonTravel.Enter(Dungeon.DungeonTravel.FirstDungeonId);
            }
            GUILayout.EndHorizontal();

            var ships = ShipManager.Instance;
            var map = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
            if (ships == null || map == null || !map.NoRaids || map.IsDungeon) return;
            Header("Корабли (архипелаг)");
            GUILayout.BeginHorizontal();
            foreach (var kind in ShipStats.All)
            {
                if (!Button(ShipStats.DisplayName(kind))) continue;
                if (ships.TrySampleSea(ScreenCentreGround(), 40f, out var at))
                {
                    ships.Spawn(kind, at, Quaternion.identity, ShipStats.MaxHealth(kind));
                    Say(ShipStats.DisplayName(kind) + " на воде");
                }
                else Say("рядом с центром экрана нет моря");
            }
            GUILayout.EndHorizontal();
            if (Button($"Флот орков ({_raidSize} ур. {_raidLevel}, размер — на вкладке «Орки»)"))
            {
                Say(ships.LaunchOrcFleet(_raidSize, _raidLevel) ? "флот орков вышел" : "у орков нет верфи");
            }
        }

        // -- Служебное ----------------------------------------------------------------------

        private void DrawService()
        {
            Label($"FPS {_fps:0}   кадр {1000f / Mathf.Max(1f, _fps):0.0} мс");
            Label(_drawCalls.Valid
                ? $"Отрисовок (draw calls): {_drawCallsSeen}   треугольников: {_trianglesSeen}"
                : "Отрисовки и треугольники видны только в редакторе и в development-сборке.");

            Header("Сохранение");
            var save = FindAnyObjectByType<GameSaveController>();
            GUILayout.BeginHorizontal();
            if (save != null && Button("Сохранить в слот «cheat»")) { save.SaveGame(CheatSlot); Say("сохранено в «cheat»"); }
            if (Button("Загрузить «cheat»") && SaveSystem.Load(CheatSlot) != null)
            {
                GameSessionIntent.SaveNameToLoad = CheatSlot;
                GameSessionIntent.NewGameMapId = null;
                LeaveTo();
            }
            GUILayout.EndHorizontal();
            if (MeshMapApplier.Instance != null && Button("Начать эту карту заново"))
            {
                GameSessionIntent.SaveNameToLoad = null;
                GameSessionIntent.NewGameMapId = InvasionMode.IsActive ? null : MeshMapApplier.Instance.CurrentMapId;
                GameSessionIntent.NewGameIsInvasion = InvasionMode.IsActive;
                LeaveTo();
            }
        }

        /// <summary>Reloads the game scene with what GameSessionIntent was just told.</summary>
        private void LeaveTo()
        {
            SetOpen(false);
            Time.timeScale = 1f;
            LoadingScreen.IntoGame(SceneManager.GetActiveScene().name);
        }

        // The render counters run only while the window is open: they cost a little every frame.
        private void StartRender()
        {
            if (!_drawCalls.Valid) _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            if (!_triangles.Valid) _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        private void StopRender()
        {
            if (_drawCalls.Valid) _drawCalls.Dispose();
            if (_triangles.Valid) _triangles.Dispose();
            _drawCalls = default;
            _triangles = default;
        }

        private void SampleRender()
        {
            if (_drawCalls.Valid) _drawCallsSeen = _drawCalls.LastValue;
            if (_triangles.Valid) _trianglesSeen = _triangles.LastValue;
        }
    }
}
#endif
