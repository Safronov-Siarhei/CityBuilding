using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityBuilder.Invasion;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The run's buildings live wholly on the sheet's invasion tab (the user, 2026-10-24: the two
    /// modes must not touch). Every building a run has needs its row there, with every column a run
    /// uses -- a missing one is no longer filled in from the sandbox's buildings tab, it is empty.
    /// </summary>
    public class InvasionTabTests
    {
        private const string Path = "Assets/_Project/Balance/invasion.csv";

        private static readonly string[] RunColumns =
        {
            "display_name", "category", "cost_wood", "cost_stone", "cost_gold", "max_health", "defense", "max_workers",
            "production_interval_sec", "fog_reveal_radius", "citizens_on_build",
            "housing_capacity", "batches_per_tick", "harvest_radius", "service_radius",
            "up2_wood", "up2_stone", "up2_gold", "up5_wood", "up5_stone", "up5_gold",
        };

        private static (List<string> header, Dictionary<string, List<string>> rows) BuildingsTable()
        {
            List<string> header = null;
            var rows = new Dictionary<string, List<string>>();
            foreach (var line in File.ReadAllLines(Path))
            {
                var cells = line.Split(',').Select(c => c.Trim()).ToList();
                if (cells[0] == "building") { header = cells; continue; }
                if (header == null) continue;
                if (cells[0].Length == 0 || cells[0] == "unit") break;
                rows[cells[0]] = cells;
            }
            return (header, rows);
        }

        [Test]
        public void EveryRunBuilding_HasItsOwnRow()
        {
            var (header, rows) = BuildingsTable();
            Assert.IsNotNull(header, "The invasion tab has no buildings table.");
            var needed = InvasionRules.Buildings.Concat(new[] { InvasionRules.House, "Castle" });
            foreach (var id in needed) Assert.IsTrue(rows.ContainsKey(id), $"{id} is in a run but has no row on the invasion tab.");
        }

        [Test]
        public void TheBuildingsTable_HasEveryColumnARunUses()
        {
            var (header, _) = BuildingsTable();
            foreach (var column in RunColumns)
                Assert.IsTrue(header.Contains(column), $"The invasion tab's buildings table has no '{column}': a run would read it empty.");
        }
    }
}
