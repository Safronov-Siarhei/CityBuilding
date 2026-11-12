using CityBuilder.Buildings;
using CityBuilder.Research;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// «What is missing, and where» (the user, 2026-11-11; card Design/features/hints.md), sandbox only.
    /// Walks the chain to the FIRST step the player can take now and points at it (GuideHint):
    /// - a building that waits on another (requiredBuilding): that one's card in the build menu;
    /// - one not researched: its button in the Laboratory's window (no Laboratory: the Laboratory's card);
    /// - a level of a building: the camera flies there, it is held, its card opens, the arrow on «Улучшить»
    ///   (the Laboratory's own: «Улучшить лабораторию» in its window);
    /// - a research waiting on a bigger Laboratory: «Улучшить лабораторию»; on the level before: that one.
    /// </summary>
    public static class BuildHints
    {
        private const int MaxSteps = 8;   // a chain longer than this is a data loop, not a design

        /// <summary>The hints work here: a sandbox game, not a run, the tutorial or a dungeon.</summary>
        public static bool Allowed =>
            !Invasion.InvasionMode.IsActive && !Invasion.InvasionRules.Active && !Tutorial.TutorialMode.IsActive && !Dungeon.DungeonMode.IsActive && !Campaign.CityDefenceMode.IsActive;

        /// <summary>A build-menu card under a padlock: not researched, or waiting on a building not yet standing.</summary>
        public static bool IsLocked(BuildingData data)
        {
            if (data == null || !Allowed) return false;
            if (!ResearchManager.BuildingUnlocked(data.buildingName)) return true;
            return data.requiredBuilding != null && !BuildingInstance.HasAny(data.requiredBuilding.buildingName);
        }

        /// <summary>A locked card tapped: to what it waits on.</summary>
        public static void LeadToBuild(BuildingData data) => LeadToBuild(data, 0);

        /// <summary>A level this building lacks (a soldier, a hero, a ship waits on it): to its «Улучшить».</summary>
        public static void LeadToUpgrade(BuildingInstance building) => LeadToUpgrade(building, 0);

        /// <summary>A research not done yet: to its button in the Laboratory.</summary>
        public static void LeadToResearch(string topicId) => LeadToResearch(ResearchCatalog.ById(topicId), 0);

        private static void LeadToBuild(BuildingData data, int step)
        {
            if (data == null || step > MaxSteps || !Allowed) return;
            if (!ResearchManager.BuildingUnlocked(data.buildingName))
            {
                LeadToResearch(ResearchCatalog.ById(ResearchTopic.UnlockBuildingId(data.buildingName)), step + 1);
                return;
            }
            var required = data.requiredBuilding;
            if (required != null && !BuildingInstance.HasAny(required.buildingName))
            {
                LeadToBuild(Find(required.buildingName) ?? required, step + 1);
                return;
            }

            // buildable: its card, the menu open on its tab
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            if (placer != null && placer.IsSelecting) placer.ClearSelection();
            CloseWindows();
            var menu = Object.FindAnyObjectByType<BuildingCategoryPanel>();
            if (menu == null) return;
            menu.Open(data.category);
            GuideHint.Point(menu.CardOf(data.buildingName));
        }

        private static void LeadToResearch(ResearchTopic topic, int step)
        {
            var research = ResearchManager.Instance;
            if (topic == null || research == null || step > MaxSteps || !Allowed) return;
            var lab = research.Laboratory;
            if (lab == null)
            {
                LeadToBuild(Find(ResearchManager.LaboratoryBuildingId), step + 1);
                return;
            }
            // the level before first, then the Laboratory's size
            while (topic.PrerequisiteTopicId != null && !research.IsCompleted(topic.PrerequisiteTopicId))
            {
                var before = ResearchCatalog.ById(topic.PrerequisiteTopicId);
                if (before == null || ++step > MaxSteps) break;
                topic = before;
            }
            var panel = OpenLaboratory(lab);
            if (panel == null) return;
            if (research.LabLevel < topic.RequiredLabLevel)
            {
                GuideHint.Point(panel.LabUpgradeButton);
                return;
            }
            GuideHint.Point(panel.ActionOf(topic));
        }

        private static void LeadToUpgrade(BuildingInstance building, int step)
        {
            if (building == null || building.Data == null || step > MaxSteps || !Allowed) return;
            if (!building.NextLevelResearched)
            {
                LeadToResearch(ResearchCatalog.ById(ResearchTopic.BuildingLevelId(building.Data.buildingName, building.Level + 1)), step + 1);
                return;
            }
            if (building.Data.buildingName == ResearchManager.LaboratoryBuildingId)
            {
                var panel = OpenLaboratory(building);
                if (panel != null) GuideHint.Point(panel.LabUpgradeButton);
                return;
            }
            var focus = BuildingFocus.Instance;
            if (focus == null) return;
            CloseWindows();
            focus.Select(building);       // the camera flies there, the ring round it
            focus.OpenDetails();          // its card
            var card = Object.FindAnyObjectByType<BuildingInfoPanelController>();
            if (card != null) GuideHint.Point(card.UpgradeButtonRect);
        }

        /// <summary>The Laboratory held and its window up (the camera goes there too).</summary>
        private static ResearchPanelController OpenLaboratory(BuildingInstance lab)
        {
            var panel = Object.FindAnyObjectByType<ResearchPanelController>(FindObjectsInactive.Include);
            if (panel == null) return null;
            if (!panel.IsOpen)
            {
                CloseWindows();
                var focus = BuildingFocus.Instance;
                if (focus != null)
                {
                    focus.Select(lab);
                    focus.OpenDetails();
                }
                if (!panel.IsOpen) panel.Show(lab);
            }
            return panel;
        }

        private static void CloseWindows()
        {
            var card = Object.FindAnyObjectByType<BuildingInfoPanelController>();
            if (card != null && card.IsOpen) card.Close();
            var lab = Object.FindAnyObjectByType<ResearchPanelController>();
            if (lab != null && lab.IsOpen) lab.Close();
        }

        private static BuildingData Find(string buildingId)
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            if (placer == null) return null;
            foreach (var data in placer.AvailableBuildings)
                if (data != null && data.buildingName == buildingId) return placer.Resolve(data);
            return null;
        }
    }
}
