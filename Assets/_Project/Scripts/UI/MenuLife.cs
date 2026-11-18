using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A little life on the main menu (the user, 2026-11-18: «название игры в главном меню должно немного двигаться,
    /// кнопка Продолжить пульсировать слегка»): Float -- the logo rises and sinks a few units and tilts a hair;
    /// Pulse -- the button breathes in scale. Unscaled time, one transform, nothing allocated.
    /// </summary>
    public class MenuLife : MonoBehaviour
    {
        public enum Kind { Float, Pulse }

        [SerializeField] private Kind kind;
        private RectTransform _rect;
        private Vector2 _home;

        public static void Add(GameObject target, Kind kind) => target.AddComponent<MenuLife>().kind = kind;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
        }

        private void Update()
        {
            var t = Time.unscaledTime;
            if (kind == Kind.Float)
            {
                _rect.anchoredPosition = _home + new Vector2(0f, 6f * Mathf.Sin(t * 1.3f));
                _rect.localRotation = Quaternion.Euler(0f, 0f, 0.8f * Mathf.Sin(t * 0.9f + 1f));
            }
            else
            {
                var k = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                _rect.localScale = Vector3.one * (1f + 0.035f * k);
            }
        }
    }
}
