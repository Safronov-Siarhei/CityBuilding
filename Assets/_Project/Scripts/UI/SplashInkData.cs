using System;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The studio splash «Чернильное перо», as exported by Design/branding/depigo/splash/export_splash_unity.py
    /// into UI/Splash/splash_ink.json -- the same generator that makes the approved HTML preview, so every
    /// shape, time and curve lives there and nothing here is tuned by hand. Coordinates are 1920x1080 screen
    /// px with y down (the preview's SVG space); times in seconds from the splash's start.
    /// </summary>
    [Serializable]
    public class SplashInkData
    {
        public float width, height, total;
        public string ink, paper;
        public float[] fadeIn, fadeOut, fadeEase;
        public float skipFade;
        public float zoomFrom;
        public float[] zoomEase, strokeEase;
        public Stroke[] strokes;
        public Dot[] dots;
        public float[] dotScale, dotKeys;
        public Fill fill;
        public Pen pen;

        [Serializable] public class Poly { public float[] p; }

        [Serializable]
        public class Stroke
        {
            public Poly[] polys;
            public float t0, dur, width;
            public bool round, roundCaps;
        }

        [Serializable] public class Dot { public float x, y, r, t0, dur; }

        [Serializable]
        public class Fill
        {
            public float x, y, size, t0, dur, radius, edge, noise, noiseScale;
            public float[] ease;
        }

        [Serializable]
        public class Pen
        {
            public float w, h, pivotX, pivotY;
            public float[] t, x, y;
        }

        public static SplashInkData Parse(string json) => JsonUtility.FromJson<SplashInkData>(json);

        /// <summary>CSS cubic-bezier(x1,y1,x2,y2) easing of u in 0..1 -- the preview's keySplines.</summary>
        public static float Ease(float[] c, float u)
        {
            u = Mathf.Clamp01(u);
            if (c == null || c.Length < 4) return u;
            float lo = 0f, hi = 1f, s = u;
            for (var i = 0; i < 24; i++)
            {
                s = (lo + hi) * 0.5f;
                if (Bez(c[0], c[2], s) < u) lo = s; else hi = s;
            }
            return Bez(c[1], c[3], s);
        }

        private static float Bez(float p1, float p2, float s) =>
            3f * (1f - s) * (1f - s) * s * p1 + 3f * (1f - s) * s * s * p2 + s * s * s;

        public static Color Hex(string hex, Color fallback) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
    }
}
