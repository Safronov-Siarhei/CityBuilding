using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The visible half of autotiling: holds the fence's models as children (straight, corner, and
    /// the T and crossroads pieces when they were modelled) and shows whichever one matches the
    /// neighbours, turned the right way. FenceNetwork decides when to call Apply.
    ///
    /// Only the model child is rotated, never this GameObject: the building's own transform carries
    /// the placement rotation the player chose, and its collider and NavMesh obstacle are aligned to
    /// it. Turning the root to fit the neighbours would turn those with it -- harmless for a square
    /// one-cell footprint today, quietly wrong for a two-cell gate later.
    /// </summary>
    public class FenceAppearance : MonoBehaviour
    {
        [SerializeField] private GameObject straightModel;
        [SerializeField] private GameObject cornerModel;
        [SerializeField] private GameObject tModel;
        [SerializeField] private GameObject crossModel;

        /// <summary>Set by SetupProject when it builds the prefab -- the models are FBX instances, not something authored in an inspector. `t` and `cross` may be null: the junctions then fall back to the straight run.</summary>
        public void SetModels(GameObject straight, GameObject corner, GameObject t = null, GameObject cross = null)
        {
            straightModel = straight;
            cornerModel = corner;
            tModel = t;
            crossModel = cross;
        }

        public void Apply(bool north, bool east, bool south, bool west)
        {
            FenceShape.Resolve(north, east, south, west, out var variant, out var rotationSteps);
            if (Model(variant) == null)
            {
                FenceShape.ResolveWithoutJunctions(north, east, south, west, out variant, out rotationSteps);
            }

            var chosen = Model(variant);
            foreach (var model in new[] { straightModel, cornerModel, tModel, crossModel })
            {
                if (model != null && model != chosen && model.activeSelf) model.SetActive(false);
            }
            if (chosen == null) return;

            if (!chosen.activeSelf) chosen.SetActive(true);
            // World rotation, so the shape stays correct whatever the root was placed at.
            chosen.transform.rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
        }

        private GameObject Model(FenceVariant variant)
        {
            switch (variant)
            {
                case FenceVariant.Corner: return cornerModel;
                case FenceVariant.T: return tModel;
                case FenceVariant.Cross: return crossModel;
                default: return straightModel;
            }
        }
    }
}
