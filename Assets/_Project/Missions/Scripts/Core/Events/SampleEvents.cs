// SampleEvents.cs — canal de eventos global de la toma de muestras.
// Quien muestrea una roca (RockSampleTarget) lo anuncia aquí; las misiones y la UI escuchan sin conocerse entre sí.
using System;
using UnityEngine;

namespace Game.Missions
{
    public static class SampleEvents
    {
        /// <summary>Se lanza una vez por roca cuando su muestra queda guardada en un tubo.</summary>
        public static event Action<RockTypeData, RockSampleTarget> OnSampleCollected;

        internal static void RaiseSampleCollected(RockTypeData type, RockSampleTarget target) =>
            OnSampleCollected?.Invoke(type, target);

        // Con "Enter Play Mode" sin recarga de dominio, los eventos estáticos sobreviven entre partidas: se limpian aquí.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => OnSampleCollected = null;
    }
}
