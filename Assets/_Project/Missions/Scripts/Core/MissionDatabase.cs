// MissionDatabase.cs — lista ordenada de misiones del juego.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    [CreateAssetMenu(menuName = "Game/Missions/Mission Database", fileName = "MissionDatabase")]
    public class MissionDatabase : ScriptableObject
    {
        public List<MissionDefinition> missions = new List<MissionDefinition>();

        public int IndexOf(MissionDefinition mission) => missions.IndexOf(mission);

        /// <summary>La misión siguiente en la lista, o null si es la última (o no está).</summary>
        public MissionDefinition GetNext(MissionDefinition mission)
        {
            int i = IndexOf(mission);
            return i >= 0 && i + 1 < missions.Count ? missions[i + 1] : null;
        }

    }
}
