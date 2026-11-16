// RocasMarteConfig.cs — references of the rock window (Resources/RocasMarte_Config.asset).
using UnityEngine;
using UnityEngine.UIElements;

namespace RocasMarte
{
    [CreateAssetMenu(menuName = "Mars Rocks/Config", fileName = "RocasMarte_Config")]
    public class RocasMarteConfig : ScriptableObject
    {
        [Tooltip("When on, everything is set up automatically when a scene with a rover and a rock field loads.")]
        public bool autoSetup = true;
        public PanelSettings panelSettings;
        public VisualTreeAsset panel;
        [Tooltip("Project fonts (Assets/fonts). Small text uses Titillium (Resources/Fonts/Free).")]
        public Font titleFont, headFont;
        [Tooltip("Maximum horizontal distance from the drill tip (or rover) to the rock edge to analyze it (m).")]
        public float range = 4f;
    }
}
