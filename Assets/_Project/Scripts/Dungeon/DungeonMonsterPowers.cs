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

        public void Bind(OrcUnit unit, DungeonMonsters owner)
        {
            _unit = unit;
            _owner = owner;
            _animator = GetComponent<FigureAnimator>();
            var c = BalanceConfig.Instance;
            _web = c.QueenWebEvery * 0.5f;
            _brood = c.QueenBroodEvery;
            _slam = c.GolemSlamEvery * 0.5f;
            _boulder = c.GolemBoulderEvery;
            unit.HitSoldier += OnHit;
            unit.Hurt += OnHurt;
        }

        private void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.HitSoldier -= OnHit;
                _unit.Hurt -= OnHurt;
            }
            if (_cocoon != null) Destroy(_cocoon);
        }

        private void OnHit(SoldierUnit soldier)
        {
            if (_unit.Kind != OrcKind.Spider || soldier == null) return;
            var c = BalanceConfig.Instance;
            soldier.Slow(c.SpiderPoisonSeconds, c.SpiderPoisonKeep);
        }

        private void OnHurt(OrcUnit unit)
        {
            var c = BalanceConfig.Instance;
            var half = unit.CurrentHealth * 2 <= unit.MaxHealth;
            if (!half) return;
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
            if (_unit.Kind == OrcKind.SpiderQueen) QueenUpdate(dt, c);
            else if (_unit.Kind == OrcKind.OreGolem) GolemUpdate(dt, c);
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
