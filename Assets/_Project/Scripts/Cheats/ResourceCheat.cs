using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>
    /// Testing tool: sets one resource to an exact amount. Configure it in the Inspector on
    /// GameCheats/Resources and press Apply (added by ResourceCheatEditor) during Play.
    ///
    /// Two conflicts with the live economy, both handled rather than ignored:
    /// - Production keeps ticking and the player keeps spending, so a value set once immediately
    ///   starts drifting. `keepLocked` pins it: the amount is re-applied whenever anything changes
    ///   that resource, so it stays put while you test something else.
    /// - ResourceManager's own F9 InfiniteResources debug toggle makes every cost free and shows
    ///   the HUD as "∞". That's a separate switch and takes precedence visually -- if it's on,
    ///   setting an exact amount here won't appear to do anything.
    ///
    /// Event-driven, with no Update: idle cost is zero, which matters because this ships in the
    /// generated scene like everything else.
    /// </summary>
    public class ResourceCheat : MonoBehaviour
    {
        [SerializeField] private ResourceType resource = ResourceType.Wood;

        [SerializeField, Min(0)] private int amount = 500;

        [Tooltip("Держать ресурс на заданном значении: производство и траты его больше не сдвинут.")]
        [SerializeField] private bool keepLocked;

        // Guards the re-entrancy the lock would otherwise cause: SetAmount raises
        // OnResourceChanged, which is the very event this listens to.
        private bool _applying;

        private void OnEnable()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
        }

        private void OnDisable()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
        }

        /// <summary>Sets the selected resource to the configured amount. Bound to the Inspector's Apply button.</summary>
        public void Apply()
        {
            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
            {
                Debug.LogWarning("ResourceCheat: нет ResourceManager в сцене -- изменить ресурс невозможно.");
                return;
            }

            _applying = true;
            resourceManager.SetAmount(resource, amount);
            _applying = false;

            EventLogManager.Instance?.Log($"[Чит] {resource} = {amount}");
        }

        private void HandleResourceChanged(ResourceType changedType, int newAmount)
        {
            if (!keepLocked || _applying || changedType != resource || newAmount == amount) return;

            _applying = true;
            ResourceManager.Instance?.SetAmount(resource, amount);
            _applying = false;
        }
    }
}
