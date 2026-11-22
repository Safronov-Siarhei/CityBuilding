using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The noise the film's ground (CineGround.shader) paints its grass zones, ruts and ripples with, baked once into a
    /// tiling texture (global _CineFbm) instead of being computed per pixel. Measured 2026-11-18 on the Redmi Pad
    /// (Mali-G57): the main menu at 16 fps, the GPU at 60 ms a frame, and the ground's per-pixel noise (up to five
    /// four-octave value-noise sums, ~80 sine hashes a pixel) the bulk of it; the menu, the film and mission 1 all
    /// stand on this ground.
    /// The texture holds a four-octave value-noise sum (weights 1/2..1/16, octaves 1x..8x), periodic over Period
    /// lattice cells so it tiles; the shader's Fbm(p) is one sample at p / Period. Same statistics as the old code
    /// (octaves at 2x rather than 2.07x), so the thresholds that cut zones out of it cover the same share of ground.
    /// </summary>
    public static class CineGroundNoise
    {
        public const int Period = 64;      // lattice cells across the tile (the shader divides by the same number)
        private const int PerCell = 8;     // texels a cell at the first octave: the fourth octave is one texel a cell
        private static Texture2D _tex;

        // Not before the first scene any more (the user, 2026-11-22: a long blank screen before the studio logo): the
        // fbm tile (a million lerps) and the painted tiles are made while the splash holds its finished logo
        // (SplashInk), or as any other scene loads -- whichever comes first; Ensure is cheap once done.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name != "Splash") Ensure();
            };
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorBoot() => Ensure();
#endif

        public static void Ensure()
        {
            Tiles();
            if (_tex != null) { Shader.SetGlobalTexture("_CineFbm", _tex); return; }
            var size = Period * PerCell;
            var px = new byte[size * size];
            var sum = new float[size * size];
            var rnd = new System.Random(20261118);
            var weight = 0.5f;
            for (var k = 0; k < 4; k++, weight *= 0.5f)
            {
                var cells = Period << k;                       // lattice points across the tile at this octave
                var lattice = new float[cells * cells];
                for (var i = 0; i < lattice.Length; i++) lattice[i] = (float)rnd.NextDouble();
                var texelsPerCell = (float)size / cells;
                for (var y = 0; y < size; y++)
                {
                    var fy = y / texelsPerCell;
                    var iy = (int)fy;
                    var ty = Smooth(fy - iy);
                    var y0 = (iy % cells) * cells;
                    var y1 = ((iy + 1) % cells) * cells;
                    for (var x = 0; x < size; x++)
                    {
                        var fx = x / texelsPerCell;
                        var ix = (int)fx;
                        var tx = Smooth(fx - ix);
                        var x0 = ix % cells;
                        var x1 = (ix + 1) % cells;
                        var a = Mathf.Lerp(lattice[y0 + x0], lattice[y0 + x1], tx);
                        var b = Mathf.Lerp(lattice[y1 + x0], lattice[y1 + x1], tx);
                        sum[y * size + x] += weight * Mathf.Lerp(a, b, ty);
                    }
                }
            }
            for (var i = 0; i < px.Length; i++) px[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(sum[i] * 255f), 0, 255);
            _tex = new Texture2D(size, size, TextureFormat.R8, true, true)
            {
                name = "CineFbm", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            _tex.SetPixelData(px, 0);
            _tex.Apply(true, true);
            Shader.SetGlobalTexture("_CineFbm", _tex);
        }

        // The painted ground tiles (Tools/textures/cine_ground_textures.py, the user's approved concept 2026-11-21):
        // the setts, and the grass's and dirt's colour multipliers. Trilinear + a little anisotropy: the play camera
        // looks at the ground at a slant.
        private static readonly (string file, string global)[] TileFiles =
        {
            ("CineGround/Cobble", "_CineCobble"), ("CineGround/GrassDetail", "_CineGrassDetail"), ("CineGround/DirtDetail", "_CineDirtDetail"),
        };

        // HELD here and marked DontUnloadUnusedAsset: a global shader texture is no reference, and the scene load's
        // unloading of unused assets dropped them -- the first build showed the ground flat grey (2026-11-21)
        private static readonly Texture2D[] _tiles = new Texture2D[TileFiles.Length];

        private static void Tiles()
        {
            for (var i = 0; i < TileFiles.Length; i++)
            {
                var (file, global) = TileFiles[i];
                var t = _tiles[i] != null ? _tiles[i] : UnityEngine.Resources.Load<Texture2D>(file);
                if (t == null) { Debug.LogWarning("CineGroundNoise: missing Resources/" + file); continue; }
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Trilinear;
                t.anisoLevel = 4;
                t.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                _tiles[i] = t;
                Shader.SetGlobalTexture(global, t);
            }
        }

        private static float Smooth(float f) => f * f * (3f - 2f * f);
    }
}
