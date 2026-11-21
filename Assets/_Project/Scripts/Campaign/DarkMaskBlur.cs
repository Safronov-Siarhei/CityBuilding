using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The darkness shader's soft edge (DungeonDark: the mask blurred by 9 bilinear taps, `_Soft`
    /// texels out) worked out ONCE on the CPU for a mask that never changes (mission 1's corridor,
    /// mission 2's lit town), at <see cref="Up"/> x the mask's resolution so the kinks of the
    /// original sit between texels. The shader then reads one tap (`_PreBlurred` = Up). Redmi Pad,
    /// 2026-11-21: the 9 taps over the whole screen were ~5.5 ms of mission 1's GPU.
    /// The dungeon's mask changes as it is explored and keeps the 9 taps.
    /// </summary>
    public static class DarkMaskBlur
    {
        public const int Up = 2;

        /// <summary>px: the w x h mask (bytes, row by row); soft: the shader's _Soft in mask texels.</summary>
        public static Texture2D Blurred(byte[] px, int w, int h, float soft, string name)
        {
            var bw = w * Up;
            var bh = h * Up;
            var outPx = new byte[bw * bh];
            var s = soft;
            var s2 = soft * 1.6f;
            for (var j = 0; j < bh; j++)
            {
                var y = (j + 0.5f) / Up;
                for (var i = 0; i < bw; i++)
                {
                    var x = (i + 0.5f) / Up;
                    var v = (Open(px, w, h, x, y) * 2f
                             + Open(px, w, h, x + s, y + s) + Open(px, w, h, x - s, y + s)
                             + Open(px, w, h, x + s, y - s) + Open(px, w, h, x - s, y - s)
                             + Open(px, w, h, x + s2, y) + Open(px, w, h, x - s2, y)
                             + Open(px, w, h, x, y + s2) + Open(px, w, h, x, y - s2)) / 10f;
                    outPx[j * bw + i] = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
                }
            }
            var tex = new Texture2D(bw, bh, TextureFormat.R8, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
            tex.SetPixelData(outPx, 0);
            tex.Apply(false);
            return tex;
        }

        /// <summary>The shader's Open(): 0 off the mask, else a bilinear read (texel centres at k + 0.5, clamped).</summary>
        private static float Open(byte[] px, int w, int h, float x, float y)
        {
            if (x < 0f || y < 0f || x > w || y > h) return 0f;
            var fx = x - 0.5f;
            var fy = y - 0.5f;
            var i0 = Mathf.FloorToInt(fx);
            var j0 = Mathf.FloorToInt(fy);
            var tx = fx - i0;
            var ty = fy - j0;
            var i1 = Mathf.Clamp(i0 + 1, 0, w - 1);
            var j1 = Mathf.Clamp(j0 + 1, 0, h - 1);
            i0 = Mathf.Clamp(i0, 0, w - 1);
            j0 = Mathf.Clamp(j0, 0, h - 1);
            var a = Mathf.Lerp(px[j0 * w + i0], px[j0 * w + i1], tx);
            var b = Mathf.Lerp(px[j1 * w + i0], px[j1 * w + i1], tx);
            return Mathf.Lerp(a, b, ty) / 255f;
        }
    }
}
