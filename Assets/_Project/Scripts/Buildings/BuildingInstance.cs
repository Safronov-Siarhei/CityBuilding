using System.Collections.Generic;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Buildings
{
    public class BuildingInstance : MonoBehaviour
    {
        public const int MaxLevel = 3;

        public BuildingData Data { get; private set; }
        public Vector2Int OriginCell { get; private set; }
        public int Level { get; private set; } = 1;

        /// <summary>0-3, each a 90-degree step around Y -- set at placement (BuildingPlacer.RotateSelection) and restored on load.</summary>
        public int RotationSteps { get; private set; }

        /// <summary>Current hit points; starts at BuildingData.maxHealth. No damage source exists yet -- this is state for a future combat/decay system to read and write.</summary>
        public int CurrentHealth { get; private set; }

        /// <summary>0 (new) to 1 (fully dilapidated). Nothing advances this yet -- reserved for a future decay-over-time system.</summary>
        public float Decay { get; private set; }

        public void Initialize(BuildingData data, Vector2Int originCell, int rotationSteps = 0)
        {
            Data = data;
            OriginCell = originCell;
            RotationSteps = ((rotationSteps % 4) + 4) % 4;
            CurrentHealth = data != null ? data.maxHealth : 0;
            Decay = 0f;

            // The single hook point for every building instantiation (fresh placement AND
            // save/load), so permanent fog reveal always matches the buildings actually present
            // without needing any separate persistence of its own.
            if (data != null)
            {
                var footprint = data.footprintSize;
                var centerCell = originCell + new Vector2Int(footprint.x / 2, footprint.y / 2);
                FogOfWarManager.Instance?.RevealPermanent(centerCell, data.fogRevealRadius);
            }
        }

        /// <summary>Used by save/load to restore an already-valid level directly, bypassing the resource-spend check.</summary>
        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, MaxLevel);
        }

        /// <summary>Used by save/load to restore already-valid runtime condition directly.</summary>
        public void SetCondition(int health, float decay)
        {
            CurrentHealth = Data != null ? Mathf.Clamp(health, 0, Data.maxHealth) : health;
            Decay = Mathf.Clamp01(decay);
        }

        /// <summary>Null once already at MaxLevel.</summary>
        public List<ResourceAmount> GetUpgradeCost()
        {
            if (Data == null || Level >= MaxLevel) return null;
            return Level == 1 ? Data.upgradeToLevel2Cost : Data.upgradeToLevel3Cost;
        }

        public bool TryUpgrade()
        {
            var cost = GetUpgradeCost();
            if (cost == null || ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost)) return false;

            Level++;
            // Visual style / stat scaling per level is a planned future addition (see
            // BuildingData's Upgrades/Condition headers) -- upgrading only advances Level so far.
            return true;
        }
    }
}
