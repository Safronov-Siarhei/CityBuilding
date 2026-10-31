#if !CB_RELEASE
using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Research;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>The cheat window's tabs for the economy: resources, buildings and research, people.</summary>
    public partial class CheatsPanel
    {
        private static readonly int[] Steps = { 1, 10, 100, 1000 };
        private static readonly string[] StepLabels = { "±1", "±10", "±100", "±1000" };

        private int _step = 2;
        private string _typed = "500";
        private bool _wholeKind;
        // the catalogue's building topics by building, in catalogue order; built once on first use
        private List<KeyValuePair<string, List<ResearchTopic>>> _buildingTopics;
        private List<KeyValuePair<string, List<ResearchTopic>>> _unitTopics;

        private int Typed => int.TryParse(_typed, out var n) ? Mathf.Max(0, n) : 0;

        // -- Ресурсы ------------------------------------------------------------------------

        private void DrawResources()
        {
            var rm = ResourceManager.Instance;
            if (rm == null)
            {
                Label("Нет ResourceManager.");
                return;
            }

            GUILayout.BeginHorizontal();
            var infinite = Switch("Ресурсы ∞", rm.InfiniteResources);
            if (infinite != rm.InfiniteResources) rm.SetInfiniteResources(infinite);
            if (Button("Заполнить склады"))
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    if (type == ResourceType.Population) continue;
                    var cap = rm.GetCapacity(type);
                    if (cap > 0) rm.SetAmount(type, cap);
                }
                Say("склады заполнены");
            }
            GUILayout.EndHorizontal();
            if (rm.InfiniteResources) Label("При «∞» траты бесплатны, а точные числа не видны в верхней панели.");

            GUILayout.BeginHorizontal();
            Label("Шаг:", 70f);
            _step = GUILayout.Toolbar(_step, StepLabels, _button, GUILayout.Height(H));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Label("Число для «=»:", 200f);
            _typed = GUILayout.TextField(_typed, 9, _field, GUILayout.Height(H));
            GUILayout.EndHorizontal();

            var step = Steps[_step];
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type == ResourceType.Population) continue;
                var amount = rm.GetAmount(type);
                var cap = rm.GetCapacity(type);
                GUILayout.BeginHorizontal();
                Label($"{ResourceNames.Of(type)}: {amount}{(cap > 0 ? " / " + cap : "")}");
                if (Button("−", 60f)) rm.SetAmount(type, Mathf.Max(0, amount - step));
                if (Button("+", 60f)) rm.SetAmount(type, amount + step);
                if (Button("0", 60f)) rm.SetAmount(type, 0);
                if (Button("=", 60f)) rm.SetAmount(type, Typed);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            Label($"Кристаллы улучшения: {Wallet.UpgradeCrystals}");
            if (Button("−", 60f)) Wallet.UpgradeCrystals = Mathf.Max(0, Wallet.UpgradeCrystals - step);
            if (Button("+", 60f)) Wallet.UpgradeCrystals += step;
            if (Button("0", 60f)) Wallet.UpgradeCrystals = 0;
            if (Button("=", 60f)) Wallet.UpgradeCrystals = Typed;
            GUILayout.EndHorizontal();
        }

        // -- Здания -------------------------------------------------------------------------

        private void DrawBuildings()
        {
            GUILayout.BeginHorizontal();
            ConstructionSite.InstantCheat = Switch("Строить мгновенно", ConstructionSite.InstantCheat);
            if (Button("Достроить всё"))
            {
                var sites = new List<ConstructionSite>(ConstructionSite.All);
                foreach (var site in sites) if (site != null) site.Finish();
                Say($"достроено: {sites.Count}");
            }
            if (Button("Починить всё"))
            {
                var mended = 0;
                foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                {
                    if (b.Data == null || b.CurrentHealth >= b.MaxHealth) continue;
                    b.SetCondition(b.MaxHealth, 0f);
                    mended++;
                }
                Say($"починено: {mended}");
            }
            GUILayout.EndHorizontal();
            Label("Бесплатная стройка — «Ресурсы ∞» на первой вкладке.");

            Header("Уровень здания");
            var focus = BuildingFocus.Instance != null ? BuildingFocus.Instance.Current : null;
            if (focus == null || focus.Data == null)
            {
                Label("Закройте окно, коснитесь здания на карте и откройте снова.");
            }
            else
            {
                Label($"Выбрано: {focus.Data.LocalizedName}, ур. {focus.Level}" +
                      (focus.Site != null ? " (строится)" : ""));
                _wholeKind = Switch("Все такие здания на карте", _wholeKind);
                GUILayout.BeginHorizontal();
                for (var level = 1; level <= BuildingInstance.MaxLevel; level++)
                {
                    if (!Button("Ур. " + level)) continue;
                    var count = 0;
                    if (_wholeKind)
                    {
                        foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                        {
                            if (b.Data != null && b.Data.buildingName == focus.Data.buildingName) { SetBuildingLevel(b, level); count++; }
                        }
                    }
                    else
                    {
                        SetBuildingLevel(focus, level);
                        count = 1;
                    }
                    Say($"{focus.Data.LocalizedName}: ур. {level} ({count} шт.)");
                }
                GUILayout.EndHorizontal();
            }

            Header("Исследования зданий");
            DrawTopics(ref _buildingTopics, ResearchCatalog.BuildingTopics);
        }

        /// <summary>A building straight to `level`: anything under way finished first, full health after.</summary>
        private static void SetBuildingLevel(BuildingInstance building, int level)
        {
            if (building.Site != null) building.Site.Finish();
            building.SetLevel(level);
            building.SetCondition(building.MaxHealth, 0f);
        }

        /// <summary>
        /// A row per building (or unit): its name, «Открыть» for the unlock and «Ур. N» per level
        /// topic, each done one shown in brackets; «Изучить всё» on top.
        /// </summary>
        private void DrawTopics(ref List<KeyValuePair<string, List<ResearchTopic>>> grouped, IReadOnlyList<ResearchTopic> topics)
        {
            var research = ResearchManager.Instance;
            if (research == null)
            {
                Label("Нет ResearchManager.");
                return;
            }
            if (Button("Изучить всё"))
            {
                var n = 0;
                foreach (var topic in topics) if (research.CompleteInstantly(topic.Id)) n++;
                Say($"изучено тем: {n}");
            }

            if (grouped == null)
            {
                grouped = new List<KeyValuePair<string, List<ResearchTopic>>>();
                var at = new Dictionary<string, int>();
                foreach (var topic in topics)
                {
                    if (!at.TryGetValue(topic.TargetId, out var i))
                    {
                        i = grouped.Count;
                        at[topic.TargetId] = i;
                        grouped.Add(new KeyValuePair<string, List<ResearchTopic>>(topic.TargetId, new List<ResearchTopic>()));
                    }
                    grouped[i].Value.Add(topic);
                }
            }

            foreach (var pair in grouped)
            {
                GUILayout.BeginHorizontal();
                Label(pair.Value[0].TargetName, 230f);
                foreach (var topic in pair.Value)
                {
                    var unlock = topic.Kind == ResearchKind.UnlockBuilding || topic.Kind == ResearchKind.UnlockUnit;
                    var text = unlock ? "Открыть" : "Ур." + topic.Level;
                    if (research.IsCompleted(topic.Id)) text = "[" + text + "]";
                    if (Button(text, unlock ? 110f : 72f) && research.CompleteInstantly(topic.Id)) Say("изучено: " + topic.Title);
                }
                GUILayout.EndHorizontal();
            }
        }

        // -- Жители -------------------------------------------------------------------------

        private void DrawPeople()
        {
            var people = CitizenManager.Instance;
            if (people == null)
            {
                Label("Нет CitizenManager.");
                return;
            }
            Label($"Жителей: {people.TotalPopulation} (мест в домах: {people.Capacity}), больных: {people.SickPopulation}");
            GUILayout.BeginHorizontal();
            foreach (var n in new[] { 1, 5, 20 })
            {
                if (Button("+" + n)) { people.AddCitizens(n); Say($"жителей +{n}"); }
            }
            foreach (var n in new[] { 1, 5 })
            {
                if (Button("−" + n)) { people.RemoveCitizens(n); Say($"жителей −{n}"); }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Вылечить всех")) { people.HealSick(people.SickPopulation); Say("все здоровы"); }
            if (Button("Заболеть: 3")) { people.AddSick(3); Say("заболели 3"); }
            GUILayout.EndHorizontal();
        }
    }
}
#endif
