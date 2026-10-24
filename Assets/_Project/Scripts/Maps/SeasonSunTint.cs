using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>Keeps the sun's own colour, so a season's tint is laid on it, never on the last season's.</summary>
    public class SeasonSunTint : MonoBehaviour
    {
        private Color _own;
        private bool _known;

        public void Apply(Color tint)
        {
            var sun = GetComponent<Light>();
            if (sun == null) return;
            if (!_known)
            {
                _own = sun.color;
                _known = true;
            }
            sun.color = _own * tint;
        }
    }
}
