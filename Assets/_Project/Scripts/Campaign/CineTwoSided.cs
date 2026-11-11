using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The film's close cameras look where the game's never does: under the roofs, under the market
    /// cloths, up into a figure's head from below (the user, 2026-11-11: «видно насквозь»). The models
    /// are made for the game's top-down view and leave those faces out, so in the film their
    /// materials draw both sides (URP Lit's _Cull = Off). One copy per material, shared; the game's own
    /// materials are left as they are.
    /// </summary>
    public static class CineTwoSided
    {
        private static readonly Dictionary<Material, Material> Copies = new Dictionary<Material, Material>();

        public static void Apply(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer) continue;
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_Cull") || m.GetFloat("_Cull") == 0f) continue;
                    if (!Copies.TryGetValue(m, out var copy) || copy == null)
                    {
                        copy = new Material(m) { name = m.name + " (two-sided)" };
                        copy.SetFloat("_Cull", 0f);
                        Copies[m] = copy;
                    }
                    mats[i] = copy;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }
    }
}
