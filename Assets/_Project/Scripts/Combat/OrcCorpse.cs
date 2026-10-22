using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// An orc's or a beast's death (the user, 2026-10-22: they vanished on the spot): its figure is
    /// taken off the unit as the unit goes, topples over backwards, lies a moment and sinks into the
    /// ground. Nothing about the fight waits on it -- the unit itself is already gone.
    /// </summary>
    public class OrcCorpse : MonoBehaviour
    {
        private const float FallSeconds = 0.35f;
        private const float LieSeconds = 0.8f;
        private const float SinkSeconds = 0.9f;
        private const float FallDegrees = 80f;
        private const float SinkMetres = 0.6f;

        private float _clock;
        private Quaternion _upright;
        private Vector3 _ground;

        /// <summary>Takes the unit's figure (its first child with something drawn, `skip` aside) and lets it fall where it stood.</summary>
        public static void Leave(Transform unit, Transform skip)
        {
            Transform figure = null;
            foreach (Transform child in unit)
            {
                if (child == skip || child.GetComponentInChildren<Renderer>() == null) continue;
                figure = child;
                break;
            }
            if (figure == null) return;

            var corpse = new GameObject("Corpse").AddComponent<OrcCorpse>();
            corpse.transform.SetPositionAndRotation(unit.position, unit.rotation);
            corpse.transform.localScale = unit.lossyScale;
            figure.SetParent(corpse.transform, true);
            corpse._upright = unit.rotation;
            corpse._ground = unit.position;
        }

        private void Update()
        {
            _clock += Time.deltaTime;
            var fall = Mathf.Clamp01(_clock / FallSeconds);
            fall *= fall;   // gathers speed as it goes over
            transform.rotation = _upright * Quaternion.AngleAxis(-FallDegrees * fall, Vector3.right);

            var sink = Mathf.Clamp01((_clock - FallSeconds - LieSeconds) / SinkSeconds);
            transform.position = _ground + Vector3.down * (SinkMetres * sink);
            if (sink >= 1f) Destroy(gameObject);
        }
    }
}
