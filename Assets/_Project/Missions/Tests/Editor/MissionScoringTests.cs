// MissionScoringTests.cs — tests EditMode de MissionScoring (calificación, bonus, multiplicadores y puntaje).
using NUnit.Framework;

namespace Game.Missions.Tests
{
    public class MissionScoringTests
    {
        const double TA = 180, TB = 300, TC = 480;

        [TestCase(0.0, Grade.A)]
        [TestCase(180.0, Grade.A)]     // límite exacto de A
        [TestCase(180.01, Grade.B)]
        [TestCase(300.0, Grade.B)]     // límite exacto de B
        [TestCase(300.01, Grade.C)]
        [TestCase(480.0, Grade.C)]     // límite exacto de C
        [TestCase(480.01, Grade.D)]
        [TestCase(9999.0, Grade.D)]
        public void Grade_LimitesExactos(double t, Grade expected)
        {
            Assert.AreEqual(expected, MissionScoring.GradeFor(t, TA, TB, TC, completed: true));
        }

        [Test]
        public void Grade_MisionIncompleta_EsD_AunqueSeaRapida()
        {
            Assert.AreEqual(Grade.D, MissionScoring.GradeFor(10, TA, TB, TC, completed: false));
        }

        [TestCase(480.0, 0)]
        [TestCase(600.0, 0)]           // más lento que C: 0, nunca negativo
        [TestCase(100000.0, 0)]
        [TestCase(470.0, 100)]
        [TestCase(0.0, 4800)]
        [TestCase(467.66, 123)]        // (480 − 467.66) × 10 = 123.4 → 123
        [TestCase(467.65, 124)]        // 123.5 → 124 (redondeo hacia arriba en .5)
        public void TimeBonus(double t, int expected)
        {
            Assert.AreEqual(expected, MissionScoring.TimeBonus(t, TC));
        }

        [Test]
        public void Bonus_NuncaNegativo()
        {
            for (double t = 0; t < 2000; t += 7.3)
                Assert.GreaterOrEqual(MissionScoring.TimeBonus(t, TC), 0);
        }

        [TestCase(Grade.A, 1.5)]
        [TestCase(Grade.B, 1.25)]
        [TestCase(Grade.C, 1.0)]
        [TestCase(Grade.D, 1.0)]
        public void Multiplicadores(Grade g, double expected)
        {
            Assert.AreEqual(expected, MissionScoring.Multiplier(g), 1e-9);
        }

        [Test]
        public void Puntaje_A()
        {
            // 5 rocas en 100 s: 500 + (480 − 100) × 10 = 4300; × 1.5 = 6450
            var s = MissionScoring.Score(5, 100, TA, TB, TC, completed: true);
            Assert.AreEqual(500, s.rockPoints);
            Assert.AreEqual(3800, s.timeBonus);
            Assert.AreEqual(1.5, s.multiplier, 1e-9);
            Assert.AreEqual(6450, s.total);
        }

        [Test]
        public void Puntaje_B()
        {
            // 250 s: 500 + 2300 = 2800; × 1.25 = 3500
            Assert.AreEqual(3500, MissionScoring.Score(5, 250, TA, TB, TC, true).total);
        }

        [Test]
        public void Puntaje_C()
        {
            // 400 s: 500 + 800 = 1300; × 1 = 1300
            Assert.AreEqual(1300, MissionScoring.Score(5, 400, TA, TB, TC, true).total);
        }

        [Test]
        public void Puntaje_D_Completada_SinBonus()
        {
            var s = MissionScoring.Score(5, 600, TA, TB, TC, true);
            Assert.AreEqual(0, s.timeBonus);
            Assert.AreEqual(1.0, s.multiplier, 1e-9);
            Assert.AreEqual(500, s.total);
        }

        [Test]
        public void Puntaje_Incompleta_SinBonusNiMultiplicador()
        {
            // terminó por tiempo con 3 rocas: solo 300, aunque el tiempo sea bajo
            var s = MissionScoring.Score(3, 50, TA, TB, TC, completed: false);
            Assert.AreEqual(300, s.rockPoints);
            Assert.AreEqual(0, s.timeBonus);
            Assert.AreEqual(1.0, s.multiplier, 1e-9);
            Assert.AreEqual(300, s.total);
        }

        [Test]
        public void Puntaje_RocasNegativas_SeTratanComoCero()
        {
            Assert.AreEqual(0, MissionScoring.Score(-2, 999, TA, TB, TC, false).total);
        }
    }
}
