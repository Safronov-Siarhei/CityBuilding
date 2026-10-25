using CityBuilder.Buildings;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    public static partial class SetupProject
    {
        /// <summary>
        /// The Barbican's four side arms (GateArms; Tools/blender/fortifiedgate1_arm.py): a stub of
        /// stone wall from each end cell's middle to its ±x edge, hidden until a fence meets that
        /// end from the side. Each hangs in a wrapper turned toward its side, so the FBX keeps
        /// whatever rotation its import gave it.
        /// </summary>
        private static void AddGateArms(BuildingData data)
        {
            if (data == null || data.prefab == null) return;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/FortifiedGate1-Arm.fbx");
            if (model == null)
            {
                Debug.LogWarning("[SetupProject] FortifiedGate1-Arm.fbx is missing -- run Tools/blender/fortifiedgate1_arm.py and export it. The Barbican's ends stay straight.");
                return;
            }

            var path = AssetDatabase.GetAssetPath(data.prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            var old = root.transform.Find("GateArms");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var holder = new GameObject("GateArms").transform;
            holder.SetParent(root.transform, false);

            var arms = new GameObject[4];
            var ends = new[] { 1f, 1f, -1f, -1f };
            var sides = new[] { 1f, -1f, 1f, -1f };
            for (var i = 0; i < arms.Length; i++)
            {
                var wrapper = new GameObject($"Arm{(ends[i] > 0 ? "N" : "S")}{(sides[i] > 0 ? "E" : "W")}").transform;
                wrapper.SetParent(holder, false);
                wrapper.localPosition = new Vector3(0f, 0f, ends[i] * CellSize);
                wrapper.localRotation = Quaternion.Euler(0f, sides[i] > 0 ? 0f : 180f, 0f);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, wrapper);
                instance.name = "Model";
                wrapper.gameObject.SetActive(false);
                arms[i] = wrapper.gameObject;
            }
            var component = root.GetComponent<GateArms>() ?? root.AddComponent<GateArms>();
            component.SetArms(arms);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
