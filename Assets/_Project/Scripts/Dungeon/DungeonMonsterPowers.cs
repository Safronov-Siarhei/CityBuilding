using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// What a dungeon monster does besides hitting (the user, 2026-10-31). Every number is the
    /// sheet's (dungeon_*).
    /// - A spider hunter's bite poisons: the hero walks slowed for a while.
    /// - The spider queen spits web at a hero in reach: he is bound for a moment. In a fight she
    ///   calls her brood now and then. At half health, once, she seals herself in a COCOON: she
    ///   neither moves nor strikes, spiderlings run to her, and every one of them left alive heals
    ///   her through the silk until she breaks out.
    /// - The ore golem slams the ground round him (both fists, everyone near is hit), throws
    ///   boulders at a hero who keeps his distance, and at half health grows ore armour that turns
    ///   aside a share of every blow.
    /// A skeleton's getting up is OrcUnit's own (GetsUpOnce).
    /// Level 2 (the user's roster, 2026-11-04):
    /// - A dwarf skeleton's rusted armour turns aside a share of every blow until half health, then
    ///   falls off (it still gets up once, as the miner does).
    /// - A drowned dwarf lies unseen under the water (no blow reaches it, nothing targets it) until a
    ///   hero comes within dungeon_ambush_reach, then rises out of it.
    /// - The dwarf king slams his hammer round him, raises stone guards now and then, and at half
    ///   health, once, sits on his throne: he neither moves nor strikes, guards stand up round him and
    ///   every one of them left alive heals him until they fall or the time runs out.
    /// - The canal serpent spits water (a ranged blow that knocks the hero off his feet a moment) and
    ///   every so often in a fight dives: under the water nothing reaches it, and it comes up at
    ///   another wading spot within dungeon_serpent_surface_reach of where it was.
    /// </summary>
    public class DungeonMonsterPowers : MonoBehaviour
    {
        private OrcUnit _unit;
        private DungeonMonsters _owner;
        private FigureAnimator _animator;
        private float _web, _brood, _slam, _boulder;
        private float _cocoonLeft, _healCarry;
        private GameObject _cocoon;
        private readonly List<OrcUnit> _cocoonBrood = new List<OrcUnit>();

        /// <summary>The queen is in her cocoon now.</summary>
        public bool InCocoon => _cocoonLeft > 0f;

        /// <summary>The queen has been in it (once a fight).</summary>
        public bool HasCocooned { get; private set; }

        /// <summary>The golem has grown his ore armour.</summary>
        public bool Armoured { get; private set; }

        // level 2
        private float _kingGuards, _throneLeft, _throneCarry, _dive, _diveLeft, _riseLeft, _ambushCheck;
        private bool _waiting, _moved;
        private readonly List<OrcUnit> _throneGuards = new List<OrcUnit>();
        private GameObject _throneGlow;
        private List<Vector3> _surface;
        private Transform _figure;
        private Vector3 _figureHome;
        private const float SinkSeconds = 0.9f;
        private const float WakeEverySeconds = 0.22f;
        private Vector3 _diveFrom, _diveTo;
        private float _wakeClock;

        /// <summary>The king has sat on his throne (once a fight).</summary>
        public bool HasThroned { get; private set; }

        /// <summary>The king is on his throne now.</summary>
        public bool OnThrone => _throneLeft > 0f;

        /// <summary>A drowned dwarf still lying in wait under the water.</summary>
        public bool InAmbush => _waiting;

        /// <summary>The serpent is under the water now.</summary>
        public bool Dived => _diveLeft > 0f;

        /// <summary>A dwarf skeleton has lost its armour.</summary>
        public bool ArmourDropped { get; private set; }

        public void Bind(OrcUnit unit, DungeonMonsters owner)
        {
            _unit = unit;
            _owner = owner;
            _animator = GetComponent<FigureAnimator>();
            var c = BalanceConfig.Instance;
            _web = c.QueenWebEvery * 0.5f;
            _brood = c.QueenBroodEvery;
            _slam = (unit.Kind == OrcKind.DwarfKing ? c.KingSlamEvery : c.GolemSlamEvery) * 0.5f;
            _boulder = c.GolemBoulderEvery;
            _kingGuards = c.KingGuardsEvery * 0.5f;
            _dive = c.SerpentDiveEvery;
            _figure = transform.Find(unit.Kind.ToString());
            if (_figure != null) _figureHome = _figure.localPosition;
            unit.HitSoldier += OnHit;
            unit.Hurt += OnHurt;
            if (unit.Kind == OrcKind.DrownedDwarf)
            {
                _waiting = true;
                Submerge(true);
                ShowFigure(false);
            }
            if (unit.Kind == OrcKind.CanalSerpent)
                _surface = DungeonMonsters.WadingSpots(transform.position, c.SerpentSurfaceReach, 1.5f);
        }

        /// <summary>Under the water or up out of it: a submerged one is out of every fight (OrcUnit.All, blows, its own thinking).</summary>
        private void Submerge(bool under)
        {
            _unit.Submerged = under;
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = !under;
            _unit.enabled = !under;
            if (!under) _unit.Asleep = false;
        }

        /// <summary>Lowers the figure by `depth` world units under its place (0: home).</summary>
        private void Lower(float depth)
        {
            if (_figure == null) return;
            var scale = Mathf.Max(0.01f, transform.lossyScale.y);
            _figure.localPosition = _figureHome + Vector3.down * (depth / scale);
        }

        private void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.HitSoldier -= OnHit;
                _unit.Hurt -= OnHurt;
            }
            if (_cocoon != null) Destroy(_cocoon);
            if (_throneGlow != null) Destroy(_throneGlow);
        }

        private void OnHit(SoldierUnit soldier)
        {
            if (soldier == null) return;
            var c = BalanceConfig.Instance;
            if (_unit.Kind == OrcKind.Spider) soldier.Slow(c.SpiderPoisonSeconds, c.SpiderPoisonKeep);
            // the serpent's spit knocks the hero off his feet a moment
            if (_unit.Kind == OrcKind.CanalSerpent) soldier.Stun(c.SerpentSpitStun);
        }

        private void OnHurt(OrcUnit unit)
        {
            var c = BalanceConfig.Instance;
            var half = unit.CurrentHealth * 2 <= unit.MaxHealth;
            if (!half) return;
            if (unit.Kind == OrcKind.DwarfSkeleton && !ArmourDropped)
            {
                ArmourDropped = true;
                unit.Armour = 0f;
            }
            if (unit.Kind == OrcKind.DwarfKing && !HasThroned) StartThrone();
            if (unit.Kind == OrcKind.SpiderQueen && !HasCocooned) StartCocoon();
            if (unit.Kind == OrcKind.OreGolem && !Armoured)
            {
                Armoured = true;
                unit.Armour = c.GolemArmour;
                GrowOre();
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_golem_armour"), EventSeverity.Warning,
                    EventGlyph.Skull, null, transform.position));
            }
        }

        private void Update()
        {
            if (_unit == null || _unit.CurrentHealth <= 0) return;
            var dt = Time.deltaTime;
            var c = BalanceConfig.Instance;
            // these run under the water too, where the unit itself is switched off
            if (_waiting || _riseLeft > 0f)
            {
                AmbushUpdate(dt, c);
                return;
            }
            if (_diveLeft > 0f)
            {
                DiveUpdate(dt, c);
                return;
            }
            if (_unit.Asleep) return;
            if (_unit.Kind == OrcKind.SpiderQueen) QueenUpdate(dt, c);
            else if (_unit.Kind == OrcKind.OreGolem) GolemUpdate(dt, c);
            else if (_unit.Kind == OrcKind.DwarfKing) KingUpdate(dt, c);
            else if (_unit.Kind == OrcKind.CanalSerpent) SerpentUpdate(dt, c);
        }

        // ---------------------------------------------------------------- the drowned dwarf's ambush

        private void AmbushUpdate(float dt, BalanceConfig c)
        {
            if (_waiting)
            {
                _ambushCheck -= dt;
                if (_ambushCheck > 0f) return;
                _ambushCheck = 0.25f;
                if (NearestHero(c.AmbushReach) == null) return;
                _waiting = false;
                _riseLeft = Mathf.Max(0.05f, c.AmbushRiseSeconds);
                ShowFigure(true);
                Lower(RiseDepth);
                return;
            }
            _riseLeft -= dt;
            var t = 1f - Mathf.Clamp01(_riseLeft / Mathf.Max(0.05f, c.AmbushRiseSeconds));
            Lower(RiseDepth * (1f - t));
            if (_riseLeft > 0f) return;
            _riseLeft = 0f;
            Lower(0f);
            Submerge(false);
        }

        private const float RiseDepth = 0.5f;

        /// <summary>Wakes a drowned dwarf at once -- for the tests.</summary>
        public void RiseNow()
        {
            if (!_waiting) return;
            _waiting = false;
            _riseLeft = 0f;
            ShowFigure(true);
            Lower(0f);
            Submerge(false);
        }

        // ---------------------------------------------------------------- the dwarf king

        private void KingUpdate(float dt, BalanceConfig c)
        {
            if (_throneLeft > 0f)
            {
                _throneLeft -= dt;
                _throneGuards.RemoveAll(s => s == null || s.CurrentHealth <= 0);
                _throneCarry += _throneGuards.Count * c.KingThroneHeal * _unit.MaxHealth * dt;
                var whole = Mathf.FloorToInt(_throneCarry);
                if (whole > 0)
                {
                    _unit.Heal(whole);
                    _throneCarry -= whole;
                }
                if (_throneGlow != null) _throneGlow.transform.localScale = new Vector3(2.6f, 0.01f, 2.6f) * (1f + 0.06f * Mathf.Sin(Time.time * 5f));
                if (_throneLeft <= 0f || _throneGuards.Count == 0) EndThrone();
                return;
            }
            if (_unit.BusyFor > 0f) return;
            if (_unit.Fighting == null) return;
            _slam -= dt;
            _kingGuards -= dt;
            if (_kingGuards <= 0f)
            {
                _kingGuards = c.KingGuardsEvery;
                for (var k = 0; k < c.KingGuardsCount; k++) CallGuard(k, c.KingGuardsCount);
            }
            if (_slam <= 0f && NearestHero(c.KingSlamRadius) != null)
            {
                _slam = c.KingSlamEvery;
                if (_animator != null) _animator.OnStruck();
                SlamRound(c.KingSlamRadius, c.KingSlamDamage);
            }
        }

        private void StartThrone()
        {
            var c = BalanceConfig.Instance;
            HasThroned = true;
            _throneLeft = c.KingThroneSeconds;
            _unit.BusyFor = c.KingThroneSeconds;
            _throneGuards.Clear();
            for (var k = 0; k < c.KingThroneGuards; k++)
            {
                var g = CallGuard(k, c.KingThroneGuards);
                if (g != null) _throneGuards.Add(g);
            }
            // the curse's cyan light under him while he draws on his guards
            _throneGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(_throneGlow.GetComponent<Collider>());
            _throneGlow.name = "ThroneGlow";
            _throneGlow.transform.position = transform.position + Vector3.up * 0.03f;
            _throneGlow.transform.localScale = new Vector3(2.6f, 0.01f, 2.6f);
            var renderer = _throneGlow.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(RuntimeShaders.Unlit) { color = new Color(0.3f, 0.85f, 1f) };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_king_throne"), EventSeverity.Warning,
                EventGlyph.Skull, null, transform.position));
        }

        private void EndThrone()
        {
            _throneLeft = 0f;
            _unit.BusyFor = 0f;
            if (_throneGlow != null) Destroy(_throneGlow);
        }

        private OrcUnit CallGuard(int k, int of)
        {
            if (_owner == null) return null;
            var angle = k * Mathf.PI * 2f / Mathf.Max(1, of) + 0.4f;
            var at = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.4f;
            return _owner.Summon(_unit, OrcKind.StoneGuard, at);
        }

        // ---------------------------------------------------------------- the canal serpent

        private void SerpentUpdate(float dt, BalanceConfig c)
        {
            if (_unit.Fighting == null || _surface == null || _surface.Count == 0) return;
            _dive -= dt;
            if (_dive > 0f) return;
            DiveNow();
        }

        /// <summary>The serpent goes under -- the fight's timer, or a test.</summary>
        public void DiveNow()
        {
            var c = BalanceConfig.Instance;
            _diveLeft = Mathf.Max(SinkSeconds * 2f + 0.1f, c.SerpentDiveSeconds);
            _moved = false;
            _diveFrom = transform.position;
            _diveTo = PickSurface();
            Submerge(true);
            // it goes down in a splash (the user, 2026-11-04: «просто исчезает и появляется в другом месте»)
            DungeonWake.Splash(_diveFrom, 3f);
            DungeonWake.Splash(_diveFrom + Vector3.forward * 0.3f, 2f);
            if (_animator != null) _animator.OnStruck();
        }

        private void DiveUpdate(float dt, BalanceConfig c)
        {
            var total = Mathf.Max(SinkSeconds * 2f + 0.1f, c.SerpentDiveSeconds);
            _diveLeft -= dt;
            var gone = total - _diveLeft;
            const float depth = 2.2f;
            if (gone < SinkSeconds)
            {
                // down slowly, head last
                var k = gone / SinkSeconds;
                Lower(depth * k * k);
            }
            else if (_diveLeft > SinkSeconds)
            {
                // under the water: a wake of rings runs from where it went down to where it comes up
                Lower(depth);
                ShowFigure(false);
                _wakeClock -= dt;
                if (_wakeClock <= 0f)
                {
                    _wakeClock = WakeEverySeconds;
                    var span = Mathf.Max(0.01f, total - SinkSeconds * 2f);
                    var along = Mathf.Clamp01((gone - SinkSeconds) / span);
                    DungeonWake.Splash(Vector3.Lerp(_diveFrom, _diveTo, along), 1.6f);
                }
            }
            else
            {
                if (!_moved)
                {
                    Resurface();
                    ShowFigure(true);
                    DungeonWake.Splash(transform.position, 3f);
                }
                var k = Mathf.Clamp01(_diveLeft / SinkSeconds);
                Lower(depth * k * k);
            }
            if (_diveLeft > 0f) return;
            _diveLeft = 0f;
            Lower(0f);
            Submerge(false);
            _dive = c.SerpentDiveEvery;
        }

        /// <summary>Picks where it comes up: another wading spot near its pool, a couple of cells from here at least.</summary>
        private void Resurface()
        {
            _moved = true;
            transform.position = _diveTo;
            _unit.MovePost(_diveTo);
        }

        /// <summary>Another wading spot near its pool, a couple of cells from here at least.</summary>
        private Vector3 PickSurface()
        {
            var here = transform.position;
            if (_surface == null || _surface.Count == 0) return here;
            for (var tries = 0; tries < 8; tries++)
            {
                var at = _surface[Random.Range(0, _surface.Count)];
                var d = at - here;
                d.y = 0f;
                if (d.magnitude < 2f && tries < 7) continue;
                return at;
            }
            return here;
        }

        /// <summary>A blow to every hero within `radius`, `mult` times its own.</summary>
        private void SlamRound(float radius, float mult)
        {
            var damage = Mathf.Max(1, Mathf.RoundToInt(_unit.AttackDamage * mult));
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                var d = soldier.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= radius) soldier.TakeDamage(damage);
            }
        }

        // ---------------------------------------------------------------- the spider queen

        private void QueenUpdate(float dt, BalanceConfig c)
        {
            if (_cocoonLeft > 0f)
            {
                _cocoonLeft -= dt;
                _cocoonBrood.RemoveAll(s => s == null || s.CurrentHealth <= 0);
                // every spiderling left alive feeds her a share of her health a second
                _healCarry += _cocoonBrood.Count * c.QueenCocoonHeal * _unit.MaxHealth * dt;
                var whole = Mathf.FloorToInt(_healCarry);
                if (whole > 0)
                {
                    _unit.Heal(whole);
                    _healCarry -= whole;
                }
                if (_cocoon != null) _cocoon.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 4f));
                if (_cocoonLeft <= 0f) EndCocoon();
                return;
            }
            var foe = _unit.Fighting;
            if (foe == null) return;
            _web -= dt;
            _brood -= dt;
            if (_web <= 0f)
            {
                var target = NearestHero(c.QueenWebRange);
                if (target != null)
                {
                    _web = c.QueenWebEvery;
                    target.Stun(c.QueenWebStun);
                    WebUnder(target, c.QueenWebStun);
                    if (_animator != null) _animator.OnStruck();
                }
            }
            if (_brood <= 0f)
            {
                _brood = c.QueenBroodEvery;
                for (var k = 0; k < c.QueenBroodCount; k++) CallBrood(k, c.QueenBroodCount);
            }
        }

        private void StartCocoon()
        {
            var c = BalanceConfig.Instance;
            HasCocooned = true;
            _cocoonLeft = c.QueenCocoonSeconds;
            _unit.BusyFor = c.QueenCocoonSeconds;
            ShowFigure(false);
            var model = OrcRaidManager.Instance != null ? OrcRaidManager.Instance.SpiderCocoonModel : null;
            if (model != null)
            {
                _cocoon = Instantiate(model, transform.position, Quaternion.identity);
                _cocoon.name = "Cocoon";
                SceneryShadows.DisableCasting(_cocoon);
            }
            _cocoonBrood.Clear();
            for (var k = 0; k < c.QueenCocoonBrood; k++)
            {
                var s = CallBrood(k, c.QueenCocoonBrood);
                if (s != null) _cocoonBrood.Add(s);
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_queen_cocoon"), EventSeverity.Warning,
                EventGlyph.Skull, null, transform.position));
        }

        private void EndCocoon()
        {
            _cocoonLeft = 0f;
            if (_cocoon != null) Destroy(_cocoon);
            ShowFigure(true);
        }

        private OrcUnit CallBrood(int k, int of)
        {
            if (_owner == null) return null;
            var angle = k * Mathf.PI * 2f / Mathf.Max(1, of) + Random.Range(0f, 0.5f);
            var at = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.2f;
            return _owner.Summon(_unit, OrcKind.Spiderling, at);
        }

        private void ShowFigure(bool on)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.GetComponentInParent<Buildings.BuildingHealthBar>() == null) r.enabled = on;
        }

        /// <summary>A sheet of web on the ground under a bound hero, gone when he is free.</summary>
        private static void WebUnder(SoldierUnit hero, float seconds)
        {
            var web = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(web.GetComponent<Collider>());
            web.name = "Web";
            web.transform.position = hero.transform.position + Vector3.up * 0.02f;
            web.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
            var renderer = web.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(RuntimeShaders.Unlit) { color = new Color(0.95f, 0.96f, 1f) };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(web, seconds);
        }

        // ---------------------------------------------------------------- the ore golem

        private void GolemUpdate(float dt, BalanceConfig c)
        {
            if (_unit.BusyFor > 0f) return;
            var foe = _unit.Fighting;
            if (foe == null) return;
            _slam -= dt;
            _boulder -= dt;
            if (_slam <= 0f && NearestHero(c.GolemSlamRadius) != null)
            {
                _slam = c.GolemSlamEvery;
                if (_animator != null) _animator.Slam();
                var damage = Mathf.Max(1, Mathf.RoundToInt(_unit.AttackDamage * c.GolemSlamDamage));
                foreach (var soldier in SoldierUnit.All)
                {
                    if (soldier == null || soldier.CurrentHealth <= 0) continue;
                    var d = soldier.transform.position - transform.position;
                    d.y = 0f;
                    if (d.magnitude <= c.GolemSlamRadius) soldier.TakeDamage(damage);
                }
                return;
            }
            if (_boulder <= 0f)
            {
                // a boulder for whoever keeps his distance
                SoldierUnit far = null;
                var best = 0f;
                foreach (var soldier in SoldierUnit.All)
                {
                    if (soldier == null || soldier.CurrentHealth <= 0) continue;
                    var d = soldier.transform.position - transform.position;
                    d.y = 0f;
                    var m = d.magnitude;
                    if (m <= c.GolemSlamRadius || m > c.GolemBoulderRange || m <= best) continue;
                    best = m;
                    far = soldier;
                }
                if (far == null) return;
                _boulder = c.GolemBoulderEvery;
                if (_animator != null) _animator.OnStruck();
                CombatProjectiles.Fire(ProjectileKind.Cannonball, CombatProjectiles.MuzzleOfUnit(transform), far, _unit.AttackDamage);
            }
        }

        /// <summary>Lumps of gold and copper ore breaking out over his body: the armour, seen.</summary>
        private void GrowOre()
        {
            var gold = new Material(RuntimeShaders.Lit) { color = new Color(0.95f, 0.75f, 0.2f) };
            var copper = new Material(RuntimeShaders.Lit) { color = new Color(0.85f, 0.45f, 0.2f) };
            var top = transform.position.y;
            foreach (var r in GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            var height = Mathf.Max(0.5f, top - transform.position.y);
            for (var k = 0; k < 10; k++)
            {
                var lump = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(lump.GetComponent<Collider>());
                lump.name = "Ore";
                var a = k * 2.4f;
                var at = transform.position + new Vector3(Mathf.Cos(a) * height * 0.22f, height * (0.35f + 0.05f * (k % 6)), Mathf.Sin(a) * height * 0.18f);
                lump.transform.SetParent(transform, true);
                lump.transform.position = at;
                lump.transform.rotation = Quaternion.Euler(k * 37f, k * 53f, k * 19f);
                lump.transform.localScale = Vector3.one * (height * 0.09f) / Mathf.Max(0.01f, transform.lossyScale.x);
                var renderer = lump.GetComponent<Renderer>();
                renderer.sharedMaterial = k % 3 == 0 ? copper : gold;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private SoldierUnit NearestHero(float range)
        {
            SoldierUnit nearest = null;
            var best = range;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                var d = soldier.transform.position - transform.position;
                d.y = 0f;
                var m = d.magnitude;
                if (m > best) continue;
                best = m;
                nearest = soldier;
            }
            return nearest;
        }
    }
}
