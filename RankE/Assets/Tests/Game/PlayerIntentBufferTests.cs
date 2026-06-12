using NUnit.Framework;
using RankE.Game;
using RankE.Sim;

namespace RankE.Game.Tests
{
    public class PlayerIntentBufferTests
    {
        static SimEvent Used(int actor, string ability) => new SimEvent
        { Type = SimEventType.AbilityUsed, Actor = actor, AbilityId = ability };

        [Test]
        public void Press_IsResubmittedUntilAcked()
        {
            var buf = new PlayerIntentBuffer();
            buf.Press("slash");

            Assert.AreEqual("slash", buf.PeekForTick());
            buf.OnTick();
            Assert.AreEqual("slash", buf.PeekForTick(), "unacked press persists across ticks");

            buf.NotifyEvent(Used(0, "slash"), 0);
            Assert.IsNull(buf.PeekForTick(), "ack clears the buffer");
        }

        [Test]
        public void LatestPressWins()
        {
            var buf = new PlayerIntentBuffer();
            buf.Press("slash");
            buf.Press("parry");
            Assert.AreEqual("parry", buf.PeekForTick());
        }

        [Test]
        public void CastStarted_AlsoAcks()
        {
            var buf = new PlayerIntentBuffer();
            buf.Press("fireball");
            buf.NotifyEvent(new SimEvent
            { Type = SimEventType.CastStarted, Actor = 0, AbilityId = "fireball" }, 0);
            Assert.IsNull(buf.PeekForTick());
        }

        [Test]
        public void ForeignEvents_DoNotAck()
        {
            var buf = new PlayerIntentBuffer();
            buf.Press("slash");

            buf.NotifyEvent(Used(1, "slash"), 0); // enemy used the same ability
            Assert.AreEqual("slash", buf.PeekForTick());

            buf.NotifyEvent(Used(0, "bash"), 0); // player used something else (auto-attack etc.)
            Assert.AreEqual("slash", buf.PeekForTick());
        }

        [Test]
        public void UnackedPress_ExpiresAfterExpiryTicks()
        {
            var buf = new PlayerIntentBuffer { ExpiryTicks = 6 };
            buf.Press("slash");

            for (int i = 0; i < 5; i++) buf.OnTick();
            Assert.AreEqual("slash", buf.PeekForTick());

            buf.OnTick();
            Assert.IsNull(buf.PeekForTick(), "press expires on the 6th tick");
        }

        [Test]
        public void Repress_ResetsExpiry()
        {
            var buf = new PlayerIntentBuffer { ExpiryTicks = 6 };
            buf.Press("slash");
            for (int i = 0; i < 5; i++) buf.OnTick();
            buf.Press("slash"); // mashing the button keeps it alive
            for (int i = 0; i < 5; i++) buf.OnTick();
            Assert.AreEqual("slash", buf.PeekForTick());
        }
    }
}
