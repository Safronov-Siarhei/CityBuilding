using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// What the pen-and-ink placement marks (SketchMarks) are made of: a little noise to make a
    /// stroke wander, and see-through unlit materials. Nothing here allocates per call except
    /// Make, which callers cache.
    /// </summary>
    internal static class Ink
    {
        internal static float Hash(int a, int b, int seed)
        {
            unchecked
            {
                var h = (uint)(a * 374761393 + b * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Smooth noise along a line, 0..1.</summary>
        internal static float Smooth(float x, int seed)
        {
            var i = Mathf.FloorToInt(x);
            var f = x - i;
            f = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash(i, 0, seed), Hash(i + 1, 0, seed), f);
        }

        /// <summary>A see-through unlit material of this colour over this texture, both faces drawn.</summary>
        internal static Material Make(Texture2D texture, Color color)
        {
            var material = new Material(Core.RuntimeShaders.Unlit) { color = color, mainTexture = texture };
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }
    }
}
