using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The «Настройки мира» scroll («Свобода песочницы», card Design/features/sandbox-freedom.md; look approved by the
    /// user 2026-11-17): a row per WorldSettings.Setting, its options as a segmented bar, the picked one gold. ALWAYS
    /// shown open with its values; a tap on another option without the purchase opens the purchase banner
    /// (FreedomBanner). On the sandbox screen it edits the next world's settings (WorldSettings.Draft); in the game
    /// it edits the running world's (WorldSettings.Current) and greys the rows that can only be set before the start.
    /// While the world is easier than the default the foot shows the piggy warning and each easier row a struck pig.
    /// Built by SetupProjectMainMenuScreens / SetupProjectHud.
    /// </summary>
    public class WorldSettingsScroll : MonoBehaviour
    {
        [SerializeField] private bool inGame;
        /// <summary>The segments of every row, flat, row by row (WorldSettings.Options(row) each).</summary>
        [SerializeField] private Button[] segments = new Button[0];
        /// <summary>The gold face of each segment (shown on the picked one).</summary>
        [SerializeField] private GameObject[] picks = new GameObject[0];
        /// <summary>Per row: the struck pig (an easier choice), the row's group (greyed in the game for start-only rows),
        /// the «задаётся при создании» note.</summary>
        [SerializeField] private GameObject[] pigMarks = new GameObject[0];
        [SerializeField] private CanvasGroup[] rowGroups = new CanvasGroup[0];
        [SerializeField] private GameObject[] startOnlyNotes = new GameObject[0];
        [SerializeField] private GameObject warning;
        [SerializeField] private GameObject hint;
        [SerializeField] private Button defaultsButton;

        private WorldSettings Target => inGame ? WorldSettings.Current : WorldSettings.Draft;

        private void Awake()
        {
            var k = 0;
            for (var r = 0; r < WorldSettings.All.Length; r++)
            {
                var setting = WorldSettings.All[r];
                for (var o = 0; o < WorldSettings.Options(setting); o++, k++)
                {
                    if (k >= segments.Length || segments[k] == null) continue;
                    var option = o;
                    segments[k].onClick.AddListener(() => Pick(setting, option));
                }
            }
            if (defaultsButton != null) defaultsButton.onClick.AddListener(Defaults);
        }

        private void OnEnable()
        {
            Wallet.FreedomChanged += Show;
            Show();
        }

        private void OnDisable() => Wallet.FreedomChanged -= Show;

        public void Pick(WorldSettings.Setting setting, int option)
        {
            if (inGame && WorldSettings.SetAtStartOnly(setting)) return;
            if (Target.Get(setting) == option) return;
            if (!Wallet.FreedomOwned)
            {
                FreedomBanner.Open();
                return;
            }
            Target.Set(setting, option);
            if (inGame) Apply(setting);
            Show();
        }

        private void Defaults()
        {
            var fresh = new WorldSettings();
            foreach (var s in WorldSettings.All)
            {
                if (inGame && WorldSettings.SetAtStartOnly(s)) continue;
                if (Target.Get(s) == fresh.Get(s)) continue;
                if (!Wallet.FreedomOwned)
                {
                    FreedomBanner.Open();
                    return;
                }
                Target.Set(s, fresh.Get(s));
                if (inGame) Apply(s);
            }
            Show();
        }

        /// <summary>What a change in the running world does at once (the rest is read where it acts).</summary>
        private static void Apply(WorldSettings.Setting setting)
        {
            // «Всегда мир» during a war: the war is over now (the user's rule, card sandbox-freedom.md)
            if (setting == WorldSettings.Setting.Neighbour && WorldSettings.Current.neighbourPeace
                && Diplomacy.BotRelations.Stance == Diplomacy.BotStance.War)
                Diplomacy.BotRelations.EndWar();
            WorldSettings.RaiseChanged();
        }

        public void Show()
        {
            var target = Target;
            var k = 0;
            for (var r = 0; r < WorldSettings.All.Length; r++)
            {
                var setting = WorldSettings.All[r];
                var picked = target.Get(setting);
                for (var o = 0; o < WorldSettings.Options(setting); o++, k++)
                    if (k < picks.Length && picks[k] != null) picks[k].SetActive(o == picked);
                if (r < pigMarks.Length && pigMarks[r] != null) pigMarks[r].SetActive(target.EasierAt(setting));
                var locked = inGame && WorldSettings.SetAtStartOnly(setting);
                if (r < rowGroups.Length && rowGroups[r] != null) rowGroups[r].alpha = locked ? 0.45f : 1f;
                if (r < startOnlyNotes.Length && startOnlyNotes[r] != null) startOnlyNotes[r].SetActive(locked);
            }
            var easier = target.Easier;
            if (warning != null) warning.SetActive(easier);
            if (hint != null) hint.SetActive(!easier);
        }
    }
}
