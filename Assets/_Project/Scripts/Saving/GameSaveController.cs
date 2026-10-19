using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Saving
{
    /// <summary>
    /// The player saves when and under what name they like (see SaveDialogController). Besides
    /// that there is ONE autosave slot (AutosaveName) that overwrites itself: every
    /// autosaveIntervalSeconds of real time (4 minutes, the user's choice), and whenever the app
    /// goes to the background or quits -- on a phone, the moment Android may kill the game and take
    /// everything since the last manual save with it. The autosave never becomes the "current"
    /// save, so the save dialog keeps offering the player's own name.
    /// </summary>
    public class GameSaveController : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private CitizenManager citizenManager;
        [SerializeField] private GameCalendar gameCalendar;
        [SerializeField] private TaxManager taxManager;
        [SerializeField] private Research.ResearchManager researchManager;
        [SerializeField] private MapTerrainGenerator mapTerrainGenerator;
        [SerializeField] private MeshMapApplier meshMapApplier;
        [SerializeField] private List<BuildingData> knownBuildings = new List<BuildingData>();

        private Dictionary<string, BuildingData> _catalog;
        private GameSaveData _pendingLoad;

        public string CurrentSaveName { get; private set; } = string.Empty;

        /// <summary>
        /// The autosave's file name. It starts with '_', which SaveSystem.SanitizeName strips from
        /// anything the player types, so no save the player names can ever collide with it.
        /// </summary>
        public const string AutosaveName = "_autosave";

        [SerializeField] private float autosaveIntervalSeconds = 240f;
        private float _sinceAutosave;

        /// <summary>Seconds of real time between autosaves (unscaled: a paused game still has a player who can lose it).</summary>
        public float AutosaveIntervalSeconds
        {
            get => autosaveIntervalSeconds;
            set => autosaveIntervalSeconds = Mathf.Max(1f, value);
        }

        /// <summary>Map id carried by the save being loaded this session, resolved in Awake so MapTerrainGenerator's Start() can read it. Empty when starting a fresh game.</summary>
        public string LoadedMapId { get; private set; } = string.Empty;

        private void Awake()
        {
            _catalog = new Dictionary<string, BuildingData>();
            foreach (var data in knownBuildings)
            {
                if (data != null) _catalog[data.buildingName] = data;
            }

            var nameToLoad = GameSessionIntent.SaveNameToLoad;
            GameSessionIntent.SaveNameToLoad = null;
            if (string.IsNullOrEmpty(nameToLoad)) return;

            _pendingLoad = SaveSystem.Load(nameToLoad);
            if (_pendingLoad == null) return;

            // Resuming from the autosave gives no name to save back under: the dialog should ask
            // for one, not offer "_autosave".
            CurrentSaveName = nameToLoad == AutosaveName ? string.Empty : nameToLoad;
            LoadedMapId = _pendingLoad.mapId;

            // Marked here (Awake, guaranteed to run before every Start() in the scene,
            // including BuildingPlacer's) so its Start() doesn't force-select the Town Hall
            // again for what is actually a resumed game. Actually instantiating the saved
            // buildings/resources needs GridManager/ResourceManager/CitizenManager's singletons,
            // which are only guaranteed ready once every Awake() has run — so that part waits
            // for Start() below.
            if (_pendingLoad.mandatoryBuildingPlaced && buildingPlacer != null)
            {
                buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
            }
        }

        private void Start()
        {
            if (_pendingLoad == null) return;
            ApplyLoadedState(_pendingLoad);
            _pendingLoad = null;
        }

        private void Update()
        {
            if (!CanAutosave) return;
            _sinceAutosave += Time.unscaledDeltaTime;
            if (_sinceAutosave >= autosaveIntervalSeconds) Autosave();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Autosave();
        }

        private void OnApplicationQuit() => Autosave();

        /// <summary>Nothing worth keeping before the Town Hall stands -- a new game starts there anyway.</summary>
        private bool CanAutosave => buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding;

        /// <summary>Writes the autosave slot now and starts the interval over. Does not touch CurrentSaveName.</summary>
        public void Autosave()
        {
            _sinceAutosave = 0f;
            if (!CanAutosave) return;
            try
            {
                SaveSystem.Save(AutosaveName, CollectSaveData());
            }
            catch (Exception e)
            {
                // A full disk or a revoked folder must not take the running game down with it.
                Debug.LogWarning($"[GameSaveController] Autosave failed: {e.Message}");
            }
        }

        /// <summary>Explicit, player-triggered save under the given (already sanitized) name.</summary>
        public void SaveGame(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return;
            CurrentSaveName = saveName;
            SaveSystem.Save(saveName, CollectSaveData());
        }

        private void ApplyLoadedState(GameSaveData data)
        {
            if (citizenManager != null)
            {
                citizenManager.SetPopulation(data.population);
                // After the headcount, which it is clamped into, and before the buildings, whose
                // restored worker counts were written by a save that already knew who was ill.
                citizenManager.SetSickPopulation(data.sickPopulation);
            }

            if (gameCalendar != null)
            {
                gameCalendar.SetCurrentDay(data.currentDay);
            }

            if (taxManager != null)
            {
                taxManager.SetTaxRate(data.taxRatePercent);
            }

            foreach (var entry in data.resources)
            {
                ResourceManager.Instance.SetAmount(entry.type, entry.amount);
            }

            ResourceManager.Instance.RestoreLifetimeProduced(data.lifetimeProduced);

            // Before the buildings: a restored building asks whether its level is researched the
            // moment it is placed, and a fresh ResearchManager would answer "no".
            if (researchManager != null)
            {
                researchManager.RestoreFromSave(data.completedResearch, data.currentResearchId,
                    data.currentResearchElapsedSeconds, data.currentResearchPaidCoins);
            }

            foreach (var entry in data.buildings)
            {
                if (!_catalog.TryGetValue(entry.buildingName, out var buildingData) || buildingData.prefab == null) continue;

                var cell = new Vector2Int(entry.cellX, entry.cellY);
                // Rotated 90/270 means the footprint's X/Z are swapped for grid-occupancy
                // purposes -- matches BuildingPlacer.RotatedFootprint at placement time.
                var footprint = entry.rotationSteps % 2 == 0
                    ? buildingData.footprintSize
                    : new Vector2Int(buildingData.footprintSize.y, buildingData.footprintSize.x);
                var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
                var rotation = Quaternion.Euler(0f, entry.rotationSteps * 90f, 0f);
                var instance = Instantiate(buildingData.prefab, center, rotation);

                var buildingInstance = instance.GetComponent<BuildingInstance>();
                if (buildingInstance == null) buildingInstance = instance.AddComponent<BuildingInstance>();
                buildingInstance.Initialize(buildingData, cell, entry.rotationSteps);
                buildingInstance.SetLevel(entry.level);
                // entry.currentHealth is 0 for saves made before this field existed (missing JSON
                // field deserializes to the int default) -- treat that as "unknown", not "destroyed".
                var health = entry.currentHealth > 0 ? entry.currentHealth : buildingData.LevelStats(entry.level).maxHealth;
                buildingInstance.SetCondition(health, entry.decay);

                GridManager.Instance.SetAreaOccupied(cell, footprint, true);

                if (buildingData.isRoad && RoadNetwork.Instance != null)
                {
                    for (var x = 0; x < footprint.x; x++)
                    {
                        for (var z = 0; z < footprint.y; z++)
                        {
                            RoadNetwork.Instance.RegisterRoad(cell + new Vector2Int(x, z));
                        }
                    }
                }

                var production = instance.GetComponent<ProductionBuilding>();
                if (production != null)
                {
                    // Before the workers: the recipe decides what those workers will be making on
                    // the very first tick after the load.
                    production.SelectRecipeById(entry.selectedRecipeId);
                    if (entry.assignedWorkers > 0) production.SetAssignedWorkers(entry.assignedWorkers);
                }
            }

            var restoredGroups = RestoreArmy(data);
            var restoredOrcs = RestoreRaids(data);
            // Last of the three: an attack order points at the portal or at an orc, and neither of
            // them exists until RestoreRaids above has put them back.
            RestoreArmyOrders(data, restoredGroups, restoredOrcs);

            // After the army: what the settlement has to feed tomorrow counts its soldiers too.
            FoodConsumptionManager.Instance?.RestoreFromSave(data.hungryDaysInARow, data.recentStarvationDeaths);

            // After the buildings, which is the whole point: MigrationManager decides whether a
            // settlement exists by asking whether a Town Hall is standing, and until the loop
            // above ran, none was.
            Citizens.MigrationManager.Instance?.RestoreFromSave(data.migrationTimerSeconds, data.settlingInSecondsRemaining);

            RestoreRocks(data);
            RestoreFires(data);
            Citizens.SicknessManager.Instance?.RestoreFromSave(data.sicknessUntreatedDays);
        }

        /// <summary>
        /// Sets alight again everything that was burning, as far in as it was.
        ///
        /// After the buildings, obviously -- but also after the ROCKS, for no reason other than
        /// keeping every "put the world back" pass in one place at the end of the load.
        /// </summary>
        private static void RestoreFires(GameSaveData data)
        {
            if (data.fires == null || data.fires.Count == 0) return;

            foreach (var entry in data.fires)
            {
                var cell = new Vector2Int(entry.cellX, entry.cellY);
                var building = FindBuildingAt(cell);
                if (building == null) continue;

                Buildings.FireManager.Ignite(building);
                var fire = building.GetComponent<Buildings.BuildingFire>();
                if (fire != null) fire.RestoreElapsed(entry.elapsedSeconds);
            }
        }

        private static BuildingInstance FindBuildingAt(Vector2Int cell)
        {
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.OriginCell == cell) return instance;
            }
            return null;
        }

        /// <summary>
        /// Puts back the exact boulders the save was made with, each as worked-out as it was.
        /// Runs after the buildings so the cells a restored building sits on are already marked
        /// occupied -- a boulder never shared a cell with a building when the save was written,
        /// and it must not be dropped onto one now.
        /// </summary>
        private static void RestoreRocks(GameSaveData data)
        {
            var spawner = Maps.RockSpawner.Instance;
            if (spawner == null || data.rocks == null || data.rocks.Count == 0) return;

            var rocks = new List<(Vector2Int cell, int remaining)>(data.rocks.Count);
            foreach (var entry in data.rocks)
            {
                rocks.Add((new Vector2Int(entry.cellX, entry.cellY), entry.remaining));
            }

            spawner.RestoreFromSave(rocks);
        }

        /// <summary>
        /// Puts the portals, the raid clock and the orcs already in the field back. Runs here in
        /// Start rather than being left to OrcRaidManager's own Update, which opens the portals on
        /// the first frame it sees a Town Hall -- and the buildings above have just given it one.
        /// </summary>
        private static List<Combat.OrcUnit> RestoreRaids(GameSaveData data)
        {
            // In save order, so a group's attack order can find the orc it was chasing by the
            // position that orc had in the saved list.
            var restored = new List<Combat.OrcUnit>();

            var raids = Combat.OrcRaidManager.Instance;
            if (raids == null) return restored;

            var campBuildings = new List<Combat.OrcRaidManager.CampBuildingRestoreState>();
            if (data.orcCampBuildings != null)
            {
                foreach (var entry in data.orcCampBuildings)
                {
                    campBuildings.Add(new Combat.OrcRaidManager.CampBuildingRestoreState(entry.portalCell,
                        (Combat.OrcCampBuildingKind)entry.kind, entry.cell, entry.yaw, entry.health));
                }
            }
            var camps = new List<Combat.OrcRaidManager.CampRestoreState>();
            if (data.orcCamps != null)
            {
                foreach (var entry in data.orcCamps)
                {
                    camps.Add(new Combat.OrcRaidManager.CampRestoreState(entry.portalCell, entry.level, entry.maxLevel,
                        entry.rank, entry.quarters, entry.cleared));
                }
            }
            // Old camp buildings belong to the old camps' plan; a save without camp states drops them.
            if (camps.Count == 0) campBuildings.Clear();
            raids.RestoreFromSave(data.portalPlaced, ReadSavedPortals(data), data.closedPortalCells, camps, campBuildings,
                data.secondsUntilNextRaid, data.hasMainPortal ? data.mainPortalCell : (Vector2Int?)null);
            // A cheat flag, and saved for exactly that reason: raids that switch themselves back on
            // at the next load arrive in the middle of whatever they were switched off to watch.
            raids.RaidsSuspended = data.raidsSuspended;

            if (data.freedPrisoners != null)
            {
                foreach (var prisoner in data.freedPrisoners) Combat.FreedPrisoner.Spawn(prisoner.position, prisoner.health);
            }

            if (data.orcs == null) return restored;
            foreach (var orc in data.orcs)
            {
                restored.Add(raids.RestoreOrc(orc.position, orc.level, orc.currentHealth, orc.kind));
            }

            return restored;
        }

        /// <summary>
        /// The portals the save holds, in save order.
        ///
        /// Falls back to the single-portal fields the save used to carry when the list is empty but
        /// a portal was placed: without this, loading a save written before the map had five
        /// portals would open none at all -- and the flag alone would then leave that map
        /// permanently un-raided and unwinnable.
        /// </summary>
        private static List<Combat.OrcRaidManager.PortalRestoreState> ReadSavedPortals(GameSaveData data)
        {
            var portals = new List<Combat.OrcRaidManager.PortalRestoreState>();

            if (data.portals != null && data.portals.Count > 0)
            {
                foreach (var entry in data.portals)
                {
                    portals.Add(new Combat.OrcRaidManager.PortalRestoreState(entry.cell, entry.health, entry.main));
                }

                return portals;
            }

            // A legacy health of zero is the other meaningful case: the one portal had already been
            // destroyed, and an empty list next to a true flag is exactly how that reads today.
            if (data.portalPlaced && data.portalHealth > 0)
            {
                portals.Add(new Combat.OrcRaidManager.PortalRestoreState(data.portalCell, data.portalHealth));
            }

            return portals;
        }

        /// <summary>
        /// Rebuilds the army the save was written with. Runs after the research above, not before:
        /// a soldier reads its health and damage off the level its type has been researched to, and
        /// a militia restored against a fresh ResearchManager would come back at level 1.
        /// </summary>
        private static List<Combat.ArmyGroup> RestoreArmy(GameSaveData data)
        {
            // Parallel to data.armyGroups, so the orders pass below can pair the two up by index.
            var restored = new List<Combat.ArmyGroup>();

            var army = Combat.ArmyManager.Instance;
            if (army == null || data.armyGroups == null) return restored;

            foreach (var groupEntry in data.armyGroups)
            {
                var group = army.RestoreGroup(groupEntry.type, groupEntry.holdPosition, groupEntry.priority);
                restored.Add(group);
                if (groupEntry.soldiers == null) continue;

                foreach (var soldier in groupEntry.soldiers)
                {
                    army.RestoreSoldier(group, soldier.position, soldier.currentHealth);
                }
            }

            return restored;
        }

        /// <summary>
        /// Re-issues the two things the player chose that the groups cannot work out again for
        /// themselves: what each group was ordered to attack, and which group was selected.
        ///
        /// Separate from RestoreArmy because both answers live outside the army -- an attack target
        /// is a portal or an orc, and neither exists until the raid restore has run.
        /// </summary>
        private static void RestoreArmyOrders(GameSaveData data, List<Combat.ArmyGroup> groups, List<Combat.OrcUnit> orcs)
        {
            var army = Combat.ArmyManager.Instance;
            if (army == null) return;

            for (var i = 0; i < groups.Count && i < data.armyGroups.Count; i++)
            {
                var entry = data.armyGroups[i];
                switch (entry.attackTargetKind)
                {
                    case ArmyAttackTargetKind.Portal:
                        // The restore above rebuilds the portals in save order, so the saved index
                        // picks out the same one. Out of range means the portal was destroyed
                        // between the save and this load -- impossible today, but the group simply
                        // holds, which is what it would have done a moment after its target fell.
                        if (entry.attackTargetPortalIndex >= 0 && entry.attackTargetPortalIndex < Combat.OrcPortal.All.Count)
                        {
                            groups[i].RestoreAttackOrder(Combat.OrcPortal.All[entry.attackTargetPortalIndex]);
                        }
                        break;
                    case ArmyAttackTargetKind.Orc:
                        // Out of range means the save predates this field, or the orc list came
                        // back shorter -- either way the group simply holds, which is what it would
                        // have done a moment after its target died anyway.
                        if (entry.attackTargetOrcIndex >= 0 && entry.attackTargetOrcIndex < orcs.Count)
                        {
                            groups[i].RestoreAttackOrder(orcs[entry.attackTargetOrcIndex]);
                        }
                        break;
                }
            }

            // Restored last, and deliberately not folded into the loop above: selection belongs to
            // the player rather than to a group, and the index means nothing until every group is
            // back in place.
            if (data.selectedGroupIndex >= 0 && data.selectedGroupIndex < groups.Count)
            {
                army.SelectGroup(groups[data.selectedGroupIndex]);
            }
        }

        /// <summary>
        /// Writes down what a group was ordered to attack, in terms the load can find again.
        ///
        /// A target that has already died is left as None. That is not laziness: the order was over
        /// anyway, and an index pointing at a dead orc would restore an attack on whichever orc
        /// ends up in that slot instead.
        /// </summary>
        private static void WriteAttackOrder(Combat.ArmyGroup group, ArmyGroupEntry entry, Dictionary<Combat.OrcUnit, int> orcIndices)
        {
            var target = group.AttackTarget;
            if (target == null || !target.IsAlive) return;

            if (target is Combat.OrcPortal portal)
            {
                entry.attackTargetKind = ArmyAttackTargetKind.Portal;
                // The saved portal list is written straight out of this same registry, in this same
                // order, so the position in it IS the index the load reads back. A linear scan over
                // at most a handful of portals, once per group, at save time.
                entry.attackTargetPortalIndex = Mathf.Max(0, IndexOfPortal(portal));
                return;
            }

            if (target is Combat.OrcUnit orc && orcIndices.TryGetValue(orc, out var index))
            {
                entry.attackTargetKind = ArmyAttackTargetKind.Orc;
                entry.attackTargetOrcIndex = index;
            }
        }

        /// <summary>Where a portal sits in the registry, or -1 if it is not in it -- which only a portal destroyed on this very frame could manage, and which the caller reads as the first one.</summary>
        private static int IndexOfPortal(Combat.OrcPortal portal)
        {
            var portals = Combat.OrcPortal.All;
            for (var i = 0; i < portals.Count; i++)
            {
                if (portals[i] == portal) return i;
            }

            return -1;
        }

        private GameSaveData CollectSaveData()
        {
            var data = new GameSaveData
            {
                mapId = meshMapApplier != null && !string.IsNullOrEmpty(meshMapApplier.CurrentMapId)
                    ? meshMapApplier.CurrentMapId
                    : (mapTerrainGenerator != null ? mapTerrainGenerator.CurrentMapId : string.Empty),
                mandatoryBuildingPlaced = buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding,
                population = citizenManager != null ? citizenManager.TotalPopulation : 0,
                currentDay = gameCalendar != null ? gameCalendar.CurrentDay : 1,
                taxRatePercent = taxManager != null ? taxManager.TaxRatePercent : 10
            };

            var migration = Citizens.MigrationManager.Instance;
            if (migration != null)
            {
                data.migrationTimerSeconds = migration.Timer;
                data.settlingInSecondsRemaining = migration.SettlingInRemaining;
            }

            var rocks = Maps.RockSpawner.Instance;
            if (rocks != null)
            {
                foreach (var (cell, remaining) in rocks.LiveRocks)
                {
                    data.rocks.Add(new RockEntry { cellX = cell.x, cellY = cell.y, remaining = remaining });
                }
            }

            if (researchManager != null)
            {
                data.completedResearch.AddRange(researchManager.CompletedTopicIds);
                researchManager.ReadCurrentForSave(out var currentResearchId, out var elapsed, out var paid);
                data.currentResearchId = currentResearchId;
                data.currentResearchElapsedSeconds = elapsed;
                data.currentResearchPaidCoins = paid;
            }

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                data.resources.Add(new ResourceEntry { type = type, amount = ResourceManager.Instance.GetAmount(type) });
            }

            data.lifetimeProduced = ResourceManager.Instance.LifetimeProduced;

            var raids = Combat.OrcRaidManager.Instance;
            if (raids != null)
            {
                data.portalPlaced = raids.PortalsPlaced;
                // Only the survivors, in registry order -- which is the order the load rebuilds
                // them in, and the order a group's attack order indexes into. An empty list next
                // to a true flag is the player having closed every portal on the map, and is what
                // stops a fresh set opening over the ruins on load.
                foreach (var portal in Combat.OrcPortal.All)
                {
                    data.portals.Add(new PortalEntry { cell = portal.Cell, health = portal.CurrentHealth, main = portal.IsMain });
                }
                data.closedPortalCells.AddRange(raids.ClosedPortalCells);
                data.hasMainPortal = raids.MainPortalCell.HasValue;
                data.mainPortalCell = raids.MainPortalCell ?? Vector2Int.zero;
                // Every camp, and of its buildings only the damaged (their health) and the fallen
                // (0, their ruin): the plan of the camp's level puts the untouched ones back.
                foreach (var camp in Combat.OrcCamp.All)
                {
                    data.orcCamps.Add(new OrcCampStateEntry
                    {
                        portalCell = camp.PortalCell, level = camp.Level, maxLevel = camp.MaxLevel, rank = camp.Rank,
                        quarters = camp.Quarters, cleared = camp.Cleared,
                    });
                    foreach (var building in camp.Buildings)
                    {
                        if (building == null || building.CurrentHealth >= building.MaxHealth) continue;
                        data.orcCampBuildings.Add(new OrcCampEntry
                        {
                            portalCell = camp.PortalCell, kind = (int)building.Kind, cell = building.Cell,
                            yaw = building.transform.eulerAngles.y, health = building.CurrentHealth,
                        });
                    }
                    foreach (var fallen in camp.Fallen)
                    {
                        data.orcCampBuildings.Add(new OrcCampEntry
                        {
                            portalCell = camp.PortalCell, kind = (int)fallen.Kind, cell = fallen.Cell, yaw = fallen.Yaw, health = 0,
                        });
                    }
                }
                data.secondsUntilNextRaid = raids.SecondsUntilNextRaid;
                data.raidsSuspended = raids.RaidsSuspended;
            }

            // Written BEFORE the army below, not after: a group's attack order on an orc is stored
            // as that orc's position in this list, so the list has to exist to point into.
            foreach (var prisoner in Combat.FreedPrisoner.All)
            {
                if (prisoner == null || prisoner.CurrentHealth <= 0) continue;
                data.freedPrisoners.Add(new PrisonerEntry { position = prisoner.transform.position, health = prisoner.CurrentHealth });
            }

            var orcIndices = new Dictionary<Combat.OrcUnit, int>();
            foreach (var orc in Combat.OrcUnit.All)
            {
                // A camp's guards are not raiders: the tent musters them again on load.
                if (orc == null || orc.IsGuard) continue;
                orcIndices[orc] = data.orcs.Count;
                data.orcs.Add(new OrcEntry { position = orc.transform.position, level = orc.Level, currentHealth = orc.CurrentHealth, kind = orc.Kind });
            }

            var army = Combat.ArmyManager.Instance;
            if (army != null)
            {
                for (var i = 0; i < army.Groups.Count; i++)
                {
                    var group = army.Groups[i];
                    // An empty group is still worth writing down: it carries the rally point and
                    // priority the player set, which ArmyManager deliberately keeps when the last
                    // member dies.
                    var entry = new ArmyGroupEntry { type = group.Type, holdPosition = group.HoldPosition, priority = group.Priority };
                    WriteAttackOrder(group, entry, orcIndices);
                    foreach (var soldier in group.Members)
                    {
                        if (soldier == null) continue;
                        entry.soldiers.Add(new SoldierEntry { position = soldier.transform.position, currentHealth = soldier.CurrentHealth });
                    }
                    data.armyGroups.Add(entry);

                    if (group == army.SelectedGroup) data.selectedGroupIndex = i;
                }
            }

            var food = FoodConsumptionManager.Instance;
            if (food != null)
            {
                data.hungryDaysInARow = food.HungryDaysInARow;
                data.recentStarvationDeaths.AddRange(food.RecentDeathsPerDay);
            }

            data.sickPopulation = citizenManager != null ? citizenManager.SickPopulation : 0;
            data.sicknessUntreatedDays = Citizens.SicknessManager.Instance != null
                ? Citizens.SicknessManager.Instance.UntreatedDaysInARow
                : 0;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null) continue;

                var fire = instance.GetComponent<Buildings.BuildingFire>();
                if (fire != null)
                {
                    data.fires.Add(new FireEntry
                    {
                        cellX = instance.OriginCell.x,
                        cellY = instance.OriginCell.y,
                        elapsedSeconds = fire.ElapsedSeconds
                    });
                }

                var production = instance.GetComponent<ProductionBuilding>();
                data.buildings.Add(new BuildingEntry
                {
                    buildingName = instance.Data.buildingName,
                    cellX = instance.OriginCell.x,
                    cellY = instance.OriginCell.y,
                    assignedWorkers = production != null ? production.AssignedWorkers : 0,
                    selectedRecipeId = production != null && production.SelectedRecipe != null ? production.SelectedRecipe.id : string.Empty,
                    level = instance.Level,
                    currentHealth = instance.CurrentHealth,
                    decay = instance.Decay,
                    rotationSteps = instance.RotationSteps
                });
            }

            return data;
        }
    }
}
