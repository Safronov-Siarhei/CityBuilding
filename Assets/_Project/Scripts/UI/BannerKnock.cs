using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.UI
{
    /// <summary>
    /// A banner that swings when tapped, like a cloth struck on its perch (the user, 2026-11-10: the
    /// sandbox screen's banner, a small touch). The swing is a damped wobble about the pivot (the
    /// perch, top centre); each tap adds to it, swinging away from the side that was hit. It allocates
    /// nothing and its Update returns at once while the banner hangs still.
    /// TRAP: the component must stay ENABLED -- the EventSystem sends no clicks to a disabled one (the
    /// first version switched itself off at rest and never swung).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BannerKnock : MonoBehaviour, IPointerClickHandler
    {
        // the feel of the swing: degrees a tap gives, how fast it wobbles and how soon it settles
        private const float KickDegrees = 9f;
        private const float MaxDegrees = 16f;
        private const float Frequency = 2.2f;
        private const float Damping = 2.6f;
        private const float RestDegrees = 0.05f;

        private RectTransform _rect;
        private float _amplitude;
        private float _time;
        private bool _swinging;

        private void Awake() => _rect = (RectTransform)transform;

        public void OnPointerClick(PointerEventData eventData)
        {
            // hit right of the perch, it swings left first, and the other way
            var side = 1f;
            if (eventData != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out var local))
            {
                side = local.x >= 0f ? 1f : -1f;
            }
            Knock(side);
        }

        public void Knock(float side)
        {
            _amplitude = Mathf.Clamp(Current() + side * KickDegrees, -MaxDegrees, MaxDegrees);
            _time = 0f;
            _swinging = true;
        }

        private float Current() => _swinging ? _amplitude * Mathf.Exp(-Damping * _time) * Mathf.Cos(_time * Frequency * Mathf.PI * 2f) : 0f;

        private void Update()
        {
            if (!_swinging) return;
            _time += Time.unscaledDeltaTime;
            var angle = Current();
            if (Mathf.Abs(_amplitude) * Mathf.Exp(-Damping * _time) < RestDegrees)
            {
                angle = 0f;
                _swinging = false;
            }
            _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable()
        {
            _swinging = false;
            if (_rect != null) _rect.localRotation = Quaternion.identity;
        }
    }
}
