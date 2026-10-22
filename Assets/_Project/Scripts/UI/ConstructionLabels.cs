using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A hammer and the time left over every building being built or upgraded (ConstructionSite,
    /// the user 2026-10-22), on the HUD canvas: a plaque with the hammer, «0:12» and a thin bar
    /// filling as the work goes. Labels are made as sites appear and reused; nothing runs with no
    /// site standing.
    /// </summary>
    public class ConstructionLabels : MonoBehaviour
    {
        private const float HeightOverBuilding = 3.2f;
        private const float RefreshSeconds = 0.1f;
        private static readonly Vector2 Size = new Vector2(150f, 52f);

        [SerializeField] private Sprite hammer;
        [SerializeField] private Sprite plaque;

        private class Label
        {
            public RectTransform Root;
            public Text Time;
            public RectTransform Fill;
        }

        private readonly List<Label> _labels = new List<Label>();
        private RectTransform _rect;
        private float _refreshIn;

        private void LateUpdate()
        {
            var sites = ConstructionSite.All;
            if (sites.Count == 0)
            {
                Hide(0);
                return;
            }
            var camera = Camera.main;
            if (camera == null) return;
            if (_rect == null) _rect = (RectTransform)transform;

            _refreshIn -= Time.unscaledDeltaTime;
            var writeText = _refreshIn <= 0f;
            if (writeText) _refreshIn = RefreshSeconds;

            for (var i = 0; i < sites.Count; i++)
            {
                var site = sites[i];
                var label = LabelAt(i);
                var world = site.transform.position + Vector3.up * HeightOverBuilding;
                var screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local))
                {
                    label.Root.gameObject.SetActive(false);
                    continue;
                }
                label.Root.gameObject.SetActive(true);
                label.Root.anchoredPosition = local;
                label.Fill.anchorMax = new Vector2(Mathf.Clamp01(site.Progress), 1f);
                if (writeText)
                {
                    var s = Mathf.CeilToInt(Mathf.Max(0f, site.Remaining));
                    label.Time.text = $"{s / 60}:{s % 60:00}";
                }
            }
            Hide(sites.Count);
        }

        private void Hide(int from)
        {
            for (var i = from; i < _labels.Count; i++)
            {
                if (_labels[i].Root.gameObject.activeSelf) _labels[i].Root.gameObject.SetActive(false);
            }
        }

        private Label LabelAt(int index)
        {
            while (_labels.Count <= index) _labels.Add(Build());
            return _labels[index];
        }

        private Label Build()
        {
            var root = new GameObject("ConstructionLabel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.SetAsFirstSibling();   // under the HUD's own panels
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Size;
            var back = root.GetComponent<Image>();
            back.sprite = plaque;
            back.type = plaque != null ? Image.Type.Sliced : Image.Type.Simple;
            back.color = plaque != null ? Color.white : new Color(0.16f, 0.1f, 0.06f, 0.9f);
            back.raycastTarget = false;

            var icon = new GameObject("Hammer", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(root, false);
            icon.sprite = hammer;
            icon.color = UiTheme.Gold;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(28f, 4f);
            iconRect.sizeDelta = new Vector2(36f, 36f);

            var time = new GameObject("Time", typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
            time.transform.SetParent(root, false);
            time.font = UiTheme.Heavy;
            time.fontSize = 28;
            time.alignment = TextAnchor.MiddleCenter;
            time.color = Color.white;
            time.raycastTarget = false;
            time.GetComponent<Outline>().effectColor = new Color(0.15f, 0.08f, 0.03f, 0.9f);
            var timeRect = time.rectTransform;
            timeRect.anchorMin = timeRect.anchorMax = new Vector2(0.5f, 0.5f);
            timeRect.anchoredPosition = new Vector2(20f, 4f);
            timeRect.sizeDelta = new Vector2(100f, 40f);

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            track.transform.SetParent(root, false);
            track.color = new Color(0.05f, 0.03f, 0.02f, 0.8f);
            track.raycastTarget = false;
            var trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.offsetMin = new Vector2(12f, 6f);
            trackRect.offsetMax = new Vector2(-12f, 12f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(track.transform, false);
            fill.color = UiTheme.Gold;
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            return new Label { Root = root, Time = time, Fill = fillRect };
        }
    }
}
