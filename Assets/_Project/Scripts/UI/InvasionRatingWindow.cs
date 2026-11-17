using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Доска славы» (approved 2026-11-17, Design/ui/InvasionRating_Window.png): the Invasion's rating for the day or
    /// the week -- the podium 1-2-3, the list from the 4th place, the player's own line pinned at the foot, the rewards
    /// by place, the time left in the period. Filled from <see cref="InvasionRating.Source"/>; while that is the device
    /// alone the board says the rating comes with Google Play.
    /// </summary>
    public class InvasionRatingWindow : MonoBehaviour
    {
        [Serializable] public class Line
        {
            public Text place;
            public Text name;
            public Text points;
            public Text prize;
            public GameObject prizeGroup;
        }

        [SerializeField] private GameObject[] tabOn = new GameObject[0];
        [SerializeField] private GameObject[] tabOff = new GameObject[0];
        [SerializeField] private Text timerText;
        [SerializeField] private Line[] podium = new Line[0];
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Sprite[] rowSprites = new Sprite[0];
        [SerializeField] private Text emptyText;
        [SerializeField] private Line you = new Line();
        [SerializeField] private Text rewardsTitle;
        [SerializeField] private Text[] tierWho = new Text[0];
        [SerializeField] private Text[] tierPrize = new Text[0];
        [SerializeField] private Text noteText;
        [SerializeField] private int rowsLoaded = 25;

        private InvasionRecords.Period _period;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private float _nextTick;
        private int _asked;

        public void Open(int period)
        {
            gameObject.SetActive(true);
            SelectTab(period);
        }

        public void Close() => gameObject.SetActive(false);

        public void SelectTab(int period)
        {
            _period = period == 1 ? InvasionRecords.Period.Week : InvasionRecords.Period.Day;
            for (var i = 0; i < tabOn.Length; i++) if (tabOn[i] != null) tabOn[i].SetActive(i == period);
            for (var i = 0; i < tabOff.Length; i++) if (tabOff[i] != null) tabOff[i].SetActive(i != period);
            var week = _period == InvasionRecords.Period.Week;
            if (rewardsTitle != null) rewardsTitle.text = Localization.Get(week ? "#rating_rewards_week" : "#rating_rewards_day");
            if (noteText != null) noteText.text = Localization.Get(week ? "#rating_note_week" : "#rating_note_day");
            ShowTiers();
            ShowTimer();
            var asked = ++_asked;
            InvasionRating.Source.Load(_period, rowsLoaded, page => { if (asked == _asked && this != null) Show(page); });
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;
            ShowTimer();
        }

        private void ShowTimer()
        {
            if (timerText == null) return;
            var left = InvasionRating.PeriodEndUtc(_period, DateTime.UtcNow) - DateTime.UtcNow;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            var span = left.TotalDays >= 1
                ? string.Format(Localization.Get("#time_dh"), (int)left.TotalDays, left.Hours)
                : string.Format(Localization.Get("#time_hm"), left.Hours, Math.Max(1, left.Minutes));
            timerText.text = string.Format(Localization.Get(_period == InvasionRecords.Period.Week ? "#rating_ends_week" : "#rating_ends_day"), span);
        }

        private void ShowTiers()
        {
            var config = BalanceConfig.Instance;
            if (config == null) return;
            var week = _period == InvasionRecords.Period.Week;
            var upTo = config.InvasionRatingUpTo(week);
            var prize = config.InvasionRatingPrize(week);
            var from = 1;
            for (var i = 0; i < tierWho.Length; i++)
            {
                var has = i < upTo.Length && i < prize.Length;
                if (tierWho[i] != null) tierWho[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;
                var to = upTo[i];
                tierWho[i].text = to <= 0 ? Localization.Get("#rating_others")
                    : to == from ? string.Format(Localization.Get("#rating_place_one"), to)
                    : string.Format(Localization.Get("#rating_places"), from, to);
                if (i < tierPrize.Length && tierPrize[i] != null) tierPrize[i].text = prize[i].ToString();
                from = to + 1;
            }
        }

        private void Show(RatingPage page)
        {
            for (var k = 0; k < podium.Length; k++)
            {
                var has = k < page.top.Count;
                Fill(podium[k], has ? page.top[k] : new RatingEntry { rank = k + 1 }, has);
            }
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();
            for (var k = podium.Length; k < page.top.Count && rowTemplate != null; k++)
            {
                var row = Instantiate(rowTemplate, listContent, false);
                row.SetActive(true);
                var bg = row.GetComponent<Image>();
                if (bg != null && rowSprites.Length > 0) bg.sprite = rowSprites[k % rowSprites.Length];
                var line = new Line
                {
                    place = Find(row, "Place"), name = Find(row, "Name"), points = Find(row, "Points"), prize = Find(row, "Prize/Number"),
                    prizeGroup = row.transform.Find("Prize")?.gameObject,
                };
                Fill(line, page.top[k], true);
                _rows.Add(row);
            }
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(!page.online || page.top.Count == 0);
                emptyText.text = Localization.Get(page.online ? "#rating_empty" : "#rating_offline");
            }
            // the player's own line
            if (you.place != null) you.place.text = page.playerRank > 0 ? page.playerRank.ToString() : "—";
            if (you.name != null) you.name.text = Localization.Get("#invasion_you");
            if (you.points != null) you.points.text = page.playerScore.ToString();
            var mine = InvasionRating.PrizeFor(_period, page.playerRank);
            if (you.prizeGroup != null) you.prizeGroup.SetActive(mine > 0);
            if (you.prize != null) you.prize.text = mine.ToString();
        }

        private void Fill(Line line, RatingEntry entry, bool has)
        {
            if (line == null) return;
            if (line.place != null) line.place.text = entry.rank.ToString();
            if (line.name != null) line.name.text = has ? entry.name : "—";
            if (line.points != null) line.points.text = has ? entry.score.ToString() : string.Empty;
            var prize = InvasionRating.PrizeFor(_period, entry.rank);
            if (line.prizeGroup != null) line.prizeGroup.SetActive(prize > 0);
            if (line.prize != null) line.prize.text = prize.ToString();
        }

        private static Text Find(GameObject row, string path)
        {
            var t = row.transform.Find(path);
            return t != null ? t.GetComponent<Text>() : null;
        }
    }
}
