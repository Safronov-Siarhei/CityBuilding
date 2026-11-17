using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Invasion hall's rating medallion, alive so the player sees it can be pressed (the user, 2026-11-17: «иначе
    /// игрок никогда не поймёт что может нажать»): it sways a little on its chains from the beam, its rays turn behind
    /// it, a glint runs round its rim now and then, its glow breathes and its sparkles twinkle. Unscaled time.
    /// </summary>
    public class MedallionMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform swing;      // pivoted at the chains' eyes
        [SerializeField] private RectTransform rays;
        [SerializeField] private RectTransform glint;
        [SerializeField] private Image glow;
        [SerializeField] private Image sparkles;
        [SerializeField] private float swayDegrees = 1.6f;
        [SerializeField] private float swaySeconds = 3.6f;
        [SerializeField] private float raysDegreesPerSecond = 9f;
        [SerializeField] private float glintSeconds = 3.2f;

        private void Update()
        {
            var t = Time.unscaledTime;
            if (swing != null) swing.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f / swaySeconds) * swayDegrees);
            if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, -t * raysDegreesPerSecond);
            if (glint != null)
            {
                // one quick run round the rim, then a rest
                var phase = Mathf.Repeat(t / glintSeconds, 1f);
                var run = Mathf.Clamp01(phase / 0.35f);
                glint.localRotation = Quaternion.Euler(0f, 0f, -run * 360f);
                var g = glint.GetComponent<Image>();
                if (g != null) { var c = g.color; c.a = phase < 0.35f ? Mathf.Sin(run * Mathf.PI) : 0f; g.color = c; }
            }
            if (glow != null) { var c = glow.color; c.a = 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(t * 2.4f)); glow.color = c; }
            if (sparkles != null) { var c = sparkles.color; c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 1.7f)); sparkles.color = c; }
        }
    }
}
