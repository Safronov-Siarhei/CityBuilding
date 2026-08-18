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
        /// Where migration had got to. Without these, reloading reset the wait for the next
        /// settler and handed a town still inside its settling-in grace the whole grace back --
        /// small either way, but both are things a player could learn to reload for.
        ///
        /// Whether the settlement exists at all is deliberately NOT here: MigrationManager asks
        /// the restored buildings, which cannot disagree with them.
        /// </summary>
        public float migrationTimerSeconds;

        public float settlingInSecondsRemaining;
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

        public List<SoldierEntry> soldiers = new List<SoldierEntry>();
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
