using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The film's close cameras look where the game's never does: under the roofs, under the market
    /// cloths, up into a figure's head from below (the user, 2026-11-11: «видно насквозь»). The models
    /// are made for the game's top-down view and leave those faces out.
    ///
    /// Drawing every material with culling off (the first try) made the back of every flush detail --
    /// a window on a wall, a roof's tiles on its slab -- fight the surface it lies on, and windows,
    /// doors and roofs flickered (the user, 2026-11-11). So the front stays as it is, and each
    /// renderer gets a twin drawing ONLY the back faces, pushed a little deeper (BuildingDetail's
    /// depth offset): wherever a front face covers the same spot it wins, and the back shows only
    /// through what was a hole. One copy per material, shared; the game's own materials untouched.
    /// </summary>
    public static class CineTwoSided
    {
        private const float OffsetFactor = 1f;
        private const float OffsetUnits = 4f;

        private static readonly Dictionary<Material, Material> Backs = new Dictionary<Material, Material>();

        public static void Apply(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.name == "Back") continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mats = r.sharedMaterials;
                var backs = new Material[mats.Length];
                var ok = mats.Length > 0;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    // every slot needs its back, or the twin would draw a slot with no material
                    if (m == null || !m.HasProperty("_Cull") || !m.HasProperty("_OffsetUnits") || m.GetFloat("_Cull") == 0f)
                    {
                        ok = false;
                        break;
                    }
                    if (!Backs.TryGetValue(m, out var back) || back == null)
                    {
                        back = new Material(m) { name = m.name + " (back)" };
                        back.SetFloat("_Cull", (float)CullMode.Front);
                        back.SetFloat("_OffsetFactor", OffsetFactor);
                        back.SetFloat("_OffsetUnits", OffsetUnits);
                        Backs[m] = back;
                    }
                    backs[i] = back;
                }
                if (!ok) continue;
                var twin = new GameObject("Back", typeof(MeshFilter), typeof(MeshRenderer));
                twin.transform.SetParent(r.transform, false);
                twin.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var tr = twin.GetComponent<MeshRenderer>();
                tr.sharedMaterials = backs;
                tr.shadowCastingMode = ShadowCastingMode.Off;
                tr.receiveShadows = r.receiveShadows;
            }
        }
    }
}
