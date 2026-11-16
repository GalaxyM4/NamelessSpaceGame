// RockTypeData.cs — un tipo de roca recolectable en las misiones (uno por cada roca de rocks.json).
using UnityEngine;

namespace Game.Missions
{
    [CreateAssetMenu(menuName = "Game/Missions/Rock Type", fileName = "RockType_")]
    public class RockTypeData : ScriptableObject
    {
        [Tooltip("Id igual que en rocks.json: CheyavaFalls, Seitah, Rochette, BroomPoint, Singita.")]
        public string id;

        [Tooltip("Nombre completo en español, p. ej. \"Basalto de Máaz (Rochette)\".")]
        public string displayName;

        [Tooltip("Nombre corto para el HUD, p. ej. \"Basalto\".")]
        public string shortName;

        [Tooltip("Icono para la UI (opcional).")]
        public Sprite icon;

        [Tooltip("Color con el que se muestra este tipo en la UI.")]
        public Color uiColor = Color.white;

        [Tooltip("Dureza 1–5 (de rocks.json). La perforación tarda 2 s × dureza.")]
        [Range(1, 5)] public int hardness = 3;

        /// <summary>Nombre corto, o el id si no hay.</summary>
        public string ShortLabel => string.IsNullOrEmpty(shortName) ? id : shortName;
    }
}
