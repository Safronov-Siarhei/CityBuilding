using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The interface, photographed and checked in the running game -- the skin (style "D": parchment,
    /// dark wood, gold, Rubik) is a look, and a look is only verified by being seen. The few things
    /// worth asserting are the ones a broken skin hides silently: a label left white on parchment
    /// (unreadable), and the old Arial face still in use.
    /// </summary>
    public class HudPhotoTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map2";

        private static bool _sceneLoaded;
        private static Vector3 _townHall;

        [UnitySetUp]
        public IEnumerator BuildATownOnce()
        {
            LogAssert.ignoreFailingMessages = true;
            if (_sceneLoaded) yield break;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            var hall = PlaytestWorld.Building(BuildingIds.TownHall);
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(hall.footprintSize + new Vector2Int(4, 4));
            var placed = PlaytestWorld.Place(hall, origin + new Vector2Int(2, 2));
            _townHall = placed.transform.position;
            Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();

            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Wood, 148);
            resources.SetAmount(ResourceType.Stone, 62);
            resources.SetAmount(ResourceType.Iron, 14);
            resources.SetAmount(ResourceType.Gold, 3);
            resources.SetAmount(ResourceType.Coins, 85);
            resources.SetAmount(ResourceType.Food, 120);

            // The hotbar comes back once the mandatory placement is over, and the bar re-lays
            // itself on the next resource change -- a few frames for both.
            for (var i = 0; i < 5; i++) yield return null;
            _sceneLoaded = true;
        }

        [OneTimeTearDown]
        public void ForgetTheScene()
        {
            _sceneLoaded = false;
        }

        [Test]
        public void NoLabelOnParchmentIsWhite()
        {
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var backing = WrittenOn(text);
                if (backing == null || backing.sprite == null) continue;
                var name = backing.sprite.name;
                if (name == "pill_wood" || name == "pill_wood_light") continue;
                if (!(name.StartsWith("panel") || name.StartsWith("pill") || name.StartsWith("card"))) continue;

                Color.RGBToHSV(text.color, out _, out var s, out var v);
                Assert.IsFalse(v > 0.85f && s < 0.2f,
                    $"'{text.name}' under '{backing.name}' is written in white on parchment -- it cannot be read.");
            }
        }

        [Test]
        public void TheInterfaceIsSetInRubik()
        {
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Assert.IsNotNull(text.font, $"'{text.name}' has no font.");
                StringAssert.StartsWith("Rubik", text.font.name, $"'{text.name}' is still set in {text.font.name}.");
            }
        }

        [UnityTest]
        public IEnumerator Photograph_TheHud()
        {
            yield return PlaytestCapture.ShootWithUi("hud", _townHall, 34f, 52f, 20f);
        }

        /// <summary>
        /// The screens the game has to look right on: a 20:9 phone (most of the test fleet), a
        /// 16:10 tablet (the Redmi Pad, 2000x1200), plain 16:9, and a 4:3 window on a PC. The HUD
        /// used to be laid out from the screen's centre for 16:9 only -- on the phone it stood in
        /// from both edges, on the tablet and the window its corners went off the screen.
        /// </summary>
        private static readonly Vector2Int[] ScreenShapes =
        {
            new Vector2Int(2400, 1080),
            new Vector2Int(2000, 1200),
            new Vector2Int(1920, 1080),
            new Vector2Int(1024, 768),
        };

        // The HUD's standing pieces, which must neither leave the screen nor sit on each other.
        private static readonly string[] HudPieces =
        {
            "MenuButton", "SaveButton", "WorkforceButton", "SettlementStatusPill",
            "TaxRate", "EventFeed", "Happiness", "Army", "Migration", "Minimap",
            "BuildingMenu/Hotbar", "BuildingMenu/CategoryBar",
        };

        [UnityTest]
        public IEnumerator TheHudFitsEveryScreenShape()
        {
            if (!PlaytestCapture.Available)
            {
                Debug.Log("[Playtest] No graphics device -- the HUD cannot be laid out at another screen size. Re-run without -nographics.");
                yield break;
            }

            var failures = new List<string>();
            foreach (var shape in ScreenShapes)
            {
                var label = $"{shape.x}x{shape.y}";
                yield return PlaytestCapture.ShootWithUiAt($"hud-{label}", shape.x, shape.y, _townHall, 34f, 52f, 20f,
                    () => CheckLayout(label, failures));
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private static void CheckLayout(string screen, List<string> failures)
        {
            var safeArea = Object.FindAnyObjectByType<SafeAreaFitter>().GetComponent<RectTransform>();
            var screenRect = safeArea.rect;
            const float tolerance = 1f;

            // Every visible thing inside the screen.
            foreach (var graphic in safeArea.GetComponentsInChildren<Graphic>(false))
            {
                if (!graphic.enabled || graphic.color.a <= 0.01f) continue;
                if (graphic is Text text && string.IsNullOrEmpty(text.text)) continue;
                if (graphic.GetComponentInParent<Mask>() != null && graphic.GetComponent<Mask>() == null) continue;

                var r = RectIn(safeArea, graphic.rectTransform);
                if (r.xMin < screenRect.xMin - tolerance || r.xMax > screenRect.xMax + tolerance
                    || r.yMin < screenRect.yMin - tolerance || r.yMax > screenRect.yMax + tolerance)
                {
                    failures.Add($"{screen}: '{Path(graphic.transform, safeArea)}' leaves the screen ({r} outside {screenRect}).");
                }
            }

            // The standing pieces, and each resource chip, clear of one another.
            var pieces = new List<(string name, Rect rect)>();
            foreach (var name in HudPieces)
            {
                var piece = safeArea.Find(name);
                if (piece == null || !piece.gameObject.activeInHierarchy) continue;
                pieces.Add((name, RectIn(safeArea, (RectTransform)piece)));
            }
            var resourceRow = safeArea.Find("ResourceHUD");
            if (resourceRow != null)
            {
                foreach (Transform chip in resourceRow)
                {
                    if (chip.name.StartsWith("Chip_") && chip.gameObject.activeInHierarchy) pieces.Add((chip.name, RectIn(safeArea, (RectTransform)chip)));
                }
            }

            for (var i = 0; i < pieces.Count; i++)
            {
                for (var j = i + 1; j < pieces.Count; j++)
                {
                    var a = Shrink(pieces[i].rect, tolerance);
                    var b = Shrink(pieces[j].rect, tolerance);
                    if (a.Overlaps(b)) failures.Add($"{screen}: '{pieces[i].name}' overlaps '{pieces[j].name}' ({pieces[i].rect} / {pieces[j].rect}).");
                }
            }
        }

        private static Rect RectIn(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                var local = (Vector2)space.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect Shrink(Rect rect, float by)
        {
            return Rect.MinMaxRect(rect.xMin + by, rect.yMin + by, rect.xMax - by, rect.yMax - by);
        }

        private static string Path(Transform t, Transform root)
        {
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        [UnityTest]
        public IEnumerator Photograph_TheExitDialog()
        {
            var dialog = Object.FindAnyObjectByType<ExitToMenuController>(FindObjectsInactive.Include);
            if (dialog != null) dialog.OpenDialog();
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-exit-dialog", _townHall, 34f, 52f, 20f);
            if (dialog != null) dialog.CloseDialog();
        }

        [UnityTest]
        public IEnumerator Photograph_TheWorkforcePanel()
        {
            var panel = Object.FindAnyObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            if (panel != null) panel.OpenPanel();
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-workforce", _townHall, 34f, 52f, 20f);
            if (panel != null) panel.ClosePanel();
        }

        /// <summary>
        /// What a label is drawn on: an image above it in the hierarchy, or -- the case the resource
        /// chips are built as -- an earlier sibling it sits inside, drawn just before it. Checking
        /// only the parents missed the chips, which is how white numbers on parchment shipped.
        /// </summary>
        private static Image WrittenOn(Text text)
        {
            var parent = text.transform.parent;
            if (parent != null)
            {
                var centre = text.rectTransform.TransformPoint(text.rectTransform.rect.center);
                for (var i = text.transform.GetSiblingIndex() - 1; i >= 0; i--)
                {
                    var sibling = parent.GetChild(i).GetComponent<Image>();
                    if (sibling == null || sibling.sprite == null || !sibling.gameObject.activeSelf) continue;
                    var local = sibling.rectTransform.InverseTransformPoint(centre);
                    if (sibling.rectTransform.rect.Contains(local)) return sibling;
                }
            }

            for (var t = text.transform.parent; t != null; t = t.parent)
            {
                var image = t.GetComponent<Image>();
                if (image != null && image.sprite != null) return image;
                if (t.GetComponent<Canvas>() != null) return null;
            }
            return null;
        }
    }
}
