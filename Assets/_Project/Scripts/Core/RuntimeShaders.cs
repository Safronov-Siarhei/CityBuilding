using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// The two URP shaders this game builds materials from at runtime, resolved once and loudly.
    ///
    /// **This exists because of a bug that only ever appeared on a real device.** Roughly twenty
    /// places call `new Material(Shader.Find(...))` to build an indicator out of primitives -- the
    /// harvest progress bar, the health bar, the radius carpet, the cell highlight. In the editor
    /// `Shader.Find` searches every shader in the project and always succeeds. In a PLAYER it can
    /// only find shaders that were built into the player, and a shader gets built in only if some
    /// material in a scene references it.
    ///
    /// Every generated material in this project is Lit, so URP/Lit travelled into the build and
    /// worked. **URP/Unlit was referenced by nothing**, so it was never built in, `Shader.Find`
    /// returned null, and Unity drew every one of those indicators with its magenta error shader.
    /// The first Android build was covered in pink rectangles; not one test could have caught it,
    /// because in the editor there was nothing to catch.
    ///
    /// Two things keep it fixed: SetupProject writes both shaders into Graphics Settings' Always
    /// Included Shaders (which is what actually puts them in the build -- see
    /// AlwaysIncludedShaderTests), and the lookup below refuses to fail silently ever again.
    /// </summary>
    public static class RuntimeShaders
    {
        public const string LitName = "Universal Render Pipeline/Lit";
        public const string UnlitName = "Universal Render Pipeline/Unlit";

        private static Shader _lit;
        private static Shader _unlit;
        private static bool _litReported;
        private static bool _unlitReported;

        /// <summary>The standard lit shader, for anything meant to catch the scene's light -- citizens, rocks, orcs, markers with volume.</summary>
        public static Shader Lit => Resolve(LitName, ref _lit, ref _litReported);

        /// <summary>The unlit shader, for flat indicators that must read the same from every angle -- progress bars, the radius carpet, cell highlights.</summary>
        public static Shader Unlit => Resolve(UnlitName, ref _unlit, ref _unlitReported);

        private static Shader Resolve(string shaderName, ref Shader cached, ref bool reported)
        {
            // Shader.Find walks the loaded shaders, so it is worth caching -- these are asked for
            // once per spawned citizen, per lit boulder, per progress bar.
            if (cached != null) return cached;

            cached = Shader.Find(shaderName);
            if (cached == null && !reported)
            {
                // Once, not per call: a missing shader means hundreds of objects are about to ask
                // for it, and a log line per object would bury the one that matters.
                reported = true;
                Debug.LogError($"[RuntimeShaders] '{shaderName}' is not in this build. Everything built from it will render magenta. " +
                               "Add it to Project Settings > Graphics > Always Included Shaders (SetupProject does this).");
            }
            return cached;
        }
    }
}
