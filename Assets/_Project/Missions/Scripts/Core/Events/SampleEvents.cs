// SampleEvents.cs — global event channel for sample collection.
// Whoever samples a rock (RockSampleTarget) announces it here; missions and UI listen without knowing each other.
using System;
using UnityEngine;

namespace Game.Missions
{
    public static class SampleEvents
    {
        /// <summary>Raised once per rock when its sample is stored in a tube.</summary>
        public static event Action<RockTypeData, RockSampleTarget> OnSampleCollected;

        internal static void RaiseSampleCollected(RockTypeData type, RockSampleTarget target) =>
            OnSampleCollected?.Invoke(type, target);

        // With "Enter Play Mode" without domain reload, static events survive between sessions: clear them here.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => OnSampleCollected = null;
    }
}
