using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// A dungeon's entrance on the town's map (Maps/Archipelago/DungeonEntrances): its level
    /// (1 nearest the start) and the dungeon it leads to. A tap opens its card
    /// (DungeonEntranceCard); a hero walking up to it opens the card by itself, once per arrival,
    /// so going down is: send a hero to the mine, then «Войти».
    /// </summary>
    public class DungeonEntrance : MonoBehaviour
    {
        private const float CheckEverySeconds = 0.5f;

        private static readonly List<DungeonEntrance> _all = new List<DungeonEntrance>();
        public static IReadOnlyList<DungeonEntrance> All => _all;

        public int Level { get; private set; }
        public string DungeonId { get; private set; }
        public Vector2Int Cell { get; private set; }

        private float _timer;
        private bool _heroWasNear;

        public void Initialize(int level, string dungeonId, Vector2Int cell)
        {
            Level = level;
            DungeonId = dungeonId;
            Cell = cell;
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>A living hero within dungeon_enter_reach_cells of the entrance (its footprint's edge, not its middle).</summary>
        public bool HeroNear()
        {
            var roster = HeroRoster.Instance;
            if (roster == null) return false;
            var balance = BalanceConfig.Instance;
            var reach = (balance != null ? balance.DungeonEnterReach : 4f) + Maps.Archipelago.DungeonEntrances.Footprint.x * 0.5f;
            foreach (var hero in roster.Heroes)
            {
                if (hero == null || hero.IsFallen || hero.Unit == null) continue;
                var d = hero.Unit.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude <= reach * reach) return true;
            }
            return false;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckEverySeconds;
            var near = HeroNear();
            if (near && !_heroWasNear && DungeonTravel.CanEnter && DungeonEntranceCard.Instance != null && !DungeonEntranceCard.Instance.IsOpen)
                DungeonEntranceCard.Instance.Show(this);
            _heroWasNear = near;
        }

        /// <summary>The entrance under a screen point, or null.</summary>
        public static DungeonEntrance At(Vector2 screenPosition, Camera camera)
        {
            if (camera == null || _all.Count == 0) return null;
            var ray = camera.ScreenPointToRay(screenPosition);
            foreach (var entrance in _all)
            {
                var box = entrance != null ? entrance.GetComponent<Collider>() : null;
                if (box != null && box.Raycast(ray, out _, 1000f)) return entrance;
            }
            return null;
        }
    }
}
