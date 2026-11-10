using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CityBuilder.UI
{
    /// <summary>A press held for half a second: fires `onLongPress` once, and the click that follows on release is swallowed (ConsumeFired).</summary>
    public class LongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float HoldSeconds = 0.5f;

        public UnityEvent onLongPress = new UnityEvent();

        private float _downAt = -1f;
        private bool _fired;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downAt = Time.unscaledTime;
            _fired = false;
        }

        public void OnPointerUp(PointerEventData eventData) => _downAt = -1f;

        public void OnPointerExit(PointerEventData eventData) => _downAt = -1f;

        private void Update()
        {
            if (_downAt < 0f || Time.unscaledTime - _downAt < HoldSeconds) return;
            _downAt = -1f;
            _fired = true;
            onLongPress.Invoke();
        }

        /// <summary>Whether a long press just fired -- and forgets it, so only the one click after it is swallowed.</summary>
        public bool ConsumeFired()
        {
            var fired = _fired;
            _fired = false;
            return fired;
        }
    }
}
