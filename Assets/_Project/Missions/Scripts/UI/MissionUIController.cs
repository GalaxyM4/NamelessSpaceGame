// MissionUIController.cs — mission UI in UI Toolkit: tutorial, briefing, HUD (timer, objectives, messages,
// drill bar) and results screen. It only listens to events from MissionManager, the drill, the battery and the tutorial.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Missions
{
    [RequireComponent(typeof(UIDocument))]
    public class MissionUIController : MonoBehaviour
    {
        public MissionManager manager;
        [Tooltip("Drill (optional). Empty = found in the scene.")]
        public SimpleSamplingInteractor interactor;

        [Header("Icons (created by Tools/Missions/Create Mission UI Prefab)")]
        public Texture2D checkIcon;
        public Texture2D crossIcon;

        [Header("Fonts")]
        public Font titleFont;
        public Font headFont;
        public Font bodyFont;

        [Tooltip("Seconds each short message stays on screen.")]
        public float messageSeconds = 2.5f;

        VisualElement _root, _hud, _hudObjectives, _messages, _drill, _drillFill, _briefing, _results, _resScore, _resObjectives, _briefingObjectives, _tutorial;
        Label _hudTitle, _hudTimer, _drillText, _tubes, _grade, _tutStep, _tutText, _tutSub;
        RoverPowerSystem _power;
        MissionTutorial _tutorialLogic;
        Button _btnNext;
        CursorLockMode _prevLock; bool _prevVisible; bool _cursorFreed;

        void OnEnable()
        {
            if (manager == null) manager = FindAnyObjectByType<MissionManager>();
            if (interactor == null) interactor = FindAnyObjectByType<SimpleSamplingInteractor>();
            _power = FindAnyObjectByType<RoverPowerSystem>();
            _tutorialLogic = FindAnyObjectByType<MissionTutorial>();

            _root = GetComponent<UIDocument>().rootVisualElement;
            _hud = _root.Q("hud");
            _hudTitle = _root.Q<Label>("hud-title");
            _hudTimer = _root.Q<Label>("hud-timer");
            _hudObjectives = _root.Q("hud-objectives");
            _messages = _root.Q("messages");
            _drill = _root.Q("drill");
            _drillText = _root.Q<Label>("drill-text");
            _drillFill = _root.Q("drill-fill");
            _tubes = _root.Q<Label>("tubes");
            _briefing = _root.Q("briefing");
            _briefingObjectives = _root.Q("briefing-objectives");
            _results = _root.Q("results");
            _resScore = _root.Q("res-score");
            _resObjectives = _root.Q("res-objectives");
            _grade = _root.Q<Label>("res-grade");
            _btnNext = _root.Q<Button>("btn-next");
            _tutorial = _root.Q("tutorial");
            _tutStep = _root.Q<Label>("tut-step");
            _tutText = _root.Q<Label>("tut-text");
            _tutSub = _root.Q<Label>("tut-sub");

            ApplyTexts();
            ApplyFonts();

            _root.Q<Button>("btn-start").clicked += () => manager?.StartMission();
            _root.Q<Button>("btn-briefing-exit").clicked += () => manager?.Abandon();
            _root.Q<Button>("btn-retry").clicked += () => manager?.Retry();
            _btnNext.clicked += () => manager?.NextMission();
            _root.Q<Button>("btn-exit").clicked += () => manager?.Abandon();

            ShowOnly(null);

            if (manager != null)
            {
                manager.OnBriefing += ShowBriefing;
                manager.OnMissionStarted += ShowHud;
                manager.OnProgressChanged += RefreshHudObjectives;
                manager.OnMessage += PushMessage;
                manager.OnMissionFinished += ShowResults;
                manager.OnMissionAbandoned += OnAbandoned;
                // if the manager already showed the briefing before the UI existed
                if (manager.State == MissionState.Briefing) ShowBriefing(manager.Current);
                else if (manager.State == MissionState.InProgress) ShowHud(manager.Current);
            }
            if (_power != null) _power.Message += PushMessage;
            if (_tutorialLogic != null)
            {
                _tutorialLogic.StepChanged += OnTutorialStep;
                _tutorialLogic.Finished += OnTutorialFinished;
            }
            if (interactor != null)
            {
                interactor.TargetChanged += OnDrillTarget;
                interactor.ProgressChanged += OnDrillProgress;
                interactor.Message += PushMessage;
            }
        }

        void OnDisable()
        {
            if (manager != null)
            {
                manager.OnBriefing -= ShowBriefing;
                manager.OnMissionStarted -= ShowHud;
                manager.OnProgressChanged -= RefreshHudObjectives;
                manager.OnMessage -= PushMessage;
                manager.OnMissionFinished -= ShowResults;
                manager.OnMissionAbandoned -= OnAbandoned;
            }
            if (interactor != null)
            {
                interactor.TargetChanged -= OnDrillTarget;
                interactor.ProgressChanged -= OnDrillProgress;
                interactor.Message -= PushMessage;
            }
            if (_power != null) _power.Message -= PushMessage;
            if (_tutorialLogic != null)
            {
                _tutorialLogic.StepChanged -= OnTutorialStep;
                _tutorialLogic.Finished -= OnTutorialFinished;
            }
            FreeCursor(false);
            SetRoverHudHidden(false);
            if (_scanner != null) _scanner.enabled = true;
            if (interactor != null) interactor.enabled = true;
            if (_roverHudRenderer != null) _roverHudRenderer.UnregisterUIReloadCallback(OnRoverHudReload);
        }

        RocasMarte.RockInfoUI _inspection;
        RocasMarte.RoverRockScanner _scanner;
        bool _roverHudHidden;

        void Update()
        {
            // with the analysis panel open, the drill prompt is not drawn on top of it
            if (_inspection == null && Time.frameCount % 30 == 0) _inspection = FindAnyObjectByType<RocasMarte.RockInfoUI>();
            bool inspecting = _inspection != null && _inspection.IsOpen;
            _drill.style.visibility = inspecting ? Visibility.Hidden : Visibility.Visible;
            _hud.EnableInClassList("hud--inspecting", inspecting);
            _tutorial.EnableInClassList("tutorial--inspecting", inspecting);

            // the rover camera UI is hidden while the analysis panel, briefing or results are on screen
            bool overlay = _briefing.resolvedStyle.display == DisplayStyle.Flex || _results.resolvedStyle.display == DisplayStyle.Flex;
            SetRoverHudHidden(inspecting || overlay);

            // no analyzing or drilling while the briefing or results are on screen
            if (_scanner == null && Time.frameCount % 30 == 0) _scanner = FindAnyObjectByType<RocasMarte.RoverRockScanner>();
            if (_scanner != null && _scanner.enabled == overlay)
            {
                _scanner.enabled = !overlay;
                if (overlay && inspecting) _inspection.Hide();
                if (overlay && _inspection != null) _inspection.SetPrompt(null);
            }
            if (interactor != null) interactor.enabled = !overlay;

            if (manager != null && manager.State == MissionState.InProgress)
                _hudTimer.text = FormatTime(manager.Elapsed, false);
            if (interactor != null)
                _tubes.text = string.Format(MissionTexts.Tubes, interactor.TubesUsed, interactor.tubeCapacity);
        }

        // ---------- rover camera UI (PerseveranceStatus: white frame, battery and crosshair) ----------
        // Its component is neither modified nor disabled: only the root of its UI is hidden while needed.
        PanelRenderer _roverHudRenderer;
        VisualElement _roverHudRoot;

        void SetRoverHudHidden(bool hide)
        {
            if (_roverHudRenderer == null && hide && Time.frameCount % 30 == 0) FindRoverHud();
            if (_roverHudRoot == null) _roverHudRoot = RootOf(_roverHudRenderer);
            if (_roverHudRoot == null || hide == _roverHudHidden) return;
            _roverHudRoot.style.display = hide ? DisplayStyle.None : DisplayStyle.Flex;
            _roverHudHidden = hide;
        }

        void FindRoverHud()
        {
            var status = FindAnyObjectByType<PerseveranceStatus>();
            if (status == null) return;
            _roverHudRenderer = status.GetComponent<PanelRenderer>();
            if (_roverHudRenderer != null) _roverHudRenderer.RegisterUIReloadCallback(OnRoverHudReload);
        }

        // if its UI reloads, the new root inherits the current state
        void OnRoverHudReload(PanelRenderer renderer, VisualElement root, int version)
        {
            _roverHudRoot = root;
            if (root != null) root.style.display = _roverHudHidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        static VisualElement RootOf(PanelRenderer r)
        {
            if (r == null) return null;
            var prop = typeof(PanelRenderer).GetProperty("rootVisualElement");
            return prop != null ? prop.GetValue(r) as VisualElement : null;
        }

        // ---------- screens ----------
        void ShowOnly(VisualElement screen)
        {
            _briefing.style.display = screen == _briefing ? DisplayStyle.Flex : DisplayStyle.None;
            _results.style.display = screen == _results ? DisplayStyle.Flex : DisplayStyle.None;
            bool hud = screen == _hud;
            _hud.style.display = hud ? DisplayStyle.Flex : DisplayStyle.None;
            _tubes.style.display = hud && interactor != null ? DisplayStyle.Flex : DisplayStyle.None;
            bool tutorial = _tutorialLogic != null && _tutorialLogic.IsRunning && screen == null;
            _drill.style.display = hud || tutorial ? DisplayStyle.Flex : DisplayStyle.None;
            if (!tutorial) _tutorial.style.display = DisplayStyle.None;
            FreeCursor(screen == _briefing || screen == _results);
        }

        void ShowBriefing(MissionDefinition m)
        {
            _root.Q<Label>("briefing-title").text = TitleText(m.title);
            _root.Q<Label>("briefing-desc").text = m.description;
            _briefingObjectives.Clear();
            foreach (var o in new MissionProgress(m).Objectives)
                _briefingObjectives.Add(ObjectiveRow(o.type, string.Format(MissionTexts.ObjectiveLine, o.type.ShortLabel, o.required), null, null));

            var limit = _root.Q<Label>("briefing-limit");
            limit.text = m.timeLimit > 0 ? string.Format(MissionTexts.TimeLimit, FormatTime(m.timeLimit, false)) : "";
            limit.style.display = m.timeLimit > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var battery = _root.Q<Label>("briefing-battery");
            battery.text = _power != null ? string.Format(MissionTexts.BatteryInfo, _power.BatteryName, _power.CapacityKAh.ToString("0.#")) : "";
            battery.style.display = _power != null ? DisplayStyle.Flex : DisplayStyle.None;

            var rec = MissionProgressStore.Get(m.id);
            _root.Q<Label>("briefing-record").text = rec != null && rec.hasTime
                ? string.Format(MissionTexts.BestRecord, FormatTime(rec.bestTime, true), rec.bestGrade, rec.bestScore)
                : MissionTexts.NoRecord;
            ShowOnly(_briefing);
        }

        void ShowHud(MissionDefinition m)
        {
            _hudTitle.text = m.title.ToUpperInvariant();
            _hudTimer.text = FormatTime(0, false);
            _messages.Clear();
            ShowOnly(_hud);
            if (manager.Progress != null) RefreshHudObjectives(manager.Progress);
            OnDrillTarget(interactor != null ? interactor.Target : null);
        }

        void RefreshHudObjectives(MissionProgress p)
        {
            _hudObjectives.Clear();
            foreach (var o in p.Objectives)
            {
                var row = new VisualElement(); row.AddToClassList("hud-row");
                var dot = new VisualElement(); dot.AddToClassList("hud-row__dot"); dot.style.backgroundColor = o.type.uiColor;
                var label = new Label(string.Format(MissionTexts.ObjectiveProgress, o.type.ShortLabel, o.collected, o.required));
                label.AddToClassList("hud-row__label"); SetFont(label, bodyFont);
                var check = new Image { image = checkIcon }; check.AddToClassList("hud-row__check");
                row.Add(dot); row.Add(label); row.Add(check);
                row.EnableInClassList("hud-row--done", o.IsComplete);
                _hudObjectives.Add(row);
            }
        }

        void ShowResults(MissionResult r)
        {
            _root.Q<Label>("res-status").text = r.completed ? MissionTexts.MissionCompleted
                : r.batteryDepleted ? MissionTexts.MissionBatteryDepleted
                : r.outOfTubes ? MissionTexts.MissionOutOfTubes
                : r.timedOut ? MissionTexts.MissionTimedOut : MissionTexts.MissionIncomplete;
            _root.Q<Label>("res-title").text = TitleText(r.mission.title);
            _root.Q<Label>("res-time").text = FormatTime(r.finalTime, true);
            var th = r.mission.gradeThresholds;
            _root.Q<Label>("res-thresholds").text = string.Format(MissionTexts.Thresholds,
                FormatTime(th.a, false), FormatTime(th.b, false), FormatTime(th.c, false));

            // score breakdown
            _resScore.Clear();
            _resScore.Add(ScoreRow(string.Format(MissionTexts.ScoreRocks, r.score.validRocks), r.score.rockPoints.ToString(), false));
            _resScore.Add(ScoreRow(MissionTexts.ScoreBonus, "+" + r.score.timeBonus, false));
            _resScore.Add(ScoreRow(string.Format(MissionTexts.ScoreMultiplier, r.grade), "×" + r.score.multiplier.ToString("0.##"), false));
            _resScore.Add(ScoreRow(MissionTexts.ScoreTotal, r.score.total.ToString(), true));

            // rocks with ✓ / ✗ (images, not font glyphs)
            _resObjectives.Clear();
            foreach (var o in r.objectives)
                _resObjectives.Add(ObjectiveRow(o.type, o.type.displayName, $"{o.collected}/{o.required}", o.completed ? checkIcon : crossIcon));

            var record = _root.Q<Label>("res-record");
            record.text = MissionTexts.NewRecord;
            record.style.display = r.IsNewRecord ? DisplayStyle.Flex : DisplayStyle.None;

            _btnNext.style.display = manager.PeekNext() != null ? DisplayStyle.Flex : DisplayStyle.None;

            // big letter: appears with a scale-in
            _grade.text = r.grade.ToString();
            foreach (var g in new[] { "A", "B", "C", "D" }) _grade.RemoveFromClassList("grade--" + g);
            _grade.AddToClassList("grade--" + r.grade);
            _grade.RemoveFromClassList("grade--show");
            ShowOnly(_results);
            _grade.schedule.Execute(() => _grade.AddToClassList("grade--show")).StartingIn(250);
        }

        void OnAbandoned(MissionDefinition _) => ShowOnly(null);

        // ---------- drilling ----------
        void OnDrillTarget(RockSampleTarget t)
        {
            bool active = (manager != null && manager.State == MissionState.InProgress)
                          || (_tutorialLogic != null && _tutorialLogic.IsRunning);
            bool show = t != null && active;
            _drill.EnableInClassList("drill--hidden", !show);
            if (show)
            {
                _drillText.text = t.IsSampled ? MissionTexts.AlreadySampled : string.Format(MissionTexts.DrillPrompt, interactor.KeyLabel);
                _drillFill.style.width = Length.Percent(0);
            }
        }

        void OnDrillProgress(float p)
        {
            _drillFill.style.width = Length.Percent(p * 100f);
            if (p > 0f) _drillText.text = string.Format(MissionTexts.Drilling, Mathf.RoundToInt(p * 100));
            else OnDrillTarget(interactor.Target);
        }

        // ---------- tutorial ----------
        void OnTutorialStep(int step, string text, string sub)
        {
            ShowOnly(null);
            _tutorial.style.display = DisplayStyle.Flex;
            _drill.style.display = DisplayStyle.Flex;
            _tutStep.text = step <= MissionTutorial.StepCount
                ? MissionTexts.TutorialHeader + "  ·  " + string.Format(MissionTexts.TutorialStep, step, MissionTutorial.StepCount)
                : MissionTexts.TutorialHeader;
            _tutText.text = text;
            _tutSub.text = sub ?? "";
            _tutSub.EnableInClassList("tutorial__sub--empty", string.IsNullOrEmpty(sub));
            _tutorial.EnableInClassList("tutorial--done", step > MissionTutorial.StepCount);
        }

        void OnTutorialFinished()
        {
            _tutorial.style.display = DisplayStyle.None;
            _drill.EnableInClassList("drill--hidden", true);
        }

        // ---------- messages ----------
        void PushMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var l = new Label(text); l.AddToClassList("msg"); SetFont(l, bodyFont);
            _messages.Add(l);
            while (_messages.childCount > 3) _messages.RemoveAt(0);
            l.schedule.Execute(() => l.AddToClassList("msg--out")).StartingIn((long)(messageSeconds * 1000));
            l.schedule.Execute(() => l.RemoveFromHierarchy()).StartingIn((long)(messageSeconds * 1000) + 400);
        }

        // ---------- building blocks ----------
        VisualElement ObjectiveRow(RockTypeData type, string label, string count, Texture2D mark)
        {
            var row = new VisualElement(); row.AddToClassList("obj-row");
            var dot = new VisualElement(); dot.AddToClassList("obj-row__dot");
            if (type != null) dot.style.backgroundColor = type.uiColor;
            if (type != null && type.icon != null) { dot.style.backgroundImage = new StyleBackground(type.icon); dot.style.backgroundColor = Color.clear; }
            var l = new Label(label); l.AddToClassList("obj-row__label"); SetFont(l, bodyFont);
            row.Add(dot); row.Add(l);
            if (count != null) { var c = new Label(count); c.AddToClassList("obj-row__count"); SetFont(c, headFont); row.Add(c); }
            if (mark != null) { var m = new Image { image = mark }; m.AddToClassList("obj-row__mark"); row.Add(m); }
            return row;
        }

        VisualElement ScoreRow(string label, string value, bool total)
        {
            var row = new VisualElement(); row.AddToClassList("score-row");
            if (total) row.AddToClassList("score-row--total");
            var a = new Label(label); a.AddToClassList("score-row__label"); SetFont(a, total ? headFont : bodyFont);
            var b = new Label(value); b.AddToClassList("score-row__value"); SetFont(b, headFont);
            row.Add(a); row.Add(b);
            return row;
        }

        void ApplyTexts()
        {
            _root.Q<Label>("briefing-header").text = MissionTexts.BriefingHeader;
            _root.Q<Label>("briefing-obj-header").text = MissionTexts.Objectives;
            _root.Q<Label>("res-time-label").text = MissionTexts.FinalTime;
            _root.Q<Label>("res-score-label").text = MissionTexts.ScoreTotal;
            _root.Q<Label>("res-samples-label").text = MissionTexts.Samples;
            _root.Q<Button>("btn-start").text = MissionTexts.Start;
            _root.Q<Button>("btn-briefing-exit").text = MissionTexts.Exit;
            _root.Q<Button>("btn-retry").text = MissionTexts.Retry;
            _root.Q<Button>("btn-next").text = MissionTexts.Next;
            _root.Q<Button>("btn-exit").text = MissionTexts.Exit;
            _root.Q<Label>("tut-skip").text = MissionTexts.TutorialSkip;
        }

        void ApplyFonts()
        {
            _root.Query(className: "f-title").ForEach(e => SetFont(e, titleFont));
            _root.Query(className: "f-head").ForEach(e => SetFont(e, headFont));
            _root.Query(className: "f-body").ForEach(e => SetFont(e, bodyFont));
        }

        static void SetFont(VisualElement e, Font f)
        {
            if (f != null) e.style.unityFontDefinition = FontDefinition.FromFont(f);
        }

        void FreeCursor(bool free)
        {
            if (free && !_cursorFreed)
            {
                _prevLock = UnityEngine.Cursor.lockState; _prevVisible = UnityEngine.Cursor.visible;
                UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true;
                _cursorFreed = true;
            }
            else if (!free && _cursorFreed)
            {
                UnityEngine.Cursor.lockState = _prevLock; UnityEngine.Cursor.visible = _prevVisible;
                _cursorFreed = false;
            }
        }

        /// <summary>Titles use Overmars, which has no accents: they are written in uppercase without them.</summary>
        static string TitleText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var n = s.ToUpperInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in n)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString();
        }

        /// <summary>mm:ss or mm:ss.cc</summary>
        public static string FormatTime(float seconds, bool centis)
        {
            if (seconds < 0) seconds = 0;
            int m = (int)(seconds / 60f);
            float s = seconds - m * 60;
            return centis ? $"{m:00}:{(int)s:00}.{(int)((s - (int)s) * 100):00}" : $"{m:00}:{(int)s:00}";
        }
    }
}
