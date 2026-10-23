using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The Captain's Сила (the user, 2026-10-23): for a while every ally near him -- soldiers,
    /// heroes, and the people when the Боевой дух has them fighting -- strikes harder. The aura
    /// moves with him; a blow asks MightAt where it is struck. Over at the end of a run (Clear).
    /// </summary>
    public static class HeroAuras
    {
        private struct Aura
        {
            public Transform Centre;
            public float RadiusSq;
            public float Factor;
            public float Until;
        }

        private static readonly List<Aura> Auras = new List<Aura>();

        public static void Might(Transform centre, float radius, float factor, float seconds)
        {
            Auras.Add(new Aura { Centre = centre, RadiusSq = radius * radius, Factor = Mathf.Max(1f, factor), Until = Time.time + seconds });
        }

        /// <summary>How much harder a blow struck here lands: 1 with no Сила about.</summary>
        public static float MightAt(Vector3 position)
        {
            if (Auras.Count == 0) return 1f;
            var factor = 1f;
            for (var i = Auras.Count - 1; i >= 0; i--)
            {
                var aura = Auras[i];
                if (aura.Centre == null || Time.time >= aura.Until)
                {
                    Auras.RemoveAt(i);
                    continue;
                }
                var d = aura.Centre.position - position;
                d.y = 0f;
                if (d.sqrMagnitude <= aura.RadiusSq) factor = Mathf.Max(factor, aura.Factor);
            }
            return factor;
        }

        public static bool AnyActive => Auras.Count > 0;

        public static void Clear() => Auras.Clear();
    }
}
