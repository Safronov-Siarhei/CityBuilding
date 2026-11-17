using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A button that breathes, so the player wants to press it (the user, 2026-11-17, on «В бой»): it swells and
    /// settles once a beat, its glow behind brightening on the swell. Unscaled time: it beats in a paused menu too.
    /// </summary>
    public class ButtonPulse : MonoBehaviour
    {
        [SerializeField] private Image glow;
        [SerializeField] private float beatSeconds = 1.3f;
        [SerializeField] private float swell = 0.055f;
        [SerializeField] private Vector2 glowAlpha = new Vector2(0.15f, 0.75f);

        private Vector3 _scale = Vector3.one;

        private void Awake() => _scale = transform.localScale;

        private void OnDisable() => transform.localScale = _scale;

        private void Update()
        {
            // a quick swell, a slow settle: a heartbeat rather than a sine's even wobble
            var t = Mathf.Repeat(Time.unscaledTime / Mathf.Max(0.2f, beatSeconds), 1f);
            var beat = t < 0.25f ? Mathf.Sin(t / 0.25f * Mathf.PI * 0.5f) : Mathf.Cos((t - 0.25f) / 0.75f * Mathf.PI * 0.5f);
            beat *= beat;
            transform.localScale = _scale * (1f + swell * beat);
            if (glow == null) return;
            var c = glow.color;
            c.a = Mathf.Lerp(glowAlpha.x, glowAlpha.y, beat);
            glow.color = c;
        }
    }
}
