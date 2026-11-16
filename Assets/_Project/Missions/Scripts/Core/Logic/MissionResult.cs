// MissionResult.cs — final result of a mission (shown on the results screen).
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
        public float finalTime;          // seconds
        public bool completed;           // every objective fulfilled
        public bool timedOut;            // ended by the time limit
        public bool outOfTubes;          // ended because there were not enough tubes left to finish
        public bool batteryDepleted;     // ended because the rover battery ran out
        public Grade grade;
        public ScoreBreakdown score;
        public List<ObjectiveResult> objectives = new List<ObjectiveResult>();
        public RecordFlags newRecords;   // which records were beaten

        public bool IsNewRecord => newRecords != RecordFlags.None;
    }
}
