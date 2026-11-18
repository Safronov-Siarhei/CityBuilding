using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Makes a shop offer card look alive (the user, 2026-11-18: «звёздочкам нужно добавить движение, а жёлтая обводка вокруг
    /// рудника и предложений кристаллов должна немного пульсировать (но с разными таймингами у каждого предложения)»):
    /// the gold rim overlay breathes in and out, each card on its own period and phase, and the sparkles twinkle --
    /// grow, shrink and turn a little, each on its own clock. Unscaled time: the shop opens over a paused game.
    /// One component per card, a few transforms touched per frame, nothing allocated.
    /// </summary>
    public class ShopCardLife : MonoBehaviour
    {
        private Image _rim;
        private RectTransform[] _sparkles;
        private float[] _phase;
        private float _rimPeriod, _rimPhase;

        public void Init(Image rim, RectTransform[] sparkles, int seed)
        {
            _rim = rim;
            _sparkles = sparkles;
            var rnd = new System.Random(seed * 7919 + 13);
            _rimPeriod = 1.6f + (float)rnd.NextDouble() * 0.9f;
            _rimPhase = (float)rnd.NextDouble() * 10f;
            _phase = new float[sparkles.Length];
            for (var i = 0; i < _phase.Length; i++) _phase[i] = (float)rnd.NextDouble() * 10f;
        }

        private void Update()
        {
            var t = Time.unscaledTime;
            if (_rim != null)
            {
                var k = 0.5f + 0.5f * Mathf.Sin((t + _rimPhase) * 2f * Mathf.PI / _rimPeriod);
                var c = _rim.color;
                c.a = 0.15f + 0.6f * k;
                _rim.color = c;
            }
            if (_sparkles == null) return;
            for (var i = 0; i < _sparkles.Length; i++)
            {
                var s = _sparkles[i];
                if (s == null) continue;
                var p = t * (1.1f + 0.15f * i) + _phase[i];
                var k = 0.5f + 0.5f * Mathf.Sin(p * 2.2f);
                s.localScale = Vector3.one * (0.45f + 0.75f * k * k);
                s.localRotation = Quaternion.Euler(0f, 0f, 20f * Mathf.Sin(p * 0.9f));
            }
        }
    }
}
