using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// What each island of the archipelago is rich in (the user, 2026-11-02: «на разных островах свои
    /// ресурсы» -- this replaces the old rule of no island per ore). The start island has what a
    /// town begins with (coal, a little copper, its fertile patch by the Town Hall); every other
    /// ordinary island one riches of its own, so sailing out is how a town gets the rest:
    /// - the nearest ordinary island is IRON (smelting can start without a long voyage), the next
    ///   COPPER, the farthest GOLD (out towards the orcs);
    /// - the rest by their shape: rocky and atoll ones STONE, flat and crescent ones FERTILE, hilly
    ///   ones IRON, long ones COPPER; the islets STONE;
    /// - the orcs' island and the bot's have none.
    /// Deterministic from the generated world alone, so a seed is the same riches everywhere.
    /// </summary>
    public static class IslandRiches
    {
        public enum Riches { None, Start, Iron, Copper, Gold, Stone, Fertile }

        /// <summary>The riches of each of world.Islands, in its order.</summary>
        public static Riches[] Assign(ArchipelagoMap world)
        {
            var riches = new Riches[world.Islands.Count];
            var ordinary = new List<int>();
            for (var i = 0; i < world.Islands.Count; i++)
            {
                var island = world.Islands[i];
                switch (island.Kind)
                {
                    case ArchipelagoGenerator.Kind.Start: riches[i] = Riches.Start; break;
                    case ArchipelagoGenerator.Kind.Orc:
                    case ArchipelagoGenerator.Kind.Bot: riches[i] = Riches.None; break;
                    case ArchipelagoGenerator.Kind.Islet: riches[i] = Riches.Stone; break;
                    default:
                        riches[i] = ByShape(island.Kind);
                        ordinary.Add(i);
                        break;
                }
            }
            var start = world.StartCell;
            ordinary.Sort((a, b) => Distance(world.Islands[a], start).CompareTo(Distance(world.Islands[b], start)));
            if (ordinary.Count > 0) riches[ordinary[0]] = Riches.Iron;
            if (ordinary.Count > 2) riches[ordinary[1]] = Riches.Copper;
            if (ordinary.Count > 1) riches[ordinary[ordinary.Count - 1]] = Riches.Gold;
            return riches;
        }

        private static Riches ByShape(ArchipelagoGenerator.Kind kind)
        {
            switch (kind)
            {
                case ArchipelagoGenerator.Kind.Rocky:
                case ArchipelagoGenerator.Kind.Atoll: return Riches.Stone;
                case ArchipelagoGenerator.Kind.Hilly: return Riches.Iron;
                case ArchipelagoGenerator.Kind.Long: return Riches.Copper;
                default: return Riches.Fertile;
            }
        }

        private static float Distance(Island island, Vector2Int from) => (island.Centre - (Vector2)from).sqrMagnitude;

        /// <summary>Per world cell (row-major), the index of the island whose land it is; -1 for the sea.</summary>
        public static int[] Label(ArchipelagoMap world)
        {
            var n = world.Size;
            var label = new int[n * n];
            for (var i = 0; i < label.Length; i++) label[i] = -1;
            for (var k = 0; k < world.Islands.Count; k++)
            {
                var island = world.Islands[k];
                if (island.Land == null) continue;
                var frame = island.Frame;
                for (var fz = 0; fz < frame; fz++)
                for (var fx = 0; fx < frame; fx++)
                {
                    if (!island.Land[fz * frame + fx]) continue;
                    int x = fx + island.OffsetX, z = fz + island.OffsetY;
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    var i = z * n + x;
                    if (world.Land[i]) label[i] = k;
                }
            }
            return label;
        }
    }
}
