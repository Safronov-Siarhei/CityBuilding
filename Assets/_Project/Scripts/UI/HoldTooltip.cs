using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.UI
{
    /// <summary>
    /// A button that is a picture only (the user, 2026-10-23): a tap does its job, a press held
    /// past HoldSeconds shows what it is instead (Hint) and does not do it. The skill bar's buttons.
    /// </summary>
    public class HoldTooltip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float HoldSeconds = 0.45f;

        /// <summary>A quick tap.</summary>
        public Action Tapped;

        /// <summary>Held: the text to show over the button; null hides it.</summary>
        public Action<HoldTooltip, bool> Hint;

        private bool _down, _shown;
        private float _heldFor;

        public void OnPointerDown(PointerEventData eventData)
        {
            _down = true;
            _shown = false;
            _heldFor = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_down) return;
            _down = false;
            if (_shown)
            {
                Hint?.Invoke(this, false);
                _shown = false;
                return;
            }
            Tapped?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // a finger slid off: neither a tap nor a hint any more
            if (!_down) return;
            _down = false;
            if (_shown) Hint?.Invoke(this, false);
            _shown = false;
        }

        private void Update()
        {
            if (!_down || _shown) return;
            _heldFor += Time.unscaledDeltaTime;
            if (_heldFor < HoldSeconds) return;
            _shown = true;
            Hint?.Invoke(this, true);
        }

        private void OnDisable()
        {
            if (_shown) Hint?.Invoke(this, false);
            _down = _shown = false;
        }
    }
}
