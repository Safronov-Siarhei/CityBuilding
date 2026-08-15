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

        /// <summary>Set by SetupProject as it builds the prefab. Entries may be null where that level has no model of its own.</summary>
        public void SetModels(List<GameObject> models)
        {
            levelModels = models;
        }

        public void Apply(int level)
        {
            var chosen = Resolve(level);
            if (chosen == null) return;

            foreach (var model in levelModels)
            {
                if (model == null) continue;
                var shouldBeActive = model == chosen;
                if (model.activeSelf != shouldBeActive) model.SetActive(shouldBeActive);
            }
        }

        /// <summary>The model for this level, or the nearest one below it -- see the fallback rule in the class summary.</summary>
        private GameObject Resolve(int level)
        {
            if (levelModels == null || levelModels.Count == 0) return null;

            var index = Mathf.Clamp(level, 1, levelModels.Count) - 1;
            for (var i = index; i >= 0; i--)
            {
                if (levelModels[i] != null) return levelModels[i];
            }

            // Nothing at or below this level: take the first model that exists at all, so a building
            // whose level 1 is somehow missing still shows something rather than nothing.
            foreach (var model in levelModels)
            {
                if (model != null) return model;
            }
            return null;
        }
    }
}
