using System;
using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Saving
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 1;
        public string mapId = string.Empty;
        /// <summary>The game's season (Core.MapSeason, picked as the menu's theme); -1 in saves made before it was kept: summer.</summary>
        public int season = -1;
        public bool mandatoryBuildingPlaced;
        public int population;
        public int currentDay = 1;
        public int taxRatePercent = 10;
        public List<ResourceEntry> resources = new List<ResourceEntry>();

        /// <summary>
        /// Everything the settlement has ever produced (see ResourceManager.LifetimeProduced). It
        /// is one of the five terms raids are sized against, and the only one that grows for the
        /// whole game and never falls -- so without it a reload handed the player back the raids of
        /// a town founded a minute ago.
        /// </summary>
        public int lifetimeProduced;
        public List<BuildingEntry> buildings = new List<BuildingEntry>();

        /// <summary>
        /// Ids of everything researched in the Laboratory (see CityBuilder.Research.ResearchTopic),
        /// and the one research under way with how far in it is and what was paid for it -- so
        /// reloading neither forgets it nor refunds it twice. Empty in saves made before research
        /// existed, which read correctly as "nothing researched yet".
        /// </summary>
        public List<string> completedResearch = new List<string>();

        /// <summary>Every research under way (one per scientist since 2026-10-29), oldest first.</summary>
        public List<RunningResearchEntry> runningResearch = new List<RunningResearchEntry>();

        // The one research under way in saves made before research ran in parallel -- read on load, never written.
        public string currentResearchId = string.Empty;
        public float currentResearchElapsedSeconds;
        public int currentResearchPaidCoins;

        /// <summary>
        /// The player's army, group by group. Empty in saves made before the army was saved at all,
        /// which reads correctly as "no soldiers" -- and which is exactly what those saves used to
        /// do to a real army, silently.
        /// </summary>
        public List<ArmyGroupEntry> armyGroups = new List<ArmyGroupEntry>();

        /// <summary>
        /// Which group the player was commanding, as an index into <see cref="armyGroups"/>, or -1
        /// for none. Not cosmetic: while a group is selected a tap on the world is an order to it
        /// and the building and citizen selectors stand down (see ArmyManager.SelectedGroup), so a
        /// load used to hand the player back a different input mode than the one they saved in.
        ///
        /// An INDEX rather than the group's own Id: ArmyGroup hands ids out of a static counter
        /// that a scene reload resets, so a saved id would point at whichever group happened to be
        /// created first next time.
        /// </summary>
        public int selectedGroupIndex = -1;

        /// <summary>The heroes (the sandbox has them since 2026-10-25): kept apart from armyGroups, which leave them out -- a hero is more than a soldier in a group.</summary>
        public List<HeroEntry> heroes = new List<HeroEntry>();

        /// <summary>
        /// How the settlement was eating. Without these two a reload forgave a starving town: the
        /// hunger streak restarted at zero, so a settlement one day away from its first deaths got
        /// its whole grace period back, and the happiness penalty for the people it had already
        /// buried disappeared with it.
        /// </summary>
        public int hungryDaysInARow;

        /// <summary>Deaths per day over the window happiness remembers, oldest first. Named for starvation because that is all it once held; illness now goes in here too, and the name is kept so the field does not vanish out of every save already written (see FoodConsumptionManager.RecordDeaths).</summary>
        public List<int> recentStarvationDeaths = new List<int>();

        /// <summary>
        /// How the settlement was ailing: how many are in bed, and how many days in a row have
        /// ended without the beds being cleared. Both matter -- the sick are people the player
        /// cannot put to work, and the streak is how close they are to burying them.
        /// </summary>
        public int sickPopulation;

        public int sicknessUntreatedDays;

        /// <summary>
        /// Everything alight, and how far into its burn it is. Without this a reload was a fire
        /// brigade: every fire went out at once, the buildings kept whatever health the flames had
        /// left them, and saving mid-blaze was the cheapest way to put one out.
        /// </summary>
        public List<FireEntry> fires = new List<FireEntry>();

        /// <summary>
        /// Whether the map has opened its portals at all. Without it a reload healed the map's
        /// objective: the portals are placed relative to the Town Hall on the first frame one
        /// exists, so a fresh set opened at full health however far the player's army had ground
        /// the old ones down -- and the orcs already on their way vanished, which made reloading a
        /// way to call off a raid.
        ///
        /// True with an empty <see cref="portals"/> list is the meaningful case of the player
        /// having closed every portal on the map: nothing is reopened over the ruins.
        /// </summary>
        public bool portalPlaced;

        /// <summary>Every portal still standing, in the order they were opened -- which is the order a group's saved attack order indexes into.</summary>
        public List<PortalEntry> portals = new List<PortalEntry>();

        /// <summary>Where the portals the player destroyed stood, so their ruins come back with the save. Empty in saves written before portals left ruins.</summary>
        public List<Vector2Int> closedPortalCells = new List<Vector2Int>();

        /// <summary>
        /// The orc camp buildings the player has damaged or broken (health 0 = its ruin), keyed by
        /// the cell of the portal its camp was built around. Untouched buildings are not written --
        /// a level-3 camp has well over a hundred pieces of wall -- the camp's plan puts them back.
        /// </summary>
        public List<OrcCampEntry> orcCampBuildings = new List<OrcCampEntry>();

        /// <summary>Every walled camp (see OrcCamp): its level and the rest of what rebuilds it. Empty in saves from before the walled bases -- those get fresh level-1 camps, and their old camp buildings are dropped.</summary>
        public List<OrcCampStateEntry> orcCamps = new List<OrcCampStateEntry>();

        /// <summary>Whether the map has a main base, and the cell of its great portal (standing or closed -- a closed one leaves the main ruin).</summary>
        public bool hasMainPortal;
        public Vector2Int mainPortalCell;

        /// <summary>Townsfolk let out of orc cages and not yet home: on the road, or waiting at the Town Hall for a free home.</summary>
        public List<PrisonerEntry> freedPrisoners = new List<PrisonerEntry>();

        /// <summary>
        /// Where the ONE portal stood, back when there was only one. Read on load when
        /// <see cref="portals"/> is empty and a portal was placed, so a save written before the map
        /// had five of them still comes back with its portal on the health it had. Never written.
        /// </summary>
        public Vector2Int portalCell;

        /// <summary>The legacy single portal's health -- see <see cref="portalCell"/>. Zero means it had been destroyed. Never written.</summary>
        public int portalHealth;

        /// <summary>Seconds left on the raid clock, so reloading cannot push the next wave back to a full interval.</summary>
        public float secondsUntilNextRaid;

        public List<OrcEntry> orcs = new List<OrcEntry>();

        /// <summary>
        /// Whether the automatic raid clock was switched off. Only the OrcSpawn cheat sets it, and
        /// that is why it is worth saving: a cheat that quietly switches itself back on at the next
        /// load lets a wave arrive in the middle of whatever it was turned off to observe.
        /// </summary>
        public bool raidsSuspended;

        /// <summary>
        /// Where migration had got to. Without these, reloading reset the wait for the next
        /// settler and handed a town still inside its settling-in grace the whole grace back --
        /// small either way, but both are things a player could learn to reload for.
        ///
        /// Whether the settlement exists at all is deliberately NOT here: MigrationManager asks
        /// the restored buildings, which cannot disagree with them.
        /// </summary>
        public float migrationTimerSeconds;

        public float settlingInSecondsRemaining;

        /// <summary>
        /// Every boulder still standing and how much stone is left in it.
        ///
        /// Here because stone is the one resource the map never makes more of (see RockSpawner):
        /// boulders are scattered by unseeded Random, so a reload without this dealt a brand new
        /// map with every rock full again, and a player could undo an hour of quarrying by saving
        /// and loading. Empty in saves made before this existed, which correctly reads as "leave
        /// the map as it was scattered".
        ///
        /// Trees are deliberately NOT here: a felled one grows back on its own within the minute,
        /// so there is nothing about a forest that a reload could unfairly restore.
        /// </summary>
        public List<RockEntry> rocks = new List<RockEntry>();
    }

    [Serializable]
    public class RunningResearchEntry
    {
        public string topicId;
        public float elapsedSeconds;
        public int paidCoins;
        /// <summary>The upgrade crystals it took -- given back if it is cancelled after a load.</summary>
        public int paidCrystals;
    }

    [Serializable]
    public class FireEntry
    {
        /// <summary>The burning building's origin cell -- the same key BuildingEntry uses, and the only thing that identifies one building among several of a kind.</summary>
        public int cellX;
        public int cellY;

        /// <summary>Seconds burnt so far. Not seconds REMAINING: how long a fire has left depends on how many firefighters can reach it, which is a question about the town as it stands after the load, not before the save.</summary>
        public float elapsedSeconds;
    }

    [Serializable]
    public class RockEntry
    {
        public int cellX;
        public int cellY;

        /// <summary>Stone left in this boulder. A boulder worked out to zero is simply absent from the list.</summary>
        public int remaining;
    }

    /// <summary>One orc camp building: its camp (by portal cell), what it is, where, which way it faces, and how much is left of it.</summary>
    [Serializable]
    public class OrcCampEntry
    {
        public Vector2Int portalCell;
        public int kind;
        public Vector2Int cell;
        public float yaw;
        public int health;
    }

    /// <summary>One walled orc camp: its portal's cell, its level, how big it may grow, its rank by distance, its facing (quarter turns), and whether its portal was closed.</summary>
    [Serializable]
    public class OrcCampStateEntry
    {
        public Vector2Int portalCell;
        public int level;
        public int maxLevel;
        public int rank;
        public int quarters;
        public bool cleared;
    }

    /// <summary>One portal still standing. The cell rather than the world position, because that is what the placement rule produced and what the grid has reserved.</summary>
    [Serializable]
    public class PortalEntry
    {
        public Vector2Int cell;
        public int health;
        /// <summary>The main base's great portal (4x4). Absent in older saves, which had none.</summary>
        public bool main;
    }

    [Serializable]
    public class PrisonerEntry
    {
        public Vector3 position;
        public int health;
    }

    [Serializable]
    public class OrcEntry
    {
        public Vector3 position;

        /// <summary>Raids send level 1; anything higher came from the OrcSpawn cheat, and its health and damage are scaled by it.</summary>
        public int level = 1;
        public int currentHealth;

        /// <summary>Raider, thrower or chief. Absent in saves from before the kinds existed, which reads as a raider -- what every orc then was.</summary>
        public Combat.OrcKind kind = Combat.OrcKind.Raider;
    }

    [Serializable]
    public class ArmyGroupEntry
    {
        public SoldierType type;

        /// <summary>The group's rally point and its standing target priority -- orders the player gave, not state the group can work out again.</summary>
        public Vector3 holdPosition;
        public TargetPriority priority;

        /// <summary>
        /// The "attack that" order the group was carrying out, if any. Without it a group sent
        /// across the map to break the portal came back standing where it had got to, holding --
        /// the one order in the game that a reload cancelled silently.
        /// </summary>
        public ArmyAttackTargetKind attackTargetKind;

        /// <summary>Index into <see cref="GameSaveData.orcs"/> when the target was an orc, -1 otherwise. A position would not do: the orc is walking, and it and the group are written at the same instant.</summary>
        public int attackTargetOrcIndex = -1;

        /// <summary>
        /// Index into <see cref="GameSaveData.portals"/> when the target was a portal. Defaults to
        /// zero rather than to -1 on purpose: that is exactly what a save written when the map had
        /// one portal means, and it makes those saves come back pointing at it.
        /// </summary>
        public int attackTargetPortalIndex;

        public List<SoldierEntry> soldiers = new List<SoldierEntry>();
    }

    /// <summary>
    /// What a saved group's attack order pointed at. Spelled out as kinds rather than as one id
    /// because the two targetable things come back by completely different routes on load: the
    /// portal from OrcRaidManager's own restore, an orc from the saved orc list beside it. Which
    /// one of each is then an index into the matching saved list.
    /// </summary>
    public enum ArmyAttackTargetKind
    {
        /// <summary>The group was holding. Also what an older save reads as -- a missing field deserializes to zero.</summary>
        None = 0,
        Portal = 1,
        Orc = 2
    }

    [Serializable]
    public class SoldierEntry
    {
        public Vector3 position;

        /// <summary>Carried across rather than reset to full: reloading in the middle of a raid must not heal the survivors, the same rule BuildingInstance follows for a damaged building.</summary>
        public int currentHealth;
    }

    [Serializable]
    public class ResourceEntry
    {
        public ResourceType type;
        public int amount;
    }

    [Serializable]
    public class HeroEntry
    {
        public SoldierType type;
        public Vector3 position;
        public int level = 1;
        public int experience;
        public int currentHealth;
        public bool fallen;
        public float respawnRemaining;
    }

    [Serializable]
    public class BuildingEntry
    {
        public string buildingName;
        public int cellX;
        public int cellY;
        public int assignedWorkers;

        /// <summary>
        /// Which recipe the building was set to, by its stable id -- empty for the great majority
        /// that only know one thing. An index would have been shorter and wrong: reordering the
        /// recipes tab would silently turn every saved furnace into a different metal.
        /// </summary>
        public string selectedRecipeId = string.Empty;
        public int level = 1;
        public int currentHealth;
        public float decay;
        public int rotationSteps;

        /// <summary>Seconds left of its building or upgrade (ConstructionSite), 0 when it stands done; siteTotal is the whole time.</summary>
        public float siteRemaining;
        public float siteTotal;
        public bool siteUpgrade;
    }
}
