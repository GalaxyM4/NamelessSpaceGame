// MissionDefinition.cs — una misión de recolección: qué rocas pedir, cuántas y los tiempos de cada calificación.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    /// <summary>Un objetivo de la misión: conseguir <see cref="amount"/> muestras de un tipo de roca.</summary>
    [Serializable]
    public class MissionObjective
    {
        public RockTypeData rockType;
        [Min(1)] public int amount = 1;
    }

    /// <summary>Umbrales de calificación en segundos: A ≤ a, B ≤ b, C ≤ c; más lento = D.</summary>
    [Serializable]
    public struct GradeThresholds
    {
        [Tooltip("Tiempo máximo (s) para A.")] public float a;
        [Tooltip("Tiempo máximo (s) para B.")] public float b;
        [Tooltip("Tiempo máximo (s) para C.")] public float c;

        public GradeThresholds(float a, float b, float c) { this.a = a; this.b = b; this.c = c; }

        /// <summary>True si los umbrales están en orden (0 &lt; a ≤ b ≤ c).</summary>
        public bool IsOrdered => a > 0f && a <= b && b <= c;
    }

    [CreateAssetMenu(menuName = "Game/Missions/Mission Definition", fileName = "Mission_")]
    public class MissionDefinition : ScriptableObject
    {
        /// <summary>Tubos de muestra que lleva el rover (como Perseverance en cada depósito).</summary>
        public const int RoverTubeCapacity = 10;

        [Tooltip("Identificador único (se usa para guardar los récords).")]
        public string id;
        public string title;
        [TextArea(3, 8)] public string description;

        public List<MissionObjective> objectives = new List<MissionObjective>();

        public GradeThresholds gradeThresholds = new GradeThresholds(180f, 300f, 480f);

        [Tooltip("Límite de tiempo en segundos. 0 = sin límite.")]
        [Min(0f)] public float timeLimit;

        /// <summary>Total de rocas que pide la misión (suma de todos los objetivos).</summary>
        public int TotalRequired
        {
            get
            {
                int n = 0;
                foreach (var o in objectives) if (o != null) n += Mathf.Max(0, o.amount);
                return n;
            }
        }

        /// <summary>True si pide más rocas de las que caben en los tubos del rover.</summary>
        public bool ExceedsTubeCapacity => TotalRequired > RoverTubeCapacity;


        void OnValidate()
        {
            if (ExceedsTubeCapacity)
                Debug.LogWarning($"[Misiones] '{title}' pide {TotalRequired} rocas y el rover solo lleva {RoverTubeCapacity} tubos.", this);
            if (!gradeThresholds.IsOrdered)
                Debug.LogWarning($"[Misiones] '{title}': los umbrales deben cumplir 0 < A ≤ B ≤ C.", this);
        }
    }
}
