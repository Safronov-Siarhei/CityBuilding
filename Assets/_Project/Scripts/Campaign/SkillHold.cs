using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// A skill button held (the user's third playtest, 2026-11-12: «при зажатии скилла должна появляться информация о
    /// том что за скилл»): after HoldSeconds the hint shows until the finger lets go, and that release casts nothing.
    /// A quick tap casts as before.
    /// </summary>
    public class SkillHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float HoldSeconds = 0.35f;

        /// <summary>The hint on (true) or off (false).</summary>
        public System.Action<bool> Shown;

        /// <summary>The press that just ended showed the hint (the button's click ignores it).</summary>
        public bool WasHint { get; private set; }

        private float _down = -1f;
        private bool _showing;

        public void OnPointerDown(PointerEventData e)
        {
            _down = Time.unscaledTime;
            WasHint = false;
        }

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e)
        {
            if (_showing) Release();
        }

        private void Release()
        {
            _down = -1f;
            WasHint = _showing;
            if (_showing) Shown?.Invoke(false);
            _showing = false;
        }

        private void Update()
        {
            if (_down < 0f || _showing || Time.unscaledTime - _down < HoldSeconds) return;
            _showing = true;
            Shown?.Invoke(true);
        }

        private void OnDisable()
        {
            if (_showing) Shown?.Invoke(false);
            _showing = false;
            _down = -1f;
        }
    }
}
