// MissionScoring.cs — grade and score. PURE static class: System only, no UnityEngine (testable).
//   Grade: A if t ≤ tA; B if t ≤ tB; C if t ≤ tC; D if slower or if the mission was not completed.
//   Score: 100 per valid rock + time bonus max(0, round((tC − t) × 10)), all × multiplier
//          (A ×1.5, B ×1.25, C ×1, D ×1). If the mission was not completed: no bonus and ×1.
using System;

namespace Game.Missions
{
    public enum Grade { A, B, C, D }

    /// <summary>Score breakdown for the results screen.</summary>
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

        /// <summary>Grade by time. A mission that was not completed (e.g. time's up) is always D.</summary>
        public static Grade GradeFor(double seconds, double tA, double tB, double tC, bool completed)
        {
            if (!completed) return Grade.D;
            if (seconds <= tA) return Grade.A;
            if (seconds <= tB) return Grade.B;
            if (seconds <= tC) return Grade.C;
            return Grade.D;
        }

        /// <summary>Time bonus = max(0, round((tC − t) × 10)). Never negative.</summary>
        public static int TimeBonus(double seconds, double tC) =>
            Math.Max(0, (int)Math.Round((tC - seconds) * BonusPerSecond, MidpointRounding.AwayFromZero));

        public static double Multiplier(Grade grade)
        {
            switch (grade)
            {
                case Grade.A: return 1.5;
                case Grade.B: return 1.25;
                default: return 1.0;   // C and D
            }
        }

        /// <summary>Full score with its breakdown.</summary>
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
