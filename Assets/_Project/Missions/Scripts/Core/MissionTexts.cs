// MissionTexts.cs — todos los textos de las misiones en un solo sitio (para traducirlos después).
namespace Game.Missions
{
    public static class MissionTexts
    {
        // --- Perforación ---
        public const string DrillPrompt = "Mantén [{0}] para perforar";             // tecla (sin revelar la roca)
        public const string Drilling = "Perforando… {0}%";                          // porcentaje
        public const string DrillCancelled = "Perforación cancelada";
        public const string AlreadySampled = "Roca ya muestreada";
        public const string NoTubesLeft = "Sin tubos de muestra libres";
        public const string SampleStored = "Muestra guardada · tubos {0}/{1}";       // usados, total
        public const string SampleStatusSampled = "MUESTREADA";

        // --- Misión ---
        public const string RockNotNeeded = "Esta roca no es necesaria para la misión";
        public const string SampleCounted = "{0} conseguida · {1}/{2}";             // roca, conseguidas, pedidas
        public const string MissionCompleted = "MISIÓN COMPLETADA";
        public const string MissionTimedOut = "TIEMPO AGOTADO";
        public const string MissionIncomplete = "MISIÓN INCOMPLETA";
        public const string MissionOutOfTubes = "SIN TUBOS LIBRES";

        // --- UI ---
        public const string BriefingHeader = "BRIEFING DE MISIÓN";
        public const string Objectives = "OBJETIVOS";
        public const string ObjectiveLine = "{0} ×{1}";                              // nombre corto, cantidad
        public const string ObjectiveProgress = "{0}  {1}/{2}";                      // nombre corto, conseguidas, pedidas
        public const string BestRecord = "Mejor marca: {0} · {1} · {2} pts";         // tiempo, letra, puntos
        public const string NoRecord = "Sin marca previa";
        public const string TimeLimit = "Límite de tiempo: {0}";
        public const string Start = "COMENZAR";
        public const string Retry = "REINTENTAR";
        public const string Next = "SIGUIENTE MISIÓN";
        public const string Exit = "SALIR";
        public const string FinalTime = "TIEMPO";
        public const string Thresholds = "A ≤ {0}   ·   B ≤ {1}   ·   C ≤ {2}";
        public const string ScoreRocks = "Rocas  {0} × 100";
        public const string ScoreBonus = "Bonus de tiempo";
        public const string ScoreMultiplier = "Multiplicador ({0})";
        public const string ScoreTotal = "PUNTAJE";
        public const string NewRecord = "¡Nuevo récord!";
        public const string Samples = "MUESTRAS";
    }
}
