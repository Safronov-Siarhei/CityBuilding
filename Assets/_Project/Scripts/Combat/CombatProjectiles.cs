using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Combat
{
    /// <summary>What flies: an arrow (the player's towers, archers, the orcs' towers) or an orc thrower's javelin.</summary>
    public enum ProjectileKind
    {
        Arrow = 0,
        Javelin = 1,
    }

    /// <summary>
    /// Every ranged shot in the game, made visible: the arrow or javelin leaves the shooter, flies a
    /// shallow arc to the target, sticks in it for a moment and throws up a few splinters where it
    /// struck. The DAMAGE lands with it -- on impact, not on release -- so a kill shot is seen
    /// arriving before the target drops, which is the whole point of drawing the shot at all.
    ///
    /// A shot homes on the target's current position (there are no misses: the rules never had
    /// any), and a target that dies on the way -- to another shot, say -- is simply not damaged:
    /// the projectile lands where it last stood. A shot in the air when the game is saved is lost
    /// with the frame; at most one hit per shooter, the same as a shot fired a moment later.
    ///
    /// Mobile cost, deliberately small: one MonoBehaviour updates every shot in one loop; arrows,
    /// javelins and splinters are pooled GameObjects made once and reused, each one renderer on a
    /// material shared by its kind, none casting shadows; nothing is allocated per shot. With
    /// towers firing every second or so there are a few dozen objects alive at the busiest.
    ///
    /// Created on the first shot rather than by SetupProject, the way CitizenSelector makes its
    /// MoveOrderMarker -- a town with no fighting never carries it. Outside play mode (edit-mode
    /// tests) there is no Update to land a shot, so the damage is applied at once instead.
    /// </summary>
    public class CombatProjectiles : MonoBehaviour
    {
        /// <summary>Where on a unit (from its feet) a shot is aimed: about chest height for a unit drawn at UnitSize.Scale.</summary>
        public const float UnitAimHeight = 0.25f;

        /// <summary>Where on a structure (from its base) a shot is aimed.</summary>
        public const float StructureAimHeight = 0.6f;

        /// <summary>How high up a building's box its shots leave from (see MuzzleOf).</summary>
        private const float MuzzleHeightShare = 0.68f;

        private const float ArrowSpeed = 14f;
        private const float JavelinSpeed = 9f;
        private const float MinFlightSeconds = 0.22f;
        private const float MaxFlightSeconds = 0.9f;
        // Peak of the arc as a share of the distance flown: an arrow flies flat, a javelin is lobbed.
        private const float ArrowArc = 0.1f;
        private const float JavelinArc = 0.16f;

        private const float StickSeconds = 0.45f;
        private const float StickShrinkSeconds = 0.15f;
        // How far the head sinks into what it hit, as a share of the projectile's length.
        private const float StickDepth = 0.3f;

        private const int ChipsPerHit = 5;
        private const float ChipSeconds = 0.35f;
        private const float ChipSize = 0.045f;
        private const float ChipSpeed = 1.6f;
        private const float ChipGravity = 7f;

        private const int MaxLiveShots = 128;
        private const int MaxLiveChips = 160;

        private static readonly Color ArrowColor = new Color(0.86f, 0.74f, 0.52f);
        private static readonly Color JavelinColor = new Color(0.62f, 0.45f, 0.3f);
        private static readonly Color ChipColor = new Color(0.93f, 0.88f, 0.76f);

        private static CombatProjectiles _instance;
        private static bool _quitting;

        private readonly List<Shot> _shots = new List<Shot>(32);
        private readonly List<Chip> _chips = new List<Chip>(64);
        private readonly Stack<Transform> _arrowPool = new Stack<Transform>();
        private readonly Stack<Transform> _javelinPool = new Stack<Transform>();
        private readonly Stack<Transform> _chipPool = new Stack<Transform>();

        private Mesh _arrowMesh;
        private Mesh _javelinMesh;
        private Mesh _chipMesh;
        private Material _arrowMaterial;
        private Material _javelinMaterial;
        private Material _chipMaterial;

        /// <summary>Shots in the air or stuck in a target right now -- for tests.</summary>
        public static int LiveShotCount => _instance != null ? _instance._shots.Count : 0;

        /// <summary>Splinters of a hit still in the air -- for tests.</summary>
        public static int LiveChipCount => _instance != null ? _instance._chips.Count : 0;

        private struct Shot
        {
            public ProjectileKind Kind;
            public Transform Visual;
            public Vector3 Start;
            public Vector3 Aim;
            public float Elapsed;
            public float Duration;
            public float Arc;
            public bool Stuck;
            public Vector3 StuckOffset;

            // Exactly one of these is the target; the rest are null.
            public IDamageTarget Enemy;
            public SoldierUnit Soldier;
            public FreedPrisoner Prisoner;
            public BuildingInstance Building;
            public Transform TargetTransform;
            /// <summary>Where on the target the shot is aimed, from its pivot: a unit's chest, or the face of a structure turned to the shooter.</summary>
            public Vector3 AimOffset;

            public int Damage;
            /// <summary>The archer who loosed it, told to the orc it hits (see OrcUnit.NotifyAttackedBy).</summary>
            public SoldierUnit Attacker;
        }

        private struct Chip
        {
            public Transform Visual;
            public Vector3 Velocity;
            public float Elapsed;
        }

        // ---------------------------------------------------------------- firing

        /// <summary>A shot at an orc, a portal or an orc camp building. The archer, if it is one, is told to the orc it hits.</summary>
        public static void Fire(ProjectileKind kind, Vector3 from, IDamageTarget target, int damage, SoldierUnit attacker = null)
        {
            if (target == null || target.Transform == null || !target.IsAlive) return;
            var instance = Instance;
            if (instance == null)
            {
                target.TakeDamage(damage);
                if (attacker != null && target is OrcUnit orc) orc.NotifyAttackedBy(attacker);
                return;
            }
            var shot = new Shot
            {
                Enemy = target,
                TargetTransform = target.Transform,
                AimOffset = target.IsStructure ? StructureAimOffset(target.Transform, from) : Vector3.up * UnitAimHeight,
                Damage = damage,
                Attacker = attacker,
            };
            instance.Launch(kind, from, ref shot);
        }

        /// <summary>A shot at one of the player's soldiers.</summary>
        public static void Fire(ProjectileKind kind, Vector3 from, SoldierUnit target, int damage)
        {
            if (target == null || target.CurrentHealth <= 0) return;
            var instance = Instance;
            if (instance == null)
            {
                target.TakeDamage(damage);
                return;
            }
            var shot = new Shot { Soldier = target, TargetTransform = target.transform, AimOffset = Vector3.up * UnitAimHeight, Damage = damage };
            instance.Launch(kind, from, ref shot);
        }

        /// <summary>A shot at a freed prisoner walking home.</summary>
        public static void Fire(ProjectileKind kind, Vector3 from, FreedPrisoner target, int damage)
        {
            if (target == null || target.CurrentHealth <= 0) return;
            var instance = Instance;
            if (instance == null)
            {
                target.TakeDamage(damage);
                return;
            }
            var shot = new Shot { Prisoner = target, TargetTransform = target.transform, AimOffset = Vector3.up * UnitAimHeight, Damage = damage };
            instance.Launch(kind, from, ref shot);
        }

        /// <summary>A shot at one of the player's buildings.</summary>
        public static void Fire(ProjectileKind kind, Vector3 from, BuildingInstance target, int damage)
        {
            if (target == null || target.CurrentHealth <= 0) return;
            var instance = Instance;
            if (instance == null)
            {
                target.TryDamage(damage);
                return;
            }
            var shot = new Shot { Building = target, TargetTransform = target.transform, AimOffset = StructureAimOffset(target.transform, from), Damage = damage };
            instance.Launch(kind, from, ref shot);
        }

        /// <summary>
        /// Where a building's shot leaves from: the side of its click box turned to the target,
        /// at MuzzleHeightShare of its height -- a tower's open platform rather than its doorstep
        /// or the inside of its roof. The click box is measured from the model on screen (see
        /// BuildingInstance.RoofHeight), so this follows the model through upgrades. Falls back to
        /// a fixed height for a building with no box.
        /// </summary>
        public static Vector3 MuzzleOf(Component building, Vector3 toward, float fallbackHeight = 1.5f)
        {
            var position = building.transform.position;
            var box = building.GetComponent<BoxCollider>();
            if (box == null || !box.enabled || box.bounds.size.y <= 0.05f)
            {
                position.y += fallbackHeight;
                return position;
            }

            var bounds = box.bounds;
            var height = Mathf.Lerp(bounds.min.y, bounds.max.y, MuzzleHeightShare);
            var probe = new Vector3(toward.x, height, toward.z);
            var edge = box.ClosestPoint(probe);
            edge.y = height;
            var outward = probe - edge;
            outward.y = 0f;
            if (outward.sqrMagnitude > 1e-4f) edge += outward.normalized * 0.1f;
            return edge;
        }

        /// <summary>
        /// The point of a structure's click box nearest the shooter, at StructureAimHeight and a
        /// hair outside the wall -- a shot at the box's centre would bury the arrow and its
        /// splinters inside the model. Falls back to straight up from the pivot without a box.
        /// </summary>
        private static Vector3 StructureAimOffset(Transform structure, Vector3 from)
        {
            var pivot = structure.position;
            var box = structure.GetComponent<BoxCollider>();
            if (box == null || !box.enabled) return Vector3.up * StructureAimHeight;

            var height = Mathf.Min(StructureAimHeight, box.bounds.size.y * 0.6f);
            var probe = new Vector3(from.x, pivot.y + height, from.z);
            var surface = box.ClosestPoint(probe);
            var outward = probe - surface;
            if (outward.sqrMagnitude > 1e-4f) surface += outward.normalized * 0.05f;
            return surface - pivot;
        }

        /// <summary>Where a unit's shot leaves from: shoulder height.</summary>
        public static Vector3 MuzzleOfUnit(Transform unit) => unit.position + Vector3.up * (UnitAimHeight + 0.08f);

        private static CombatProjectiles Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (!Application.isPlaying || _quitting) return null;
                _instance = new GameObject("CombatProjectiles").AddComponent<CombatProjectiles>();
                return _instance;
            }
        }

        private void Launch(ProjectileKind kind, Vector3 from, ref Shot shot)
        {
            if (_shots.Count >= MaxLiveShots)
            {
                // A pile-up beyond anything the game should produce: land this one at once rather than drawing it.
                ApplyDamage(ref shot);
                return;
            }

            var aim = shot.TargetTransform.position + shot.AimOffset;
            var distance = Vector3.Distance(from, aim);
            var speed = kind == ProjectileKind.Javelin ? JavelinSpeed : ArrowSpeed;

            shot.Kind = kind;
            shot.Start = from;
            shot.Aim = aim;
            shot.Duration = Mathf.Clamp(distance / speed, MinFlightSeconds, MaxFlightSeconds);
            shot.Arc = distance * (kind == ProjectileKind.Javelin ? JavelinArc : ArrowArc);
            shot.Visual = Take(kind);
            PlaceInFlight(ref shot, 0f);
            _shots.Add(shot);
        }

        // ---------------------------------------------------------------- per frame

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            for (var i = _shots.Count - 1; i >= 0; i--)
            {
                var shot = _shots[i];
                shot.Elapsed += deltaTime;

                if (!shot.Stuck)
                {
                    if (shot.TargetTransform != null) shot.Aim = shot.TargetTransform.position + shot.AimOffset;
                    var t = shot.Elapsed / shot.Duration;
                    if (t < 1f)
                    {
                        PlaceInFlight(ref shot, t);
                        _shots[i] = shot;
                        continue;
                    }

                    PlaceInFlight(ref shot, 1f);
                    ApplyDamage(ref shot);
                    Splinter(shot.Aim);
                    shot.Stuck = true;
                    shot.Elapsed = 0f;
                    shot.StuckOffset = shot.TargetTransform != null ? shot.Visual.position - shot.TargetTransform.position : Vector3.zero;
                    _shots[i] = shot;
                    continue;
                }

                // A target that died -- of this shot or before it landed -- takes the arrow with it
                // rather than leaving it hanging in the air where its chest was.
                if (shot.Elapsed >= StickSeconds || shot.TargetTransform == null)
                {
                    Return(shot.Kind, shot.Visual);
                    _shots.RemoveAt(i);
                    continue;
                }

                if (shot.TargetTransform != null) shot.Visual.position = shot.TargetTransform.position + shot.StuckOffset;
                var left = StickSeconds - shot.Elapsed;
                if (left < StickShrinkSeconds) shot.Visual.localScale = Vector3.one * (left / StickShrinkSeconds);
                _shots[i] = shot;
            }

            for (var i = _chips.Count - 1; i >= 0; i--)
            {
                var chip = _chips[i];
                chip.Elapsed += deltaTime;
                if (chip.Elapsed >= ChipSeconds)
                {
                    chip.Visual.gameObject.SetActive(false);
                    _chipPool.Push(chip.Visual);
                    _chips.RemoveAt(i);
                    continue;
                }

                chip.Velocity.y -= ChipGravity * deltaTime;
                chip.Visual.position += chip.Velocity * deltaTime;
                chip.Visual.localScale = Vector3.one * (ChipSize * (1f - chip.Elapsed / ChipSeconds));
                _chips[i] = chip;
            }
        }

        /// <summary>Along the straight line from start to aim, lifted by a parabola that peaks mid-flight, and pointed along the way it is going.</summary>
        private static void PlaceInFlight(ref Shot shot, float t)
        {
            var position = PointOnArc(shot.Start, shot.Aim, shot.Arc, t);
            var ahead = PointOnArc(shot.Start, shot.Aim, shot.Arc, Mathf.Min(1f, t + 0.05f)) - position;
            if (t >= 1f || ahead.sqrMagnitude < 1e-6f) ahead = PointOnArc(shot.Start, shot.Aim, shot.Arc, 1f) - PointOnArc(shot.Start, shot.Aim, shot.Arc, 0.95f);

            var visual = shot.Visual;
            visual.position = position;
            if (ahead.sqrMagnitude > 1e-6f) visual.rotation = Quaternion.LookRotation(ahead);
            // On impact the head sinks into the target rather than stopping at its surface.
            if (t >= 1f) visual.position += visual.forward * (LengthOf(shot.Kind) * StickDepth);
        }

        private static Vector3 PointOnArc(Vector3 start, Vector3 end, float arc, float t)
        {
            var point = Vector3.LerpUnclamped(start, end, t);
            point.y += arc * 4f * t * (1f - t);
            return point;
        }

        private static void ApplyDamage(ref Shot shot)
        {
            if (shot.Enemy != null)
            {
                if (shot.Enemy.Transform == null || !shot.Enemy.IsAlive) return;
                shot.Enemy.TakeDamage(shot.Damage);
                // An orc has to be able to come after whoever is shooting it (see SoldierUnit.TickAttack).
                if (shot.Attacker != null && shot.Enemy is OrcUnit orc && orc != null && orc.CurrentHealth > 0) orc.NotifyAttackedBy(shot.Attacker);
                return;
            }
            if (shot.Soldier != null)
            {
                if (shot.Soldier.CurrentHealth > 0) shot.Soldier.TakeDamage(shot.Damage);
                return;
            }
            if (shot.Prisoner != null)
            {
                if (shot.Prisoner.CurrentHealth > 0) shot.Prisoner.TakeDamage(shot.Damage);
                return;
            }
            if (shot.Building != null && shot.Building.CurrentHealth > 0) shot.Building.TryDamage(shot.Damage);
        }

        private void Splinter(Vector3 at)
        {
            for (var i = 0; i < ChipsPerHit && _chips.Count < MaxLiveChips; i++)
            {
                var visual = _chipPool.Count > 0 ? _chipPool.Pop() : CreatePiece("ShotChip", ChipMesh, ChipMaterial);
                visual.gameObject.SetActive(true);
                visual.position = at;
                visual.rotation = Random.rotationUniform;
                visual.localScale = Vector3.one * ChipSize;

                var outward = Random.insideUnitCircle.normalized;
                var velocity = new Vector3(outward.x, 0f, outward.y) * (ChipSpeed * Random.Range(0.5f, 1f));
                velocity.y = ChipSpeed * Random.Range(0.6f, 1.2f);
                _chips.Add(new Chip { Visual = visual, Velocity = velocity });
            }
        }

        // ---------------------------------------------------------------- pools and pieces

        private Transform Take(ProjectileKind kind)
        {
            var pool = kind == ProjectileKind.Javelin ? _javelinPool : _arrowPool;
            var visual = pool.Count > 0
                ? pool.Pop()
                : kind == ProjectileKind.Javelin
                    ? CreatePiece("Javelin", JavelinMesh, JavelinMaterial)
                    : CreatePiece("Arrow", ArrowMesh, ArrowMaterial);
            visual.localScale = Vector3.one;
            visual.gameObject.SetActive(true);
            return visual;
        }

        private void Return(ProjectileKind kind, Transform visual)
        {
            if (visual == null) return;
            visual.gameObject.SetActive(false);
            (kind == ProjectileKind.Javelin ? _javelinPool : _arrowPool).Push(visual);
        }

        private Transform CreatePiece(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        private static float LengthOf(ProjectileKind kind) => kind == ProjectileKind.Javelin ? 0.7f : 0.52f;

        // Drawn larger than life on purpose: at the game's zoom a true-to-scale arrow is a pixel.
        private Mesh ArrowMesh => _arrowMesh != null ? _arrowMesh : _arrowMesh = BuildShaft("ShotArrow", LengthOf(ProjectileKind.Arrow), 0.036f, 0.09f, 0.13f);
        private Mesh JavelinMesh => _javelinMesh != null ? _javelinMesh : _javelinMesh = BuildShaft("ShotJavelin", LengthOf(ProjectileKind.Javelin), 0.042f, 0.095f, 0.15f);
        private Mesh ChipMesh => _chipMesh != null ? _chipMesh : _chipMesh = BuildChip();

        private Material ArrowMaterial => _arrowMaterial != null ? _arrowMaterial : _arrowMaterial = new Material(RuntimeShaders.Lit) { color = ArrowColor };
        private Material JavelinMaterial => _javelinMaterial != null ? _javelinMaterial : _javelinMaterial = new Material(RuntimeShaders.Lit) { color = JavelinColor };
        private Material ChipMaterial => _chipMaterial != null ? _chipMaterial : _chipMaterial = new Material(RuntimeShaders.Unlit) { color = ChipColor };

        /// <summary>
        /// A square shaft along +Z ending in a four-sided head, its tip at the origin's +Z end so
        /// the piece's position is where it is going. One mesh, one material: a single draw.
        /// </summary>
        private static Mesh BuildShaft(string name, float length, float thickness, float headWidth, float headLength)
        {
            var h = thickness * 0.5f;
            var w = headWidth * 0.5f;
            var tail = -length;
            var neck = -headLength;

            var vertices = new List<Vector3>(48);
            var triangles = new List<int>(72);

            // Shaft: four side quads, from the tail to the neck, plus the tail cap.
            Vector3[] ring =
            {
                new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f),
            };
            for (var side = 0; side < 4; side++)
            {
                var a = ring[side];
                var b = ring[(side + 1) % 4];
                AddQuad(vertices, triangles,
                    a + Vector3.forward * tail, b + Vector3.forward * tail,
                    b + Vector3.forward * neck, a + Vector3.forward * neck);
            }
            AddQuad(vertices, triangles,
                ring[3] + Vector3.forward * tail, ring[2] + Vector3.forward * tail,
                ring[1] + Vector3.forward * tail, ring[0] + Vector3.forward * tail);

            // Head: a pyramid from a square at the neck to the tip, plus its back face.
            Vector3[] head =
            {
                new Vector3(-w, -w, neck), new Vector3(w, -w, neck), new Vector3(w, w, neck), new Vector3(-w, w, neck),
            };
            var tip = Vector3.zero;
            for (var side = 0; side < 4; side++)
            {
                var start = vertices.Count;
                vertices.Add(head[side]);
                vertices.Add(head[(side + 1) % 4]);
                vertices.Add(tip);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
            AddQuad(vertices, triangles, head[3], head[2], head[1], head[0]);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A unit octahedron: the cheapest thing that reads as a splinter of something.</summary>
        private static Mesh BuildChip()
        {
            var vertices = new List<Vector3>(24);
            var triangles = new List<int>(24);
            Vector3[] axes = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
            for (var i = 0; i < 4; i++)
            {
                var a = axes[i] * 0.5f;
                var b = axes[(i + 1) % 4] * 0.5f;
                AddTriangle(vertices, triangles, a, Vector3.up * 0.5f, b);
                AddTriangle(vertices, triangles, b, Vector3.down * 0.5f, a);
            }
            var mesh = new Mesh { name = "ShotChip" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Corners in order round the face; the face points along (b - a) x (c - a), which is Unity's front side.</summary>
        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        // ---------------------------------------------------------------- lifetime

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_arrowMesh != null) Destroy(_arrowMesh);
            if (_javelinMesh != null) Destroy(_javelinMesh);
            if (_chipMesh != null) Destroy(_chipMesh);
            if (_arrowMaterial != null) Destroy(_arrowMaterial);
            if (_javelinMaterial != null) Destroy(_javelinMaterial);
            if (_chipMaterial != null) Destroy(_chipMaterial);
        }
    }
}
