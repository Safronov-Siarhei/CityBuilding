using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A call-to-action button breathing to catch the eye (the user, 2026-11-17, the sandbox's «Начать»: no glow, a
    /// smooth pulse with a small turn of a couple of degrees): its scale swells and settles while it rocks a little,
    /// a quarter of a beat out of step. Unscaled time, so a paused game still breathes.
    /// </summary>
    public class ButtonBreath : MonoBehaviour
    {
        [SerializeField] private float period = 1.8f;
        [SerializeField] private float scale = 0.04f;
        [SerializeField] private float degrees = 2f;

        private void Update()
        {
            var t = Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, period);
            transform.localScale = Vector3.one * (1f + scale * Mathf.Sin(t));
            transform.localRotation = Quaternion.Euler(0f, 0f, degrees * Mathf.Cos(t));
        }

        private void OnDisable()
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }
    }
}
