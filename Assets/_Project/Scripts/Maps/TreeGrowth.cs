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
        private Renderer _renderer;

        public bool IsFullyGrown { get; private set; }

        private void Awake()
        {
            _fullScale = transform.localScale;
            transform.localScale = _fullScale * StartScaleFactor;
            _renderer = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            // Growth is purely cosmetic (scale animation) -- skip it entirely while off-screen,
            // both to save the per-frame cost and because nobody can see the difference. Resumes
            // from wherever it left off once the tree re-enters the camera's view.
            if (_renderer != null && !_renderer.isVisible) return;

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / GrowDurationSeconds);
            transform.localScale = _fullScale * Mathf.Lerp(StartScaleFactor, 1f, t);

            if (t >= 1f)
            {
                IsFullyGrown = true;
                // Once grown there's nothing left for this component to do -- removing it means
                // a fully-grown tree costs zero Update() overhead for the rest of its lifetime
                // (CitizenVisualsManager.FindNearestFreeNode already treats a missing TreeGrowth
                // as "fully grown").
                Destroy(this);
            }
        }
    }
}
