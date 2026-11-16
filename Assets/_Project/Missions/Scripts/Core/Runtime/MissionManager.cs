// MissionManager.cs — runs a mission: Briefing → InProgress → Completed (or Abandoned).
// Listens to SampleEvents.OnSampleCollected and notifies the UI through C# events. It knows neither the UI nor the rover.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    public enum MissionState { Inactive, Briefing, InProgress, Completed, Abandoned }

    public class MissionManager : MonoBehaviour
    {
        public MissionDatabase database;
        [Tooltip("Mission shown at start (optional). Empty = random or the first one in the database.")]
        public MissionDefinition startMission;
        [Tooltip("Show the briefing automatically when the scene starts (off when a tutorial shows it later).")]
        public bool briefingOnStart = true;
        [Tooltip("Random order: at start and with \"Next mission\" a random mission is picked, without repeats until all were played.")]
        public bool randomMissions = true;

        public MissionState State { get; private set; } = MissionState.Inactive;
        public MissionDefinition Current { get; private set; }
        public MissionProgress Progress { get; private set; }
        /// <summary>Mission seconds (scaled time: pausing with timeScale = 0 stops it).</summary>
        public float Elapsed { get; private set; }

        // ---------- events for the UI ----------
        public event Action<MissionDefinition> OnBriefing;
        public event Action<MissionDefinition> OnMissionStarted;
        public event Action<MissionProgress> OnProgressChanged;
        public event Action<string> OnMessage;
        public event Action<MissionResult> OnMissionFinished;
        public event Action<MissionDefinition> OnMissionAbandoned;

        readonly List<RockSampleTarget> _sampledThisRun = new List<RockSampleTarget>();

        void OnEnable() => SampleEvents.OnSampleCollected += HandleSample;
        void OnDisable() => SampleEvents.OnSampleCollected -= HandleSample;

        void Start()
        {
            if (briefingOnStart) ShowStartBriefing();
        }

        /// <summary>Shows the briefing of the first mission (fixed, random or the database's first).</summary>
        public void ShowStartBriefing()
        {
            var m = startMission != null ? startMission
                  : randomMissions ? DrawRandom()
                  : (database != null && database.missions.Count > 0 ? database.missions[0] : null);
            if (m != null) ShowBriefing(m);
        }

        void Update()
        {
            if (State != MissionState.InProgress) return;
            Elapsed += Time.deltaTime;
            if (Current.timeLimit > 0f && Elapsed >= Current.timeLimit)
            {
                Elapsed = Current.timeLimit;
                Finish(EndReason.TimeUp);
            }
        }

        // ---------- flow ----------
        public void ShowBriefing(MissionDefinition mission)
        {
            if (mission == null) return;
            Current = mission;
            if (_peeked == mission) _peeked = null;
            Progress = new MissionProgress(mission);
            Elapsed = 0f;
            State = MissionState.Briefing;
            OnBriefing?.Invoke(mission);
        }

        /// <summary>"Start" button: starts the timer.</summary>
        public void StartMission()
        {
            if (Current == null || State == MissionState.InProgress) return;
            ResetSampledRocks();
            Progress = new MissionProgress(Current);
            Elapsed = 0f;
            State = MissionState.InProgress;
            OnMissionStarted?.Invoke(Current);
            OnProgressChanged?.Invoke(Progress);
        }

        /// <summary>"Retry" button: the same mission from scratch.</summary>
        public void Retry()
        {
            if (Current == null) return;
            ResetSampledRocks();
            ShowBriefing(Current);
        }

        /// <summary>"Next mission" button. Returns false if there is no next mission.</summary>
        public bool NextMission()
        {
            var next = PeekNext();
            if (next == null) return false;
            ResetSampledRocks();
            ShowBriefing(next);
            return true;
        }

        public MissionDefinition PeekNext()
        {
            if (database == null) return null;
            if (!randomMissions) return database.GetNext(Current);
            if (_peeked == null || _peeked == Current) _peeked = DrawRandom();
            return _peeked;
        }

        // ---------- random order ("bag": no repeats until every mission came out) ----------
        readonly List<MissionDefinition> _bag = new List<MissionDefinition>();
        MissionDefinition _peeked;

        MissionDefinition DrawRandom()
        {
            if (database == null || database.missions.Count == 0) return null;
            if (_bag.Count == 0)
                foreach (var m in database.missions)
                    if (m != null && m != Current) _bag.Add(m);
            if (_bag.Count == 0) return Current;   // there is only one mission
            int i = UnityEngine.Random.Range(0, _bag.Count);
            var pick = _bag[i];
            _bag.RemoveAt(i);
            return pick;
        }

        /// <summary>"Exit" button, or abandoning during the mission.</summary>
        public void Abandon()
        {
            if (State == MissionState.Inactive) return;
            var m = Current;
            State = MissionState.Abandoned;
            OnMissionAbandoned?.Invoke(m);
        }

        /// <summary>
        /// The drill reports its free tubes after every sample. If they are not enough for the rocks still missing,
        /// the mission can no longer be completed: it ends now ("OUT OF SAMPLE TUBES", grade D).
        /// </summary>
        public void ReportTubesLeft(int tubesLeft)
        {
            if (State != MissionState.InProgress || Progress == null) return;
            if (!Progress.CanStillComplete(tubesLeft)) Finish(EndReason.OutOfTubes);
        }

        /// <summary>The rover battery reached 0%: the mission ends with what was collected ("BATTERY DEPLETED", grade D).</summary>
        public void ReportBatteryDepleted()
        {
            if (State == MissionState.InProgress) Finish(EndReason.BatteryDepleted);
        }

        /// <summary>Debug (F10): end now with the current time.</summary>
        public void DebugFinishNow()
        {
            if (State == MissionState.InProgress) Finish(EndReason.Forced);
        }

        // ---------- samples ----------
        void HandleSample(RockTypeData type, RockSampleTarget target)
        {
            if (State != MissionState.InProgress) return;
            if (target != null) _sampledThisRun.Add(target);

            var r = Progress.TryAdd(type);
            if (r != SampleCountResult.Counted)
            {
                OnMessage?.Invoke(MissionTexts.RockNotNeeded);   // does not count and is not penalized
                return;
            }
            var o = Progress.Find(type);
            OnMessage?.Invoke(string.Format(MissionTexts.SampleCounted, type.ShortLabel, o.collected, o.required));
            OnProgressChanged?.Invoke(Progress);
            if (Progress.AllComplete) Finish(EndReason.Completed);
        }

        enum EndReason { Completed, TimeUp, OutOfTubes, BatteryDepleted, Forced }

        void Finish(EndReason reason)
        {
            bool completed = Progress.AllComplete;
            var th = Current.gradeThresholds;
            var result = new MissionResult
            {
                mission = Current,
                finalTime = Elapsed,
                completed = completed,
                timedOut = reason == EndReason.TimeUp && !completed,
                outOfTubes = reason == EndReason.OutOfTubes && !completed,
                batteryDepleted = reason == EndReason.BatteryDepleted && !completed,
                grade = MissionScoring.GradeFor(Elapsed, th.a, th.b, th.c, completed),
                score = MissionScoring.Score(Progress.ValidCollected, Elapsed, th.a, th.b, th.c, completed),
            };
            foreach (var o in Progress.Objectives)
                result.objectives.Add(new ObjectiveResult { type = o.type, collected = o.collected, required = o.required, completed = o.IsComplete });
            result.newRecords = MissionProgressStore.Submit(result);

            State = MissionState.Completed;
            OnMissionFinished?.Invoke(result);
        }

        void ResetSampledRocks()
        {
            foreach (var t in _sampledThisRun) if (t != null) t.ResetSample();
            _sampledThisRun.Clear();
        }
    }
}
