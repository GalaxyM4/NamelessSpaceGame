// MissionDatabase.cs — ordered list of the game's missions.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    [CreateAssetMenu(menuName = "Game/Missions/Mission Database", fileName = "MissionDatabase")]
    public class MissionDatabase : ScriptableObject
    {
        public List<MissionDefinition> missions = new List<MissionDefinition>();

        public int IndexOf(MissionDefinition mission) => missions.IndexOf(mission);

        /// <summary>The next mission in the list, or null if this is the last one (or not in the list).</summary>
        public MissionDefinition GetNext(MissionDefinition mission)
        {
            int i = IndexOf(mission);
            return i >= 0 && i + 1 < missions.Count ? missions[i + 1] : null;
        }
    }
}
