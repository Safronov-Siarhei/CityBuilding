using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>Mirrors EventLogManager.Entries into a single multi-line Text, refreshed whenever a new entry is logged.</summary>
    public class EventLogController : MonoBehaviour
    {
        [SerializeField] private Text logText;

        private void Start()
        {
            if (EventLogManager.Instance != null) EventLogManager.Instance.OnLogChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (logText == null || EventLogManager.Instance == null) return;
            logText.text = string.Join("\n", EventLogManager.Instance.Entries);
        }
    }
}
