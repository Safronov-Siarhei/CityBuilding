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

        // The pieces of levels 2..5, four per level in Straight, Corner, T, Cross order: level 2 is
        // the stone wall (FortifiedFence1, the user 2026-10-23), levels 3-5 grow it (Fence1-lvl3..5,
        // the user 2026-10-28: every level its own model). A level with no straight run modelled
        // shows the nearest modelled level below it.
        [SerializeField] private GameObject[] levelModels = new GameObject[0];

        public const int Shapes = 4;

        private int _look = 1;
        private bool _north, _east, _south, _west;

        public void SetLevelModels(GameObject[] models) => levelModels = models;

        /// <summary>Whether this level has a look of its own: level 1 the wood, any level above it whose pieces were modelled.</summary>
        public bool HasOwnModel(int level) => level <= 1 || LookFor(level) == level;

        /// <summary>The level's own pieces (or the nearest modelled level below it), keeping the shape the neighbours give it.</summary>
        public void SetLevel(int level)
        {
            var look = LookFor(level);
            if (look == _look) return;
            _look = look;
            Apply(_north, _east, _south, _west);
        }

        private int LookFor(int level)
        {
            for (var l = level; l >= 2; l--)
            {
                if (Piece(l, 0) != null) return l;
            }
            return 1;
        }

        private GameObject Piece(int level, int shape)
        {
            var i = (level - 2) * Shapes + shape;
            return levelModels != null && i >= 0 && i < levelModels.Length ? levelModels[i] : null;
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
            foreach (var model in new[] { straightModel, cornerModel, tModel, crossModel })
            {
                if (model != null && model != chosen && model.activeSelf) model.SetActive(false);
            }
            if (levelModels != null)
            {
                foreach (var model in levelModels)
                {
                    if (model != null && model != chosen && model.activeSelf) model.SetActive(false);
                }
            }
            if (chosen == null) return;

            if (!chosen.activeSelf) chosen.SetActive(true);
            // World rotation, so the shape stays correct whatever the root was placed at.
            chosen.transform.rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
        }

        private GameObject Model(FenceVariant variant)
        {
            if (_look >= 2)
            {
                switch (variant)
                {
                    case FenceVariant.Corner: return Piece(_look, 1);
                    case FenceVariant.T: return Piece(_look, 2);
                    case FenceVariant.Cross: return Piece(_look, 3);
                    default: return Piece(_look, 0);
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
