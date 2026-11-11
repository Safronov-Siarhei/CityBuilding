using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's stick (the user, 2026-11-10/11): it appears where the finger touches, the knob
    /// follows the finger up to the ring's edge, and it is gone when the finger lifts. A mouse drag
    /// does the same on a PC, and WASD/arrows steer too. Also draws the quiet hint under the middle
    /// of the screen: a ghost ring with a dot gliding in it and one line, faded out once the player
    /// has run a little. Sprites are drawn here (two soft circles), so it needs no art.
    /// </summary>
    public class FloatingJoystick : MonoBehaviour
    {
        private RectTransform _base, _knob, _hint, _hintDot;
        private CanvasGroup _stick, _hintGroup;
        private Canvas _canvas;
        private float _radius, _dead;
        private Vector2 _origin;
        private float _hintDelay, _hintFadeAfter, _moved, _age;

        /// <summary>The stick's reading: length 0..1, screen up = +y.</summary>
        public Vector2 Value { get; private set; }

        public static FloatingJoystick Create(float radius, float knob, float deadzone, float hintDelay, float hintFadeAfter, string hintText)
        {
            var go = new GameObject("Joystick", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var j = go.AddComponent<FloatingJoystick>();
            j._canvas = canvas;
            j._radius = radius;
            j._dead = deadzone;
            j._hintDelay = hintDelay;
            j._hintFadeAfter = hintFadeAfter;

            var ring = Circle(128, 0.86f, 1f);
            var disc = Circle(64, 0f, 1f);
            j._stick = new GameObject("Stick", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            j._stick.transform.SetParent(go.transform, false);
            j._stick.alpha = 0f;
            j._stick.blocksRaycasts = false;
            j._base = Image(j._stick.transform, "Ring", ring, radius * 2f, new Color(1f, 0.97f, 0.88f, 0.55f));
            j._base.anchorMin = j._base.anchorMax = Vector2.zero;
            j._knob = Image(j._base, "Knob", disc, knob * 2f, new Color(1f, 0.97f, 0.88f, 0.8f));

            // the hint: a ghost of the stick low in the middle, a dot gliding in it, one line under it
            j._hintGroup = new GameObject("Hint", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            j._hintGroup.transform.SetParent(go.transform, false);
            j._hintGroup.alpha = 0f;
            j._hintGroup.blocksRaycasts = false;
            j._hint = Image(j._hintGroup.transform, "Ghost", ring, radius * 1.4f, new Color(1f, 1f, 1f, 0.45f));
            j._hint.anchorMin = j._hint.anchorMax = new Vector2(0.5f, 0f);
            j._hint.anchoredPosition = new Vector2(0f, radius * 1.4f);
            j._hintDot = Image(j._hint, "Dot", disc, knob * 1.4f, new Color(1f, 1f, 1f, 0.7f));
            var textGO = new GameObject("Line", typeof(RectTransform), typeof(Text), typeof(Shadow));
            textGO.transform.SetParent(j._hint, false);
            var rect = (RectTransform)textGO.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(520f, 40f);
            var text = textGO.GetComponent<Text>();
            text.font = UI.UiTheme.Regular;
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(1f, 1f, 1f, 0.85f);
            text.text = hintText;
            text.raycastTarget = false;
            textGO.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return j;
        }

        /// <summary>Put away (a scene has the player's say): no ring, no hint, no reading.</summary>
        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                Value = Vector2.zero;
                _stick.alpha = 0f;
                _hintGroup.alpha = 0f;
            }
        }

        private bool _locked;

        private void Update()
        {
            if (_locked) return;
            var dt = Time.unscaledDeltaTime;
            _age += dt;
            var pressed = false;
            var at = Vector2.zero;
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed)
            {
                pressed = true;
                at = ts.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pressed = true;
                at = Mouse.current.position.ReadValue();
            }
            var scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            var v = Vector2.zero;
            if (pressed)
            {
                if (_stick.alpha < 0.5f) _origin = at;                 // the stick appears where the finger lands
                var off = (at - _origin) / scale;
                if (off.magnitude > _radius) off = off.normalized * _radius;
                v = off / _radius;
                _base.anchoredPosition = _origin / scale;
                _knob.anchoredPosition = off;
                _stick.alpha = 1f;
            }
            else
            {
                _stick.alpha = 0f;
                var k = Keyboard.current;
                if (k != null)
                {
                    v.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
                    v.y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
                    if (v.sqrMagnitude > 1f) v.Normalize();
                }
            }
            Value = v.magnitude < _dead ? Vector2.zero : v;
            Hint(dt, Value.sqrMagnitude > 0f);
        }

        /// <summary>Steers from outside (the batchmode check, which has no finger): also runs the hint's clock.</summary>
        public void Feed(Vector2 v, float dt)
        {
            enabled = false;
            Value = v.magnitude < _dead ? Vector2.zero : Vector2.ClampMagnitude(v, 1f);
            _age += dt;
            Hint(dt, Value.sqrMagnitude > 0f);
        }

        private void Hint(float dt, bool moving)
        {
            if (moving) _moved += dt;
            var want = _age >= _hintDelay && _moved < _hintFadeAfter ? 1f : 0f;
            _hintGroup.alpha = Mathf.MoveTowards(_hintGroup.alpha, want, dt * 2f);
            // the dot glides out and back, now this way, now that
            var a = Mathf.Floor(_age / 1.6f) * 1.3f;
            var r = Mathf.Sin(Mathf.Repeat(_age, 1.6f) / 1.6f * Mathf.PI) * _hint.sizeDelta.x * 0.3f;
            _hintDot.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        /// <summary>The game's own look (the user's playtest: «джойстик выглядит дёшево»): a dark
        /// glass well in a gold rim for the base, a polished gold knob with its own rim and a glint;
        /// the hint's ghost the same rim, faint.</summary>
        public void Skin(Sprite disc, Sprite rim, Color well, Color gold)
        {
            if (disc == null || rim == null) return;
            var b = _base.GetComponent<Image>();
            b.sprite = disc;
            b.color = well;
            Image(_base, "Rim", rim, _base.sizeDelta.x, new Color(1f, 1f, 1f, 0.95f)).SetAsFirstSibling();
            _base.Find("Rim").SetSiblingIndex(0);
            var k = _knob.GetComponent<Image>();
            k.sprite = disc;
            k.color = gold;
            Image(_knob, "Rim", rim, _knob.sizeDelta.x, Color.white);
            var glint = Image(_knob, "Glint", Circle(32, 0f, 1f), _knob.sizeDelta.x * 0.34f, new Color(1f, 1f, 0.92f, 0.55f));
            glint.anchoredPosition = new Vector2(-_knob.sizeDelta.x * 0.16f, _knob.sizeDelta.y * 0.18f);
            _knob.SetAsLastSibling();
            var h = _hint.GetComponent<Image>();
            h.sprite = rim;
            h.color = new Color(1f, 1f, 1f, 0.5f);
        }

        private static RectTransform Image(Transform parent, string name, Sprite sprite, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }

        /// <summary>A soft white circle: a disc (inner 0) or a ring (inner as a share of the radius).</summary>
        private static Sprite Circle(int size, float inner, float outer)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var half = size * 0.5f;
            var soft = 1.5f / half;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                    var a = Mathf.Clamp01((outer - d) / soft);
                    if (inner > 0f) a *= Mathf.Clamp01((d - inner) / soft);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
