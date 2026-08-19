using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Takes the map's own geometry -- ground, water, and the authored placement zones -- out of
    /// the physics scene's collision matrix, while leaving it fully raycastable.
    ///
    /// **Why this exists, in one measurement.** A CharacterController.Move in the loaded scene
    /// costs about 15 us. Disabling the ground's MeshColliders takes that to 7,6 us: half the
    /// price of every walking citizen, every frame, was the capsule sweeping against the map mesh.
    /// The 740 trigger boxes on trees and boulders, by contrast, cost 0,8 us for all of them
    /// together -- the spawners were right to make those triggers.
    ///
    /// **And nothing is standing on that collision.** No Rigidbody exists in this project and
    /// nothing calls SimpleMove: CitizenAgent, SoldierUnit and OrcUnit all use Move() with a
    /// horizontal vector and pin Y explicitly to GridManager.GroundHeight (see
    /// CitizenAgent.PinToGroundHeight). Gravity never touches these agents, so the ground was
    /// carrying them in no sense at all -- it was only being swept against.
    ///
    /// **What the ground IS still needed for is raycasts, and those are untouched.** The collision
    /// matrix governs contacts and sweeps, not queries: Physics.Raycast picks its targets from the
    /// layerMask ARGUMENT, and every mask in this project is ~0 (BuildingPlacer.groundLayerMask,
    /// BuildingSelector/CitizenSelector.raycastMask). MeshMapApplier.TryRaycastGround does not even
    /// go through the scene -- it asks each ground collider directly. NavMesh baking is likewise
    /// unaffected: MeshMapApplier.BuildNavMesh reads the collider meshes itself rather than
    /// collecting sources by layer.
    /// </summary>
    public static class TerrainPhysicsLayer
    {
        /// <summary>An empty slot in the project's layer list. Named in SetupProject so it does not read as "Layer 6" in the Inspector.</summary>
        public const int Layer = 6;

        public const string LayerName = "Terrain";

        private const int LayerCount = 32;

        /// <summary>Moves a whole map object (and its sub-meshes -- Map-1-Ground.fbx is several pieces) onto the terrain layer.</summary>
        public static void Assign(GameObject root)
        {
            if (root == null) return;

            root.layer = Layer;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = Layer;
            }
        }

        /// <summary>
        /// Clears the terrain layer's entire row in the collision matrix, itself included.
        ///
        /// Done at runtime rather than baked into DynamicsManager.asset on purpose: this is the
        /// rule the code above depends on, and a project setting is exactly the kind of thing that
        /// gets reverted by a merge or a fresh checkout without anything failing loudly. Applying
        /// it where the map is built means the guarantee travels with the code that needs it.
        /// </summary>
        public static void ExcludeFromCollisions()
        {
            for (var other = 0; other < LayerCount; other++)
            {
                Physics.IgnoreLayerCollision(Layer, other, true);
            }
        }
    }
}
