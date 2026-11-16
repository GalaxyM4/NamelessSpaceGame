// RockInfoUI.cs — controla la ventana de análisis de rocas (UI Toolkit).
// Ponlo en el mismo GameObject que el UIDocument que usa RockInfoPanel.uxml.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RocasMarte
{
    [Serializable]
    public class RockInfo
    {
        public string id, name, type, location, size, composition, sample, age;
        public int water_level, hardness, astro_interest;
        public string water_text, description, source;
        public string tag;   // texto en lugar de "ID · id" (rocas genéricas: "SIN CATALOGAR")
        public string scan;  // carpeta de RockWire a usar (por defecto, el id)
        public bool estimated;
    }

    [Serializable]
    class RockInfoList { public RockInfo[] rocks; }

    [RequireComponent(typeof(UIDocument))]
    public class RockInfoUI : MonoBehaviour
    {
        [Tooltip("JSON con los datos (por defecto Resources/rocks_info.json).")]
        public TextAsset rocksJson;
        public int meterSegments = 5;
        [Tooltip("Fotogramas por segundo del giro del escaneo 3D.")]
        public float scanFps = 12f;

        [Header("Fuentes (opcional: si se dejan vacías se cargan solas desde Resources/Fonts)")]
        public Font titleFont, headFont, labelFont, valueFont, bodyFont, italicFont;

        public bool IsOpen { get; private set; }
        public event Action<bool> OpenChanged;

        // tus fuentes (Resources/Fonts) y, si no están, las libres de respaldo (Resources/Fonts/Libres)
        static readonly (string cls, string mine, string free)[] FontRoles =
        {
            ("f-title",  "Overmars-Jpdzj",             "TitilliumWeb-SemiBold"),
            ("f-head",   "FutureNowRegular-25Bl",      "TitilliumWeb-SemiBold"),
            ("f-label",  "Libres/TitilliumWeb-SemiBold", "TitilliumWeb-SemiBold"),
            ("f-value",  "Libres/TitilliumWeb-SemiBold", "TitilliumWeb-SemiBold"),
            ("f-body",   "Libres/TitilliumWeb-SemiBold", "TitilliumWeb-SemiBold"),
            ("f-italic", "Libres/TitilliumWeb-SemiBold", "TitilliumWeb-SemiBold"),
        };

        readonly Dictionary<string, RockInfo> _rocks = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Texture2D[]> _scanFrames = new(StringComparer.OrdinalIgnoreCase);
        VisualElement _root, _panel, _prompt, _scanImage, _scanLine;
        Label _promptText;
        Texture2D[] _frames;
        int _frame;
        float _scanT;
        IVisualElementScheduledItem _scanTick;

        void OnEnable()
        {
            LoadData();
            _root = GetComponent<UIDocument>().rootVisualElement;
            _panel = _root.Q<VisualElement>("panel");
            _prompt = _root.Q<VisualElement>("prompt");
            _promptText = _root.Q<Label>("prompt-text");
            _scanImage = _root.Q<VisualElement>("scan-image");
            _scanLine = _root.Q<VisualElement>("scan-line");
            _panel.style.display = DisplayStyle.None;
            _root.Q<Button>("close-button").clicked += Hide;
            foreach (var n in new[] { "m-water", "m-hardness", "m-astro" }) BuildMeter(_root.Q<VisualElement>(n));
            ApplyFonts();
            _scanTick = _root.schedule.Execute(TickScan).Every(16);
            _scanTick.Pause();
        }

        void OnDisable() => _scanTick?.Pause();

        // ---------- datos ----------
        void LoadData()
        {
            _rocks.Clear();
            var json = rocksJson != null ? rocksJson : Resources.Load<TextAsset>("rocks_info");
            if (json == null) { Debug.LogError("[RockInfoUI] No encuentro rocks_info.json en Resources."); return; }
            foreach (var r in JsonUtility.FromJson<RockInfoList>(json.text).rocks) _rocks[r.id] = r;
        }


        // ---------- fuentes ----------
        void ApplyFonts()
        {
            var manual = new Dictionary<string, Font>
            {
                ["f-title"] = titleFont, ["f-head"] = headFont, ["f-label"] = labelFont,
                ["f-value"] = valueFont, ["f-body"] = bodyFont, ["f-italic"] = italicFont,
            };
            foreach (var (cls, mine, free) in FontRoles)
            {
                // (sin ?? : los campos vacíos de Unity son "falsos null" y ?? no los detecta)
                Font font = manual[cls];
                if (font == null) font = Resources.Load<Font>("Fonts/" + mine);
                if (font == null) font = Resources.Load<Font>("Fonts/Libres/" + free);
                if (font == null) continue;
                var def = MakeCrispFont(font);
                _root.Query(className: cls).ForEach(e => e.style.unityFontDefinition = def);
            }
        }

        // Fuente SDF: se mantiene nítida aunque la ventana se reduzca (la fuente normal
        // se rasteriza a un tamaño fijo y al escalar se ve borrosa / "doble").
        // Caché por instancia (no static): con 'Reload Domain' desactivado, una caché static guardaba
        // fuentes ya destruidas de la partida anterior y Unity mostraba su letra por defecto.
        readonly Dictionary<Font, FontDefinition> _sdfCache = new Dictionary<Font, FontDefinition>();
        FontDefinition MakeCrispFont(Font font)
        {
            if (_sdfCache.TryGetValue(font, out var cached)) return cached;
            FontDefinition def;
            try
            {
                var fa = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                def = fa != null ? FontDefinition.FromSDFFont(fa) : FontDefinition.FromFont(font);
            }
            catch (Exception) { def = FontDefinition.FromFont(font); }
            _sdfCache[font] = def;
            return def;
        }

        // Overmars no tiene tildes ni Ñ: el título se escribe sin ellas (como "SELECCIONA UNA MISION").
        static string SinTildes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var n = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in n)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // ---------- medidores ----------
        void BuildMeter(VisualElement bar)
        {
            bar.Clear();
            for (int i = 0; i < meterSegments; i++)
            {
                var s = new VisualElement();
                s.AddToClassList("seg");
                if (i == meterSegments - 1) s.AddToClassList("seg--last");
                bar.Add(s);
            }
        }

        void SetMeter(string bar, string num, int value)
        {
            int i = 0;
            foreach (var s in _root.Q<VisualElement>(bar).Children()) s.EnableInClassList("seg--on", i++ < value);
            _root.Q<Label>(num).text = $"{value}/{meterSegments}";
        }

        // ---------- escaneo 3D (fotogramas renderizados en Blender: Resources/RockWire/<Id>/) ----------
        Texture2D[] FramesFor(string id)
        {
            if (!_scanFrames.TryGetValue(id, out var f))
            {
                f = Resources.LoadAll<Texture2D>("RockWire/" + id).OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
                _scanFrames[id] = f;
            }
            return f;
        }

        void TickScan()
        {
            if (!IsOpen) return;
            _scanT += 0.016f;
            if (_frames != null && _frames.Length > 0)
            {
                int f = (int)(_scanT * scanFps) % _frames.Length;
                if (f != _frame) { _frame = f; _scanImage.style.backgroundImage = new StyleBackground(_frames[f]); }
            }
            // línea de barrido que baja y vuelve a empezar
            float h = _scanLine.parent.resolvedStyle.height;
            if (h > 0) _scanLine.style.top = (_scanT * 90f) % h;
        }

        // ---------- aviso ----------
        public void SetPrompt(string rockId, string key = "E")
        {
            bool show = rockId != null && !IsOpen && _rocks.ContainsKey(rockId);
            _prompt.EnableInClassList("prompt--hidden", !show);
            if (show)
            {
                _prompt.Q<Label>("prompt-key").text = key;
                _promptText.text = "ANALIZAR";   // sin el nombre: se descubre al abrir la ficha
            }
        }


        // ---------- abrir / cerrar ----------
        public void Show(string rockId, string sizeOverride = null)
        {
            if (!_rocks.TryGetValue(rockId, out var r)) { Debug.LogWarning($"[RockInfoUI] Roca desconocida: {rockId}"); return; }
            _root.Q<Label>("rock-name").text = SinTildes(r.name);
            _root.Q<Label>("rock-type").text = r.type;
            _root.Q<Label>("rock-id").text = string.IsNullOrEmpty(r.tag) ? "ID · " + r.id : r.tag;
            _root.Q<Label>("v-location").text = r.location;
            _root.Q<Label>("v-size").text = string.IsNullOrEmpty(sizeOverride) ? r.size : sizeOverride;
            _root.Q<Label>("v-composition").text = r.composition;
            _root.Q<Label>("v-sample").text = r.sample;
            _root.Q<Label>("v-age").text = r.age;
            _root.Q<Label>("v-water").text = r.water_text;
            _root.Q<Label>("v-description").text = r.description;
            SetMeter("m-water", "n-water", r.water_level);
            SetMeter("m-hardness", "n-hardness", r.hardness);
            SetMeter("m-astro", "n-astro", r.astro_interest);

            _frames = FramesFor(string.IsNullOrEmpty(r.scan) ? r.id : r.scan);
            _frame = -1;
            _scanT = 0;
            _scanImage.style.backgroundImage = _frames.Length > 0 ? new StyleBackground(_frames[0]) : StyleKeyword.None;

            _prompt.AddToClassList("prompt--hidden");
            _panel.style.display = DisplayStyle.Flex;
            _panel.AddToClassList("panel--hidden");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel--hidden")).StartingIn(16);
            IsOpen = true;
            _scanTick.Resume();
            OpenChanged?.Invoke(true);
        }

        /// <summary>Cambia la etiqueta de la esquina de la ficha abierta (p. ej. "MUESTREADA").</summary>
        public void SetTag(string text)
        {
            if (IsOpen && !string.IsNullOrEmpty(text)) _root.Q<Label>("rock-id").text = text;
        }

        public void Hide()
        {
            if (!IsOpen) return;
            _panel.AddToClassList("panel--hidden");
            _panel.schedule.Execute(() => { if (!IsOpen) _panel.style.display = DisplayStyle.None; }).StartingIn(200);
            IsOpen = false;
            _scanTick.Pause();
            OpenChanged?.Invoke(false);
        }
    }
}
