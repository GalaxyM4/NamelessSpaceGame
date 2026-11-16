// MissionManager.cs — controla una misión: Briefing → EnCurso → Completada (o Abandonada).
// Escucha SampleEvents.OnSampleCollected y avisa a la UI con eventos C#. No conoce la UI ni el rover.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    public enum MissionState { Inactiva, Briefing, EnCurso, Completada, Abandonada }

    public class MissionManager : MonoBehaviour
    {
        public MissionDatabase database;
        [Tooltip("Misión que se muestra al empezar (opcional). Vacío = la primera de la base de datos.")]
        public MissionDefinition startMission;
        [Tooltip("Mostrar el briefing automáticamente al iniciar la escena.")]
        public bool briefingOnStart = true;
        [Tooltip("Misiones en orden aleatorio: al empezar y con \"Siguiente misión\" sale una al azar, sin repetir hasta haber jugado todas.")]
        public bool randomMissions = true;

        public MissionState State { get; private set; } = MissionState.Inactiva;
        public MissionDefinition Current { get; private set; }
        public MissionProgress Progress { get; private set; }
        /// <summary>Segundos de misión (tiempo escalado: la pausa con timeScale = 0 lo detiene).</summary>
        public float Elapsed { get; private set; }

        // ---------- eventos para la UI ----------
        public event Action<MissionDefinition> OnBriefing;
        public event Action<MissionDefinition> OnMissionStarted;
        public event Action<MissionProgress> OnProgressChanged;
        public event Action<string> OnMessage;
        public event Action<MissionResult> OnMissionFinished;
        public event Action<MissionDefinition> OnMissionAbandoned;

        readonly List<RockSampleTarget> _sampledThisRun = new List<RockSampleTarget>();

        void OnEnable() => SampleEvents.OnSampleCollected += HandleSample;
        void OnDisable() => SampleEvents.OnSampleCollected -= HandleSample;

        void Start()
        {
            if (!briefingOnStart) return;
            var m = startMission != null ? startMission
                  : randomMissions ? DrawRandom()
                  : (database != null && database.missions.Count > 0 ? database.missions[0] : null);
            if (m != null) ShowBriefing(m);
        }

        void Update()
        {
            if (State != MissionState.EnCurso) return;
            Elapsed += Time.deltaTime;
            if (Current.timeLimit > 0f && Elapsed >= Current.timeLimit)
            {
                Elapsed = Current.timeLimit;
                Finish(timedOut: true);
            }
        }

        // ---------- flujo ----------
        public void ShowBriefing(MissionDefinition mission)
        {
            if (mission == null) return;
            Current = mission;
            if (_peeked == mission) _peeked = null;
            Progress = new MissionProgress(mission);
            Elapsed = 0f;
            SetState(MissionState.Briefing);
            OnBriefing?.Invoke(mission);
        }

        /// <summary>Botón "Comenzar": arranca el cronómetro.</summary>
        public void StartMission()
        {
            if (Current == null || State == MissionState.EnCurso) return;
            ResetSampledRocks();
            Progress = new MissionProgress(Current);
            Elapsed = 0f;
            SetState(MissionState.EnCurso);
            OnMissionStarted?.Invoke(Current);
            OnProgressChanged?.Invoke(Progress);
        }

        /// <summary>Botón "Reintentar": la misma misión desde cero.</summary>
        public void Retry()
        {
            if (Current == null) return;
            ResetSampledRocks();
            ShowBriefing(Current);
        }

        /// <summary>Botón "Siguiente misión". Devuelve false si no hay siguiente.</summary>
        public bool NextMission()
        {
            var next = PeekNext();
            if (next == null) return false;
            ResetSampledRocks();
            ShowBriefing(next);
            return true;
        }

        public MissionDefinition PeekNext()
        {
            if (database == null) return null;
            if (!randomMissions) return database.GetNext(Current);
            if (_peeked == null || _peeked == Current) _peeked = DrawRandom();
            return _peeked;
        }

        // ---------- orden aleatorio ("bolsa": no se repite ninguna hasta haber salido todas) ----------
        readonly List<MissionDefinition> _bag = new List<MissionDefinition>();
        MissionDefinition _peeked;

        MissionDefinition DrawRandom()
        {
            if (database == null || database.missions.Count == 0) return null;
            if (_bag.Count == 0)
                foreach (var m in database.missions)
                    if (m != null && m != Current) _bag.Add(m);
            if (_bag.Count == 0) return Current;   // solo hay una misión
            int i = UnityEngine.Random.Range(0, _bag.Count);
            var pick = _bag[i];
            _bag.RemoveAt(i);
            return pick;
        }

        /// <summary>Botón "Salir" o abandonar durante la misión.</summary>
        public void Abandon()
        {
            if (State == MissionState.Inactiva) return;
            var m = Current;
            SetState(MissionState.Abandonada);
            OnMissionAbandoned?.Invoke(m);
        }

        /// <summary>
        /// El perforador avisa de sus tubos libres tras cada muestra. Si ya no alcanzan para las rocas que faltan,
        /// la misión no se puede completar: termina ya ("SIN TUBOS LIBRES", calificación D).
        /// </summary>
        public void ReportTubesLeft(int tubesLeft)
        {
            if (State != MissionState.EnCurso || Progress == null) return;
            if (!Progress.CanStillComplete(tubesLeft)) Finish(timedOut: false, outOfTubes: true);
        }

        /// <summary>Depuración (F10): termina ya con el tiempo actual.</summary>
        public void DebugFinishNow()
        {
            if (State == MissionState.EnCurso) Finish(timedOut: false);
        }

        // ---------- muestras ----------
        void HandleSample(RockTypeData type, RockSampleTarget target)
        {
            if (State != MissionState.EnCurso) return;
            if (target != null) _sampledThisRun.Add(target);

            var r = Progress.TryAdd(type);
            if (r != SampleCountResult.Contada)
            {
                OnMessage?.Invoke(MissionTexts.RockNotNeeded);   // no suma y no penaliza
                return;
            }
            var o = Progress.Find(type);
            OnMessage?.Invoke(string.Format(MissionTexts.SampleCounted, type.ShortLabel, o.collected, o.required));
            OnProgressChanged?.Invoke(Progress);
            if (Progress.AllComplete) Finish(timedOut: false);
        }

        void Finish(bool timedOut, bool outOfTubes = false)
        {
            bool completed = Progress.AllComplete;
            var th = Current.gradeThresholds;
            var result = new MissionResult
            {
                mission = Current,
                finalTime = Elapsed,
                completed = completed,
                timedOut = timedOut && !completed,
                outOfTubes = outOfTubes && !completed,
                grade = MissionScoring.GradeFor(Elapsed, th.a, th.b, th.c, completed),
                score = MissionScoring.Score(Progress.ValidCollected, Elapsed, th.a, th.b, th.c, completed),
            };
            foreach (var o in Progress.Objectives)
                result.objectives.Add(new ObjectiveResult { type = o.type, collected = o.collected, required = o.required, completed = o.IsComplete });
            result.newRecords = MissionProgressStore.Submit(result);

            SetState(MissionState.Completada);
            OnMissionFinished?.Invoke(result);
        }

        void ResetSampledRocks()
        {
            foreach (var t in _sampledThisRun) if (t != null) t.ResetSample();
            _sampledThisRun.Clear();
        }

        void SetState(MissionState s)
        {
            State = s;
        }
    }
}
