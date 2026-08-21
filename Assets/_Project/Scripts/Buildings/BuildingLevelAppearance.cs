using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Shows the model that belongs to a building's current upgrade level.
    ///
    /// Every level's model is a child of the prefab, built once at project setup, and upgrading
    /// only swaps which one is active -- the building itself is never re-instantiated, so its
    /// collider, NavMesh obstacle, workers and damage survive the upgrade untouched.
    ///
    /// Levels the artist hasn't modelled yet fall back to the highest level that does have a model,
    /// by explicit request: a building must never vanish because a file isn't drawn yet. A level-3
    /// wall with only a level-1 model looks like level 1 and fights like level 3.
    /// </summary>
    public class BuildingLevelAppearance : MonoBehaviour
    {
        [SerializeField] private List<GameObject> levelModels = new List<GameObject>();

        // Parallel to levelModels: how tall each one measured at project setup. Zero where that
        // level has no model of its own, and never read in that case -- the fallback below hands
        // back the level actually being shown, and its height is the one the click box wants.
        [SerializeField] private List<float> levelHeights = new List<float>();

        /// <summary>Set by SetupProject as it builds the prefab. Entries may be null where that level has no model of its own.</summary>
        public void SetModels(List<GameObject> models, List<float> heights)
        {
            levelModels = models;
            levelHeights = heights;
        }

        public void Apply(int level)
        {
            var index = Resolve(level);
            if (index < 0) return;

            var chosen = levelModels[index];
            foreach (var model in levelModels)
            {
                if (model == null) continue;
                var shouldBeActive = model == chosen;
                if (model.activeSelf != shouldBeActive) model.SetActive(shouldBeActive);
            }

            ResizeClickBox(index);
        }

        /// <summary>
        /// Keeps the box a player's finger meets the height of the model actually on screen. A
        /// level-3 tower drawn twice as tall as its level-1 stub is otherwise clickable only around
        /// the ankles, and a building whose model SHRANK with the level leaves an invisible box
        /// above it eating taps meant for the ground -- the same failure AuthoredModelTests was
        /// written for, arriving one upgrade later.
        ///
        /// Only the height moves. The footprint stays whatever level 1 measured (see
        /// SetupProject.AddLevelModels), so the cells this building occupies never change under a
        /// town that has already been built around it.
        /// </summary>
        private void ResizeClickBox(int index)
        {
            if (levelHeights == null || index >= levelHeights.Count) return;

            var height = levelHeights[index];
            if (height <= 0.01f) return;

            var box = GetComponent<BoxCollider>();
            if (box == null) return;

            box.size = new Vector3(box.size.x, height, box.size.z);
            box.center = new Vector3(box.center.x, height * 0.5f, box.center.z);
        }

        /// <summary>The index of the model for this level, or of the nearest one below it -- see the fallback rule in the class summary. -1 when there are no models at all.</summary>
        private int Resolve(int level)
        {
            if (levelModels == null || levelModels.Count == 0) return -1;

            var index = Mathf.Clamp(level, 1, levelModels.Count) - 1;
            for (var i = index; i >= 0; i--)
            {
                if (levelModels[i] != null) return i;
            }

            // Nothing at or below this level: take the first model that exists at all, so a building
            // whose level 1 is somehow missing still shows something rather than nothing.
            for (var i = 0; i < levelModels.Count; i++)
            {
                if (levelModels[i] != null) return i;
            }
            return -1;
        }
    }
}
