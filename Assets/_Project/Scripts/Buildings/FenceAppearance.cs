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

        // The stone pieces (FortifiedFence1) an upgraded fence turns into from level 2 (the user,
        // 2026-10-23: the upgrade drew a silver disc over the wood instead of a new fence).
        [SerializeField] private GameObject stoneStraightModel;
        [SerializeField] private GameObject stoneCornerModel;
        [SerializeField] private GameObject stoneTModel;
        [SerializeField] private GameObject stoneCrossModel;

        private bool _stone;
        private bool _north, _east, _south, _west;

        public void SetStoneModels(GameObject straight, GameObject corner, GameObject t, GameObject cross)
        {
            stoneStraightModel = straight;
            stoneCornerModel = corner;
            stoneTModel = t;
            stoneCrossModel = cross;
        }

        /// <summary>Whether this level has a look of its own: level 1 the wood, the stone above it when it was modelled.</summary>
        public bool HasOwnModel(int level) => level <= 1 || stoneStraightModel != null;

        /// <summary>Wood at level 1, stone above it (when modelled), keeping the shape the neighbours give it.</summary>
        public void SetLevel(int level)
        {
            var stone = level >= 2 && stoneStraightModel != null;
            if (stone == _stone) return;
            _stone = stone;
            Apply(_north, _east, _south, _west);
        }

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
            _north = north;
            _east = east;
            _south = south;
            _west = west;
            FenceShape.Resolve(north, east, south, west, out var variant, out var rotationSteps);
            if (Model(variant) == null)
            {
                FenceShape.ResolveWithoutJunctions(north, east, south, west, out variant, out rotationSteps);
            }

            var chosen = Model(variant);
            foreach (var model in new[] { straightModel, cornerModel, tModel, crossModel, stoneStraightModel, stoneCornerModel, stoneTModel, stoneCrossModel })
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
            if (_stone)
            {
                switch (variant)
                {
                    case FenceVariant.Corner: return stoneCornerModel;
                    case FenceVariant.T: return stoneTModel;
                    case FenceVariant.Cross: return stoneCrossModel;
                    default: return stoneStraightModel;
                }
            }
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
