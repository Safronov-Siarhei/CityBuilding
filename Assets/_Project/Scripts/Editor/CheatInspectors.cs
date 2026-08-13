using CityBuilder.Cheats;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Adds the action buttons to the cheat components' Inspectors. Unity draws serialized fields
    /// on its own but has no built-in "call this method" control, so the buttons have to come from
    /// a custom editor. Both are gated to Play mode: spawning orcs or setting resources depends on
    /// live singletons (OrcRaidManager, ResourceManager) that only exist while the game runs.
    /// </summary>
    [CustomEditor(typeof(OrcSpawnCheat))]
    public class OrcSpawnCheatEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var cheat = (OrcSpawnCheat)target;

            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Кнопки работают только в режиме Play.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Spawn", GUILayout.Height(28f)))
            {
                cheat.SpawnNow();
            }

            // Re-pushes the suspend flag: the checkbox above writes a serialized field, which by
            // itself doesn't reach OrcRaidManager until the component is re-enabled.
            if (GUILayout.Button("Применить настройку набегов"))
            {
                cheat.ApplyRaidSuspension();
            }
        }
    }

    [CustomEditor(typeof(ResourceCheat))]
    public class ResourceCheatEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var cheat = (ResourceCheat)target;

            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Кнопка работает только в режиме Play.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Apply", GUILayout.Height(28f)))
            {
                cheat.Apply();
            }
        }
    }
}
