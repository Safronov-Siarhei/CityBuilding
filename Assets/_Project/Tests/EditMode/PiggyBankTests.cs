using CityBuilder.Core;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>The piggy bank's rules (card Design/features/piggy-bank.md): capacity, the break, the gain, the tasks.</summary>
    public class PiggyBankTests
    {
        private static readonly string[] Keys = { "piggy.level", "piggy.stored", "piggy.tasks", "piggy.task_lost", "store.pending.piggy_1", "store.pending.piggy_2" };
        private readonly string[] _saved = new string[Keys.Length];
        private readonly bool[] _had = new bool[Keys.Length];
        private int _wallet;

        [SetUp]
        public void Remember()
        {
            // the same PlayerPrefs the developer plays with: kept and put back
            for (var i = 0; i < Keys.Length; i++)
            {
                _had[i] = PlayerPrefs.HasKey(Keys[i]);
                _saved[i] = PlayerPrefs.GetString(Keys[i], PlayerPrefs.GetInt(Keys[i], 0).ToString());
            }
            _wallet = Wallet.UpgradeCrystals;
            PiggyBank.CheatSet(0, 0);
            PiggyTasks.ResetForTests();
        }

        [TearDown]
        public void PutBack()
        {
            PlayerPrefs.SetInt("piggy.level", 0);
            for (var i = 0; i < Keys.Length; i++)
            {
                if (!_had[i]) { PlayerPrefs.DeleteKey(Keys[i]); continue; }
                if (Keys[i] == "piggy.tasks") PlayerPrefs.SetString(Keys[i], _saved[i]);
                else PlayerPrefs.SetInt(Keys[i], int.TryParse(_saved[i], out var n) ? n : 0);
            }
            Wallet.UpgradeCrystals = _wallet;
            PiggyTasks.ResetForTests();
            PlayerPrefs.Save();
        }

        [Test]
        public void ItTakesNoMoreThanItHolds()
        {
            var cap = PiggyBank.Capacity;
            Assert.AreEqual(cap - 10, PiggyBank.Add(cap - 10));
            Assert.AreEqual(10, PiggyBank.Add(50), "only what fits goes in");
            Assert.AreEqual(0, PiggyBank.Add(5), "a full piggy takes nothing");
            Assert.IsTrue(PiggyBank.Full);
        }

        [Test]
        public void BreakingIt_PaysEveryCrystal_AndBringsTheNextLevel()
        {
            PiggyBank.Add(PiggyBank.BreakMin - 1);
            Assert.IsFalse(Store.Buy(PiggyBank.ProductId), "below piggy_break_min it cannot be broken");
            PiggyBank.Add(PiggyBank.Capacity);
            var stored = PiggyBank.Stored;
            var before = Wallet.UpgradeCrystals;
            var id = PiggyBank.ProductId;
            Assert.IsTrue(Store.Buy(id));
            Assert.AreEqual(before + stored, Wallet.UpgradeCrystals);
            Assert.AreEqual(1, PiggyBank.Level);
            Assert.AreEqual(0, PiggyBank.Stored);
            Assert.AreNotEqual(id, PiggyBank.ProductId, "the next level is another product with its own price");
            Assert.Greater(Store.UsdOf(PiggyBank.ProductId), Store.UsdOf(id));
        }

        [Test]
        public void TheGain_IsAboutHalfWhenFull_AndNoneBelowTwoThirds()
        {
            for (var level = 0; level < PiggyBank.Levels; level++)
            {
                var cap = BalanceConfig.Sandbox.PiggyCap(level);
                var full = PiggyBank.GainPercent(cap, level);
                Assert.That(full, Is.InRange(40, 60), $"level {level + 1}: a full piggy is ~+50 % over its pack");
                Assert.LessOrEqual(PiggyBank.GainPercent(cap * 6 / 10, level), 0, $"level {level + 1}: no gain below break-even");
            }
        }

        [Test]
        public void TheTasks_AreThreeDifferentOnes_AndADoneOneFillsThePiggy()
        {
            Assert.AreEqual(BalanceConfig.Sandbox.PiggyTasksPerSlot, PiggyTasks.Count);
            Assert.AreEqual("easy", PiggyTasks.Task(0).difficulty);
            Assert.AreEqual("medium", PiggyTasks.Task(1).difficulty);
            Assert.AreNotEqual(PiggyTasks.Task(0).id, PiggyTasks.Task(2).id);
            var task = PiggyTasks.Task(0);
            PiggyTasks.Report(task.kind, task.target - 1);
            Assert.IsFalse(PiggyTasks.Done(0));
            Assert.AreEqual(0, PiggyBank.Stored);
            PiggyTasks.Report(task.kind, 5);
            Assert.IsTrue(PiggyTasks.Done(0));
            Assert.AreEqual(task.target, PiggyTasks.Progress(0), "progress stops at the target");
            Assert.AreEqual(task.reward, PiggyBank.Stored, "its crystals went into the piggy by themselves");
        }
    }
}
