using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The event feed in the running game: a real game event puts an icon up with the right
    /// picture, a tap on it takes the camera to where the event happened, and the column looks
    /// right (photographed -- a look is only verified by being seen).
    /// </summary>
    public class EventFeedTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        private static bool _sceneLoaded;
        private static Vector3 _townHall;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

                var hall = PlaytestWorld.Building(BuildingIds.TownHall);
                var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(hall.footprintSize + new Vector2Int(4, 4));
                _townHall = PlaytestWorld.Place(hall, origin + new Vector2Int(2, 2)).transform.position;
                Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();
                _sceneLoaded = true;
            }

            Time.timeScale = 1f;
            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
            Research.ResearchManager.Instance?.CompleteEverything();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (type != ResourceType.Population) ResourceManager.Instance.SetAmount(type, 5000);
            }

            ClearFeed();
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
        }

        [OneTimeTearDown]
        public void ForgetTheScene()
        {
            _sceneLoaded = false;
        }

        [UnityTest]
        public IEnumerator UpgradingABuilding_PutsItsIconUp_AndATapTakesTheCameraThere()
        {
            var well = Place("Well");
            var rig = Object.FindAnyObjectByType<RTSCameraController>();
            rig.FocusOn(_townHall);
            yield return WaitForCamera(rig);

            Assert.IsTrue(well.TryUpgrade(), "The Well could not be upgraded -- the test needs a building that can.");
            yield return null;

            var view = VisibleIconShowing("Well");
            Assert.IsNotNull(view, "Upgrading the Well put no icon showing the Well's picture on screen.");

            var before = Flat(rig.transform.position);
            Assert.Greater(Vector3.Distance(before, Flat(well.transform.position)), 3f, "The camera already looks at the Well; the tap would prove nothing.");

            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return WaitForCamera(rig);

            Assert.Less(Vector3.Distance(Flat(rig.transform.position), Flat(well.transform.position)), 0.5f,
                "A tap on the icon did not bring the camera to the upgraded building.");
            Assert.IsTrue(view.gameObject.activeSelf, "A tapped icon vanished -- it should only lead to the event and go when its time runs out.");

            // ...and it does go then. The longest clock still running bounds the Well icon's own.
            var feed = Object.FindAnyObjectByType<EventFeedController>();
            var remaining = 0f;
            foreach (var slot in feed.Model.Slots) remaining = Mathf.Max(remaining, slot.Remaining);
            yield return new WaitForSecondsRealtime(remaining + 0.3f);
            Assert.IsFalse(view.gameObject.activeSelf, "The tapped icon outlived its timer.");
        }

        [UnityTest]
        public IEnumerator ARaid_FollowsTheOrcs_NotThePortalTheyLeft()
        {
            var feed = Object.FindAnyObjectByType<EventFeedController>();
            var orc = new GameObject("StandInOrc").transform;
            orc.position = _townHall + new Vector3(12f, 0f, 0f);
            var raid = GameEvent.At("raid", EventSeverity.Danger, EventGlyph.Swords, null, _townHall - new Vector3(15f, 0f, 0f), orc);
            EventLogManager.Instance.Post(raid);
            yield return null;

            orc.position = _townHall + new Vector3(0f, 0f, 14f);
            var view = VisibleIcons()[0];
            var rig = Object.FindAnyObjectByType<RTSCameraController>();
            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return WaitForCamera(rig);

            Assert.Less(Vector3.Distance(Flat(rig.transform.position), Flat(orc.position)), 0.5f);
            Object.Destroy(orc.gameObject);
            // Tapping leads to the raid; it does not take the raid's icon down before its time.
            var stillUp = false;
            foreach (var slot in feed.Model.Slots) stillUp |= ReferenceEquals(raid, slot.Event);
            Assert.IsTrue(stillUp, "Tapping the raid's icon took it down -- it should stay until its timer runs out.");
        }

        [UnityTest]
        public IEnumerator TheSameEventAgain_CountsOnOneIcon_AndLogOnlyEventsShowNothing()
        {
            var fence = Place("Fence");
            for (var i = 0; i < 3; i++)
            {
                EventLogManager.Instance.Post(GameEvent.About(fence, "ruin", EventSeverity.Warning, EventGlyph.Ruin));
            }
            EventLogManager.Instance.Log("a line for the history only");
            yield return null;

            // Counted by picture, not in total: the live town may post something of its own meanwhile.
            var fences = 0;
            foreach (var icon in VisibleIcons())
            {
                var picture = icon.transform.Find("Picture").GetComponent<Image>();
                if (picture.enabled && picture.sprite != null && picture.sprite.name == "Fence") fences++;
            }
            Assert.AreEqual(1, fences, "Three identical events should make exactly one icon.");
            Assert.IsFalse(HasLogOnlyIcon(), "A log-only line put an icon up.");
            var counter = VisibleIconShowing("Fence").transform.Find("Counter");
            Assert.IsTrue(counter.gameObject.activeSelf, "The icon does not show its count.");
            Assert.AreEqual("3", counter.GetComponentInChildren<Text>().text);
        }

        /// <summary>
        /// More events than rows: the icon pushed out must leave the screen too. The model reuses
        /// the evicted slot for the new event at once, and the first version of the feed lost
        /// track of which view to hide -- a stale icon stayed up forever.
        /// </summary>
        [UnityTest]
        public IEnumerator AFullFeed_ShowsNoMoreIconsThanItHasRows()
        {
            var feed = Object.FindAnyObjectByType<EventFeedController>();
            var rows = feed.Model.Capacity;
            var pictures = new[] { "Baker", "Well", "Farm", "Sawmill", "Quarry", "Windmill" };
            for (var i = 0; i < rows + 2; i++)
            {
                EventLogManager.Instance.Post(GameEvent.Nowhere("x", EventSeverity.Good, EventGlyph.Star, pictures[i]));
                yield return null;
            }

            // Counting the icons proves nothing: a stale icon left up still makes `rows` of them,
            // with the newest event the one missing. So: the newest is showing, the oldest are not.
            Assert.AreEqual(rows, VisibleIcons().Count);
            Assert.IsNotNull(VisibleIconShowing(pictures[rows + 1]), "The newest event never made it onto the screen.");
            Assert.IsNull(VisibleIconShowing(pictures[0]), "An icon pushed out of the feed is still on screen.");
            Assert.IsNull(VisibleIconShowing(pictures[1]), "An icon pushed out of the feed is still on screen.");
        }

        [UnityTest]
        public IEnumerator IconsFadeAwayOnTheirOwn()
        {
            EventLogManager.Instance.Post(GameEvent.Nowhere("good news", EventSeverity.Good, EventGlyph.Star, "Laboratory"));
            yield return null;
            Assert.AreEqual(1, VisibleIcons().Count);

            var feed = Object.FindAnyObjectByType<EventFeedController>();
            var deadline = Time.unscaledTime + 15f;
            while (feed.Model.Slots.Count > 0 && Time.unscaledTime < deadline) yield return null;

            Assert.AreEqual(0, VisibleIcons().Count, "A good-news icon was still up after 15 seconds.");
        }

        [UnityTest]
        public IEnumerator Photograph_TheFeed()
        {
            var bakery = Place("Baker");
            var fence = Place("Fence");
            var log = EventLogManager.Instance;
            log.Post(GameEvent.About(fence, "Разрушено от ветхости: Забор", EventSeverity.Warning, EventGlyph.Ruin));
            log.Post(GameEvent.About(fence, "Разрушено от ветхости: Забор", EventSeverity.Warning, EventGlyph.Ruin));
            log.Post(GameEvent.ForSettlement("Изучено: Колодец 2", EventSeverity.Good, EventGlyph.Star, "Laboratory"));
            log.Post(GameEvent.At("Орки начали набег (5)", EventSeverity.Danger, EventGlyph.Swords, null, _townHall));
            log.Post(GameEvent.About(bakery, "Пожар: Пекарня", EventSeverity.Danger, EventGlyph.Fire));

            // Half a second in: the pop is over and the rings have started to drain.
            var until = Time.unscaledTime + 0.6f;
            while (Time.unscaledTime < until) yield return null;

            // The newest icon held: its caption out to the right.
            ExecuteEvents.Execute(VisibleIcons()[0].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return null;

            yield return PlaytestCapture.ShootWithUi("event-feed", _townHall, 34f, 52f, 20f);
        }

        // ---------------------------------------------------------------- helpers

        private BuildingInstance Place(string buildingId)
        {
            var data = PlaytestWorld.Building(buildingId);
            Assert.IsNotNull(data, $"No '{buildingId}' in the catalogue.");
            // near the town rather than in the map's first free corner: a corner can sit at the edge
            // the camera is not allowed to reach, and the tests follow the camera to the building
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(data.footprintSize + new Vector2Int(2, 2));
            var instance = PlaytestWorld.Place(data, origin + Vector2Int.one);
            _placed.Add(instance);
            return instance;
        }

        /// <summary>The icons showing, top row first.</summary>
        private static List<EventIconView> VisibleIcons()
        {
            var feed = Object.FindAnyObjectByType<EventFeedController>();
            Assert.IsNotNull(feed, "The scene has no event feed.");

            var icons = new List<EventIconView>();
            foreach (var view in feed.GetComponentsInChildren<EventIconView>(true))
            {
                if (view.gameObject.activeSelf) icons.Add(view);
            }
            icons.Sort((a, b) => b.Rect.anchoredPosition.y.CompareTo(a.Rect.anchoredPosition.y));
            return icons;
        }

        private static EventIconView VisibleIconShowing(string picture)
        {
            foreach (var view in VisibleIcons())
            {
                var image = view.transform.Find("Picture").GetComponent<Image>();
                if (image.enabled && image.sprite != null && image.sprite.name == picture) return view;
            }
            return null;
        }

        private static bool HasLogOnlyIcon()
        {
            foreach (var slot in Object.FindAnyObjectByType<EventFeedController>().Model.Slots)
            {
                if (slot.Event.Severity == EventSeverity.LogOnly) return true;
            }
            return false;
        }

        private static void ClearFeed()
        {
            var feed = Object.FindAnyObjectByType<EventFeedController>();
            if (feed == null || feed.Model == null) return;

            foreach (var slot in new List<EventFeedSlot>(feed.Model.Slots))
            {
                feed.Model.Remove(slot);
            }
            foreach (var view in feed.GetComponentsInChildren<EventIconView>(true)) view.Hide();
        }

        private static IEnumerator WaitForCamera(RTSCameraController rig)
        {
            var deadline = Time.unscaledTime + 3f;
            while (rig.IsFocusing && Time.unscaledTime < deadline) yield return null;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
