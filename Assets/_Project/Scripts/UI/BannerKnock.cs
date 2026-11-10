using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.UI
{
    /// <summary>
    /// A banner that swings when tapped, like a cloth struck on its perch (the user, 2026-11-10: the
    /// sandbox screen's banner, a small touch). The swing is a damped wobble about the pivot (the
    /// perch, top centre); each tap adds to it, swinging away from the side that was hit. It runs only
    /// while it moves and allocates nothing.
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

        private void Awake()
        {
            _rect = (RectTransform)transform;
            enabled = false;
        }

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
            var current = Current();
            _amplitude = Mathf.Clamp(current + side * KickDegrees, -MaxDegrees, MaxDegrees);
            _time = 0f;
            enabled = true;
        }

        private float Current() => enabled ? _amplitude * Mathf.Exp(-Damping * _time) * Mathf.Cos(_time * Frequency * Mathf.PI * 2f) : 0f;

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            var angle = Current();
            if (Mathf.Abs(_amplitude) * Mathf.Exp(-Damping * _time) < RestDegrees)
            {
                angle = 0f;
                enabled = false;
            }
            _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable()
        {
            if (_rect != null) _rect.localRotation = Quaternion.identity;
        }
    }
}
