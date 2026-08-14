using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The visible half of autotiling: holds both fence models as children and shows whichever one
    /// matches the neighbours, turned the right way. FenceNetwork decides when to call Apply.
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

        /// <summary>Set by SetupProject when it builds the prefab -- the models are FBX instances, not something authored in an inspector.</summary>
        public void SetModels(GameObject straight, GameObject corner)
        {
            straightModel = straight;
            cornerModel = corner;
        }

        public void Apply(bool north, bool east, bool south, bool west)
        {
            FenceShape.Resolve(north, east, south, west, out var variant, out var rotationSteps);

            var chosen = variant == FenceVariant.Corner ? cornerModel : straightModel;
            var other = variant == FenceVariant.Corner ? straightModel : cornerModel;

            if (other != null && other.activeSelf) other.SetActive(false);
            if (chosen == null) return;

            if (!chosen.activeSelf) chosen.SetActive(true);
            // World rotation, so the shape stays correct whatever the root was placed at.
            chosen.transform.rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
        }
    }
}
