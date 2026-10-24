using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// An orc's or a beast's death (the user, 2026-10-22: they vanished on the spot): its figure is
    /// taken off the unit as the unit goes, topples over backwards, lies a moment and sinks into the
    /// ground. Nothing about the fight waits on it -- the unit itself is already gone.
    ///
    /// A jointed figure (a hero's, a soldier's, a beast's: FigureAnimator) goes limp as it falls:
    /// the arms fly up before it, one leg kicks, the head drops back (the user, 2026-10-24:
    /// «бег, стойка, удар, смерть»).
    /// </summary>
    public class OrcCorpse : MonoBehaviour
    {
        private const float FallSeconds = 0.35f;
        private const float LieSeconds = 0.8f;
        private const float SinkSeconds = 0.9f;
        private const float FallDegrees = 80f;
        private const float SinkMetres = 0.6f;

        // degrees forward of the pose the figure died in, at the end of the fall
        private const float ArmFling = 95f;
        private const float OffArmFling = 70f;
        private const float LegKick = 35f;
        private const float HeadBack = -25f;
        private const float Forward = -1f;     // as FigureAnimator's

        private float _clock;
        private Transform[] _limbs;
        private Quaternion[] _limbRest;
        private float[] _limbFling;
        private Quaternion _upright;
        private Vector3 _ground;

        /// <summary>Takes the unit's figure (every child with something drawn, `skip` aside -- an orc's one model, a soldier's cubes) and lets it fall where it stood.</summary>
        public static void Leave(Transform unit, Transform skip) => Fall(unit, skip, copy: false);

        /// <summary>
        /// The same fall for a unit that stays (a hero: he gets up again at the castle, the user
        /// 2026-10-23 -- the Queen used to stand and vanish): a copy of his figure falls, in the
        /// pose he had, and the caller hides his own.
        /// </summary>
        public static void LeaveCopy(Transform unit, Transform skip) => Fall(unit, skip, copy: true);

        private static void Fall(Transform unit, Transform skip, bool copy)
        {
            OrcCorpse corpse = null;
            for (var i = unit.childCount - 1; i >= 0; i--)
            {
                var child = unit.GetChild(i);
                if (child == skip || child.GetComponentInChildren<Renderer>() == null) continue;
                if (corpse == null)
                {
                    corpse = new GameObject("Corpse").AddComponent<OrcCorpse>();
                    corpse.transform.SetPositionAndRotation(unit.position, unit.rotation);
                    corpse.transform.localScale = unit.lossyScale;
                    corpse._upright = unit.rotation;
                    corpse._ground = unit.position;
                }
                if (copy) Instantiate(child.gameObject, child.position, child.rotation, corpse.transform).transform.localScale = child.localScale;
                else child.SetParent(corpse.transform, true);
            }
            if (corpse != null) corpse.FindLimbs();
        }

        private void FindLimbs()
        {
            var limbs = new System.Collections.Generic.List<Transform>();
            var fling = new System.Collections.Generic.List<float>();
            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                var name = part.name;
                float degrees;
                if (name.EndsWith("ArmSword") || name.EndsWith("ArmStrike") || name.EndsWith("ArmDraw")) degrees = ArmFling;
                else if (name.EndsWith("ArmShield") || name.EndsWith("ArmOff") || name.EndsWith("ArmBow")) degrees = OffArmFling;
                else if (name.EndsWith("LegR")) degrees = LegKick;
                else if (name.EndsWith("LegL")) degrees = LegKick * 0.3f;
                else if (name.EndsWith("Head")) degrees = HeadBack;
                else continue;
                limbs.Add(part);
                fling.Add(degrees);
            }
            if (limbs.Count == 0) return;
            _limbs = limbs.ToArray();
            _limbFling = fling.ToArray();
            _limbRest = new Quaternion[_limbs.Length];
            for (var i = 0; i < _limbs.Length; i++) _limbRest[i] = _limbs[i].localRotation;
        }

        /// <summary>How far a jointed figure's limbs have flown loose, 0..1 -- for the tests.</summary>
        public float Limp { get; private set; }

        private void Update()
        {
            _clock += Time.deltaTime;
            var fall = Mathf.Clamp01(_clock / FallSeconds);
            fall *= fall;   // gathers speed as it goes over
            transform.rotation = _upright * Quaternion.AngleAxis(-FallDegrees * fall, Vector3.right);
            if (_limbs != null)
            {
                // the limbs lead the body: loose before it hits the ground
                Limp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_clock / (FallSeconds * 0.8f)));
                for (var i = 0; i < _limbs.Length; i++)
                {
                    if (_limbs[i] == null) continue;
                    _limbs[i].localRotation = Quaternion.AngleAxis(Forward * _limbFling[i] * Limp, Vector3.right) * _limbRest[i];
                }
            }

            var sink = Mathf.Clamp01((_clock - FallSeconds - LieSeconds) / SinkSeconds);
            transform.position = _ground + Vector3.down * (SinkMetres * sink);
            if (sink >= 1f) Destroy(gameObject);
        }
    }
}
