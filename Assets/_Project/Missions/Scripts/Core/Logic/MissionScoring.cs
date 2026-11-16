// MissionScoring.cs — calificación y puntaje. Clase estática PURA: solo System, sin UnityEngine (testeable).
//   Calificación: A si t ≤ tA; B si t ≤ tB; C si t ≤ tC; D si es más lento o si la misión no se completó.
//   Puntaje: 100 por roca válida + bonus de tiempo max(0, round((tC − t) × 10)), todo × multiplicador
//            (A ×1.5, B ×1.25, C ×1, D ×1). Si la misión no se completó: sin bonus y ×1.
using System;

namespace Game.Missions
{
    public enum Grade { A, B, C, D }

    /// <summary>Desglose del puntaje para la pantalla de resultados.</summary>
    public readonly struct ScoreBreakdown
    {
        public readonly int validRocks;
        public readonly int rockPoints;   // validRocks × 100
        public readonly int timeBonus;
        public readonly double multiplier;
        public readonly int total;

        public ScoreBreakdown(int validRocks, int rockPoints, int timeBonus, double multiplier, int total)
        {
            this.validRocks = validRocks; this.rockPoints = rockPoints; this.timeBonus = timeBonus;
            this.multiplier = multiplier; this.total = total;
        }
    }

    public static class MissionScoring
    {
        public const int PointsPerRock = 100;
        public const double BonusPerSecond = 10.0;

        /// <summary>Calificación por tiempo. Una misión no completada (p. ej. tiempo agotado) siempre es D.</summary>
        public static Grade GradeFor(double seconds, double tA, double tB, double tC, bool completed)
        {
            if (!completed) return Grade.D;
            if (seconds <= tA) return Grade.A;
            if (seconds <= tB) return Grade.B;
            if (seconds <= tC) return Grade.C;
            return Grade.D;
        }

        /// <summary>Bonus de tiempo = max(0, round((tC − t) × 10)). Nunca negativo.</summary>
        public static int TimeBonus(double seconds, double tC) =>
            Math.Max(0, (int)Math.Round((tC - seconds) * BonusPerSecond, MidpointRounding.AwayFromZero));

        public static double Multiplier(Grade grade)
        {
            switch (grade)
            {
                case Grade.A: return 1.5;
                case Grade.B: return 1.25;
                default: return 1.0;   // C y D
            }
        }

        /// <summary>Puntaje completo con su desglose.</summary>
        public static ScoreBreakdown Score(int validRocks, double seconds, double tA, double tB, double tC, bool completed)
        {
            validRocks = Math.Max(0, validRocks);
            var grade = GradeFor(seconds, tA, tB, tC, completed);
            int rockPoints = validRocks * PointsPerRock;
            int bonus = completed ? TimeBonus(seconds, tC) : 0;
            double mult = completed ? Multiplier(grade) : 1.0;
            int total = (int)Math.Round((rockPoints + bonus) * mult, MidpointRounding.AwayFromZero);
            return new ScoreBreakdown(validRocks, rockPoints, bonus, mult, total);
        }
    }
}
