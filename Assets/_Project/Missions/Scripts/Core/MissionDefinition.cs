// MissionDefinition.cs — a collection mission: which rocks to ask for, how many, and the time for each grade.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    /// <summary>A mission objective: collect <see cref="amount"/> samples of one rock type.</summary>
    [Serializable]
    public class MissionObjective
    {
        public RockTypeData rockType;
        [Min(1)] public int amount = 1;
    }

    /// <summary>Grade thresholds in seconds: A ≤ a, B ≤ b, C ≤ c; slower = D.</summary>
    [Serializable]
    public struct GradeThresholds
    {
        [Tooltip("Maximum time (s) for an A.")] public float a;
        [Tooltip("Maximum time (s) for a B.")] public float b;
        [Tooltip("Maximum time (s) for a C.")] public float c;

        public GradeThresholds(float a, float b, float c) { this.a = a; this.b = b; this.c = c; }

        /// <summary>True if the thresholds are in order (0 &lt; a ≤ b ≤ c).</summary>
        public bool IsOrdered => a > 0f && a <= b && b <= c;
    }

    [CreateAssetMenu(menuName = "Game/Missions/Mission Definition", fileName = "Mission_")]
    public class MissionDefinition : ScriptableObject
    {
        /// <summary>Sample tubes carried by the rover.</summary>
        public const int RoverTubeCapacity = 10;

        [Tooltip("Unique identifier (used to save records).")]
        public string id;
        public string title;
        [TextArea(3, 8)] public string description;

        public List<MissionObjective> objectives = new List<MissionObjective>();

        public GradeThresholds gradeThresholds = new GradeThresholds(180f, 300f, 480f);

        [Tooltip("Time limit in seconds. 0 = no limit.")]
        [Min(0f)] public float timeLimit;

        /// <summary>Total rocks the mission asks for (sum of all objectives).</summary>
        public int TotalRequired
        {
            get
            {
                int n = 0;
                foreach (var o in objectives) if (o != null) n += Mathf.Max(0, o.amount);
                return n;
            }
        }

        /// <summary>True if it asks for more rocks than fit in the rover's tubes.</summary>
        public bool ExceedsTubeCapacity => TotalRequired > RoverTubeCapacity;

        void OnValidate()
        {
            if (ExceedsTubeCapacity)
                Debug.LogWarning($"[Missions] '{title}' asks for {TotalRequired} rocks but the rover only carries {RoverTubeCapacity} tubes.", this);
            if (!gradeThresholds.IsOrdered)
                Debug.LogWarning($"[Missions] '{title}': thresholds must satisfy 0 < A ≤ B ≤ C.", this);
        }
    }
}
