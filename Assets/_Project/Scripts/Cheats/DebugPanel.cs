using System.Collections.Generic;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>
    /// A testing panel that reaches the PHONE (the user's request, 2026-10-16): the other cheats
    /// are Inspector buttons and the F9 toggle needs a keyboard, so nothing late-game could be
    /// tried on a device. A small «Отладка» button in the top-right corner opens it:
    ///
    /// - free resources, raids paused, game speed x1/x3/x6, an FPS readout;
    /// - the orc camps one by one: fly the camera there, rebuild the camp at level 1, 2 or 3;
    /// - a squad of five soldiers of any tier just outside the camp's gate, ordered at its portal
    ///   -- the walls, the gate, the towers and the guards all get a real fight.
    ///
    /// IMGUI, deliberately: it is scaffolding, built and torn down with no assets, and costs one
    /// button while closed. While it is open the world's input is blocked (ModalGate), so a tap
    /// on the panel does not also land on the map. It ships in the build for now; take it out
    /// (or gate it) before a release that strangers will play.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private bool _open;
        private int _campIndex;
        private float _fps;
        private float _fpsTimer;
        private int _fpsFrames;
        private GUIStyle _button;
        private GUIStyle _label;
        private GUIStyle _box;
        private float _scale = 1f;
        // Rebuilt only when the reading changes (twice a second): OnGUI runs twice a frame, and
        // formatting the label each time would be garbage on every frame of the game.
        private string _toggleLabel = "Отладка";

        private static readonly (SoldierType type, string name)[] Tiers =
        {
            (SoldierType.Militia, "Ополчение"), (SoldierType.Spearman, "Копейщики"),
            (SoldierType.Archer, "Лучники"), (SoldierType.ManAtArms, "Латники"),
        };

        private void Update()
        {
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer < 0.5f) return;
            _fps = _fpsFrames / _fpsTimer;
            _toggleLabel = "Отладка " + Mathf.RoundToInt(_fps);
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }

        private void EnsureStyles()
        {
            _scale = Mathf.Max(1f, Screen.height / 720f);
            if (_button != null) return;
            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * _scale) };
            _label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * _scale) };
            _label.normal.textColor = Color.white;
            _box = new GUIStyle(GUI.skin.box);
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = _scale;
            var w = 150f * s;
            var h = 46f * s;
            var toggle = new Rect(Screen.width - w - 12f * s, 150f * s, w, h);
            if (GUI.Button(toggle, _open ? "Закрыть" : _toggleLabel, _button))
            {
                _open = !_open;
                ModalGate.SetBlocked(_open);
            }
            if (!_open) return;

            var panelW = 560f * s;
            var area = new Rect(Screen.width - panelW - 12f * s, toggle.yMax + 8f * s, panelW, Screen.height - toggle.yMax - 20f * s);
            GUI.Box(area, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(area.x + 10f * s, area.y + 10f * s, area.width - 20f * s, area.height - 20f * s));

            var resources = ResourceManager.Instance;
            var raids = OrcRaidManager.Instance;
            GUILayout.Label($"FPS {_fps:0}   кадр {1000f / Mathf.Max(1f, _fps):0.0} мс", _label);

            GUILayout.BeginHorizontal();
            if (resources != null && GUILayout.Button(resources.InfiniteResources ? "Ресурсы: ∞" : "Ресурсы: обычно", _button, GUILayout.Height(h)))
            {
                resources.SetInfiniteResources(!resources.InfiniteResources);
            }
            if (raids != null && GUILayout.Button(raids.RaidsSuspended ? "Набеги: пауза" : "Набеги: идут", _button, GUILayout.Height(h)))
            {
                raids.RaidsSuspended = !raids.RaidsSuspended;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            foreach (var speed in new[] { 1f, 3f, 6f })
            {
                var label = Mathf.Approximately(Time.timeScale, speed) ? $"[x{speed:0}]" : $"x{speed:0}";
                if (GUILayout.Button(label, _button, GUILayout.Height(h))) Time.timeScale = speed;
            }
            GUILayout.EndHorizontal();

            var camps = new List<OrcCamp>(OrcCamp.All);
            if (camps.Count == 0)
            {
                GUILayout.Label("Лагерей нет: поставьте Ратушу.", _label);
                GUILayout.EndArea();
                return;
            }
            _campIndex = Mathf.Clamp(_campIndex, 0, camps.Count - 1);
            var camp = camps[_campIndex];

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", _button, GUILayout.Height(h), GUILayout.Width(70f * s)))
            {
                _campIndex = (_campIndex + camps.Count - 1) % camps.Count;
                Focus(camps[_campIndex]);
            }
            GUILayout.Label($"Лагерь {_campIndex + 1}/{camps.Count}: ур. {camp.Level} (макс {camp.MaxLevel}), " +
                            $"стража {camp.Guards.Count}{(camp.Cleared ? ", разграблен" : "")}", _label);
            if (GUILayout.Button("▶", _button, GUILayout.Height(h), GUILayout.Width(70f * s)))
            {
                _campIndex = (_campIndex + 1) % camps.Count;
                Focus(camps[_campIndex]);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("К лагерю", _button, GUILayout.Height(h))) Focus(camp);
            for (var level = camp.IsMain ? OrcBaseLayout.MainLevel : 1; level <= camp.MaxLevel; level++)
            {
                if (GUILayout.Button($"Ур. {level}", _button, GUILayout.Height(h)) && raids != null)
                {
                    camp.RebuildAt(level, raids);
                    Focus(camp);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Отряд из 5 у ворот, в атаку на портал:", _label);
            GUILayout.BeginHorizontal();
            foreach (var (type, name) in Tiers)
            {
                if (GUILayout.Button(name, _button, GUILayout.Height(h))) SendSquad(camp, type);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static void Focus(OrcCamp camp)
        {
            var rig = FindAnyObjectByType<RTSCameraController>();
            if (rig != null && camp != null) rig.FocusOn(camp.LocalToWorld(new Vector2(0f, camp.Layout.Bounds.yMin * 0.6f)));
        }

        /// <summary>Five soldiers of `type` just outside the camp's gate, sent at its totem while it stands, then at its portal.</summary>
        private static void SendSquad(OrcCamp camp, SoldierType type)
        {
            var army = ArmyManager.Instance;
            if (army == null || camp == null) return;
            var outside = camp.InFrontOfGate(4f);
            var group = army.RestoreGroup(type, outside, TargetPriority.Structures);
            for (var i = 0; i < 5; i++)
            {
                army.RestoreSoldier(group, outside + new Vector3((i - 2) * 0.6f, 0f, 0f), int.MaxValue);
            }
            // The totem first while it stands: hitting a shielded portal does nothing.
            IDamageTarget target = camp.Portal;
            foreach (var building in camp.Buildings)
            {
                if (building != null && building.Kind == OrcCampBuildingKind.Totem) target = building;
            }
            if (target != null) group.OrderAttack(target);
            Focus(camp);
        }
    }
}
