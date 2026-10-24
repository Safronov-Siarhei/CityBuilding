using CityBuilder.Invasion;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Blinking «!» marks on the minimap where an Invasion wave is coming from (the user,
    /// 2026-10-22): one per lane the wave underway still has orcs on, at the head of that lane's
    /// column -- or, when the column is beyond what the minimap frames, on its rim in that
    /// direction. Nothing shows between waves or outside a run. The raiding party has a «!» of its
    /// own, in another colour (the user, 2026-10-24), on it for as long as any of it is out.
    /// </summary>
    public class MinimapAttackAlerts : MonoBehaviour
    {
        private const float RefreshSeconds = 0.1f;
        private const float BlinkPerSecond = 2.5f;
        // How far out a mark may sit, as a share of the minimap's radius: inside the ring.
        private const float RimShare = 0.82f;

        [SerializeField] private MinimapCameraIndicator map;
        [SerializeField] private CanvasGroup[] marks;
        [SerializeField] private CanvasGroup partyMark;

        private float _refreshIn;

        private void Start()
        {
            if (!InvasionMode.IsActive) enabled = false;
            Hide(0);
        }

        private void Update()
        {
            var t = Time.unscaledTime * BlinkPerSecond * Mathf.PI * 2f;
            var alpha = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t));
            foreach (var mark in marks)
            {
                if (mark != null && mark.gameObject.activeSelf) mark.alpha = alpha;
            }
            if (partyMark != null && partyMark.gameObject.activeSelf) partyMark.alpha = alpha;

            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Place();
        }

        private void Place()
        {
            var mode = InvasionMode.Instance;
            var waves = mode != null ? mode.Waves : null;
            PlaceParty(waves);
            if (waves == null || map == null || waves.Current != InvasionWaves.Phase.Underway)
            {
                Hide(0);
                return;
            }

            var castle = mode.Castle != null ? mode.Castle.transform.position : Vector3.zero;
            var rim = map.MapHalfExtent * RimShare;
            var shown = 0;
            foreach (var lane in waves.LastLanes)
            {
                if (shown >= marks.Length) break;
                if (!waves.TryLaneFront(lane, castle, out var front)) continue;
                var at = map.ToMapUnclamped(front);
                if (at.magnitude > rim) at = at.normalized * rim;
                var mark = marks[shown++];
                mark.gameObject.SetActive(true);
                ((RectTransform)mark.transform).anchoredPosition = at;
            }
            Hide(shown);
        }

        /// <summary>The raiding party's mark, on the first of it still standing -- it outlives the wave it came with.</summary>
        private void PlaceParty(InvasionWaves waves)
        {
            if (partyMark == null) return;
            Combat.OrcUnit head = null;
            if (waves != null && map != null)
            {
                foreach (var orc in waves.RaidParty)
                {
                    if (orc == null || orc.CurrentHealth <= 0) continue;
                    head = orc;
                    break;
                }
            }
            if (head == null)
            {
                if (partyMark.gameObject.activeSelf) partyMark.gameObject.SetActive(false);
                return;
            }
            var rim = map.MapHalfExtent * RimShare;
            var at = map.ToMapUnclamped(head.transform.position);
            if (at.magnitude > rim) at = at.normalized * rim;
            partyMark.gameObject.SetActive(true);
            ((RectTransform)partyMark.transform).anchoredPosition = at;
        }

        /// <summary>Whether the raiding party's mark is up. For the tests.</summary>
        public bool PartyMarkShown => partyMark != null && partyMark.gameObject.activeSelf;

        private void Hide(int from)
        {
            for (var i = from; i < marks.Length; i++)
            {
                if (marks[i] != null && marks[i].gameObject.activeSelf) marks[i].gameObject.SetActive(false);
            }
        }
    }
}
