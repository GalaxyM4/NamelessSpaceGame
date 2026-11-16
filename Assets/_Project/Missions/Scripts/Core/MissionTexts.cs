// MissionTexts.cs — every mission text in one place (easy to localize).
namespace Game.Missions
{
    public static class MissionTexts
    {
        // --- Drilling ---
        public const string DrillPrompt = "Hold [{0}] to drill";                       // key (does not reveal the rock)
        public const string Drilling = "Drilling… {0}%";                                // percent
        public const string DrillCancelled = "Drilling cancelled";
        public const string AlreadySampled = "Rock already sampled";
        public const string NoTubesLeft = "No free sample tubes";
        public const string SampleStored = "Sample stored · tubes {0}/{1}";           // used, total
        public const string SampleStatusSampled = "SAMPLED";

        // --- Mission ---
        public const string RockNotNeeded = "This rock is not needed for the mission";
        public const string SampleCounted = "{0} collected · {1}/{2}";                // rock, collected, required
        public const string MissionCompleted = "MISSION COMPLETE";
        public const string MissionTimedOut = "TIME'S UP";
        public const string MissionIncomplete = "MISSION INCOMPLETE";
        public const string MissionOutOfTubes = "OUT OF SAMPLE TUBES";
        public const string MissionBatteryDepleted = "BATTERY DEPLETED";
        public const string BatteryLow = "Low battery: {0}%";

        // --- UI ---
        public const string BriefingHeader = "MISSION BRIEFING";
        public const string Objectives = "OBJECTIVES";
        public const string ObjectiveLine = "{0} ×{1}";                                // short name, amount
        public const string ObjectiveProgress = "{0}  {1}/{2}";                        // short name, collected, required
        public const string BestRecord = "Best: {0} · {1} · {2} pts";                  // time, grade, points
        public const string NoRecord = "No previous record";
        public const string TimeLimit = "Time limit: {0}";
        public const string BatteryInfo = "Battery: {0} ({1} kAh)";                   // module name, capacity
        public const string Start = "START";
        public const string Retry = "RETRY";
        public const string Next = "NEXT MISSION";
        public const string Exit = "EXIT";
        public const string FinalTime = "TIME";
        public const string Thresholds = "A ≤ {0}   ·   B ≤ {1}   ·   C ≤ {2}";
        public const string ScoreRocks = "Rocks  {0} × 100";
        public const string ScoreBonus = "Time bonus";
        public const string ScoreMultiplier = "Multiplier ({0})";
        public const string ScoreTotal = "SCORE";
        public const string NewRecord = "New record!";
        public const string Samples = "SAMPLES";
        public const string Tubes = "TUBES {0}/{1}";

        // --- Tutorial ---
        public const string TutorialHeader = "TUTORIAL";
        public const string TutorialStep = "STEP {0}/{1}";
        public const string TutorialSkip = "[TAB] Skip tutorial";
        public const string TutorialMove = "Drive the rover with W A S D";
        public const string TutorialBrake = "Brake with SPACE";
        public const string TutorialCameraYaw = "Rotate the camera left and right with Q / E";
        public const string TutorialCameraPitch = "Raise and lower the camera with SHIFT / CTRL";
        public const string TutorialAnalyze = "Drive to the marked rock and press F to analyze it";
        public const string TutorialAnalyzeDistance = "Marked rock: {0} m";
        public const string TutorialDrill = "Now hold R to drill a sample from it";
        public const string TutorialDone = "Tutorial complete! Get ready for your mission";
    }
}
