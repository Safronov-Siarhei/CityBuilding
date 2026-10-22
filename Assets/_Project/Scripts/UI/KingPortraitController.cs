using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The King's portrait in an Invasion run (top-right, where the sandbox shows contentment):
    /// his face, his level, a health bar and an experience bar; greyed with a countdown while he is
    /// down; a gold ring while he is selected. A tap on it selects him -- the next tap on the map
    /// is his order -- and brings the camera to him; let go of, he goes back to guarding the base
    /// by himself (KingHero). Hidden outside a run.
    ///
    /// Also answers the world tap that lands on the King himself (WorldInputDispatcher), so he can
    /// be picked up where he stands, and draws the ring on the ground under him while selected.
    /// </summary>
    public class KingPortraitController : MonoBehaviour
    {
        /// <summary>How close to the King's figure on screen a tap has to land to pick him, at a 1080-high screen.</summary>
        private const float PickPixels = 70f;
        private const float RefreshSeconds = 0.1f;

        [SerializeField] private GameObject widget;
        [SerializeField] private Text levelText;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private RectTransform xpFill;
        [SerializeField] private GameObject fallenOverlay;
        [SerializeField] private Text fallenText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject guardMark;

        private static KingPortraitController _instance;
        private bool _wasFallen;
        private float _refreshIn;
        private LineRenderer _ring;

        private static KingHero King => InvasionMode.Instance != null ? InvasionMode.Instance.King : null;

        private void Awake() => _instance = this;

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            if (widget != null) widget.SetActive(InvasionMode.IsActive);
            if (!InvasionMode.IsActive) enabled = false;
        }

        /// <summary>The portrait tapped: select him and look at him.</summary>
        public void SelectKing()
        {
            var king = King;
            var army = ArmyManager.Instance;
            if (king == null || army == null) return;
            if (!king.IsFallen) army.SelectGroup(king.Unit.Group);
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null) rig.JumpTo(king.IsFallen ? king.Home : king.transform.position);
        }

        /// <summary>Back to guarding the base, and let go of.</summary>
        public void GuardBase()
        {
            var king = King;
            if (king == null || king.IsFallen) return;
            king.Guard();
            ArmyManager.Instance?.SelectGroup(null);
        }

        /// <summary>A world tap on the King himself selects him. False when the tap was not on him.</summary>
        public static bool TrySelectAt(Vector2 screenPosition, Camera camera)
        {
            var king = King;
            if (_instance == null || king == null || king.IsFallen || camera == null || ArmyManager.Instance == null) return false;
            var onScreen = camera.WorldToScreenPoint(king.transform.position + Vector3.up * 0.4f);
            if (onScreen.z <= 0f) return false;
            var reach = PickPixels * Screen.height / 1080f;
            if (((Vector2)onScreen - screenPosition).sqrMagnitude > reach * reach) return false;
            ArmyManager.Instance.SelectGroup(king.Unit.Group);
            return true;
        }

        private void Update()
        {
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            var king = King;
            if (king == null || king.Unit == null) return;

            if (_wasFallen && !king.IsFallen) ShowReturn(king);
            _wasFallen = king.IsFallen;

            var selected = ArmyManager.Instance != null && king.Unit.Group != null && ArmyManager.Instance.SelectedGroup == king.Unit.Group;
            if (levelText != null) levelText.text = king.Level.ToString();
            SetFill(healthFill, king.IsFallen ? 0f : king.Unit.CurrentHealth / (float)Mathf.Max(1, king.Unit.MaxHealth));
            var toNext = KingHero.XpForLevel(king.Level);
            SetFill(xpFill, king.ExperienceToNext <= 0 ? 1f : king.Experience / (float)Mathf.Max(1, toNext));
            if (fallenOverlay != null) fallenOverlay.SetActive(king.IsFallen);
            if (fallenText != null && king.IsFallen) fallenText.text = Mathf.CeilToInt(king.RespawnRemaining).ToString();
            if (selectedFrame != null) selectedFrame.SetActive(selected && !king.IsFallen);
            if (guardMark != null) guardMark.SetActive(king.Guarding && !king.IsFallen);
            ShowRing(king, selected && !king.IsFallen);
        }

        /// <summary>He is back (the user, 2026-10-22: the player must see it): the camera goes to him, a column of light, a line in the log.</summary>
        private static void ShowReturn(KingHero king)
        {
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null) rig.JumpTo(king.transform.position);
            RespawnFlash.Play(king.transform.position);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_inv_king_back"), EventSeverity.Good,
                EventGlyph.Check, null, king.transform.position, king.transform));
        }

        private static void SetFill(RectTransform fill, float fraction)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), fill.anchorMax.y);
        }

        /// <summary>A gold ring on the ground under the selected King.</summary>
        private void ShowRing(KingHero king, bool show)
        {
            if (_ring == null)
            {
                if (!show) return;
                var go = new GameObject("KingSelectionRing");
                _ring = go.AddComponent<LineRenderer>();
                _ring.useWorldSpace = false;
                _ring.loop = true;
                _ring.positionCount = 24;
                _ring.widthMultiplier = 0.05f;
                _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ring.receiveShadows = false;
                _ring.material = new Material(RuntimeShaders.Unlit) { color = UiTheme.Gold };
                for (var i = 0; i < 24; i++)
                {
                    var a = i / 24f * Mathf.PI * 2f;
                    _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.45f, 0.04f, Mathf.Sin(a) * 0.45f));
                }
            }
            _ring.gameObject.SetActive(show);
            if (show) _ring.transform.position = king.transform.position;
        }

        private void LateUpdate()
        {
            // the ring follows him between refreshes
            var king = King;
            if (_ring != null && _ring.gameObject.activeSelf && king != null) _ring.transform.position = king.transform.position;
        }
    }
}
