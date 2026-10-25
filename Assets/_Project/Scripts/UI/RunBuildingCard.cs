using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A building's card, after the user's sketch (2026-10-25: an Invasion run's first, then the
    /// sandbox's too -- «перенеси так же интерфейс»):
    ///
    ///   the name, «Уровень N», a heart and a shield with their numbers;
    ///   −  Рабочие / Свободно  +   (a workplace only);
    ///   the next level's picture with what it adds drawn over it (+health, +defence, +a place in
    ///   the house, +a worker, +reach) -- gone at the top level, and with it the upgrade;
    ///   «Стоимость улучшения», its price, «Улучшить»;
    ///   «Стоимость починки», its price, «Починить» -- only while damaged;
    ///   «Снести здание», «Закрыть».
    ///
    /// No recruiting: the Barracks hires and the castle buys heroes from the buttons by the «!» at
    /// the bottom of the screen (FocusHireBar). In the sandbox a band opens under the workers --
    /// what the building makes (or that its reach is bare), and a Плавильня's metal buttons -- and
    /// the card grows to fit it (ShowBand); a run's card has no band, as the user drew it.
    /// BuildingInfoPanelController shows this card and owns the buttons.
    /// </summary>
    public class RunBuildingCard : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text level;
        [SerializeField] private Text health;
        [SerializeField] private Text defense;
        [SerializeField] private GameObject workerRow;
        [SerializeField] private Text workers;
        [SerializeField] private Text free;
        [SerializeField] private GameObject nextLevel;
        [SerializeField] private Image nextLevelImage;
        [SerializeField] private RectTransform gains;
        [SerializeField] private GameObject upgradeBlock;
        [SerializeField] private Transform upgradeCostRow;
        [SerializeField] private GameObject upgradeButton;
        [SerializeField] private Text upgradeLock;
        [SerializeField] private GameObject repairBlock;
        [SerializeField] private Transform repairCostRow;
        [SerializeField] private GameObject demolishButton;

        [Header("The sandbox's band: what it makes, a Плавильня's recipes")]
        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private GameObject band;
        [SerializeField] private Text bandText;
        [SerializeField] private RectTransform recipeRow;
        [SerializeField] private float bandHeight = 100f;

        [Header("The next levels' pictures: <building id>_L<level> (Tools/blender/render_icons.py)")]
        [SerializeField] private string[] levelIconKeys = new string[0];
        [SerializeField] private Sprite[] levelIcons = new Sprite[0];

        [Header("What an upgrade adds")]
        [SerializeField] private Sprite gainPlate;
        [SerializeField] private Sprite heartGlyph;
        [SerializeField] private Sprite shieldGlyph;
        [SerializeField] private Sprite houseGlyph;
        [SerializeField] private Sprite workerGlyph;
        [SerializeField] private Sprite radiusGlyph;

        private string _gainsFor;
        private float _cardHeight = -1f;

        /// <summary>Where a Плавильня's metal buttons go (BuildingInfoPanelController builds them).</summary>
        public RectTransform RecipeRow => recipeRow;

        /// <summary>
        /// The band under the workers: `text` (null for none) -- amber when it is a warning -- and
        /// the recipe row when the building has a choice. The card grows by the band's height, its
        /// upper half moving up and its lower half down, so nothing else on it moves against
        /// anything else; with no band it is the run's card exactly.
        /// </summary>
        public void ShowBand(string text, bool warning, bool recipes)
        {
            var shown = !string.IsNullOrEmpty(text) || recipes;
            if (band != null) band.SetActive(shown);
            if (bandText != null)
            {
                bandText.gameObject.SetActive(!string.IsNullOrEmpty(text));
                bandText.text = text ?? string.Empty;
                bandText.color = warning ? new Color(1f, 0.72f, 0.35f) : new Color(1f, 1f, 1f, 0.85f);
            }
            if (recipeRow != null) recipeRow.gameObject.SetActive(recipes);

            if (card == null) return;
            if (_cardHeight < 0f) _cardHeight = card.sizeDelta.y;
            var half = shown ? bandHeight * 0.5f : 0f;
            card.sizeDelta = new Vector2(card.sizeDelta.x, _cardHeight + half * 2f);
            if (top != null) top.anchoredPosition = new Vector2(0f, half);
            if (bottom != null) bottom.anchoredPosition = new Vector2(0f, -half);
        }

        /// <summary>Fills the card for `building`; `fillCostRow` lays a price out (BuildingInfoPanelController's chips).</summary>
        public void Refresh(BuildingInstance building, ProductionBuilding production, Action<Transform, List<ResourceAmount>> fillCostRow, Func<float, string> clock)
        {
            var data = building.Data;
            if (title != null) title.text = data.LocalizedName;
            if (level != null) level.text = Localization.Format("#building_level_run", building.Level);
            if (health != null) health.text = $"{building.CurrentHealth}/{building.MaxHealth}";
            if (defense != null) defense.text = building.Defense.ToString();

            var hasWorkers = production != null && production.MaxWorkers > 0;
            if (workerRow != null) workerRow.SetActive(hasWorkers);
            if (hasWorkers)
            {
                if (workers != null) workers.text = Localization.Format("#building_workers", production.AssignedWorkers, production.MaxWorkers);
                if (free != null) free.text = Localization.Format("#building_free", CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0);
            }

            // the next level: its picture and gains, its price -- none at the top level
            var upgradeCost = building.GetUpgradeCost();
            var site = building.Site;
            var hasNext = upgradeCost != null && building.Level < BuildingInstance.MaxLevel;
            var showUpgrade = hasNext || (site != null && site.IsUpgrade);
            if (nextLevel != null) nextLevel.SetActive(showUpgrade);
            if (upgradeBlock != null) upgradeBlock.SetActive(showUpgrade || site != null);
            if (showUpgrade)
            {
                // an upgrade under way levels up when it is done: the next level is still the one above
                var next = building.Level + 1;
                if (nextLevelImage != null) nextLevelImage.sprite = LevelIcon(data.buildingName, next);
                ShowGains(data, next);
            }
            var open = site == null && hasNext && building.NextLevelResearched;
            if (upgradeButton != null) upgradeButton.SetActive(open);
            if (upgradeCostRow != null) upgradeCostRow.gameObject.SetActive(open);
            fillCostRow(upgradeCostRow, open ? upgradeCost : null);
            if (upgradeLock != null)
            {
                upgradeLock.gameObject.SetActive(!open && (site != null || hasNext));
                if (site != null) upgradeLock.text = Localization.Format(site.IsUpgrade ? "#building_upgrading" : "#building_constructing", clock(site.Remaining));
                else if (hasNext) upgradeLock.text = Localization.Format("#building_upgrade_locked", building.Level + 1);
            }

            var repairCost = building.GetRepairCost();
            if (repairBlock != null) repairBlock.SetActive(repairCost != null);
            fillCostRow(repairCostRow, repairCost);

            if (demolishButton != null) demolishButton.SetActive(building.CanDemolish);
        }

        private Sprite LevelIcon(string buildingId, int level)
        {
            var key = $"{buildingId}_L{level}";
            for (var i = 0; i < levelIconKeys.Length && i < levelIcons.Length; i++)
            {
                if (levelIconKeys[i] == key) return levelIcons[i];
            }
            return null;
        }

        /// <summary>The badges over the next level's picture: each number that level raises, and by how much.</summary>
        private void ShowGains(BuildingData data, int next)
        {
            var key = $"{data.buildingName}:{next}";
            if (gains == null || _gainsFor == key) return;
            _gainsFor = key;
            for (var i = gains.childCount - 1; i >= 0; i--) Destroy(gains.GetChild(i).gameObject);

            var now = data.LevelStats(next - 1);
            var then = data.LevelStats(next);
            Gain(heartGlyph, then.maxHealth - now.maxHealth);
            Gain(shieldGlyph, then.defense - now.defense);
            Gain(houseGlyph, then.housingCapacity - now.housingCapacity);
            Gain(workerGlyph, then.maxWorkers - now.maxWorkers);
            Gain(radiusGlyph, Mathf.Max(then.harvestRadius - now.harvestRadius, then.serviceRadius - now.serviceRadius));
        }

        private void Gain(Sprite glyph, int amount)
        {
            if (amount <= 0) return;
            var plate = new GameObject("Gain", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            plate.transform.SetParent(gains, false);
            var image = plate.GetComponent<Image>();
            image.sprite = gainPlate;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.13f, 0.06f, 0.92f);
            image.raycastTarget = false;
            var layout = plate.GetComponent<LayoutElement>();
            layout.preferredWidth = 112f;
            layout.preferredHeight = 48f;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(plate.transform, false);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = glyph;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.color = new Color(1f, 0.86f, 0.5f);
            var iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-28f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);

            var text = new GameObject("Amount", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(plate.transform, false);
            text.font = UiTheme.Heavy;
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.text = $"+{amount}";
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(22f, 0f);
            textRect.sizeDelta = new Vector2(66f, 40f);
        }
    }
}
