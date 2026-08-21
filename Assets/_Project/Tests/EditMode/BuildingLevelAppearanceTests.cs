using System.Collections.Generic;
using CityBuilder.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The rules that decide what an upgraded building looks like, and how big the box a player's
    /// finger meets is once it does.
    ///
    /// Both matter long before the art exists. Levels 2 and 3 have no models at all yet, so the
    /// fallback -- show the highest level that HAS one -- is the path every building in the game
    /// takes today, and it must never leave a building drawing nothing. The click box matters the
    /// day the first level-2 model lands: a model that grew and a box that did not is a roof
    /// nobody can tap, and a model that shrank leaves an invisible box eating taps meant for the
    /// ground next to it.
    /// </summary>
    public class BuildingLevelAppearanceTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            foreach (var go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        [Test]
        public void Apply_ShowsOnlyTheModelForThatLevel()
        {
            var appearance = Building(out var models, 2f, 4f, 6f);

            appearance.Apply(2);

            Assert.IsFalse(models[0].activeSelf, "Level 1's model is still drawn on a level-2 building.");
            Assert.IsTrue(models[1].activeSelf, "Level 2's model is not drawn on a level-2 building.");
            Assert.IsFalse(models[2].activeSelf, "Level 3's model is drawn on a level-2 building.");
        }

        [Test]
        public void Apply_WithNoModelForThatLevel_FallsBackToTheHighestBelowIt()
        {
            // The state of every building in the game today: level 1 drawn, nothing above it.
            var appearance = Building(out var models, 2f, 0f, 0f);

            appearance.Apply(3);

            Assert.IsTrue(models[0].activeSelf, "A level-3 building with only a level-1 model stopped drawing anything at all.");
        }

        [Test]
        public void Apply_SkipsAGapRatherThanShowingTheLevelAbove()
        {
            var appearance = Building(out var models, 2f, 0f, 6f);

            appearance.Apply(2);

            Assert.IsTrue(models[0].activeSelf, "Level 2 has no model of its own and should have fallen back to level 1.");
            Assert.IsFalse(models[2].activeSelf, "A level-2 building is showing level 3's model -- the fallback must only ever go downwards.");
        }

        [Test]
        public void Apply_TakesTheClickBoxToTheHeightOfTheModelOnScreen()
        {
            var appearance = Building(out _, 2f, 4f, 6f);
            var box = appearance.GetComponent<BoxCollider>();

            appearance.Apply(3);

            Assert.AreEqual(6f, box.size.y, 0.001f, "The click box is still the height of the model this building no longer shows.");
            Assert.AreEqual(3f, box.center.y, 0.001f, "The click box kept its height but not its footing -- it should stand on the ground and reach the roof.");
        }

        [Test]
        public void Apply_KeepsTheHeightOfTheModelItFellBackTo()
        {
            var appearance = Building(out _, 2f, 0f, 0f);
            var box = appearance.GetComponent<BoxCollider>();

            appearance.Apply(3);

            Assert.AreEqual(2f, box.size.y, 0.001f,
                "An undrawn level flattened the click box: the building still shows level 1's model, so it still has level 1's height.");
        }

        [Test]
        public void Apply_NeverChangesTheFootprintOfTheClickBox()
        {
            // The cells a building occupies are reserved when it is placed and built around; an
            // upgrade that widened them would have to fit into ground the player has already used.
            var appearance = Building(out _, 2f, 4f, 6f);
            var box = appearance.GetComponent<BoxCollider>();
            var before = new Vector2(box.size.x, box.size.z);

            appearance.Apply(3);

            Assert.AreEqual(before.x, box.size.x, 0.001f, "The click box grew sideways with the level.");
            Assert.AreEqual(before.y, box.size.z, 0.001f, "The click box grew sideways with the level.");
        }

        /// <summary>A building whose levels measure the given heights; a height of 0 means that level has no model of its own.</summary>
        private BuildingLevelAppearance Building(out List<GameObject> models, params float[] heights)
        {
            var root = new GameObject("TestBuilding");
            _created.Add(root);

            // Deliberately a height no level has. Start it at level 1's and every assertion about
            // the click box would pass on a building that fell back to level 1 even if Apply never
            // touched the collider at all.
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(3f, 99f, 2f);
            box.center = new Vector3(0f, 49.5f, 0f);

            models = new List<GameObject>();
            var levelHeights = new List<float>();
            for (var i = 0; i < heights.Length; i++)
            {
                if (heights[i] <= 0f)
                {
                    models.Add(null);
                    levelHeights.Add(0f);
                    continue;
                }

                var model = new GameObject($"Model_lvl{i + 1}");
                model.transform.SetParent(root.transform, false);
                model.SetActive(i == 0);
                models.Add(model);
                levelHeights.Add(heights[i]);
            }

            var appearance = root.AddComponent<BuildingLevelAppearance>();
            appearance.SetModels(models, levelHeights);
            return appearance;
        }
    }
}
