using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>Grows a freshly spawned tree from 10% to 100% of its natural scale over a fixed duration; not choppable until fully grown (see CitizenVisualsManager's node search).</summary>
    public class TreeGrowth : MonoBehaviour
    {
        private const float StartScaleFactor = 0.1f;
        private const float GrowDurationSeconds = 60f;

        private Vector3 _fullScale;
        private float _elapsed;

        public bool IsFullyGrown { get; private set; }

        private void Awake()
        {
            _fullScale = transform.localScale;
            transform.localScale = _fullScale * StartScaleFactor;
        }

        private void Update()
        {
            if (IsFullyGrown) return;

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / GrowDurationSeconds);
            transform.localScale = _fullScale * Mathf.Lerp(StartScaleFactor, 1f, t);

            if (t >= 1f) IsFullyGrown = true;
        }
    }
}
