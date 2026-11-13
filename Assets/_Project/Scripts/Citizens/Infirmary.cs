using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// The sandbox Лечебница (the user, 2026-11-13): beds, not a radius.
    ///
    /// - Falling ill stays SicknessManager's daily roll; each sick person here is one of the walking
    ///   villagers (a Patient), a green skull over his head. He leaves his job and pays no tax.
    /// - The moment he falls ill he is given a free bed in the NEAREST Лечебница on his island that has
    ///   a healer (beds 2/3/4/5/6 by level, BuildingLevelStats.beds); he walks there and his
    ///   treatment counts from then. Nobody in a bed dies.
    /// - With no free bed he lives infirmary_wait_sec (the walk counts): the skull wears a red ring
    ///   that drains. A bed freed in time goes to whoever fell ill FIRST; none, and he dies.
    /// - Treatment takes infirmary_treat_sec per patient, in order, as many at once as the building
    ///   has healers (its workers, 1/2/2/3/3). A Лечебница with no healer takes nobody.
    /// - Notices: «На острове не хватает мест в лечебнице», «Назначьте рабочего лечебницы».
    ///
    /// The headcount stays CitizenManager's SickPopulation; this keeps who the sick are beside it
    /// and follows it (Reconcile), so a loaded town's sick come back as patients. Off in an
    /// Invasion run, the tutorial and the campaign town: those keep SicknessManager's daily healer.
    /// </summary>
    public class Infirmary : MonoBehaviour
    {
        public const string HealerId = "HealerHouse";

        public static Infirmary Instance { get; private set; }

        /// <summary>The sandbox's rules are on (not in an Invasion run, the tutorial or the campaign town).</summary>
        public static bool Active =>
            Instance != null && !Invasion.InvasionMode.IsActive && !Tutorial.TutorialMode.IsActive && !Campaign.CityDefenceMode.IsActive;

        [SerializeField] private Material markMaterial;

        public class Patient
        {
            public CitizenAgent Figure;
            public BuildingInstance Home;
            public int Island = -1;
            public int Order;
            /// <summary>Seconds without a bed (the death clock; stops in a bed).</summary>
            public float Waited;
            public BuildingInstance Bed;
            public float Treated;
            public bool Inside;
            public GameObject Mark;
            public MaterialPropertyBlock Block;
        }

        private readonly List<Patient> _patients = new List<Patient>();
        private readonly List<BuildingInstance> _healers = new List<BuildingInstance>();
        private readonly List<Patient> _scratch = new List<Patient>();
        private readonly Dictionary<int, float> _toldFull = new Dictionary<int, float>();
        private int _order;
        private static Mesh _quad;

        // a loaded town's patients, waiting for their figures (Reconcile takes them first)
        private readonly List<Patient> _pending = new List<Patient>();

        // the islands today's illness struck, one entry a person (SicknessManager): new patients come from there
        private readonly Queue<int> _expected = new Queue<int>();

        /// <summary>So many fell ill on this island today: the next patients are its people.</summary>
        public void Expect(int island, int count)
        {
            for (var i = 0; i < count; i++) _expected.Enqueue(island);
        }

        /// <summary>A save's patients (GameSaveController.RestorePatients): each back in his house and bed, his clocks as they were.</summary>
        public void RestoreFromSave(List<Patient> patients)
        {
            ClearAll();
            _pending.Clear();
            if (patients == null) return;
            patients.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var p in patients)
            {
                p.Figure = null;
                p.Mark = null;
                _pending.Add(p);
                _order = Mathf.Max(_order, p.Order);
            }
        }

        private const float MarkSize = 0.75f;
        private const float NoticeEverySeconds = 20f;
        private const float DoorReach = 1.6f;

        public IReadOnlyList<Patient> Patients => _patients;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static float TreatSeconds => Mathf.Max(1f, BalanceConfig.Instance.InfirmaryTreatSeconds);
        private static float WaitSeconds => Mathf.Max(1f, BalanceConfig.Instance.InfirmaryWaitSeconds);

        /// <summary>The sick living on this island -- not free to work there (HousePeople.IdleOn).</summary>
        public int SickOn(int island)
        {
            var n = 0;
            foreach (var p in _patients) if (HousePeople.Same(p.Island, island)) n++;
            return n;
        }

        public static bool IsHealer(BuildingInstance b) => b != null && b.Data != null && b.Data.buildingName == HealerId && !b.IsBot;

        /// <summary>Sickbeds of this Лечебница at its level.</summary>
        public static int BedsOf(BuildingInstance healer) => healer != null && healer.Data != null ? healer.Data.LevelStats(healer.Level).beds : 0;

        private static int HealersIn(BuildingInstance healer)
        {
            var work = healer != null ? healer.GetComponent<ProductionBuilding>() : null;
            return work != null ? work.AssignedWorkers : 0;
        }

        /// <summary>Takes patients: standing, built, with at least one healer.</summary>
        private static bool Takes(BuildingInstance healer) =>
            healer != null && healer.CurrentHealth > 0 && healer.IsOperational && HealersIn(healer) > 0 && BedsOf(healer) > 0;

        /// <summary>This Лечебница's patients, oldest first (the bottom bar's beds).</summary>
        public List<Patient> InBed(BuildingInstance healer, List<Patient> into)
        {
            into.Clear();
            foreach (var p in _patients) if (p.Bed == healer) into.Add(p);
            into.Sort((a, b) => a.Order.CompareTo(b.Order));
            return into;
        }

        /// <summary>Of a patient in a bed, how far his treatment is (0..1); -1 while he waits for a free healer.</summary>
        public float Progress(Patient p) => p.Bed == null || !Treating(p) ? -1f : Mathf.Clamp01(p.Treated / TreatSeconds);

        private bool Treating(Patient p)
        {
            var place = 0;
            foreach (var other in _patients)
            {
                if (other.Bed != p.Bed || other == p) continue;
                if (other.Order < p.Order) place++;
            }
            return place < HealersIn(p.Bed);
        }

        /// <summary>The card's line: beds taken / beds, the treatment and the wait (null if not the sandbox's Лечебница).</summary>
        public static string CardLine(BuildingInstance building)
        {
            if (!Active || !IsHealer(building)) return null;
            var taken = Instance.InBed(building, Instance._scratch).Count;
            return Localization.Format("#healer_card", taken, BedsOf(building), Mathf.RoundToInt(TreatSeconds), Mathf.RoundToInt(WaitSeconds));
        }

        // ---------------------------------------------------------------- the frame

        private void Update()
        {
            if (!Active)
            {
                if (_patients.Count > 0) ClearAll();
                return;
            }
            var citizens = CitizenManager.Instance;
            if (citizens == null) return;

            Reconcile(citizens);
            if (_patients.Count == 0) return;

            CollectHealers();
            var dt = Time.deltaTime;

            // a bed lost (the building gone, or its last healer taken off): back to waiting
            foreach (var p in _patients)
            {
                if (p.Bed == null) continue;
                if (Takes(p.Bed)) continue;
                p.Bed = null;
                p.Treated = 0f;
                p.Waited = 0f;
                if (p.Inside) LetOut(p);
            }

            // free beds go to whoever fell ill first
            _scratch.Clear();
            foreach (var p in _patients) if (p.Bed == null) _scratch.Add(p);
            _scratch.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var p in _scratch)
            {
                var bed = NearestFreeBed(p);
                if (bed == null) continue;
                p.Bed = bed;
                p.Waited = 0f;
                if (p.Figure != null) p.Figure.MoveTo(Door(bed));
            }

            // treatment, the waiting, the figures
            for (var i = _patients.Count - 1; i >= 0; i--)
            {
                var p = _patients[i];
                EnsureFigure(p);
                if (p.Bed != null)
                {
                    if (Treating(p)) p.Treated += dt;
                    if (p.Treated >= TreatSeconds)
                    {
                        Heal(p, citizens);
                        continue;
                    }
                    WalkIn(p);
                }
                else
                {
                    p.Waited += dt;
                    if (p.Waited >= WaitSeconds)
                    {
                        Die(p, citizens);
                        continue;
                    }
                    TellShort(p);
                }
                UpdateMark(p);
            }
        }

        /// <summary>The patients follow the town's sick count: new ones are taken from the villagers, cured or dead ones dropped.</summary>
        private void Reconcile(CitizenManager citizens)
        {
            var sick = citizens.SickPopulation;
            var guard = 0;
            while (_patients.Count < sick && guard++ < 64)
            {
                var p = MakePatient();
                if (p == null) break;
                _patients.Add(p);
            }
            while (_patients.Count > sick)
            {
                var p = _patients[_patients.Count - 1];
                _patients.RemoveAt(_patients.Count - 1);
                Release(p, true);
            }
        }

        private Patient MakePatient()
        {
            var visuals = CitizenVisualsManager.Instance;
            if (_pending.Count > 0)
            {
                // one from the save: a figure of his own house if there is one, back where he was
                var saved = _pending[0];
                _pending.RemoveAt(0);
                saved.Figure = visuals != null ? visuals.TakeForPatient(this, saved.Home) : null;
                if (saved.Home == null && saved.Figure != null)
                {
                    saved.Home = saved.Figure.Home;
                    saved.Island = saved.Home != null ? HousePeople.IslandOf(saved.Home) : HousePeople.IslandOf(saved.Figure.transform.position);
                }
                if (saved.Figure != null && saved.Bed != null)
                {
                    if (saved.Inside)
                    {
                        saved.Figure.Teleport(Door(saved.Bed));
                        saved.Figure.gameObject.SetActive(false);
                    }
                    else saved.Figure.MoveTo(Door(saved.Bed));
                }
                else saved.Inside = false;
                HousePeople.Instance?.LayOffIslandExcess();
                return saved;
            }
            var island = _expected.Count > 0 ? _expected.Dequeue() : -1;
            var figure = visuals != null ? visuals.TakeForPatient(this, null, island) : null;
            var p = new Patient { Figure = figure, Order = ++_order };
            if (figure != null)
            {
                p.Home = figure.Home;
                p.Island = figure.Home != null ? HousePeople.IslandOf(figure.Home) : HousePeople.IslandOf(figure.transform.position);
            }
            HousePeople.Instance?.LayOffIslandExcess();
            return p;
        }

        /// <summary>Whether this figure is somebody's patient now (the walking villagers skip it).</summary>
        public bool IsPatient(CitizenAgent agent)
        {
            if (agent == null) return false;
            foreach (var p in _patients) if (p.Figure == agent) return true;
            return false;
        }

        /// <summary>A patient made before the villagers were there (a load) gets his figure when one is free.</summary>
        private void EnsureFigure(Patient p)
        {
            if (p.Figure != null) return;
            var visuals = CitizenVisualsManager.Instance;
            var figure = visuals != null ? visuals.TakeForPatient(this) : null;
            if (figure == null) return;
            p.Figure = figure;
            p.Home = figure.Home;
            p.Island = figure.Home != null ? HousePeople.IslandOf(figure.Home) : HousePeople.IslandOf(figure.transform.position);
            if (p.Bed != null) figure.MoveTo(Door(p.Bed));
        }

        private void CollectHealers()
        {
            _healers.Clear();
            foreach (var b in BuildingInstance.All)
                if (IsHealer(b)) _healers.Add(b);
        }

        private int TakenBeds(BuildingInstance healer)
        {
            var n = 0;
            foreach (var p in _patients) if (p.Bed == healer) n++;
            return n;
        }

        private BuildingInstance NearestFreeBed(Patient p)
        {
            var from = p.Figure != null ? p.Figure.transform.position : p.Home != null ? p.Home.transform.position : Vector3.zero;
            BuildingInstance best = null;
            var bestDistance = float.MaxValue;
            foreach (var healer in _healers)
            {
                if (!Takes(healer) || !HousePeople.Same(HousePeople.IslandOf(healer), p.Island)) continue;
                if (TakenBeds(healer) >= BedsOf(healer)) continue;
                var d = (healer.transform.position - from).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = healer; }
            }
            return best;
        }

        private static Vector3 Door(BuildingInstance building)
        {
            var visuals = CitizenVisualsManager.Instance;
            return visuals != null ? visuals.DoorOf(building) : building.transform.position;
        }

        /// <summary>At the door: in he goes (the figure hidden until he is cured).</summary>
        private void WalkIn(Patient p)
        {
            if (p.Inside || p.Figure == null) return;
            var door = Door(p.Bed);
            var at = p.Figure.transform.position;
            at.y = door.y;
            // arrived, or his walk ended short (stuck, another island): he is taken in all the same
            if ((at - door).sqrMagnitude > DoorReach * DoorReach && !p.Figure.IsIdle) return;
            p.Inside = true;
            p.Figure.gameObject.SetActive(false);
        }

        private void LetOut(Patient p)
        {
            p.Inside = false;
            if (p.Figure == null) return;
            p.Figure.gameObject.SetActive(true);
        }

        private void Heal(Patient p, CitizenManager citizens)
        {
            _patients.Remove(p);
            var at = p.Bed != null ? Door(p.Bed) : p.Figure != null ? p.Figure.transform.position : Vector3.zero;
            if (p.Mark != null) Destroy(p.Mark);
            if (p.Figure != null)
            {
                p.Figure.gameObject.SetActive(true);
                CitizenVisualsManager.Instance?.ReturnFromPatient(p.Figure, at);
            }
            citizens.HealSick(1);
        }

        private void Die(Patient p, CitizenManager citizens)
        {
            _patients.Remove(p);
            if (p.Mark != null) Destroy(p.Mark);
            var at = p.Figure != null ? p.Figure.transform.position : p.Home != null ? p.Home.transform.position : Vector3.zero;
            // the very one goes: his house loses him, the town counts one fewer, and he is no longer ill
            if (p.Figure != null && CitizenVisualsManager.Instance != null) CitizenVisualsManager.Instance.KillAgent(p.Figure);
            else
            {
                HousePeople.Instance?.Lose(p.Home);
                citizens.KillCitizens(1);
            }
            citizens.HealSick(1);
            FoodConsumptionManager.Instance?.RecordDeaths(1);
            Plague.Instance?.RecordIllnessDeath(p.Island);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_plague_deaths", 1), EventSeverity.Danger, EventGlyph.Skull, "Res_Population", at));
        }

        /// <summary>A patient with no bed: the notice says why, once in a while per island.</summary>
        private void TellShort(Patient p)
        {
            if (_toldFull.TryGetValue(p.Island, out var last) && Time.time - last < NoticeEverySeconds) return;
            _toldFull[p.Island] = Time.time;
            BuildingInstance unstaffed = null;
            var any = false;
            foreach (var healer in _healers)
            {
                if (!HousePeople.Same(HousePeople.IslandOf(healer), p.Island) || healer.CurrentHealth <= 0) continue;
                any = true;
                if (HealersIn(healer) <= 0 && healer.IsOperational) unstaffed = healer;
                else if (HealersIn(healer) > 0) { unstaffed = null; break; }
            }
            var log = EventLogManager.Instance;
            if (log == null) return;
            if (any && unstaffed != null)
                log.Post(GameEvent.About(unstaffed, Localization.Get("#log_infirmary_no_worker"), EventSeverity.Warning, EventGlyph.Sick));
            else
            {
                var at = p.Figure != null ? p.Figure.transform.position : p.Home != null ? p.Home.transform.position : Vector3.zero;
                log.Post(GameEvent.At(Localization.Get("#log_infirmary_full"), EventSeverity.Warning, EventGlyph.Sick, HealerId, at,
                    p.Figure != null ? p.Figure.transform : null));
            }
        }

        // ---------------------------------------------------------------- the mark

        /// <summary>The green skull over his head; a red ring round it draining while he has no bed.</summary>
        private void UpdateMark(Patient p)
        {
            if (markMaterial == null || p.Figure == null || p.Inside)
            {
                if (p.Mark != null && p.Mark.activeSelf) p.Mark.SetActive(false);
                return;
            }
            if (p.Mark == null)
            {
                p.Mark = new GameObject("SickMark", typeof(MeshFilter), typeof(MeshRenderer));
                p.Mark.transform.SetParent(p.Figure.transform, false);
                p.Mark.GetComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = p.Mark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = markMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                p.Block = new MaterialPropertyBlock();
                var top = p.Figure.transform.position.y + 1f;
                foreach (var r in p.Figure.GetComponentsInChildren<Renderer>())
                    if (r.gameObject != p.Mark) top = Mathf.Max(top, r.bounds.max.y);
                p.Mark.transform.position = new Vector3(p.Figure.transform.position.x, top + 0.45f, p.Figure.transform.position.z);
            }
            if (!p.Mark.activeSelf) p.Mark.SetActive(true);
            p.Block.SetFloat("_Size", MarkSize);
            p.Block.SetFloat("_Ring", p.Bed == null ? 1f - Mathf.Clamp01(p.Waited / WaitSeconds) : -1f);
            p.Mark.GetComponent<MeshRenderer>().SetPropertyBlock(p.Block);
        }

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = 0.5f;
            _quad = new Mesh
            {
                name = "SickMarkQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return _quad;
        }

        // ---------------------------------------------------------------- letting go

        private void Release(Patient p, bool cured)
        {
            if (p.Mark != null) Destroy(p.Mark);
            if (p.Figure == null) return;
            p.Figure.gameObject.SetActive(true);
            CitizenVisualsManager.Instance?.ReturnFromPatient(p.Figure, p.Inside && p.Bed != null ? Door(p.Bed) : p.Figure.transform.position);
        }

        private void ClearAll()
        {
            foreach (var p in _patients) Release(p, false);
            _patients.Clear();
        }
    }
}
