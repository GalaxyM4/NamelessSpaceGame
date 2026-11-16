// MissionProgress.cs — cuenta las muestras de una misión en curso (lógica pura, sin MonoBehaviour).
// Los objetivos repetidos del mismo tipo se suman. Una roca no pedida, o de un tipo ya completo, no suma.
using System.Collections.Generic;

namespace Game.Missions
{
    public enum SampleCountResult { Contada, NoPedida, YaCompleto }

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

        /// <summary>Intenta sumar una muestra de este tipo. Nunca supera la cantidad pedida.</summary>
        public SampleCountResult TryAdd(RockTypeData type)
        {
            var o = type != null ? Find(type) : null;
            if (o == null) return SampleCountResult.NoPedida;
            if (o.IsComplete) return SampleCountResult.YaCompleto;
            o.collected++;
            return SampleCountResult.Contada;
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

        /// <summary>Rocas válidas conseguidas (las que suman a algún objetivo).</summary>
        public int ValidCollected
        {
            get { int n = 0; foreach (var o in _objectives) n += o.collected; return n; }
        }

        /// <summary>Rocas que aún faltan para completar la misión.</summary>
        public int Remaining => TotalRequired - ValidCollected;

        /// <summary>True si con estos tubos libres todavía se puede completar la misión.</summary>
        public bool CanStillComplete(int tubesLeft) => Remaining <= tubesLeft;

        public int TotalRequired
        {
            get { int n = 0; foreach (var o in _objectives) n += o.required; return n; }
        }
    }
}
