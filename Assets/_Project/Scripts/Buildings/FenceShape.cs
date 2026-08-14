namespace CityBuilder.Buildings
{
    /// <summary>Which of the fence's models a segment shows. Rotation does the rest -- see FenceShape.</summary>
    public enum FenceVariant
    {
        Straight,
        Corner,
    }

    /// <summary>
    /// Turns "which sides have a fence next to me" into "which model, turned how far".
    ///
    /// Pure and static on purpose: this is the whole autotiling rule, and it is worth testing
    /// exhaustively (all 16 neighbour combinations) without a scene, a grid or a prefab.
    ///
    /// The base orientations come from the FBX models themselves, not from a convention chosen
    /// here: Fence-1-Straight runs north-south, and Fence-1-Corner joins south and east. A
    /// rotation step is 90 degrees clockwise seen from above, which is what Unity's positive Y
    /// rotation does -- so each step maps north to east, east to south, south to west, west to north.
    ///
    /// With only two models there is nothing to show for a T-junction or a crossroads. Those fall
    /// back to the straight piece laid along the axis that has both sides occupied; the segments on
    /// the remaining sides still reach the shared cell border from their own side, so the join
    /// reads as a fence running into a fence rather than as a hole.
    /// </summary>
    public static class FenceShape
    {
        public static void Resolve(bool north, bool east, bool south, bool west,
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
