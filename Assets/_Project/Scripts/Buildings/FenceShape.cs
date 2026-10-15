namespace CityBuilder.Buildings
{
    /// <summary>Which of the fence's models a segment shows. Rotation does the rest -- see FenceShape.</summary>
    public enum FenceVariant
    {
        Straight,
        Corner,
        T,
        Cross,
    }

    /// <summary>
    /// Turns "which sides have a fence next to me" into "which model, turned how far".
    ///
    /// Pure and static on purpose: this is the whole autotiling rule, and it is worth testing
    /// exhaustively (all 16 neighbour combinations) without a scene, a grid or a prefab.
    ///
    /// The base orientations come from the FBX models themselves, not from a convention chosen
    /// here: Fence1-lvl1-Straight runs north-south, -Corner joins south and east, -T joins north,
    /// south and east (its missing arm is west), and -Cross joins all four. A rotation step is 90
    /// degrees clockwise seen from above, which is what Unity's positive Y rotation does -- so each
    /// step maps north to east, east to south, south to west, west to north. The same rule was
    /// checked against the models in Blender (Tools/blender/render_fence_test.py).
    /// </summary>
    public static class FenceShape
    {
        public static void Resolve(bool north, bool east, bool south, bool west,
            out FenceVariant variant, out int rotationSteps)
        {
            var count = (north ? 1 : 0) + (east ? 1 : 0) + (south ? 1 : 0) + (west ? 1 : 0);

            if (count == 4)
            {
                variant = FenceVariant.Cross;
                rotationSteps = 0;
                return;
            }

            if (count == 3)
            {
                // The T as modelled is missing its west arm; each turn moves the gap one side on.
                variant = FenceVariant.T;
                if (!west) rotationSteps = 0;
                else if (!north) rotationSteps = 1;
                else if (!east) rotationSteps = 2;
                else rotationSteps = 3; // south is the open side
                return;
            }

            ResolveWithoutJunctions(north, east, south, west, out variant, out rotationSteps);
        }

        /// <summary>
        /// The rule for a fence that has only the straight and the corner model. A T-junction or a
        /// crossroads falls back to the straight piece laid along the axis that has both sides
        /// occupied; the segments on the remaining sides still reach the shared cell border from
        /// their own side, so the join reads as a fence running into a fence rather than as a hole.
        /// </summary>
        public static void ResolveWithoutJunctions(bool north, bool east, bool south, bool west,
            out FenceVariant variant, out int rotationSteps)
        {
            var count = (north ? 1 : 0) + (east ? 1 : 0) + (south ? 1 : 0) + (west ? 1 : 0);

            // Exactly two neighbours that aren't opposite each other: the one case the corner
            // model exists for.
            if (count == 2 && !(north && south) && !(east && west))
            {
                variant = FenceVariant.Corner;
                if (south && east) rotationSteps = 0;
                else if (south && west) rotationSteps = 1;
                else if (north && west) rotationSteps = 2;
                else rotationSteps = 3; // north && east
                return;
            }

            variant = FenceVariant.Straight;

            // A straight piece spans one axis, so prefer the axis that is occupied on both sides.
            // Everything else -- a dead end, a lone post, a T, a crossroads -- lines up with
            // whichever side does have a neighbour, and a fence with no neighbours at all just
            // keeps the model's own north-south orientation.
            if (north && south) rotationSteps = 0;
            else if (east && west) rotationSteps = 1;
            else if (north || south) rotationSteps = 0;
            else if (east || west) rotationSteps = 1;
            else rotationSteps = 0;
        }
    }
}
