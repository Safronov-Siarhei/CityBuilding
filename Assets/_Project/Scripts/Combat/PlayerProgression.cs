using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// How far along the player is, as one number -- the design backlog's "player progression
    /// score", and what raids are now sized, levelled and paced against.
    ///
    /// It replaced the calendar day, which measured how long the player had SAT there rather than
    /// what they had made of the place: a town that spent twenty days doing nothing was raided
    /// exactly as hard as one that spent them building an empire, so turtling was punished and
    /// rushing was free.
    ///
    /// The five terms are the backlog's, and each reads as an answer to "why would the orcs care":
    /// what is standing and how far it has been upgraded; how many people live there; how big and
    /// how well trained the garrison is; how well defended the place is; and how much the
    /// settlement has ever produced.
    ///
    /// That last one is a LIFETIME total, deliberately. The backlog swapped it in for "current
    /// stockpile" because a stockpile rewards spending everything down to nothing right before a
    /// raid -- and a rate of production would have the same hole one level up, since a player
    /// watching the raid clock could pull every worker off their job for a minute and be raided as
    /// a pauper. A total that only ever grows cannot be gamed at all, and "progression" is the
    /// right word for something monotonic.
    ///
    /// Read on demand -- once per raid, so roughly once a minute -- and never per frame. The scan
    /// over BuildingInstance is the whole cost, and it is the same scan HappinessManager already
    /// makes for three of its own factors.
    /// </summary>
    public static class PlayerProgression
    {
        /// <summary>What the settlement is worth right now, at the sheet's current weights.</summary>
        public static int Score()
        {
            ReadSettlement(out var buildingLevels, out var defence);

            var balance = BalanceConfig.Instance;
            var resources = ResourceManager.Instance;

            return Compute(
                buildingLevels,
                CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0,
                SoldierLevels(),
                defence,
                resources != null ? resources.LifetimeProduced : 0,
                balance.ProgressPerBuildingLevel,
                balance.ProgressPerCitizen,
                balance.ProgressPerSoldierLevel,
                balance.ProgressPerDefencePoint,
                balance.ProgressPerProducedUnit);
        }

        /// <summary>
        /// The formula on its own, taking its weights explicitly so a test can state them instead
        /// of depending on whatever the sheet holds today -- the same split OrcRaidManager's ramp
        /// formulas use, and for the same reason: the SHAPE is what has to survive a retune.
        /// </summary>
        public static int Compute(int buildingLevels, int population, int soldierLevels, int defence, int lifetimeProduced,
            float perBuildingLevel, float perCitizen, float perSoldierLevel, float perDefencePoint, float perProducedUnit)
        {
            var score = Mathf.Max(0, buildingLevels) * perBuildingLevel
                        + Mathf.Max(0, population) * perCitizen
                        + Mathf.Max(0, soldierLevels) * perSoldierLevel
                        + Mathf.Max(0, defence) * perDefencePoint
                        + Mathf.Max(0, lifetimeProduced) * perProducedUnit;

            return Mathf.Max(0, Mathf.RoundToInt(score));
        }

        /// <summary>
        /// One pass over the buildings for both settlement terms.
        ///
        /// Levels are summed rather than counted, so an upgraded town scores above a sprawling one
        /// -- "number of buildings AND their upgrade level" in a single number. Defence is summed
        /// as the DEFENCE STAT, not as a count of defensive buildings: BuildingInstance has already
        /// resolved it for the level the building stands at, so it says how well defended the place
        /// is rather than how many walls happen to be up. Multiplying it by Level here would count
        /// the same upgrade twice, exactly as it would in HappinessManager.
        /// </summary>
        private static void ReadSettlement(out int buildingLevels, out int defence)
        {
            buildingLevels = 0;
            defence = 0;

            foreach (var instance in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null) continue;
                buildingLevels += instance.Level;
                defence += instance.Defense;
            }
        }

        /// <summary>
        /// The garrison, counted as soldiers times the level their type has been researched to --
        /// so twenty militia the player never bothered to train are worth less to the orcs than ten
        /// that have been.
        /// </summary>
        private static int SoldierLevels()
        {
            var army = ArmyManager.Instance;
            if (army == null) return 0;

            var total = 0;
            foreach (var group in army.Groups)
            {
                total += group.Count * SoldierStats.CurrentLevel(group.Type);
            }
            return total;
        }
    }
}
