using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The half of the procedural building generator that makes one building look like ITSELF.
    ///
    /// The shells in SetupProject (hut, fortification, flat tile) give every building a plinth,
    /// walls, a door and a roof, which is enough to read as "a building" and not nearly enough to
    /// read as a bakery rather than a barracks. A motif is what is added on top: the sails that
    /// make a windmill a windmill, the headframe over a mine, the spire on a church. One shell plus
    /// one motif covers forty-six buildings without forty-six generators.
    ///
    /// Everything here is sized from the footprint it is handed, never from a typed-in number, so a
    /// motif on a 1x1 plot and the same motif on a 4x4 one are the same building at two sizes. The
    /// footprint itself comes from the balance/design decision, not from the art -- see
    /// SetupProject.CreateBuildingData.
    ///
    /// All of it disappears the moment a real model exists: an `<id>1-lvl1.fbx` in Models/Buildings
    /// wins over the whole generator, which is the contract this exists to hold open.
    /// </summary>
    public static partial class SetupProject
    {
        private const string GeneratedMeshFolder = "Assets/_Project/Models/Generated";

        /// <summary>What makes each building recognisable on top of its shell. See AddMotif.</summary>
        private enum BuildingMotif
        {
            None,
            Chimney,
            Sails,
            Spire,
            Headframe,
            LogPile,
            StoneYard,
            Stack,
            Silo,
            BarnDoors,
            Vault,
            Banner,
            Cross,
            HoseTower,
            Dome,
            Colonnade,
            Arena,
            WellHead,
            Fountain,
            Tree,
            Bush,
            GardenBeds,
            Orchard,
            FlagPole,
            Pen,
            DryingRacks,
            Field,
            TavernSign,
            Archway,
            Keep,
        }

        /// <summary>
        /// Adds the motif's geometry and answers how tall the building now stands, so the caller's
        /// click box still reaches the top of what it can see.
        ///
        /// `shellTop` is where the shell finished; `height` is the building's nominal height, which
        /// motifs that stand well above the roof (a spire, a headframe) deliberately exceed.
        /// </summary>
        private static float AddMotif(
            Transform parent, BuildingMotif motif, string id,
            float sizeX, float sizeZ, float shellTop, float height,
            Material wallMaterial, Material roofMaterial, Material trimMaterial)
        {
            if (motif == BuildingMotif.None) return shellTop;

            var minSide = Mathf.Min(sizeX, sizeZ);

            switch (motif)
            {
                case BuildingMotif.Chimney:
                    return AddChimney(parent, sizeX, sizeZ, shellTop, height, wallMaterial, trimMaterial);

                case BuildingMotif.Sails:
                    return AddSails(parent, id, sizeX, sizeZ, shellTop, height, wallMaterial, trimMaterial);

                case BuildingMotif.Spire:
                    return AddSpire(parent, sizeX, sizeZ, shellTop, height, wallMaterial, roofMaterial, trimMaterial);

                case BuildingMotif.Headframe:
                    return AddHeadframe(parent, id, sizeX, sizeZ, shellTop, height, trimMaterial);

                case BuildingMotif.LogPile:
                    AddLogPile(parent, id, sizeX, sizeZ, trimMaterial);
                    return shellTop;

                case BuildingMotif.StoneYard:
                    AddStoneYard(parent, id, sizeX, sizeZ, wallMaterial);
                    return shellTop;

                case BuildingMotif.Stack:
                    return AddStack(parent, id, sizeX, sizeZ, shellTop, height, trimMaterial);

                case BuildingMotif.Silo:
                    return AddSilos(parent, id, sizeX, sizeZ, height, wallMaterial, roofMaterial);

                case BuildingMotif.BarnDoors:
                    AddBarnDoors(parent, id, sizeX, sizeZ, height, trimMaterial);
                    return shellTop;

                case BuildingMotif.Vault:
                    AddVault(parent, id, sizeX, sizeZ, trimMaterial);
                    return shellTop;

                case BuildingMotif.Banner:
                    return AddBanner(parent, id, sizeX, sizeZ, shellTop, height, trimMaterial);

                case BuildingMotif.Cross:
                    return AddCross(parent, id, sizeX, sizeZ, shellTop, height);

                case BuildingMotif.HoseTower:
                    return AddHoseTower(parent, id, sizeX, sizeZ, shellTop, height, wallMaterial, trimMaterial);

                case BuildingMotif.Dome:
                    return AddDome(parent, id, sizeX, sizeZ, shellTop, height, trimMaterial);

                case BuildingMotif.Colonnade:
                    return AddColonnade(parent, sizeX, sizeZ, height, wallMaterial, trimMaterial);

                case BuildingMotif.Arena:
                    return AddArena(parent, sizeX, sizeZ, height, wallMaterial, trimMaterial);

                case BuildingMotif.WellHead:
                    return AddWellHead(parent, id, minSide, wallMaterial, trimMaterial);

                case BuildingMotif.Fountain:
                    return AddFountain(parent, id, sizeX, sizeZ, wallMaterial);

                case BuildingMotif.Tree:
                    return AddTreeCluster(parent, id, sizeX, sizeZ, height, 1);

                case BuildingMotif.Bush:
                    return AddBushes(parent, id, sizeX, sizeZ, height);

                case BuildingMotif.GardenBeds:
                    return AddGardenBeds(parent, id, sizeX, sizeZ);

                case BuildingMotif.Orchard:
                    return AddTreeCluster(parent, id, sizeX, sizeZ, height, 4);

                case BuildingMotif.FlagPole:
                    return AddFlagPole(parent, id, height, trimMaterial);

                case BuildingMotif.Pen:
                    AddPen(parent, sizeX, sizeZ, trimMaterial);
                    return shellTop;

                case BuildingMotif.DryingRacks:
                    return AddDryingRacks(parent, id, sizeX, sizeZ, trimMaterial);

                case BuildingMotif.Field:
                    AddField(parent, id, sizeX, sizeZ);
                    return shellTop;

                case BuildingMotif.TavernSign:
                    AddTavernSign(parent, id, sizeX, sizeZ, height, trimMaterial);
                    return shellTop;

                case BuildingMotif.Archway:
                    return AddArchway(parent, sizeX, sizeZ, height, wallMaterial, trimMaterial);

                case BuildingMotif.Keep:
                    return AddKeep(parent, id, sizeX, sizeZ, shellTop, height, wallMaterial, roofMaterial, trimMaterial);

                default:
                    return shellTop;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Motifs
        // ---------------------------------------------------------------------------------------

        /// <summary>A brick stack off the ridge with a capstone -- the house/workshop tell.</summary>
        private static float AddChimney(Transform parent, float sizeX, float sizeZ, float shellTop, float height, Material wallMaterial, Material trimMaterial)
        {
            var stackHeight = height * 0.28f;
            var thickness = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.16f, 0.12f, 0.34f);
            var x = sizeX * 0.3f;
            var z = sizeZ * 0.3f;
            var baseY = Mathf.Max(0f, shellTop - height * 0.32f);

            AddCubePart(parent, "Chimney", new Vector3(x, baseY + stackHeight * 0.5f, z), new Vector3(thickness, stackHeight, thickness), wallMaterial);
            AddCubePart(parent, "ChimneyCap", new Vector3(x, baseY + stackHeight + 0.04f, z), new Vector3(thickness * 1.5f, 0.08f, thickness * 1.5f), trimMaterial);
            return Mathf.Max(shellTop, baseY + stackHeight + 0.08f);
        }

        /// <summary>
        /// A cap tower and four sails on the front face. The sails are thin slats crossed at 45
        /// degrees rather than a modelled lattice -- at the distance this game is played from, the
        /// cross is the whole silhouette.
        /// </summary>
        private static float AddSails(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material wallMaterial, Material trimMaterial)
        {
            var capHeight = height * 0.22f;
            var capSize = Mathf.Min(sizeX, sizeZ) * 0.55f;
            AddCylinderPart(parent, "MillCap", new Vector3(0f, shellTop + capHeight * 0.5f, 0f), capSize, capHeight, wallMaterial);

            var hubY = shellTop + capHeight * 0.6f;
            var hubZ = -sizeZ * 0.5f - 0.12f;
            var sailMaterial = CreateLitMaterial($"Building_{id}_Sail", new Color(0.86f, 0.82f, 0.7f));
            AddCubePart(parent, "SailHub", new Vector3(0f, hubY, hubZ), new Vector3(0.16f, 0.16f, 0.14f), trimMaterial);

            var span = Mathf.Max(sizeX, height * 0.7f) * 0.95f;
            for (var i = 0; i < 2; i++)
            {
                var blade = AddCubePart(parent, $"Sail{i}", new Vector3(0f, hubY, hubZ - 0.02f), new Vector3(span, 0.16f, 0.05f), sailMaterial);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, 45f + i * 90f);
            }
            return Mathf.Max(shellTop + capHeight, hubY + span * 0.5f);
        }

        /// <summary>A bell tower with a tapered spire and a cross -- the one silhouette nothing else in the town has.</summary>
        private static float AddSpire(Transform parent, float sizeX, float sizeZ, float shellTop, float height, Material wallMaterial, Material roofMaterial, Material trimMaterial)
        {
            var towerSide = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.42f, 0.5f, 1.6f);
            var towerHeight = height * 0.75f;
            var x = 0f;
            var z = -sizeZ * 0.5f + towerSide * 0.6f;
            var baseY = Mathf.Max(0f, shellTop - height * 0.55f);

            AddCubePart(parent, "BellTower", new Vector3(x, baseY + towerHeight * 0.5f, z), new Vector3(towerSide, towerHeight, towerSide), wallMaterial);
            var top = baseY + towerHeight;

            AddCubePart(parent, "BellArch", new Vector3(x, top - towerHeight * 0.2f, z - towerSide * 0.5f), new Vector3(towerSide * 0.45f, towerHeight * 0.28f, 0.08f), trimMaterial);

            var spireHeight = height * 0.5f;
            AddConePart(parent, "Spire", new Vector3(x, top + spireHeight * 0.5f, z), towerSide * 0.78f, spireHeight, roofMaterial);
            top += spireHeight;

            AddCubePart(parent, "CrossPost", new Vector3(x, top + 0.22f, z), new Vector3(0.06f, 0.44f, 0.06f), trimMaterial);
            AddCubePart(parent, "CrossArm", new Vector3(x, top + 0.3f, z), new Vector3(0.26f, 0.06f, 0.06f), trimMaterial);
            return top + 0.44f;
        }

        /// <summary>
        /// The pit-head frame every mine in the game shares: four legs leaning into a winding wheel,
        /// with an ore heap at the foot. Tinted per mine by the caller's wall colour, so iron, coal,
        /// copper and gold read apart at a glance without four generators.
        /// </summary>
        private static float AddHeadframe(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material trimMaterial)
        {
            var frameHeight = height * 0.95f;
            var spread = Mathf.Min(sizeX, sizeZ) * 0.34f;
            var z = sizeZ * 0.18f;

            for (var i = 0; i < 4; i++)
            {
                var sx = (i % 2 == 0 ? -1f : 1f) * spread;
                var sz = (i < 2 ? -1f : 1f) * spread * 0.6f + z;
                var leg = AddCubePart(parent, $"HeadframeLeg{i}", new Vector3(sx, frameHeight * 0.5f, sz), new Vector3(0.1f, frameHeight, 0.1f), trimMaterial);
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, sx > 0f ? -7f : 7f);
            }

            AddCubePart(parent, "HeadframeBeam", new Vector3(0f, frameHeight, z), new Vector3(spread * 2.2f, 0.12f, 0.12f), trimMaterial);
            AddCylinderPart(parent, "WindingWheel", new Vector3(0f, frameHeight + 0.18f, z), spread * 0.75f, 0.08f, trimMaterial, Vector3.forward);
            return Mathf.Max(shellTop, frameHeight + spread * 0.6f);
        }

        /// <summary>Felled trunks stacked beside the saw shed.</summary>
        private static void AddLogPile(Transform parent, string id, float sizeX, float sizeZ, Material trimMaterial)
        {
            var logMaterial = CreateLitMaterial($"Building_{id}_Log", new Color(0.55f, 0.38f, 0.22f));
            var length = sizeZ * 0.7f;
            var radius = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.11f, 0.08f, 0.2f);
            var x = sizeX * 0.5f - radius * 1.6f;

            for (var i = 0; i < 3; i++)
            {
                var row = i < 2 ? 0 : 1;
                var offset = i < 2 ? (i * 2 - 1) * radius * 1.05f : 0f;
                AddCylinderPart(parent, $"Log{i}", new Vector3(x + offset, radius + row * radius * 1.8f, 0f), radius * 2f, length, logMaterial, Vector3.forward);
            }

            AddCubePart(parent, "SawTrestle", new Vector3(-sizeX * 0.42f, 0.22f, -sizeZ * 0.3f), new Vector3(0.12f, 0.44f, 0.12f), trimMaterial);
            AddCubePart(parent, "SawBench", new Vector3(-sizeX * 0.42f, 0.46f, -sizeZ * 0.3f), new Vector3(0.5f, 0.08f, 0.3f), trimMaterial);
        }

        /// <summary>Cut blocks and rubble in the yard, which is all a quarry really is above ground.</summary>
        private static void AddStoneYard(Transform parent, string id, float sizeX, float sizeZ, Material wallMaterial)
        {
            var blockMaterial = CreateLitMaterial($"Building_{id}_Block", new Color(0.62f, 0.6f, 0.56f));
            var block = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.22f, 0.18f, 0.42f);

            AddCubePart(parent, "Block0", new Vector3(sizeX * 0.42f, block * 0.5f, -sizeZ * 0.3f), new Vector3(block, block, block), blockMaterial);
            AddCubePart(parent, "Block1", new Vector3(sizeX * 0.42f, block * 0.5f, sizeZ * 0.02f), new Vector3(block, block, block), blockMaterial);
            AddCubePart(parent, "Block2", new Vector3(sizeX * 0.42f, block * 1.5f, -sizeZ * 0.14f), new Vector3(block, block, block), wallMaterial);
            AddCubePart(parent, "Rubble", new Vector3(-sizeX * 0.4f, block * 0.3f, sizeZ * 0.34f), new Vector3(block * 1.4f, block * 0.6f, block * 1.2f), blockMaterial);
        }

        /// <summary>A tapered industrial stack, taller than anything domestic -- the smelter and the smokehouse.</summary>
        private static float AddStack(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material trimMaterial)
        {
            var stackMaterial = CreateLitMaterial($"Building_{id}_Stack", new Color(0.36f, 0.33f, 0.31f));
            var stackHeight = height * 0.85f;
            var radius = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.2f, 0.16f, 0.4f);
            var x = sizeX * 0.26f;
            var z = sizeZ * 0.24f;
            var baseY = Mathf.Max(0f, shellTop - height * 0.45f);

            AddCylinderPart(parent, "Stack", new Vector3(x, baseY + stackHeight * 0.5f, z), radius * 2f, stackHeight, stackMaterial);
            AddCylinderPart(parent, "StackRim", new Vector3(x, baseY + stackHeight, z), radius * 2.5f, 0.1f, trimMaterial);
            AddCubePart(parent, "FireDoor", new Vector3(-sizeX * 0.22f, 0.32f, -sizeZ * 0.5f), new Vector3(sizeX * 0.3f, 0.5f, 0.08f),
                CreateLitMaterial($"Building_{id}_Fire", new Color(0.85f, 0.42f, 0.16f)));
            return Mathf.Max(shellTop, baseY + stackHeight + 0.1f);
        }

        /// <summary>Round grain bins with conical caps, standing alongside the store.</summary>
        private static float AddSilos(Transform parent, string id, float sizeX, float sizeZ, float height, Material wallMaterial, Material roofMaterial)
        {
            var siloHeight = height * 0.9f;
            var radius = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.24f, 0.2f, 0.5f);
            var count = sizeX > 2.2f ? 2 : 1;
            var top = 0f;

            for (var i = 0; i < count; i++)
            {
                var x = count == 1 ? sizeX * 0.34f : sizeX * (i == 0 ? 0.3f : 0.3f);
                var z = count == 1 ? sizeZ * 0.28f : sizeZ * (i == 0 ? 0.3f : -0.3f);
                AddCylinderPart(parent, $"Silo{i}", new Vector3(x, siloHeight * 0.5f, z), radius * 2f, siloHeight, wallMaterial);
                AddConePart(parent, $"SiloCap{i}", new Vector3(x, siloHeight + radius * 0.35f, z), radius * 2.2f, radius * 0.7f, roofMaterial);
                top = siloHeight + radius * 0.7f;
            }
            return top;
        }

        /// <summary>The wide double door and hay hatch that says "things are kept here", not "somebody lives here".</summary>
        private static void AddBarnDoors(Transform parent, string id, float sizeX, float sizeZ, float height, Material trimMaterial)
        {
            var doorMaterial = CreateLitMaterial($"Building_{id}_BarnDoor", new Color(0.42f, 0.26f, 0.16f));
            var doorWidth = sizeX * 0.52f;
            var doorHeight = height * 0.42f;
            var z = -sizeZ * 0.5f - 0.03f;

            AddCubePart(parent, "BarnDoorLeft", new Vector3(-doorWidth * 0.26f, doorHeight * 0.5f, z), new Vector3(doorWidth * 0.48f, doorHeight, 0.06f), doorMaterial);
            AddCubePart(parent, "BarnDoorRight", new Vector3(doorWidth * 0.26f, doorHeight * 0.5f, z), new Vector3(doorWidth * 0.48f, doorHeight, 0.06f), doorMaterial);
            AddCubePart(parent, "BarnDoorBrace", new Vector3(0f, doorHeight * 0.5f, z - 0.02f), new Vector3(doorWidth, 0.07f, 0.04f), trimMaterial);
            // The hatch stays on the wall and the hoist beam pokes out just above it, under the
            // gable, which is where a beam for lifting hay into a loft actually lives.
            AddCubePart(parent, "HayHatch", new Vector3(0f, height * 0.45f, z), new Vector3(doorWidth * 0.4f, doorHeight * 0.4f, 0.06f), doorMaterial);
            AddCubePart(parent, "HoistBeam", new Vector3(0f, height * 0.56f, z - 0.16f), new Vector3(0.1f, 0.1f, 0.42f), trimMaterial);
        }

        /// <summary>An iron-banded strongbox and a stack of coins by the door: a treasury from across the map.</summary>
        private static void AddVault(Transform parent, string id, float sizeX, float sizeZ, Material trimMaterial)
        {
            var goldMaterial = CreateLitMaterial($"Building_{id}_Gold", new Color(0.85f, 0.7f, 0.28f));
            var chest = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.26f, 0.24f, 0.5f);
            var x = sizeX * 0.38f;
            var z = -sizeZ * 0.34f;

            AddCubePart(parent, "Strongbox", new Vector3(x, chest * 0.4f, z), new Vector3(chest * 1.3f, chest * 0.8f, chest), trimMaterial);
            AddCubePart(parent, "StrongboxBand", new Vector3(x, chest * 0.4f, z), new Vector3(chest * 1.35f, chest * 0.18f, chest * 1.05f), goldMaterial);
            for (var i = 0; i < 3; i++)
            {
                AddCylinderPart(parent, $"Coins{i}", new Vector3(x - chest * 0.9f, 0.03f + i * 0.05f, z + chest * 0.5f), chest * 0.5f, 0.05f, goldMaterial);
            }
        }

        /// <summary>A war banner on a pole: barracks, keeps and the fortified line.</summary>
        private static float AddBanner(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material trimMaterial)
        {
            var bannerMaterial = CreateLitMaterial($"Building_{id}_Banner", new Color(0.68f, 0.18f, 0.16f));
            var poleHeight = height * 0.55f;
            var x = -sizeX * 0.36f;
            var z = -sizeZ * 0.36f;

            AddCubePart(parent, "BannerPole", new Vector3(x, shellTop + poleHeight * 0.5f, z), new Vector3(0.07f, poleHeight, 0.07f), trimMaterial);
            AddCubePart(parent, "Banner", new Vector3(x + 0.16f, shellTop + poleHeight * 0.68f, z), new Vector3(0.3f, poleHeight * 0.45f, 0.04f), bannerMaterial);
            return shellTop + poleHeight;
        }

        /// <summary>The healer's cross over the door -- the one building a player must find in a hurry.</summary>
        private static float AddCross(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height)
        {
            var crossMaterial = CreateLitMaterial($"Building_{id}_Cross", new Color(0.78f, 0.2f, 0.18f));
            var arm = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.3f, 0.3f, 0.6f);
            // Just under the eaves. The hut shell's wall ends around 0.52 of the building's height,
            // so anything hung higher than that is nailed to the roof, or to nothing at all.
            var y = height * 0.44f;
            var z = -sizeZ * 0.5f - 0.05f;

            AddCubePart(parent, "CrossPost", new Vector3(0f, y, z), new Vector3(arm * 0.3f, arm, 0.06f), crossMaterial);
            AddCubePart(parent, "CrossArm", new Vector3(0f, y, z), new Vector3(arm, arm * 0.3f, 0.06f), crossMaterial);
            return shellTop;
        }

        /// <summary>A drying tower for the hoses and a bell to call the crew.</summary>
        private static float AddHoseTower(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material wallMaterial, Material trimMaterial)
        {
            var side = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.3f, 0.32f, 0.7f);
            var towerHeight = height * 1.15f;
            var x = sizeX * 0.5f - side * 0.6f;
            var z = sizeZ * 0.5f - side * 0.6f;

            AddCubePart(parent, "HoseTower", new Vector3(x, towerHeight * 0.5f, z), new Vector3(side, towerHeight, side), wallMaterial);
            AddCubePart(parent, "TowerRail", new Vector3(x, towerHeight, z), new Vector3(side * 1.3f, 0.08f, side * 1.3f), trimMaterial);
            AddCylinderPart(parent, "Bell", new Vector3(x, towerHeight + 0.16f, z), side * 0.45f, 0.24f,
                CreateLitMaterial($"Building_{id}_Bell", new Color(0.72f, 0.6f, 0.26f)));
            return Mathf.Max(shellTop, towerHeight + 0.28f);
        }

        /// <summary>An observatory dome: nothing else in a medieval town is round on top.</summary>
        private static float AddDome(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material trimMaterial)
        {
            var domeMaterial = CreateLitMaterial($"Building_{id}_Dome", new Color(0.42f, 0.46f, 0.62f));
            var radius = Mathf.Min(sizeX, sizeZ) * 0.3f;

            AddCylinderPart(parent, "DomeDrum", new Vector3(0f, shellTop + radius * 0.25f, 0f), radius * 2f, radius * 0.5f, trimMaterial);
            AddSpherePart(parent, "Dome", new Vector3(0f, shellTop + radius * 0.5f, 0f), radius * 2f, domeMaterial);
            var scope = AddCubePart(parent, "Telescope", new Vector3(0f, shellTop + radius * 1.1f, -radius * 0.3f), new Vector3(0.12f, 0.12f, radius * 1.6f), trimMaterial);
            scope.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            return shellTop + radius * 1.4f;
        }

        /// <summary>A columned portico across the front -- the theatre, and the only classical thing in town.</summary>
        private static float AddColonnade(Transform parent, float sizeX, float sizeZ, float height, Material wallMaterial, Material trimMaterial)
        {
            var columnHeight = height * 0.62f;
            var radius = Mathf.Clamp(sizeX * 0.06f, 0.09f, 0.2f);
            // Inside the front edge, not out in front of it: a portico that overhangs the plot
            // stands in the neighbour's yard, and the plots here are one cell apart.
            var z = -sizeZ * 0.5f + radius * 2.4f;
            var count = Mathf.Clamp(Mathf.RoundToInt(sizeX / 0.9f), 3, 7);

            AddCubePart(parent, "PorticoStep", new Vector3(0f, 0.06f, z), new Vector3(sizeX * 0.98f, 0.12f, radius * 5f), trimMaterial);
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5f : i / (count - 1f);
                var x = Mathf.Lerp(-sizeX * 0.42f, sizeX * 0.42f, t);
                AddCylinderPart(parent, $"Column{i}", new Vector3(x, 0.12f + columnHeight * 0.5f, z), radius * 2f, columnHeight, wallMaterial);
            }

            AddCubePart(parent, "Architrave", new Vector3(0f, 0.12f + columnHeight + 0.09f, z), new Vector3(sizeX * 0.98f, 0.18f, radius * 4f), trimMaterial);
            AddCubePart(parent, "Pediment", new Vector3(0f, 0.12f + columnHeight + 0.3f, z), new Vector3(sizeX * 0.8f, 0.24f, radius * 3f), wallMaterial);
            return 0.12f + columnHeight + 0.42f;
        }

        /// <summary>A tiered ring of arches around an open floor. The only building in the game with a hole in the middle.</summary>
        private static float AddArena(Transform parent, float sizeX, float sizeZ, float height, Material wallMaterial, Material trimMaterial)
        {
            var wallHeight = height * 0.72f;
            var thickness = Mathf.Clamp(Mathf.Min(sizeX, sizeZ) * 0.14f, 0.25f, 0.6f);

            // Four stands rather than a ring of segments: a closed rectangle of walls with the
            // middle left empty is the same silhouette at a fraction of the parts.
            AddCubePart(parent, "StandNorth", new Vector3(0f, wallHeight * 0.5f, sizeZ * 0.5f - thickness * 0.5f), new Vector3(sizeX, wallHeight, thickness), wallMaterial);
            AddCubePart(parent, "StandSouth", new Vector3(0f, wallHeight * 0.5f, -sizeZ * 0.5f + thickness * 0.5f), new Vector3(sizeX, wallHeight, thickness), wallMaterial);
            AddCubePart(parent, "StandWest", new Vector3(-sizeX * 0.5f + thickness * 0.5f, wallHeight * 0.5f, 0f), new Vector3(thickness, wallHeight, sizeZ), wallMaterial);
            AddCubePart(parent, "StandEast", new Vector3(sizeX * 0.5f - thickness * 0.5f, wallHeight * 0.5f, 0f), new Vector3(thickness, wallHeight, sizeZ), wallMaterial);

            AddCubePart(parent, "ArenaFloor", new Vector3(0f, 0.05f, 0f), new Vector3(sizeX - thickness * 2f, 0.1f, sizeZ - thickness * 2f),
                CreateLitMaterial("Building_Colosseum_Sand", new Color(0.78f, 0.68f, 0.46f)));

            var arches = Mathf.Clamp(Mathf.RoundToInt(sizeX / 0.8f), 3, 8);
            for (var i = 0; i < arches; i++)
            {
                var t = (i + 0.5f) / arches;
                var x = Mathf.Lerp(-sizeX * 0.44f, sizeX * 0.44f, t);
                AddCubePart(parent, $"ArchS{i}", new Vector3(x, wallHeight * 0.3f, -sizeZ * 0.5f + thickness * 0.5f), new Vector3(sizeX / arches * 0.45f, wallHeight * 0.45f, thickness * 1.1f), trimMaterial);
                AddCubePart(parent, $"ArchN{i}", new Vector3(x, wallHeight * 0.3f, sizeZ * 0.5f - thickness * 0.5f), new Vector3(sizeX / arches * 0.45f, wallHeight * 0.45f, thickness * 1.1f), trimMaterial);
            }

            AddCubePart(parent, "Cornice", new Vector3(0f, wallHeight + 0.06f, 0f), new Vector3(sizeX * 1.02f, 0.12f, sizeZ * 1.02f), trimMaterial);
            return wallHeight + 0.12f;
        }

        /// <summary>A stone ring, two posts and a little roof over the bucket. One cell, and it has to read at a glance.</summary>
        private static float AddWellHead(Transform parent, string id, float side, Material wallMaterial, Material trimMaterial)
        {
            var radius = side * 0.34f;
            var ringHeight = 0.34f;
            AddCylinderPart(parent, "WellRing", new Vector3(0f, ringHeight * 0.5f, 0f), radius * 2f, ringHeight, wallMaterial);
            AddCylinderPart(parent, "WellWater", new Vector3(0f, ringHeight - 0.02f, 0f), radius * 1.5f, 0.04f,
                CreateLitMaterial($"Building_{id}_Water", new Color(0.25f, 0.45f, 0.62f)));

            var postHeight = side * 0.62f;
            for (var i = 0; i < 2; i++)
            {
                var x = (i == 0 ? -1f : 1f) * radius * 0.9f;
                AddCubePart(parent, $"WellPost{i}", new Vector3(x, ringHeight + postHeight * 0.5f, 0f), new Vector3(0.08f, postHeight, 0.08f), trimMaterial);
            }

            var top = ringHeight + postHeight;
            AddCubePart(parent, "WellBeam", new Vector3(0f, top, 0f), new Vector3(radius * 2.2f, 0.08f, 0.08f), trimMaterial);
            AddCubePart(parent, "WellBucket", new Vector3(0f, top - 0.16f, 0f), new Vector3(0.14f, 0.16f, 0.14f), trimMaterial);
            AddConePart(parent, "WellRoof", new Vector3(0f, top + 0.16f, 0f), radius * 2.6f, 0.32f,
                CreateLitMaterial($"Building_{id}_Roof", new Color(0.45f, 0.3f, 0.2f)));
            return top + 0.32f;
        }

        /// <summary>Paving, benches and a fountain: the square is a place, not a building.</summary>
        private static float AddFountain(Transform parent, string id, float sizeX, float sizeZ, Material wallMaterial)
        {
            var radius = Mathf.Min(sizeX, sizeZ) * 0.26f;
            var waterMaterial = CreateLitMaterial($"Building_{id}_Water", new Color(0.28f, 0.5f, 0.66f));

            AddCylinderPart(parent, "FountainBasin", new Vector3(0f, 0.16f, 0f), radius * 2f, 0.32f, wallMaterial);
            AddCylinderPart(parent, "FountainWater", new Vector3(0f, 0.31f, 0f), radius * 1.7f, 0.04f, waterMaterial);
            AddCylinderPart(parent, "FountainStem", new Vector3(0f, 0.5f, 0f), radius * 0.5f, 0.42f, wallMaterial);
            AddCylinderPart(parent, "FountainBowl", new Vector3(0f, 0.72f, 0f), radius * 1.1f, 0.1f, wallMaterial);

            for (var i = 0; i < 2; i++)
            {
                var z = (i == 0 ? -1f : 1f) * sizeZ * 0.36f;
                AddCubePart(parent, $"Bench{i}", new Vector3(0f, 0.2f, z), new Vector3(sizeX * 0.45f, 0.08f, 0.16f), wallMaterial);
                AddCubePart(parent, $"BenchLegA{i}", new Vector3(-sizeX * 0.18f, 0.1f, z), new Vector3(0.07f, 0.2f, 0.14f), wallMaterial);
                AddCubePart(parent, $"BenchLegB{i}", new Vector3(sizeX * 0.18f, 0.1f, z), new Vector3(0.07f, 0.2f, 0.14f), wallMaterial);
            }
            return 0.82f;
        }

        /// <summary>One tree, or an orchard's worth, laid out from the footprint rather than placed by hand.</summary>
        private static float AddTreeCluster(Transform parent, string id, float sizeX, float sizeZ, float height, int perSide)
        {
            var trunkMaterial = CreateLitMaterial($"Building_{id}_Trunk", new Color(0.38f, 0.26f, 0.16f));
            var leafMaterial = CreateLitMaterial($"Building_{id}_Leaves", new Color(0.28f, 0.5f, 0.24f));
            var trees = Mathf.Max(1, perSide);
            var trunkHeight = height * (trees > 1 ? 0.45f : 0.5f);
            var canopy = Mathf.Min(sizeX, sizeZ) / (trees > 1 ? 2.4f : 1.5f);
            var top = 0f;

            for (var i = 0; i < trees; i++)
            {
                var x = trees == 1 ? 0f : Mathf.Lerp(-sizeX * 0.28f, sizeX * 0.28f, i % 2);
                var z = trees == 1 ? 0f : Mathf.Lerp(-sizeZ * 0.28f, sizeZ * 0.28f, i / 2);
                AddCylinderPart(parent, $"Trunk{i}", new Vector3(x, trunkHeight * 0.5f, z), canopy * 0.22f, trunkHeight, trunkMaterial);
                AddSpherePart(parent, $"Canopy{i}", new Vector3(x, trunkHeight + canopy * 0.42f, z), canopy, leafMaterial);
                AddSpherePart(parent, $"CanopyTop{i}", new Vector3(x, trunkHeight + canopy * 0.78f, z), canopy * 0.7f, leafMaterial);
                top = trunkHeight + canopy * 1.1f;
            }
            return top;
        }

        /// <summary>A clump of shrubs, deliberately never taller than a citizen.</summary>
        private static float AddBushes(Transform parent, string id, float sizeX, float sizeZ, float height)
        {
            var leafMaterial = CreateLitMaterial($"Building_{id}_Leaves", new Color(0.32f, 0.52f, 0.28f));
            var size = Mathf.Min(sizeX, sizeZ) * 0.42f;

            AddSpherePart(parent, "Bush0", new Vector3(0f, size * 0.42f, 0f), size, leafMaterial);
            AddSpherePart(parent, "Bush1", new Vector3(sizeX * 0.2f, size * 0.32f, sizeZ * 0.16f), size * 0.72f, leafMaterial);
            AddSpherePart(parent, "Bush2", new Vector3(-sizeX * 0.18f, size * 0.3f, -sizeZ * 0.18f), size * 0.66f, leafMaterial);
            return Mathf.Min(height, size * 0.95f);
        }

        /// <summary>Planted beds with a low border -- a garden reads by its rows, not by its height.</summary>
        private static float AddGardenBeds(Transform parent, string id, float sizeX, float sizeZ)
        {
            var soilMaterial = CreateLitMaterial($"Building_{id}_Soil", new Color(0.36f, 0.27f, 0.19f));
            var bloomMaterial = CreateLitMaterial($"Building_{id}_Bloom", new Color(0.82f, 0.5f, 0.6f));
            var leafMaterial = CreateLitMaterial($"Building_{id}_Leaves", new Color(0.36f, 0.55f, 0.3f));

            AddCubePart(parent, "Soil", new Vector3(0f, 0.06f, 0f), new Vector3(sizeX, 0.12f, sizeZ), soilMaterial);
            AddCubePart(parent, "BorderNorth", new Vector3(0f, 0.1f, sizeZ * 0.5f), new Vector3(sizeX, 0.2f, 0.08f), leafMaterial);
            AddCubePart(parent, "BorderSouth", new Vector3(0f, 0.1f, -sizeZ * 0.5f), new Vector3(sizeX, 0.2f, 0.08f), leafMaterial);

            var rows = Mathf.Clamp(Mathf.RoundToInt(sizeX / 0.5f), 2, 6);
            for (var i = 0; i < rows; i++)
            {
                var t = (i + 0.5f) / rows;
                var x = Mathf.Lerp(-sizeX * 0.42f, sizeX * 0.42f, t);
                AddSpherePart(parent, $"Bloom{i}", new Vector3(x, 0.22f, 0f), 0.2f, i % 2 == 0 ? bloomMaterial : leafMaterial);
            }
            return 0.32f;
        }

        /// <summary>A flag on a pole and nothing else -- the cheapest thing a player can plant.</summary>
        private static float AddFlagPole(Transform parent, string id, float height, Material trimMaterial)
        {
            var clothMaterial = CreateLitMaterial($"Building_{id}_Cloth", new Color(0.72f, 0.2f, 0.18f));

            AddCylinderPart(parent, "PoleBase", new Vector3(0f, 0.06f, 0f), 0.36f, 0.12f, trimMaterial);
            AddCylinderPart(parent, "Pole", new Vector3(0f, height * 0.5f, 0f), 0.09f, height, trimMaterial);
            AddCubePart(parent, "Flag", new Vector3(0.22f, height * 0.82f, 0f), new Vector3(0.42f, height * 0.24f, 0.03f), clothMaterial);
            return height;
        }

        /// <summary>A rail fence around the yard, which is what makes a shed a pig farm.</summary>
        private static void AddPen(Transform parent, float sizeX, float sizeZ, Material trimMaterial)
        {
            var posts = Mathf.Clamp(Mathf.RoundToInt(sizeX / 0.6f), 3, 8);
            const float postHeight = 0.46f;
            var z = sizeZ * 0.5f - 0.06f;

            for (var i = 0; i < posts; i++)
            {
                var t = i / (posts - 1f);
                var x = Mathf.Lerp(-sizeX * 0.48f, sizeX * 0.48f, t);
                AddCubePart(parent, $"PenPost{i}", new Vector3(x, postHeight * 0.5f, z), new Vector3(0.08f, postHeight, 0.08f), trimMaterial);
            }
            AddCubePart(parent, "PenRailTop", new Vector3(0f, postHeight * 0.85f, z), new Vector3(sizeX * 0.96f, 0.06f, 0.05f), trimMaterial);
            AddCubePart(parent, "PenRailLow", new Vector3(0f, postHeight * 0.45f, z), new Vector3(sizeX * 0.96f, 0.06f, 0.05f), trimMaterial);
        }

        /// <summary>Nets on a rack: the fisher's hut, told apart from every other one-room shed.</summary>
        private static float AddDryingRacks(Transform parent, string id, float sizeX, float sizeZ, Material trimMaterial)
        {
            var netMaterial = CreateLitMaterial($"Building_{id}_Net", new Color(0.62f, 0.6f, 0.44f));
            var rackHeight = 0.8f;
            var x = sizeX * 0.5f - 0.06f;

            for (var i = 0; i < 2; i++)
            {
                var z = (i == 0 ? -1f : 1f) * sizeZ * 0.3f;
                AddCubePart(parent, $"RackPost{i}", new Vector3(x, rackHeight * 0.5f, z), new Vector3(0.07f, rackHeight, 0.07f), trimMaterial);
            }
            AddCubePart(parent, "RackBeam", new Vector3(x, rackHeight, 0f), new Vector3(0.07f, 0.07f, sizeZ * 0.7f), trimMaterial);
            AddCubePart(parent, "Net", new Vector3(x, rackHeight * 0.62f, 0f), new Vector3(0.04f, rackHeight * 0.6f, sizeZ * 0.6f), netMaterial);
            return rackHeight;
        }

        /// <summary>Furrows and a couple of stooks in front of the shed.</summary>
        private static void AddField(Transform parent, string id, float sizeX, float sizeZ)
        {
            var soilMaterial = CreateLitMaterial($"Building_{id}_Soil", new Color(0.4f, 0.29f, 0.19f));
            var cropMaterial = CreateLitMaterial($"Building_{id}_Crop", new Color(0.78f, 0.66f, 0.3f));

            // Half in, half out: a farm this small has no room for a field inside its own walls,
            // and a bed that reads as a field is worth a hand's width of overhang.
            var fieldDepth = Mathf.Min(sizeZ * 0.42f, 0.34f);
            var z = -sizeZ * 0.5f - fieldDepth * 0.3f;
            AddCubePart(parent, "Field", new Vector3(0f, 0.05f, z), new Vector3(sizeX, 0.1f, fieldDepth), soilMaterial);

            var rows = Mathf.Clamp(Mathf.RoundToInt(sizeX / 0.35f), 2, 6);
            for (var i = 0; i < rows; i++)
            {
                var t = (i + 0.5f) / rows;
                var rx = Mathf.Lerp(-sizeX * 0.42f, sizeX * 0.42f, t);
                AddCubePart(parent, $"Furrow{i}", new Vector3(rx, 0.16f, z), new Vector3(sizeX / rows * 0.4f, 0.14f, fieldDepth * 0.86f), cropMaterial);
            }
        }

        /// <summary>A hanging sign and barrels by the wall. A tavern is a promise, and the sign is the promise.</summary>
        private static void AddTavernSign(Transform parent, string id, float sizeX, float sizeZ, float height, Material trimMaterial)
        {
            var signMaterial = CreateLitMaterial($"Building_{id}_Sign", new Color(0.72f, 0.56f, 0.24f));
            var barrelMaterial = CreateLitMaterial($"Building_{id}_Barrel", new Color(0.46f, 0.3f, 0.18f));
            var y = height * 0.42f;
            var z = -sizeZ * 0.5f - 0.18f;

            AddCubePart(parent, "SignArm", new Vector3(sizeX * 0.3f, y + 0.2f, z + 0.1f), new Vector3(0.06f, 0.06f, 0.34f), trimMaterial);
            AddCubePart(parent, "Sign", new Vector3(sizeX * 0.3f, y, z), new Vector3(0.34f, 0.3f, 0.04f), signMaterial);

            for (var i = 0; i < 2; i++)
            {
                var x = -sizeX * 0.34f + i * 0.32f;
                AddCylinderPart(parent, $"Barrel{i}", new Vector3(x, 0.18f, z + 0.06f), 0.3f, 0.36f, barrelMaterial);
            }
        }

        /// <summary>Two piers and a lintel with a portcullis between them: a gate has to look passable.</summary>
        private static float AddArchway(Transform parent, float sizeX, float sizeZ, float height, Material wallMaterial, Material trimMaterial)
        {
            var pierWidth = Mathf.Clamp(sizeX * 0.24f, 0.28f, 0.6f);
            var arch = height * 0.62f;

            for (var i = 0; i < 2; i++)
            {
                var x = (i == 0 ? -1f : 1f) * (sizeX * 0.5f - pierWidth * 0.5f);
                AddCubePart(parent, $"Pier{i}", new Vector3(x, arch * 0.5f, 0f), new Vector3(pierWidth, arch, sizeZ), wallMaterial);
            }

            AddCubePart(parent, "Lintel", new Vector3(0f, arch + height * 0.1f, 0f), new Vector3(sizeX, height * 0.2f, sizeZ), wallMaterial);
            AddCubePart(parent, "Portcullis", new Vector3(0f, arch * 0.62f, 0f), new Vector3(sizeX - pierWidth * 2f, arch * 0.72f, 0.08f), trimMaterial);
            AddCrenellationRing(parent, "GateMerlon", sizeX, sizeZ, arch + height * 0.2f + height * 0.06f, height * 0.12f, trimMaterial);
            return arch + height * 0.2f + height * 0.12f;
        }

        /// <summary>
        /// The keep the settlement is built around: a taller inner block with a banner, standing out
        /// of the fortified shell. The Town Hall is the only building whose loss ends the game, so it
        /// is the one silhouette that has to be findable from anywhere on the map.
        /// </summary>
        private static float AddKeep(Transform parent, string id, float sizeX, float sizeZ, float shellTop, float height, Material wallMaterial, Material roofMaterial, Material trimMaterial)
        {
            var keepSide = Mathf.Min(sizeX, sizeZ) * 0.46f;
            var keepHeight = height * 0.8f;

            AddCubePart(parent, "Keep", new Vector3(0f, shellTop + keepHeight * 0.5f, 0f), new Vector3(keepSide, keepHeight, keepSide), wallMaterial);
            var top = shellTop + keepHeight;
            AddCrenellationRing(parent, "KeepMerlon", keepSide, keepSide, top + height * 0.05f, height * 0.1f, trimMaterial);
            top += height * 0.1f;

            var roofHeight = height * 0.3f;
            AddConePart(parent, "KeepRoof", new Vector3(0f, top + roofHeight * 0.5f, 0f), keepSide * 0.9f, roofHeight, roofMaterial);
            top += roofHeight;

            return AddBanner(parent, id, keepSide, keepSide, top, height * 0.5f, trimMaterial);
        }

        // ---------------------------------------------------------------------------------------
        // Primitives the motifs are built from. AddCubePart lives in SetupProject.cs; these are the
        // round ones, which the shells never needed.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// A cylinder of the given DIAMETER and length. Unity's cylinder primitive is 1 across and 2
        /// tall, which is exactly the sort of arithmetic that ends up wrong in one call out of ten,
        /// so it is done here once. `axis` turns it: up by default, Vector3.forward for a log or a
        /// winding wheel lying on its side.
        /// </summary>
        private static GameObject AddCylinderPart(Transform parent, string partName, Vector3 localPosition, float diameter, float length, Material material, Vector3 axis = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = partName;
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            if (axis != default) go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>
        /// A cone: a spire, a silo cap, the roof over a well. Unity has no cone primitive, so one is
        /// made by pinching the top ring of a cylinder to a point.
        ///
        /// ONE unit cone is built and saved as an asset, then scaled by the transform like every
        /// other part. A mesh built in memory and handed to a prefab does not survive being saved --
        /// the prefab keeps a reference to an object that was never in the asset database, and the
        /// spire silently disappears the next time anything loads it.
        /// </summary>
        private static GameObject AddConePart(Transform parent, string partName, Vector3 localPosition, float diameter, float height, Material material)
        {
            var go = AddCylinderPart(parent, partName, localPosition, diameter, height, material);
            go.GetComponent<MeshFilter>().sharedMesh = UnitConeMesh();
            return go;
        }

        private static Mesh _unitConeMesh;

        private static Mesh UnitConeMesh()
        {
            if (_unitConeMesh != null) return _unitConeMesh;

            const string path = GeneratedMeshFolder + "/UnitCone.asset";
            _unitConeMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (_unitConeMesh != null) return _unitConeMesh;

            var template = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var mesh = Object.Instantiate(template.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(template);

            mesh.name = "UnitCone";
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y <= 0f) continue;
                vertices[i] = new Vector3(0f, vertices[i].y, 0f);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            System.IO.Directory.CreateDirectory(GeneratedMeshFolder);
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            _unitConeMesh = mesh;
            return mesh;
        }

        /// <summary>A sphere of the given DIAMETER -- canopies and domes.</summary>
        private static GameObject AddSpherePart(Transform parent, string partName, Vector3 localPosition, float diameter, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = partName;
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(diameter, diameter, diameter);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
