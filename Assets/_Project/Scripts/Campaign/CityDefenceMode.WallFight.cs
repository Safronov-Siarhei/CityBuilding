using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The fight ON THE WALL (the user's seventh playtest, 2026-11-13): an orc that climbs a siege tower steps onto the
    /// wall-walk where the tower stands and fights the defenders up there -- it walks the walk (WallWalk's cells) to the
    /// nearest of them and strikes. Only when no defender stands on the wall within m2_wall_fight_cells does it jump
    /// down into the town (and makes for the inner gate). The town's infantry climb up against it,
    /// m2_wall_defenders_per_orc to each orc, and come down when it is gone. The docked tower is a stair onto the wall
    /// for the defenders too (WallWalk.Dock): Ардан, close to its foot, climbs it; the stick then walks him along the
    /// wall (sideways) or brings him down (into the town or out by the tower).
    /// </summary>
    public partial class CityDefenceMode
    {
        private class WallOrc
        {
            public OrcUnit orc;
            public Vector2Int cell;
            public float hitIn;
            public bool jumping;
            public readonly List<Vector2Int> path = new List<Vector2Int>();
        }

        private readonly List<WallOrc> _wallOrcs = new List<WallOrc>();
        // the infantry sent up, and the orc each is after
        private readonly Dictionary<SoldierUnit, OrcUnit> _wallDefenders = new Dictionary<SoldierUnit, OrcUnit>();
        private float _wallDefenceIn;
        private float _ardenWallIn;

        private static float Cell => GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

        /// <summary>The orc stands on the wall-walk at `cell`, steered from here.</summary>
        private void PutOnWall(OrcUnit orc, Vector2Int cell)
        {
            if (orc == null) return;
            orc.Steered = true;
            orc.Untargetable = false;
            orc.enabled = true;
            orc.LaneOnly = true;
            orc.FollowLane(null);
            orc.transform.position = WallWalk.Top(cell);
            _wallOrcs.Add(new WallOrc { orc = orc, cell = cell, hitIn = 0.5f });
        }

        private bool OnTheWall(OrcUnit orc)
        {
            foreach (var w in _wallOrcs) if (w.orc == orc) return true;
            return false;
        }

        private void TickWallOrcs(float dt)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;
            var b = B;
            for (var i = _wallOrcs.Count - 1; i >= 0; i--)
            {
                var w = _wallOrcs[i];
                if (w.orc == null || w.orc.CurrentHealth <= 0)
                {
                    _wallOrcs.RemoveAt(i);
                    continue;
                }
                if (w.jumping) continue;
                var at = w.orc.transform.position;
                var foe = NearestOnWall(at, b.wallFightCells * Cell);
                if (foe == null)
                {
                    // nobody left up here: down into the town
                    w.jumping = true;
                    StartCoroutine(JumpIn(w));
                    continue;
                }
                var to = foe.transform.position - at;
                to.y = 0f;
                if (to.magnitude <= Mathf.Max(w.orc.UnitRange, 0.6f * Cell) + 0.3f)
                {
                    if (to.sqrMagnitude > 0.0001f) w.orc.transform.rotation = Quaternion.LookRotation(to);
                    w.hitIn -= dt;
                    if (w.hitIn > 0f) continue;
                    w.hitIn = Mathf.Max(0.3f, w.orc.AttackInterval);
                    w.orc.StrikeSoldier(foe);
                    continue;
                }
                // along the walk towards him, cell by cell
                var goal = grid.WorldToCell(foe.transform.position);
                if (!WallWalk.Path(w.cell, goal, w.path) || w.path.Count < 2)
                {
                    // not on this stretch: he cannot be reached from here -- off the wall then
                    w.jumping = true;
                    StartCoroutine(JumpIn(w));
                    continue;
                }
                var next = WallWalk.Top(w.path[1]);
                var step = next - at;
                var move = b.wallOrcSpeed * Cell * dt;
                if (step.magnitude <= move)
                {
                    w.orc.transform.position = next;
                    w.cell = w.path[1];
                }
                else
                {
                    w.orc.transform.position = at + step.normalized * move;
                    step.y = 0f;
                    if (step.sqrMagnitude > 0.0001f) w.orc.transform.rotation = Quaternion.LookRotation(step);
                }
            }
            TickWallDefence(dt);
        }

        /// <summary>The nearest live defender up on the wall (Ардан too), within `reach` metres.</summary>
        private SoldierUnit NearestOnWall(Vector3 at, float reach)
        {
            SoldierUnit best = null;
            var bestD = reach;
            foreach (var s in SoldierUnit.All)
            {
                if (s == null || s.CurrentHealth <= 0 || !s.OnWall) continue;
                var d = Flat(s.transform.position - at);
                if (d >= bestD) continue;
                bestD = d;
                best = s;
            }
            return best;
        }

        /// <summary>A jump off the wall-walk into the town, then on to the inner gate.</summary>
        private IEnumerator JumpIn(WallOrc w)
        {
            var orc = w.orc;
            var from = orc.transform.position;
            var drop = OnMesh(from + Vector3.back * (2.2f * Cell));
            const float seconds = 0.6f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (orc == null || orc.CurrentHealth <= 0) yield break;
                var k = t / seconds;
                orc.transform.position = Vector3.Lerp(from, drop, k) + Vector3.up * (0.6f * 4f * k * (1f - k));
                yield return null;
            }
            if (orc == null) yield break;
            orc.transform.position = drop;
            var body = orc.GetComponent<CharacterController>();
            if (body != null) body.enabled = true;
            orc.Steered = false;
            _wallOrcs.Remove(w);
            ToInnerGate(orc);
        }

        /// <summary>The town's infantry up against the orcs on the wall; down again once theirs is gone.</summary>
        private void TickWallDefence(float dt)
        {
            _wallDefenceIn -= dt;
            if (_wallDefenceIn > 0f) return;
            _wallDefenceIn = 0.7f;
            var grid = GridManager.Instance;
            var need = Mathf.Max(0, B.wallDefendersPerOrc);
            // those whose orc is gone (dead, or down in the town) come down
            var done = new List<SoldierUnit>();
            foreach (var pair in _wallDefenders)
            {
                var s = pair.Key;
                if (s == null || s.CurrentHealth <= 0) { done.Add(s); continue; }
                var orc = pair.Value;
                if (orc != null && orc.CurrentHealth > 0 && OnTheWall(orc))
                {
                    // keep after him along the walk
                    if (s.OnWall && Flat(s.transform.position - orc.transform.position) > 1.2f * Cell) s.GoToWall(grid.WorldToCell(orc.transform.position));
                    continue;
                }
                done.Add(s);
                if (s != null && s.CurrentHealth > 0 && s.WallBound) s.LeaveWall();
            }
            foreach (var s in done) _wallDefenders.Remove(s);
            if (need == 0) return;
            foreach (var w in _wallOrcs)
            {
                if (w.jumping || w.orc == null) continue;
                var have = 0;
                foreach (var pair in _wallDefenders) if (pair.Value == w.orc) have++;
                while (have < need)
                {
                    var s = FreeInfantry(w.orc.transform.position);
                    if (s == null) break;
                    if (!s.GoToWall(w.cell)) break;
                    _wallDefenders[s] = w.orc;
                    have++;
                }
            }
        }

        /// <summary>The nearest of the garrison's infantry on the ground and not already sent up.</summary>
        private SoldierUnit FreeInfantry(Vector3 near)
        {
            SoldierUnit best = null;
            var bestD = float.MaxValue;
            foreach (var s in _garrison)
            {
                if (s == null || s.CurrentHealth <= 0 || s.IsRanged || s.Type == SoldierType.Monk || s.WallBound || _wallDefenders.ContainsKey(s)) continue;
                // the street patrols keep to their rounds (their next order would pull a man off the wall)
                var patrolling = false;
                foreach (var p in _patrols) if (p.group == s.Group) { patrolling = true; break; }
                if (patrolling) continue;
                var d = Flat(s.transform.position - near);
                if (d >= bestD) continue;
                bestD = d;
                best = s;
            }
            return best;
        }

        /// <summary>
        /// Ардан and the wall (the user's ninth playtest): he goes up only by stepping on the small yellow circle at a
        /// Barbican tower's door (town side) -- or at a docked orc tower's foot; up there the stick walks him freely
        /// along the wall (SoldierUnit.SlideAlongWall) and, pushed towards the town, brings him down by the nearest
        /// tower door. Never down on the field side.
        /// </summary>
        private void TickArdenWall(float dt)
        {
            // a gate's towers fallen: their door circles go (the user's eleventh playtest)
            foreach (var door in _doorCircles)
            {
                if (door.mark == null || WallWalk.IsStair(door.stair)) continue;
                Destroy(door.mark);
                door.mark = null;
            }
            if (!ArdenAlive) return;
            var stick = new Vector3(Stick.x, 0f, Stick.y);
            // along the wall, every frame
            if (Arden.OnWall && stick.sqrMagnitude > 0.04f && Mathf.Abs(stick.x) >= Mathf.Abs(stick.z))
            {
                Arden.SlideAlongWall(stick.normalized, dt);
                return;
            }
            _ardenWallIn -= dt;
            if (_ardenWallIn > 0f) return;
            _ardenWallIn = 0.1f;
            var grid = GridManager.Instance;
            if (grid == null) return;
            var at = Arden.transform.position;
            if (!Arden.WallBound)
            {
                // a door circle: stepped on (after having stepped off it since he came down)
                var onCircle = false;
                foreach (var door in _doorCircles)
                {
                    if (door.mark == null || !WallWalk.IsStair(door.stair)) continue;
                    if (Flat(at - door.at) > DoorCircleRadius * Cell) continue;
                    onCircle = true;
                    if (_ardenOffCircle && Arden.GoToWall(door.stair))
                    {
                        _ardenOffCircle = false;
                        _ardenWallIn = 1f;
                    }
                    break;
                }
                if (!onCircle) _ardenOffCircle = true;
                // never up an orc siege tower (the user's thirteenth playtest: he climbed one that had not reached the
                // wall, and it stood there for good)
                return;
            }
            // down: only towards the town (south, the stick pulled down)
            if (!Arden.OnWall || stick.z > -0.5f || Mathf.Abs(stick.z) < Mathf.Abs(stick.x)) return;
            Arden.LeaveWall(at + Vector3.back * (3f * Cell));
            _ardenOffCircle = false;
            _ardenWallIn = 1.5f;
        }


        // ------------------------------------------------------------------ the Barbican tower doors

        private class DoorCircle
        {
            public Vector2Int stair;
            public Vector3 at;
            public GameObject mark;
        }

        private readonly List<DoorCircle> _doorCircles = new List<DoorCircle>();
        private bool _ardenOffCircle = true;
        // the circle's radius (cells): a step onto it takes him up
        private const float DoorCircleRadius = 0.32f;
        // the door stands this far from the tower cell's middle, on its town face (cells): 0,485 + the step
        private const float DoorOut = 0.72f;

        /// <summary>The town-side door of each Barbican end tower: a yellow circle on the ground before it, and the
        /// way up and down for everyone (SoldierUnit.StairFootAt).</summary>
        private void MarkTowerDoors()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;
            WallWalk.Ensure();
            WallWalk.StairsInto(_stairs);
            foreach (var cell in _stairs)
            {
                if (WallWalk.IsDockStair(cell)) continue;
                var centre = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                var at = OnMesh(centre + Vector3.back * (DoorOut * grid.CellSize));
                at.y = grid.GroundHeight;
                // «Искры у порога» (the user approved the draft 2026-11-15, Tools/blender/tower_door_fx_drafts.py)
                // instead of the flat yellow disc they called cheap: a ring of gold dashes, turning and breathing,
                // and sparks rising from inside it (TickDoorSparks)
                var mark = new GameObject("M2DoorSparks", typeof(MeshFilter), typeof(MeshRenderer));
                mark.transform.position = at + Vector3.up * 0.03f;
                mark.transform.localScale = Vector3.one * (DoorCircleRadius * grid.CellSize);
                mark.GetComponent<MeshFilter>().sharedMesh = DoorRingMesh();
                var r = mark.GetComponent<MeshRenderer>();
                r.sharedMaterial = DoorRingMaterial();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                _doorCircles.Add(new DoorCircle { stair = cell, at = at, mark = mark });
            }
            SoldierUnit.StairFootAt = DoorOf;
        }

        private Vector3? DoorOf(Vector2Int stair)
        {
            foreach (var door in _doorCircles)
                if (door.stair == stair) return door.at;
            return null;
        }

        // the door sparks' look (the approved draft): 12 dashes on the ring, 0.34 / 0.07 of its radius long / wide;
        // the ring turns DoorRingSpin degrees a second and breathes between DoorRingLow and 1 of its gold
        private const int DoorDashes = 12;
        private const float DoorDashLength = 0.34f, DoorDashWidth = 0.07f, DoorRingSpin = 18f, DoorRingLow = 0.5f, DoorRingBreath = 1.6f;
        // sparks per second at each door, how high (in ring radii) they climb over their life, their size (ring radii)
        private const float DoorSparkRate = 5f, DoorSparkRise = 3f, DoorSparkLife = 1.3f, DoorSparkSize = 0.16f;
        private static readonly Color DoorGold = new Color(1f, 0.68f, 0.08f, 1f);
        private static readonly Color DoorSparkGold = new Color(1f, 0.8f, 0.3f, 1f);

        private Mesh _doorRingMesh;
        private Material _doorRingMaterial;
        private ParticleSystem _doorSparks;
        private float _doorSparkDue;

        /// <summary>A unit ring of short dashes in the XZ plane, UVs on the soft dot's bright middle (fxGlow).</summary>
        private Mesh DoorRingMesh()
        {
            if (_doorRingMesh != null) return _doorRingMesh;
            var v = new Vector3[DoorDashes * 4];
            var uv = new Vector2[v.Length];
            var c = new Color[v.Length];
            var tri = new int[DoorDashes * 12];
            for (var i = 0; i < DoorDashes; i++)
            {
                var a = Mathf.PI * 2f * i / DoorDashes;
                var along = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * (DoorDashLength * 0.5f);
                var outw = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var w = outw * (DoorDashWidth * 0.5f);
                v[i * 4] = outw - along - w;
                v[i * 4 + 1] = outw - along + w;
                v[i * 4 + 2] = outw + along + w;
                v[i * 4 + 3] = outw + along - w;
                for (var k = 0; k < 4; k++)
                {
                    uv[i * 4 + k] = new Vector2(0.5f, 0.5f);
                    c[i * 4 + k] = Color.white;
                }
                // both windings: seen from above whichever way the quad turned
                var t = i * 12;
                tri[t] = i * 4; tri[t + 1] = i * 4 + 1; tri[t + 2] = i * 4 + 2;
                tri[t + 3] = i * 4; tri[t + 4] = i * 4 + 2; tri[t + 5] = i * 4 + 3;
                tri[t + 6] = i * 4; tri[t + 7] = i * 4 + 2; tri[t + 8] = i * 4 + 1;
                tri[t + 9] = i * 4; tri[t + 10] = i * 4 + 3; tri[t + 11] = i * 4 + 2;
            }
            _doorRingMesh = new Mesh { name = "M2DoorRing", vertices = v, uv = uv, colors = c, triangles = tri };
            _doorRingMesh.RecalculateBounds();
            return _doorRingMesh;
        }

        private Material DoorRingMaterial()
        {
            if (_doorRingMaterial != null) return _doorRingMaterial;
            _doorRingMaterial = fxGlow != null
                ? new Material(fxGlow) { name = "M2DoorRing" }
                : new Material(Core.RuntimeShaders.Unlit) { name = "M2DoorRing" };
            _doorRingMaterial.color = DoorGold;
            return _doorRingMaterial;
        }

        /// <summary>Every frame: the rings turn and breathe (one shared material), each live door sends sparks up.</summary>
        private void TickDoorSparks(float dt)
        {
            if (_doorCircles.Count == 0) return;
            if (_doorRingMaterial != null)
            {
                var g = DoorGold;
                g.a = Mathf.Lerp(DoorRingLow, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / DoorRingBreath));
                _doorRingMaterial.color = g;
            }
            if (_doorSparks == null && fxGlow != null) _doorSparks = MakeDoorSparks();
            var radius = DoorCircleRadius * Cell;
            var spin = DoorRingSpin * dt;
            _doorSparkDue += dt * DoorSparkRate;
            var emit = (int)_doorSparkDue;
            _doorSparkDue -= emit;
            var p = new ParticleSystem.EmitParams();
            foreach (var door in _doorCircles)
            {
                if (door.mark == null || !door.mark.activeInHierarchy) continue;
                door.mark.transform.Rotate(0f, spin, 0f, Space.Self);
                if (_doorSparks == null) continue;
                for (var i = 0; i < emit; i++)
                {
                    var off = Random.insideUnitCircle * (radius * 0.8f);
                    p.position = door.at + new Vector3(off.x, 0.05f, off.y);
                    p.velocity = new Vector3(Random.Range(-0.05f, 0.05f), DoorSparkRise * radius / DoorSparkLife * Random.Range(0.6f, 1.2f), Random.Range(-0.05f, 0.05f));
                    p.startColor = DoorSparkGold;
                    p.startSize = DoorSparkSize * radius * Random.Range(0.6f, 1.3f);
                    p.startLifetime = DoorSparkLife * Random.Range(0.7f, 1.1f);
                    _doorSparks.Emit(p, 1);
                }
            }
        }

        /// <summary>One world-space system for every door's sparks, fed by Emit: weightless, fading in and out.</summary>
        private ParticleSystem MakeDoorSparks()
        {
            var go = new GameObject("M2DoorSparkFx", typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.gravityModifier = 0f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = fxGlow;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private void ForgetWallFight()
        {
            _wallOrcs.Clear();
            _wallDefenders.Clear();
        }
    }
}
