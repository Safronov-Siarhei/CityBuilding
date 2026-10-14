using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Ties the HUD to the edges of the screen instead of its middle.
    ///
    /// Every builder in this generator positions its piece from the canvas centre, in a 1920x1080
    /// reference -- which is exact on a 16:9 screen and wrong on anything else. On a 20:9 phone the
    /// canvas is 2400 wide, so the corner pieces sat 240 units in from the edges with empty margins
    /// outside them; on a 16:10 tablet or a 4:3 window it is narrower than 1920 and they went off
    /// the edge. Re-anchoring each piece to the corner it belongs to keeps its 16:9 position exactly
    /// while letting the screen's shape decide where that corner is.
    ///
    /// The canvas scaler's Expand mode is the other half (see ConfigureScaler): the canvas is never
    /// smaller than 1920x1080 in either direction, so a 16:9 layout always fits and a different
    /// shape only adds room.
    /// </summary>
    public static partial class SetupProject
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);

        private static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        /// <summary>The HUD's standing pieces, each moved onto the corner it lives in. Direct children of the safe-area root only.</summary>
        private static void PinHudToEdges(Transform uiRoot)
        {
            // Everything else is anchored to its own edge where it is built (SetupProjectHud).
            Pin(uiRoot, "Minimap", BottomLeft);
            Pin(uiRoot, "MinimapRing", BottomLeft);
        }

        /// <summary>
        /// Moves a centre-anchored rect's anchor to `anchor` without moving it on a 16:9 screen:
        /// the anchor point shifts by (anchor - 0.5) of the reference, so the offset shifts back by
        /// the same amount. The pivot is untouched, which is why this works for any pivot.
        /// </summary>
        private static void Pin(Transform uiRoot, string childName, Vector2 anchor)
        {
            var child = uiRoot.Find(childName);
            if (child == null) return;

            var rect = (RectTransform)child;
            var oldAnchor = rect.anchorMin;
            var shift = Vector2.Scale(anchor - oldAnchor, ReferenceResolution);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition -= shift;
        }
    }
}
