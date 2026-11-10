using System.Collections.Generic;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Nothing under the fog is drawn (the user, 2026-11-10: «если есть туман, то ничего не должно
    /// быть под ним» -- the phones' frame time). Every FogCulled object is checked a slice at a time:
    /// while none of its sample cells is revealed its renderers are off; the moment the fog opens
    /// over it, they come back. `Seen` is also asked by the simulations that only matter on view
    /// (the bot's castle raids and garrison, the orc camps' patrols, the fish shoals).
    /// </summary>
    public static class FogCulling
    {
        private const int PerFrame = 40;

        private static readonly List<FogCulled> Items = new List<FogCulled>();
        private static int _cursor;
        private static Driver _driver;

        /// <summary>
        /// Whether any of the cells round `at` (its own, and four `radiusCells` away) is open to
        /// view -- or there is no fog to hide anything (none on this map, the cheat, a dungeon).
        /// </summary>
        public static bool Seen(Vector3 at, int radiusCells)
        {
            var fog = FogOfWarManager.Instance;
            var grid = GridManager.Instance;
            if (fog == null || grid == null || !fog.IsActive) return true;
            var cell = grid.WorldToCell(at);
            if (fog.IsCellRevealed(cell)) return true;
            if (radiusCells <= 0) return false;
            return fog.IsCellRevealed(cell + new Vector2Int(radiusCells, 0))
                || fog.IsCellRevealed(cell + new Vector2Int(-radiusCells, 0))
                || fog.IsCellRevealed(cell + new Vector2Int(0, radiusCells))
                || fog.IsCellRevealed(cell + new Vector2Int(0, -radiusCells));
        }

        /// <summary>Puts an object under the fog's rule: hidden while it lies in cloud.</summary>
        public static FogCulled Cull(GameObject go, int radiusCells)
        {
            if (go == null) return null;
            var culled = go.GetComponent<FogCulled>();
            if (culled == null) culled = go.AddComponent<FogCulled>();
            culled.RadiusCells = Mathf.Max(0, radiusCells);
            return culled;
        }

        internal static void Add(FogCulled item)
        {
            Items.Add(item);
            if (_driver == null)
            {
                var go = new GameObject("FogCulling");
                _driver = go.AddComponent<Driver>();
            }
        }

        internal static void Remove(FogCulled item)
        {
            var i = Items.IndexOf(item);
            if (i < 0) return;
            // swap-remove: the order does not matter, the slice just walks on
            Items[i] = Items[Items.Count - 1];
            Items.RemoveAt(Items.Count - 1);
        }

        private static void Tick()
        {
            var count = Items.Count;
            if (count == 0) return;
            var n = Mathf.Min(PerFrame, count);
            for (var k = 0; k < n; k++)
            {
                if (_cursor >= Items.Count) _cursor = 0;
                var item = Items[_cursor++];
                if (item == null) continue;
                item.Apply(Seen(item.transform.position, item.RadiusCells));
            }
        }

        /// <summary>The one MonoBehaviour that walks the list, made with the first culled object; gone with its scene.</summary>
        private class Driver : MonoBehaviour
        {
            private void Update() => Tick();

            private void OnDestroy()
            {
                if (_driver == this) _driver = null;
                Items.Clear();
                _cursor = 0;
            }
        }
    }
}
