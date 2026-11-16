// RocasMarteConfig.cs — referencias de la ventana de rocas (Resources/RocasMarte_Config.asset).
using UnityEngine;
using UnityEngine.UIElements;

namespace RocasMarte
{
    [CreateAssetMenu(menuName = "Rocas Marte/Config", fileName = "RocasMarte_Config")]
    public class RocasMarteConfig : ScriptableObject
    {
        [Tooltip("Si está activo, al cargar una escena con rover y campo de rocas se monta todo solo.")]
        public bool autoSetup = true;
        public PanelSettings panelSettings;
        public VisualTreeAsset panel;
        [Tooltip("Fuentes del proyecto (Assets/fonts). El texto pequeño usa Titillium (Resources/Fonts/Libres).")]
        public Font titleFont, headFont;
        [Tooltip("Distancia máxima del rover a la superficie de la roca para poder analizarla (m).")]
        public float range = 4f;
    }
}
