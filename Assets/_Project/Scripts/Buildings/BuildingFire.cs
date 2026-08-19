using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// One building, on fire. Added by FireManager and removed by itself.
    ///
    /// The fire eats the building's health while it burns and goes out on its own after
    /// `fire_burn_seconds` -- so a town with no Пожарная бригада does not simply lose every
    /// building that ever catches, it loses a chunk of one. What the brigade buys is the difference
    /// between a scar and a ruin.
    ///
    /// The duration is recomputed from the CURRENT number of firefighters in reach every time it is
    /// checked, exactly the way ResearchManager recomputes a research from the scientists standing
    /// in the Laboratory right now. That is not an implementation detail: it means staffing the
    /// brigade WHILE the fire burns shortens it, and pulling those people away lengthens it again,
    /// which is the only interesting decision a fire offers.
    /// </summary>
    public class BuildingFire : MonoBehaviour
    {
        /// <summary>How often the firefighter count is re-read. Fast enough that sending people to the station is felt, slow enough that a scan is nothing.</summary>
        private const float CoverageRefreshSeconds = 0.5f;

        private static readonly Color FlameColor = new Color(1f, 0.45f, 0.1f);

        private BuildingInstance _building;
        private GameObject _marker;
        private Material _markerMaterial;

        private float _elapsedSeconds;
        private float _coverageTimer;
        private int _firefighters;

        /// <summary>Damage owed but not yet dealt. BuildingInstance takes whole points, and a fire doing three a second at sixty frames would otherwise round every frame down to nothing.</summary>
        private float _damageDebt;

        /// <summary>How long this fire will last in total, at the number of firefighters currently reaching it.</summary>
        public float TotalSeconds => BurnSeconds(_firefighters);

        public float RemainingSeconds => Mathf.Max(0f, TotalSeconds - _elapsedSeconds);

        /// <summary>How far in it is, for a save that has to put it back exactly as hot as it was.</summary>
        public float ElapsedSeconds => _elapsedSeconds;

        private void Awake()
        {
            _building = GetComponent<BuildingInstance>();
            BuildMarker();
        }

        /// <summary>Puts a saved fire back where it had got to. Clamped, because a fire restored past its own end would never tick and would burn forever.</summary>
        public void RestoreElapsed(float elapsedSeconds)
        {
            _elapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        }

        private void Update()
        {
            if (_building == null || _building.CurrentHealth <= 0)
            {
                // The building lost. Nothing to announce -- BuildingInstance already logged its own
                // destruction, and "the fire is out" over a pile of ash reads as a success.
                Destroy(this);
                return;
            }

            _coverageTimer -= Time.deltaTime;
            if (_coverageTimer <= 0f)
            {
                _coverageTimer = CoverageRefreshSeconds;
                _firefighters = FireManager.Instance != null ? FireManager.Instance.FirefightersCovering(transform.position) : 0;
            }

            _elapsedSeconds += Time.deltaTime;

            _damageDebt += BalanceConfig.Instance.FireDamagePerSecond * Time.deltaTime;
            var whole = Mathf.FloorToInt(_damageDebt);
            if (whole > 0)
            {
                _damageDebt -= whole;
                // The same door orcs come through, deliberately: a Town Hall lost to a fire the
                // player let spread is as much a defeat as one lost to a raid, and this is the path
                // GameOverManager listens on.
                _building.TryDamage(whole);
            }

            if (_elapsedSeconds < TotalSeconds) return;

            EventLogManager.Instance?.Log(Localization.Format("#log_fire_out", _building.Data != null ? _building.Data.LocalizedName : string.Empty));
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (_marker != null) Destroy(_marker);
            if (_markerMaterial != null) Destroy(_markerMaterial);
        }

        /// <summary>
        /// Pure: how long a fire lasts with this many firefighters on it. Subtracted rather than
        /// scaled, and floored -- the same shape research uses for its scientists, so a player who
        /// has learnt one has learnt the other.
        /// </summary>
        public static float BurnSeconds(int firefighters, float unattendedSeconds, float secondsSavedEach, float minimumSeconds)
        {
            var seconds = unattendedSeconds - Mathf.Max(0, firefighters) * secondsSavedEach;
            return Mathf.Max(minimumSeconds, seconds);
        }

        private static float BurnSeconds(int firefighters)
        {
            var config = BalanceConfig.Instance;
            return BurnSeconds(firefighters, config.FireBurnSeconds, config.FireSecondsSavedPerFirefighter, config.FireMinBurnSeconds);
        }

        /// <summary>
        /// A flame the player can find from the camera's height. Unparented would be safer against
        /// the FBX rotation trap that ate the chopping bar -- but a building's prefab root carries
        /// no corrective rotation the way the tree models do, and a marker that has to follow a
        /// building nothing ever moves is simpler as a child.
        /// </summary>
        private void BuildMarker()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var height = 2f;
            foreach (var renderer in renderers)
            {
                height = Mathf.Max(height, renderer.bounds.max.y - transform.position.y);
            }

            _marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _marker.name = "FireMarker";
            Destroy(_marker.GetComponent<BoxCollider>());
            _marker.transform.SetParent(transform, false);
            _marker.transform.localPosition = new Vector3(0f, height + 0.6f, 0f);
            _marker.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            _markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = FlameColor };
            _marker.GetComponent<Renderer>().sharedMaterial = _markerMaterial;
        }
    }
}
