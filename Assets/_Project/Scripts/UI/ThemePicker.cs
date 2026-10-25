using System;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The round theme buttons of the sandbox and invasion screens (the user, 2026-10-25): one per
    /// MapThemes.All, the picked one lit (its gold face shown over the plain one). Remembers the
    /// pick per mode in GameSettings and says so, for the screen to show that theme's maps or picture.
    /// </summary>
    public class ThemePicker : MonoBehaviour
    {
        [SerializeField] private bool invasion;
        /// <summary>In MapThemes.All order.</summary>
        [SerializeField] private Button[] buttons = new Button[0];
        [SerializeField] private GameObject[] picked = new GameObject[0];

        public event Action<MapTheme> Changed;

        public MapTheme Current => invasion ? GameSettings.InvasionTheme : GameSettings.SandboxTheme;

        private void Awake()
        {
            for (var i = 0; i < buttons.Length && i < MapThemes.All.Length; i++)
            {
                var theme = MapThemes.All[i];
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => Pick(theme));
            }
        }

        private void OnEnable() => Show();

        public void Pick(MapTheme theme)
        {
            if (invasion) GameSettings.InvasionTheme = theme;
            else GameSettings.SandboxTheme = theme;
            Show();
            Changed?.Invoke(theme);
        }

        private void Show()
        {
            var current = Current;
            for (var i = 0; i < picked.Length && i < MapThemes.All.Length; i++)
            {
                if (picked[i] != null) picked[i].SetActive(MapThemes.All[i] == current);
            }
        }
    }
}
