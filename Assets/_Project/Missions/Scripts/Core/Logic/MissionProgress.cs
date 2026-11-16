// MissionProgress.cs — counts the samples of a mission in progress (pure logic, no MonoBehaviour).
// Repeated objectives of the same type are added together. A rock that was not requested,
// or of a type that is already complete, does not count.
using System.Collections.Generic;

namespace Game.Missions
{
    public enum SampleCountResult { Counted, NotRequested, AlreadyComplete }

    public class ObjectiveProgress
    {
        public RockTypeData type;
        public int required;
        public int collected;
        public bool IsComplete => collected >= required;
    }

    public class MissionProgress
    {
        readonly List<ObjectiveProgress> _objectives = new List<ObjectiveProgress>();
        public IReadOnlyList<ObjectiveProgress> Objectives => _objectives;
        public MissionDefinition Mission { get; }

        public MissionProgress(MissionDefinition mission)
        {
            Mission = mission;
            foreach (var o in mission.objectives)
            {
                if (o?.rockType == null || o.amount <= 0) continue;
                var existing = Find(o.rockType);
                if (existing != null) existing.required += o.amount;
                else _objectives.Add(new ObjectiveProgress { type = o.rockType, required = o.amount });
            }
        }

        public ObjectiveProgress Find(RockTypeData type)
        {
            foreach (var o in _objectives) if (o.type == type) return o;
            return null;
        }

        /// <summary>Tries to count one sample of this type. Never exceeds the requested amount.</summary>
        public SampleCountResult TryAdd(RockTypeData type)
        {
            var o = type != null ? Find(type) : null;
            if (o == null) return SampleCountResult.NotRequested;
            if (o.IsComplete) return SampleCountResult.AlreadyComplete;
            o.collected++;
            return SampleCountResult.Counted;
        }

        public bool AllComplete
        {
            get
            {
                if (_objectives.Count == 0) return false;
                foreach (var o in _objectives) if (!o.IsComplete) return false;
                return true;
            }
        }

        /// <summary>Valid rocks collected (the ones that count towards an objective).</summary>
        public int ValidCollected
        {
            get { int n = 0; foreach (var o in _objectives) n += o.collected; return n; }
        }

        /// <summary>Rocks still missing to complete the mission.</summary>
        public int Remaining => TotalRequired - ValidCollected;

        /// <summary>True if the mission can still be completed with this many free tubes.</summary>
        public bool CanStillComplete(int tubesLeft) => Remaining <= tubesLeft;

        public int TotalRequired
        {
            get { int n = 0; foreach (var o in _objectives) n += o.required; return n; }
        }
    }
}
