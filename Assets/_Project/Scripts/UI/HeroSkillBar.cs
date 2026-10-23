using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The selected hero's skills where the build menu stands (the user, 2026-10-23): four round
    /// pictures, a dark sweep while one recharges, a lock with the hero level that opens it. A tap
    /// uses the skill, a held press shows what it does (HoldTooltip). Let the hero go and the build
    /// menu comes back (BuildingPlacerUIVisibility reads Showing).
    ///
    /// Also draws the gold glow round the screen's edge while the King's Blessing holds.
    /// </summary>
    public class HeroSkillBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.05f;

        [SerializeField] private GameObject bar;
        [SerializeField] private HoldTooltip[] buttons;
        [SerializeField] private Image[] icons;
        [SerializeField] private Image[] cooldowns;
        [SerializeField] private GameObject[] locks;
        [SerializeField] private Text[] lockLevels;
        [SerializeField] private GameObject tooltip;
        [SerializeField] private Text tooltipText;

        private Image _blessingGlow;

        /// <summary>Up: a hero is in hand and his skills stand where the build menu would.</summary>
        public static bool Showing { get; private set; }

        public HeroSkills Current { get; private set; }

        private float _refreshIn;

        private void Awake()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                buttons[i].Tapped = () => Use(index);
                buttons[i].Hint = (button, show) => ShowHint(index, button, show);
            }
            if (tooltip != null) tooltip.SetActive(false);
            if (bar != null) bar.SetActive(false);
        }

        private void OnDestroy() => Showing = false;

        /// <summary>The skill under this button, used now; false when it is locked, recharging or the hero is down.</summary>
        public bool Use(int index) => Current != null && Current.Use(index);

        private void Update()
        {
            Current = SelectedHero();
            var show = Current != null;
            Showing = show;
            if (bar != null && bar.activeSelf != show) bar.SetActive(show);
            ShowBlessing();
            if (!show) return;

            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            for (var i = 0; i < buttons.Length; i++)
            {
                var has = i < Current.Slots.Count;
                if (buttons[i].gameObject.activeSelf != has) buttons[i].gameObject.SetActive(has);
                if (!has) continue;
                var slot = Current.Slots[i];
                var open = Current.IsOpen(i);
                locks[i].SetActive(!open);
                if (!open) lockLevels[i].text = slot.Balance.unlockLevel.ToString();
                cooldowns[i].fillAmount = open && slot.Balance.cooldownSeconds > 0f ? slot.Cooldown / slot.Balance.cooldownSeconds : 0f;
                icons[i].color = Current.IsReady(i) || !open ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
            }
        }

        /// <summary>The hero the player holds, or null: the selected group's hero, while he stands.</summary>
        private static HeroSkills SelectedHero()
        {
            var army = ArmyManager.Instance;
            var group = army != null ? army.SelectedGroup : null;
            if (group == null || group.Type != SoldierType.King || group.Count == 0) return null;
            var member = group.Members[0];
            return member != null ? member.GetComponent<HeroSkills>() : null;
        }

        /// <summary>The held-press hint of this button, shown or taken down (also for the photo tests).</summary>
        public void ShowHint(int index, bool show)
        {
            if (index >= 0 && index < buttons.Length) ShowHint(index, buttons[index], show);
        }

        private void ShowHint(int index, HoldTooltip button, bool show)
        {
            if (tooltip == null) return;
            if (!show || Current == null || index >= Current.Slots.Count)
            {
                tooltip.SetActive(false);
                return;
            }
            tooltipText.text = Describe(Current, index);
            var rect = (RectTransform)tooltip.transform;
            var at = button.transform;
            // over the held button, at the height the HUD gave it
            rect.position = new Vector3(at.position.x, rect.position.y, rect.position.z);
            tooltip.SetActive(true);
        }

        /// <summary>«Name» over what it does, in this hero level's numbers; a locked one says when it opens.</summary>
        public static string Describe(HeroSkills skills, int index)
        {
            var skill = skills.Slots[index].Balance;
            var level = skills.Level;
            var power = skill.PowerAt(level);
            // a share of health and a multiplier both read as whole percent
            var percent = Mathf.RoundToInt((skill.id == HeroSkills.KingHeal ? power : power - 1f) * 100f);
            var text = Localization.Get($"#skill_{skill.id}") + "\n" +
                       Localization.Format($"#skill_{skill.id}_desc", percent, Mathf.RoundToInt(skill.DurationAt(level)), Mathf.RoundToInt(skill.cooldownSeconds));
            if (!skills.IsOpen(index)) text += "\n" + Localization.Format("#skill_locked", skill.unlockLevel);
            return text;
        }

        /// <summary>The screen's edge glows gold while nothing of the player's can be hurt.</summary>
        private void ShowBlessing()
        {
            var on = HeroBlessing.Active;
            if (_blessingGlow == null)
            {
                if (!on) return;
                _blessingGlow = BuildGlow();
            }
            if (_blessingGlow.gameObject.activeSelf != on) _blessingGlow.gameObject.SetActive(on);
            if (!on) return;
            var c = _blessingGlow.color;
            c.a = 0.6f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f);
            _blessingGlow.color = c;
        }

        /// <summary>A screen-wide picture, clear in the middle and gold towards the edges, under the rest of the HUD.</summary>
        private Image BuildGlow()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    var v = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    var edge = Mathf.Max(u, v);
                    pixels[y * size + x] = new Color(1f, 0.82f, 0.3f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, edge)));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var go = new GameObject("BlessingGlow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
            go.transform.SetAsFirstSibling();
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            image.raycastTarget = false;
            return image;
        }
    }
}
