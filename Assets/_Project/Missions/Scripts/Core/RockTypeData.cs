// RockTypeData.cs — a rock type that can be collected in missions (one per rock in rocks.json).
using UnityEngine;

namespace Game.Missions
{
    [CreateAssetMenu(menuName = "Game/Missions/Rock Type", fileName = "RockType_")]
    public class RockTypeData : ScriptableObject
    {
        [Tooltip("Same id as in rocks.json: CheyavaFalls, Seitah, Rochette, BroomPoint, Singita.")]
        public string id;

        [Tooltip("Full display name, e.g. \"Máaz basalt (Rochette)\".")]
        public string displayName;

        [Tooltip("Short name for the HUD, e.g. \"Basalt\".")]
        public string shortName;

        [Tooltip("UI icon (optional).")]
        public Sprite icon;

        [Tooltip("Color used for this type in the UI.")]
        public Color uiColor = Color.white;

        [Tooltip("Hardness 1–5 (from rocks.json). Drilling takes 2 s × hardness.")]
        [Range(1, 5)] public int hardness = 3;

        /// <summary>Short name, or the id if there is none.</summary>
        public string ShortLabel => string.IsNullOrEmpty(shortName) ? id : shortName;
    }
}
