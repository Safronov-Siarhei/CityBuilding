using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// How long the Invasion run has gone on, on a plaque under the minimap (the user, 2026-10-28:
    /// «чтобы игрок понимал сколько времени длится забег»). Hidden outside a run; the text is
    /// written only when the second changes.
    /// </summary>
    public class RunClockLabel : MonoBehaviour
    {
        [SerializeField] private GameObject plaque;
        [SerializeField] private Text time;

        private int _shown = -1;

        private void LateUpdate()
        {
            var mode = InvasionMode.Instance;
            var on = InvasionMode.IsActive && mode.RunStarted && mode.Score != null;
            if (plaque.activeSelf != on) plaque.SetActive(on);
            if (!on) return;
            var seconds = Mathf.FloorToInt(mode.Score.RunSeconds);
            if (seconds == _shown) return;
            _shown = seconds;
            time.text = InvasionScore.Clock(seconds);
        }

        /// <summary>What it reads now (for the tests).</summary>
        public string Text => plaque.activeSelf ? time.text : string.Empty;
    }
}
