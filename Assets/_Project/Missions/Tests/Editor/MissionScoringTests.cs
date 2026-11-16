// MissionScoringTests.cs — EditMode tests for MissionScoring (grade, bonus, multipliers and score).
using NUnit.Framework;

namespace Game.Missions.Tests
{
    public class MissionScoringTests
    {
        const double TA = 180, TB = 300, TC = 480;

        [TestCase(0.0, Grade.A)]
        [TestCase(180.0, Grade.A)]     // exact A limit
        [TestCase(180.01, Grade.B)]
        [TestCase(300.0, Grade.B)]     // exact B limit
        [TestCase(300.01, Grade.C)]
        [TestCase(480.0, Grade.C)]     // exact C limit
        [TestCase(480.01, Grade.D)]
        [TestCase(9999.0, Grade.D)]
        public void Grade_ExactLimits(double t, Grade expected)
        {
            Assert.AreEqual(expected, MissionScoring.GradeFor(t, TA, TB, TC, completed: true));
        }

        [Test]
        public void Grade_IncompleteMission_IsD_EvenIfFast()
        {
            Assert.AreEqual(Grade.D, MissionScoring.GradeFor(10, TA, TB, TC, completed: false));
        }

        [TestCase(480.0, 0)]
        [TestCase(600.0, 0)]           // slower than C: 0, never negative
        [TestCase(100000.0, 0)]
        [TestCase(470.0, 100)]
        [TestCase(0.0, 4800)]
        [TestCase(467.66, 123)]        // (480 − 467.66) × 10 = 123.4 → 123
        [TestCase(467.65, 124)]        // 123.5 → 124 (rounds up at .5)
        public void TimeBonus(double t, int expected)
        {
            Assert.AreEqual(expected, MissionScoring.TimeBonus(t, TC));
        }

        [Test]
        public void Bonus_NeverNegative()
        {
            for (double t = 0; t < 2000; t += 7.3)
                Assert.GreaterOrEqual(MissionScoring.TimeBonus(t, TC), 0);
        }

        [TestCase(Grade.A, 1.5)]
        [TestCase(Grade.B, 1.25)]
        [TestCase(Grade.C, 1.0)]
        [TestCase(Grade.D, 1.0)]
        public void Multipliers(Grade g, double expected)
        {
            Assert.AreEqual(expected, MissionScoring.Multiplier(g), 1e-9);
        }

        [Test]
        public void Score_A()
        {
            // 5 rocks in 100 s: 500 + (480 − 100) × 10 = 4300; × 1.5 = 6450
            var s = MissionScoring.Score(5, 100, TA, TB, TC, completed: true);
            Assert.AreEqual(500, s.rockPoints);
            Assert.AreEqual(3800, s.timeBonus);
            Assert.AreEqual(1.5, s.multiplier, 1e-9);
            Assert.AreEqual(6450, s.total);
        }

        [Test]
        public void Score_B()
        {
            // 250 s: 500 + 2300 = 2800; × 1.25 = 3500
            Assert.AreEqual(3500, MissionScoring.Score(5, 250, TA, TB, TC, true).total);
        }

        [Test]
        public void Score_C()
        {
            // 400 s: 500 + 800 = 1300; × 1 = 1300
            Assert.AreEqual(1300, MissionScoring.Score(5, 400, TA, TB, TC, true).total);
        }

        [Test]
        public void Score_D_Completed_NoBonus()
        {
            var s = MissionScoring.Score(5, 600, TA, TB, TC, true);
            Assert.AreEqual(0, s.timeBonus);
            Assert.AreEqual(1.0, s.multiplier, 1e-9);
            Assert.AreEqual(500, s.total);
        }

        [Test]
        public void Score_Incomplete_NoBonusNoMultiplier()
        {
            // ended by time with 3 rocks: only 300, even with a low time
            var s = MissionScoring.Score(3, 50, TA, TB, TC, completed: false);
            Assert.AreEqual(300, s.rockPoints);
            Assert.AreEqual(0, s.timeBonus);
            Assert.AreEqual(1.0, s.multiplier, 1e-9);
            Assert.AreEqual(300, s.total);
        }

        [Test]
        public void Score_NegativeRocks_TreatedAsZero()
        {
            Assert.AreEqual(0, MissionScoring.Score(-2, 999, TA, TB, TC, false).total);
        }
    }
}
