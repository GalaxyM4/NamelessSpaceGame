// MissionUIPrefabCreator.cs — Tools/Missions/Create Mission UI Prefab
// Creates: ✓ and ✗ icons (PNGs drawn by code, so there are never empty squares for missing glyphs),
// the theme and Panel Settings (1920×1080 with scaling) and the MissionUI prefab (UIDocument + MissionUIController).
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Missions.EditorTools
{
    public static class MissionUIPrefabCreator
    {
        const string UiFolder = MissionsEditorPaths.Root + "/UI";
        const string IconFolder = UiFolder + "/Icons";
        public const string PrefabPath = MissionsEditorPaths.Root + "/Prefabs/MissionUI.prefab";

        [MenuItem("Tools/Missions/Create Mission UI Prefab", priority = 5)]
        public static void CreateMenu()
        {
            var prefab = Create();
            Selection.activeObject = prefab;
            EditorUtility.DisplayDialog("Missions", "UI prefab created:\n" + PrefabPath, "OK");
        }

        public static GameObject Create()
        {
            MissionsEditorPaths.EnsureFolder(IconFolder);
            MissionsEditorPaths.EnsureFolder(MissionsEditorPaths.Root + "/Prefabs");

            var check = Icon("Icon_Check", new Color(0.30f, 0.88f, 0.54f), DrawCheck);
            var cross = Icon("Icon_Cross", new Color(0.88f, 0.34f, 0.30f), DrawCross);

            // tema + Panel Settings
            var themePath = UiFolder + "/MissionUI_Theme.tss";
            if (!File.Exists(themePath)) File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(themePath);
            var psPath = UiFolder + "/MissionUI_PanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, psPath); }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 20;
            EditorUtility.SetDirty(ps);

            var go = new GameObject("MissionUI");
            try
            {
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiFolder + "/MissionUI.uxml");
                var ui = go.AddComponent<MissionUIController>();
                ui.checkIcon = check;
                ui.crossIcon = cross;
                // project fonts (Assets/fonts) and Titillium from the rock window for small text
                ui.titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/Overmars-Jpdzj.otf");
                ui.headFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/FutureNowRegular-25Bl.ttf");
                ui.bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/RocasMarte_UI/Resources/Fonts/Libres/TitilliumWeb-SemiBold.ttf");
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                Object.DestroyImmediate(go);
                go = null;
                RuntimeSettingsCreator.CreateOrUpdate();   // the game uses this prefab in MarsScene
                return prefab;
            }
            finally { if (go != null) Object.DestroyImmediate(go); }
        }

        // ---------- iconos ----------
        delegate float Shape(Vector2 p);   // distance (px) to the shape

        static Texture2D Icon(string name, Color color, Shape shape)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = shape(new Vector2(x + 0.5f, y + 0.5f));
                    float a = Mathf.Clamp01(5.5f - d);          // ~11 px stroke with a soft edge
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
                }
            var path = $"{IconFolder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
        static float DrawCheck(Vector2 p) => Mathf.Min(Seg(p, new Vector2(12, 34), new Vector2(26, 18)), Seg(p, new Vector2(26, 18), new Vector2(53, 48)));
        static float DrawCross(Vector2 p) => Mathf.Min(Seg(p, new Vector2(14, 14), new Vector2(50, 50)), Seg(p, new Vector2(14, 50), new Vector2(50, 14)));
    }
}
