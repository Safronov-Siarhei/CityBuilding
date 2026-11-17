using System.Collections;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The letter with the wax seal in the main menu's top row (the user, 2026-11-17: icon 4; card Design/features/news.md).
    /// The tap opens «Новости»; the red dot shows while News.AnyDot; on entering the menu the window opens by itself
    /// once a launch for a player with news_auto_open_hours behind them (News.ShouldOpenByItself), never over another
    /// menu window.
    /// </summary>
    public class NewsButton : MonoBehaviour
    {
        [SerializeField] private GameObject dot;

        /// <summary>The button (a persistent listener).</summary>
        public void Open() => NewsWindow.Show();

        private void OnEnable()
        {
            News.Changed += Redraw;
            Redraw();
        }

        private void OnDisable() => News.Changed -= Redraw;

        private IEnumerator Start()
        {
            // a moment for the menu to settle (and for any window the menu opens itself)
            yield return new WaitForSecondsRealtime(0.8f);
            if (!News.ShouldOpenByItself || NewsWindow.IsOpen || MenuShopWindow.IsOpen || PiggyWindow.IsOpen
                || SupportOfferWindow.IsOpen || RateUsWindow.IsOpen) yield break;
            News.NoteOpenedByItself();
            NewsWindow.Show();
        }

        private void Redraw()
        {
            if (dot != null) dot.SetActive(News.AnyDot);
        }
    }
}
