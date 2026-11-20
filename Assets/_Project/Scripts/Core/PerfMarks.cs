using System.Diagnostics;

namespace CityBuilder.Core
{
    /// <summary>
    /// Time marks inside the army's per-frame work (2026-11-20, «разбери армию»): PerformanceProfileTests switches
    /// them on and reads each one's total. Off (one bool test) everywhere else. ProfilerRecorder on custom markers
    /// read 0 for everything inside Update in a batchmode PlayMode test, so these keep their own Stopwatch ticks.
    /// </summary>
    public static class PerfMarks
    {
        public static bool On;

        public sealed class Mark
        {
            public readonly string Name;
            public long Ticks;
            public long Calls;
            private int _depth;
            public Mark(string name) { Name = name; }
            public Scope Auto() => new Scope(On ? this : null);
            internal void Enter() { _depth++; }
            internal void Exit(long ticks) { if (--_depth == 0) Ticks += ticks; Calls++; }
            public double Ms => Ticks * 1000.0 / Stopwatch.Frequency;
            public void Reset() { Ticks = 0; Calls = 0; }
        }

        public readonly struct Scope : System.IDisposable
        {
            private readonly Mark _mark;
            private readonly long _start;
            public Scope(Mark mark)
            {
                _mark = mark;
                _start = mark != null ? Stopwatch.GetTimestamp() : 0;
                mark?.Enter();
            }
            public void Dispose() { _mark?.Exit(Stopwatch.GetTimestamp() - _start); }
        }

        public static readonly Mark SoldierWall = new Mark("Soldier.Wall");
        public static readonly Mark SoldierTarget = new Mark("Soldier.Target");
        public static readonly Mark SoldierRoute = new Mark("Soldier.Route");
        public static readonly Mark SoldierStep = new Mark("Soldier.Step");
        public static readonly Mark SoldierMove = new Mark("Soldier.Move (capsule)");
        public static readonly Mark SoldierAttack = new Mark("Soldier.Attack");
        public static readonly Mark SoldierSlot = new Mark("Soldier.Slot");
        public static readonly Mark OrcScan = new Mark("Orc.ScanSoldiers");
        public static readonly Mark OrcFight = new Mark("Orc.FightSoldier");
        public static readonly Mark OrcHuntCitizen = new Mark("Orc.HuntCitizen");
        public static readonly Mark OrcHuntPrisoner = new Mark("Orc.HuntPrisoner");
        public static readonly Mark OrcGuard = new Mark("Orc.Guard");
        public static readonly Mark OrcGuardWalk = new Mark("Orc.GuardWalk");
        public static readonly Mark OrcTarget = new Mark("Orc.Target");
        public static readonly Mark OrcAdvance = new Mark("Orc.Advance");
        public static readonly Mark OrcLate = new Mark("Orc.LateUpdate");
        public static readonly Mark SoldierUpdate = new Mark("Soldier.Update (all)");
        public static readonly Mark OrcUpdate = new Mark("Orc.Update (all)");
        public static readonly Mark CampShoot = new Mark("Camp.Update");

        public static readonly Mark[] All =
        {
            SoldierWall, SoldierTarget, SoldierRoute, SoldierStep, SoldierMove, SoldierAttack, SoldierSlot,
            OrcScan, OrcFight, OrcHuntCitizen, OrcHuntPrisoner, OrcGuard, OrcGuardWalk, OrcTarget, OrcAdvance, OrcLate, CampShoot,
            SoldierUpdate, OrcUpdate,
        };
    }
}
