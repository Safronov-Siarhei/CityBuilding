using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Core
{
    /// <summary>
    /// Takes an object out of the shadow pass while leaving it lit and still shadow-RECEIVING.
    ///
    /// **Why this exists.** Every renderer that casts is drawn a SECOND time, into the shadow map,
    /// so scene geometry is paid for roughly twice. Counted against a mature town, the fixed
    /// scenery -- the ground mesh alone is 35 728 triangles, plus ~250 visible trees, ~40 rock
    /// clusters and up to 150 two-cube citizens -- comes to something like 83 000 triangles before
    /// a single building is drawn, and the shadow pass takes that to roughly the whole triangle
    /// budget of the weakest phone we test on. None of those shadows are worth that:
    ///
    /// - The GROUND is the floor. Nothing is under it to receive its shadow, so it casts into
    ///   nothing. It still RECEIVES, which is the half that matters -- buildings keep their
    ///   shadows on the ground.
    /// - TREES, ROCKS and CITIZENS are small and, at this camera angle and zoom, their own shadows
    ///   read as noise rather than as depth.
    ///
    /// Buildings deliberately keep casting: they are the objects whose shadow tells the player
    /// where a thing stands and how tall it is.
    /// </summary>
    public static class SceneryShadows
    {
        /// <summary>Stops root and everything under it from casting, including renderers on disabled objects (a sapling's, a building level's spare model).</summary>
        public static void DisableCasting(GameObject root)
        {
            if (root == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
