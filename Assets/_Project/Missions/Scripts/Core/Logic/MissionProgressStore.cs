// MissionProgressStore.cs — per-mission records (best time, grade and score) stored in PlayerPrefs as JSON.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    [Flags]
    public enum RecordFlags { None = 0, Time = 1, Grade = 2, Score = 4 }

    [Serializable]
    public class MissionRecord
    {
        public string missionId;
        public bool hasTime;     // only completed missions count for best time
        public float bestTime;
        public Grade bestGrade = Grade.D;
        public int bestScore;
    }

    public static class MissionProgressStore
    {
        const string Key = "Game.Missions.Records";

        [Serializable] class RecordList { public List<MissionRecord> records = new List<MissionRecord>(); }

        static RecordList Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new RecordList();
            try { return JsonUtility.FromJson<RecordList>(json) ?? new RecordList(); }
            catch { return new RecordList(); }
        }

        static void Save(RecordList list)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }

        /// <summary>Saved record of a mission (null if never played).</summary>
        public static MissionRecord Get(string missionId) =>
            Load().records.Find(r => r.missionId == missionId);

        /// <summary>Saves the result and returns which records were beaten.</summary>
        public static RecordFlags Submit(MissionResult result)
        {
            if (result?.mission == null) return RecordFlags.None;
            var list = Load();
            var rec = list.records.Find(r => r.missionId == result.mission.id);
            bool first = rec == null;
            if (first) { rec = new MissionRecord { missionId = result.mission.id }; list.records.Add(rec); }

            var flags = RecordFlags.None;
            if (result.completed && (!rec.hasTime || result.finalTime < rec.bestTime))
            {
                rec.hasTime = true; rec.bestTime = result.finalTime; flags |= RecordFlags.Time;
            }
            if (first || result.grade < rec.bestGrade) { rec.bestGrade = result.grade; if (result.completed) flags |= RecordFlags.Grade; }
            if (result.score.total > rec.bestScore) { rec.bestScore = result.score.total; flags |= RecordFlags.Score; }

            Save(list);
            return flags;
        }
    }
}
