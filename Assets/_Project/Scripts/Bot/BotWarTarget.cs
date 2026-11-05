using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Diplomacy;
using UnityEngine;

namespace CityBuilder.Bot
{
    /// <summary>
    /// A bot building as something the town's soldiers may strike -- only at war (the user,
    /// 2026-11-03: a war ends with the bot's Town Hall razed). At peace it is not alive, so a tap
    /// on it is no attack order and nobody picks it as a target.
    /// </summary>
    public class BotWarTarget : MonoBehaviour, IDamageTarget
    {
        private BuildingInstance _building;

        private void Awake() => _building = GetComponent<BuildingInstance>();

        public Transform Transform => this != null ? transform : null;

        public bool IsAlive => _building != null && _building.CurrentHealth > 0 && BotRelations.Stance == BotStance.War;

        public bool IsStructure => true;

        /// <summary>Half its footprint, in world units: a soldier strikes it from its walls, not its middle.</summary>
        public float Reach
        {
            get
            {
                if (_building == null || _building.Data == null) return 0f;
                var size = _building.Data.footprintSize;
                var cell = Grid.GridManager.Instance != null ? Grid.GridManager.Instance.CellSize : 1f;
                return Mathf.Max(size.x, size.y) * 0.5f * cell;
            }
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            var level = _building.Level;
            _building.TryDamage(amount);
            // razed by the town's army: the ransom falls (the user, 2026-11-05)
            if (_building == null || _building.CurrentHealth <= 0) BotRelations.NoteRazed(level);
        }
    }
}
