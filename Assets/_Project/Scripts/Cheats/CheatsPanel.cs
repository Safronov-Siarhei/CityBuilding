using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>
    /// The cheat window, reachable on a PHONE (was the «Отладка» panel of 2026-10-16; turned into
    /// «Читы» with tabs by the user, 2026-10-31). A «Читы» button in the top-right corner opens a
    /// plain draggable window with eight tabs:
    ///
    /// - Ресурсы: every resource to an exact amount (a step of 1/10/100/1000, or a typed number),
    ///   storehouses filled, crystals, the old «∞» switch;
    /// - Здания: research opened per building and level, the building in focus (or all of its
    ///   kind) to any level, everything under way finished, everything mended, instant building;
    /// - Жители: people added or taken, the sick cured or made ill;
    /// - Армия: units researched, a squad of any tier at the screen's centre, invulnerability;
    /// - Герои: each hero given free, to any level, healed, raised, skills without cooldown;
    /// - Орки: raids paused, a raid now of any size and level, the wait ended, every orc killed,
    ///   the camps one by one (fly there, rebuild at a level, send a squad), the Invasion's waves;
    /// - Мир: game speed, the day ended, the fog lifted or laid back, lighthouses recharged, ships;
    /// - Служебное: FPS, frame time, draw calls and triangles, save/load a cheat slot, restart.
    ///
    /// IMGUI, deliberately: it is scaffolding, built with no assets, and costs one button while
    /// closed. While the window is open the world's input is blocked (ModalGate), so a tap on it
    /// does not also land on the map. A release build (-release) defines CB_RELEASE and this
    /// compiles to an empty component (the user, 2026-10-31: cheats leave the release).
    /// </summary>
    public partial class CheatsPanel : MonoBehaviour
    {
#if !CB_RELEASE
        private static readonly string[] TabNames = { "Ресурсы", "Здания", "Жители", "Армия", "Герои", "Орки", "Мир", "Служебное" };

        private bool _open;
        private int _tab;
        private Vector2 _scroll;
        private Rect _window;
        private float _fps;
        private float _fpsTimer;
        private int _fpsFrames;
        private GUIStyle _button;
        private GUIStyle _label;
        private GUIStyle _field;
        private GUIStyle _windowStyle;
        private float _scale = 1f;
        private string _message = string.Empty;
        // Rebuilt only when the reading changes (twice a second): OnGUI runs twice a frame, and
        // formatting the label each time would be garbage on every frame of the game.
        private string _toggleLabel = "Читы";

        private float H => 44f * _scale;

        private void Update()
        {
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer < 0.5f) return;
            _fps = _fpsFrames / _fpsTimer;
            _toggleLabel = "Читы " + Mathf.RoundToInt(_fps);
            _fpsFrames = 0;
            _fpsTimer = 0f;
            SampleRender();
        }

        private void OnDisable() => StopRender();

        private void EnsureStyles()
        {
            _scale = Mathf.Max(1f, Screen.height / 720f);
            if (_button != null) return;
            var font = Mathf.RoundToInt(19 * _scale);
            _button = new GUIStyle(GUI.skin.button) { fontSize = font };
            _label = new GUIStyle(GUI.skin.label) { fontSize = font, wordWrap = true };
            _label.normal.textColor = Color.white;
            _field = new GUIStyle(GUI.skin.textField) { fontSize = font, alignment = TextAnchor.MiddleLeft };
            _windowStyle = new GUIStyle(GUI.skin.window) { fontSize = font, padding = new RectOffset(10, 10, Mathf.RoundToInt(34 * _scale), 10) };
            // a finger needs a scrollbar it can hit
            GUI.skin.verticalScrollbar.fixedWidth = 28f * _scale;
            GUI.skin.verticalScrollbarThumb.fixedWidth = 28f * _scale;
            var w = Mathf.Min(Screen.width - 24f, 820f * _scale);
            _window = new Rect(Screen.width - w - 12f, 60f * _scale, w, Screen.height - 80f * _scale);
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = _scale;
            var toggle = new Rect(Screen.width - 150f * s - 12f * s, 150f * s, 150f * s, H);
            if (!_open)
            {
                if (GUI.Button(toggle, _toggleLabel, _button)) SetOpen(true);
                return;
            }
            _window = GUI.Window(0x43484541, _window, DrawWindow, "Читы", _windowStyle);
        }

        private void SetOpen(bool open)
        {
            _open = open;
            ModalGate.SetBlocked(open);
            if (open) StartRender();
            else StopRender();
        }

        private void DrawWindow(int id)
        {
            var s = _scale;
            if (GUI.Button(new Rect(_window.width - 130f * s, 2f, 124f * s, 30f * s), "Закрыть", _button))
            {
                SetOpen(false);
                return;
            }

            _tab = GUILayout.SelectionGrid(_tab, TabNames, 4, _button, GUILayout.Height(H * 2f));
            if (_message.Length > 0) GUILayout.Label(_message, _label);

            // Dragging the content scrolls it: a phone has no wheel, and IMGUI's scroll view does
            // not follow a finger on its own.
            var e = Event.current;
            if (e.type == EventType.MouseDrag && e.mousePosition.y > H * 2.5f)
            {
                _scroll.y -= e.delta.y;
                e.Use();
            }
            _scroll = GUILayout.BeginScrollView(_scroll, false, true);
            switch (_tab)
            {
                case 0: DrawResources(); break;
                case 1: DrawBuildings(); break;
                case 2: DrawPeople(); break;
                case 3: DrawArmy(); break;
                case 4: DrawHeroes(); break;
                case 5: DrawOrcs(); break;
                case 6: DrawWorld(); break;
                case 7: DrawService(); break;
            }
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, _window.width - 140f * s, 32f * s));
        }

        // -- small layout helpers -------------------------------------------------------------

        private bool Button(string text, float width = 0f) =>
            width > 0f
                ? GUILayout.Button(text, _button, GUILayout.Height(H), GUILayout.Width(width * _scale))
                : GUILayout.Button(text, _button, GUILayout.Height(H));

        private void Label(string text, float width = 0f)
        {
            if (width > 0f) GUILayout.Label(text, _label, GUILayout.Width(width * _scale));
            else GUILayout.Label(text, _label);
        }

        private void Header(string text)
        {
            GUILayout.Space(8f * _scale);
            GUILayout.Label("— " + text + " —", _label);
        }

        private bool Switch(string text, bool on, float width = 0f)
        {
            if (Button((on ? "[✓] " : "[  ] ") + text, width)) return !on;
            return on;
        }

        private void Say(string text)
        {
            _message = text;
            EventLogManager.Instance?.Log("[Чит] " + text);
        }

        /// <summary>The ground point at the middle of the screen: where a squad or a ship appears.</summary>
        private static Vector3 ScreenCentreGround()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, 2000f)) return hit.point;
            var plane = new Plane(Vector3.up, Vector3.zero);
            return plane.Raycast(ray, out var d) ? ray.GetPoint(d) : Vector3.zero;
        }
#endif
    }
}
