using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// An object FogCulling hides while it lies under the fog: its renderers are switched off --
    /// only those it found on, so a renderer something else had switched off stays off when the
    /// fog opens -- and back on once any of its cells is revealed.
    /// </summary>
    public class FogCulled : MonoBehaviour
    {
        /// <summary>How far round its middle, in cells, a revealed cell counts as seeing it (a building's half-size, a camp's radius).</summary>
        public int RadiusCells = 1;

        public bool Hidden { get; private set; }

        private static readonly List<Renderer> Scratch = new List<Renderer>();
        private readonly List<Renderer> _off = new List<Renderer>();

        private void OnEnable() => FogCulling.Add(this);

        private void OnDisable()
        {
            FogCulling.Remove(this);
            Show();
        }

        internal void Apply(bool seen)
        {
            if (seen == !Hidden) return;
            if (seen) Show();
            else Hide();
        }

        private void Hide()
        {
            Hidden = true;
            _off.Clear();
            GetComponentsInChildren(false, Scratch);
            for (var i = 0; i < Scratch.Count; i++)
            {
                var r = Scratch[i];
                if (r == null || !r.enabled) continue;
                r.enabled = false;
                _off.Add(r);
            }
            Scratch.Clear();
        }

        private void Show()
        {
            Hidden = false;
            for (var i = 0; i < _off.Count; i++)
                if (_off[i] != null) _off[i].enabled = true;
            _off.Clear();
        }
    }
}
