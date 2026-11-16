// MissionResult.cs — resultado final de una misión (lo muestra la pantalla de resultados).
using System.Collections.Generic;

namespace Game.Missions
{
    public class ObjectiveResult
    {
        public RockTypeData type;
        public int collected;
        public int required;
        public bool completed;
    }

    public class MissionResult
    {
        public MissionDefinition mission;
        public float finalTime;          // segundos
        public bool completed;           // todos los objetivos cumplidos
        public bool timedOut;            // terminó por límite de tiempo
        public bool outOfTubes;          // terminó porque ya no quedaban tubos para completarla
        public Grade grade;
        public ScoreBreakdown score;
        public List<ObjectiveResult> objectives = new List<ObjectiveResult>();
        public RecordFlags newRecords;   // qué récords se batieron

        public bool IsNewRecord => newRecords != RecordFlags.None;
    }
}
