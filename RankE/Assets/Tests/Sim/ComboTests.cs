using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class ComboTests
    {
        static Battle ComboDuel() => TestKit.Duel(
            TestKit.Config("A", PocContent.Slash(), PocContent.Bash(),
                PocContent.Fireball(), PocContent.Parry()),
            TestKit.Config("B"));

        [Test]
        public void FullCombo_EmpowersFinisher()
        {
            var b = ComboDuel();

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);    // Opener
            TestKit.UseAt(b, 20, 0, PocContent.BashId);    // Linker
            TestKit.UseAt(b, 40, 0, PocContent.FireballId); // Finisher (cast → lands tick 80)
            TestKit.StepUntil(b, 81);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.ComboCompleted).Count);

            var fireballHits = TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId);
            Assert.AreEqual(60, fireballHits.Single().Amount); // 40 × 1.5

            // Finisher bonus break (+10) landed on top of bash's 20.
            Assert.IsTrue(TestKit.EventsOf(b, SimEventType.BreakDamaged).Any(e => e.Amount == 10));

            // Gem refunded: fireball cost 1, refund 1 → back to start.
            Assert.AreEqual(8, b.Fighters[0].SpellGems);
        }

        [Test]
        public void WindowTimeout_ResetsChain()
        {
            var b = ComboDuel();

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            TestKit.UseAt(b, 100, 0, PocContent.BashId);     // > 80t after opener → reset
            TestKit.UseAt(b, 120, 0, PocContent.FireballId); // chain is cold
            TestKit.StepUntil(b, 161);

            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.ComboCompleted));
            Assert.AreEqual(40, TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId)
                .Single().Amount);
        }

        [Test]
        public void WrongTag_ResetsChain()
        {
            var b = ComboDuel();

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);     // Opener
            TestKit.UseAt(b, 20, 0, PocContent.FireballId); // Finisher ≠ expected Linker
            TestKit.StepUntil(b, 61);

            Assert.IsTrue(TestKit.EventsOf(b, SimEventType.ComboReset).Count > 0);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.ComboCompleted));
            Assert.AreEqual(40, TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId)
                .Single().Amount);
        }

        [Test]
        public void QuickActions_AreComboNeutral()
        {
            var b = ComboDuel();

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            TestKit.UseAt(b, 2, 0, PocContent.ParryId);    // quick: neither advances nor resets
            TestKit.UseAt(b, 20, 0, PocContent.BashId);
            TestKit.UseAt(b, 40, 0, PocContent.FireballId);
            TestKit.StepUntil(b, 81);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.ComboCompleted).Count);
            Assert.AreEqual(60, TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId)
                .Single().Amount);
        }

        [Test]
        public void Opener_RestartsBrokenChain()
        {
            var b = ComboDuel();

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);   // Opener (chain 1)
            TestKit.UseAt(b, 80, 0, PocContent.SlashId);  // wrong tag mid-chain → restarts as Opener
            TestKit.UseAt(b, 100, 0, PocContent.BashId);  // Linker
            TestKit.UseAt(b, 120, 0, PocContent.FireballId);
            TestKit.StepUntil(b, 161);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.ComboCompleted).Count);
            Assert.AreEqual(60, TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId)
                .Single().Amount);
        }
    }
}
