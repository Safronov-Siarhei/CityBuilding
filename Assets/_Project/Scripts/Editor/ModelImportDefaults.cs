using UnityEditor;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Blender-exported FBX models (Assets/_Project/Models/) come in with Unity's Z-up-to-Y-up
    /// axis correction applied as a root Transform rotation (visible as ~-90 on X) rather than
    /// baked into the mesh, unless bakeAxisConversion is enabled. Runtime code that instantiates
    /// these prefabs with an explicit Quaternion.identity (see MeshMapApplier, TreesAreaSpawner)
    /// would otherwise silently discard that correction, so every model under this project's
    /// Models/ folder gets the axis conversion baked into its mesh data at import time instead,
    /// keeping the root Transform at zero rotation.
    /// </summary>
    public class ModelImportDefaults : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/_Project/Models/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsFolder)) return;
            if (assetImporter is ModelImporter modelImporter)
            {
                modelImporter.bakeAxisConversion = true;
            }
        }
    }
}
