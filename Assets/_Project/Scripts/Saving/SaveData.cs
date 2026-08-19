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
        public bool mandatoryBuildingPlaced;
        public int population;
        public int currentDay = 1;
        public int taxRatePercent = 10;
        public List<ResourceEntry> resources = new List<ResourceEntry>();
        public List<BuildingEntry> buildings = new List<BuildingEntry>();

        /// <summary>
        /// Ids of everything researched in the Laboratory (see CityBuilder.Research.ResearchTopic),
        /// and the one research under way with how far in it is and what was paid for it -- so
        /// reloading neither forgets it nor refunds it twice. Empty in saves made before research
        /// existed, which read correctly as "nothing researched yet".
        /// </summary>
        public List<string> completedResearch = new List<string>();

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

        /// <summary>
        /// How the settlement was eating. Without these two a reload forgave a starving town: the
        /// hunger streak restarted at zero, so a settlement one day away from its first deaths got
        /// its whole grace period back, and the happiness penalty for the people it had already
        /// buried disappeared with it.
        /// </summary>
        public int hungryDaysInARow;

        /// <summary>Starvation deaths per day over the window happiness remembers, oldest first.</summary>
        public List<int> recentStarvationDeaths = new List<int>();

        /// <summary>
        /// The raid source. Without it a reload healed the map's objective: the portal is placed
        /// relative to the Town Hall on the first frame one exists, so a fresh one opened at full
        /// health however far the player's army had ground the old one down -- and the orcs already
        /// on their way vanished, which made reloading a way to call off a raid.
        ///
        /// One portal, matching what OrcRaidManager spawns today; the design's five per map turn
        /// this into a list.
        /// </summary>
        public bool portalPlaced;

        public Vector2Int portalCell;

        /// <summary>Zero means it was placed and then destroyed -- no new one is opened over it.</summary>
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
    public class RockEntry
    {
        public int cellX;
        public int cellY;

        /// <summary>Stone left in this boulder. A boulder worked out to zero is simply absent from the list.</summary>
        public int remaining;
    }

    [Serializable]
    public class OrcEntry
    {
        public Vector3 position;

        /// <summary>Raids send level 1; anything higher came from the OrcSpawn cheat, and its health and damage are scaled by it.</summary>
        public int level = 1;
        public int currentHealth;
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

        public List<SoldierEntry> soldiers = new List<SoldierEntry>();
    }

    /// <summary>
    /// What a saved group's attack order pointed at. Spelled out as kinds rather than as one id
    /// because the two targetable things come back by completely different routes on load: the
    /// portal from OrcRaidManager's own restore, an orc from the saved orc list beside it.
    ///
    /// One portal, matching what the game spawns today; the design's five per map turn Portal into
    /// an index, the way orcs already are.
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
    }
}
