// RockInspectionSampleStatus.cs — conecta las misiones con la ventana de análisis de rocas (RocasMarte_UI):
// si la roca que se inspecciona ya fue muestreada, la ficha lo indica ("MUESTREADA") en la etiqueta de la esquina.
using UnityEngine;

namespace Game.Missions
{
    public static class RockInspectionSampleStatus
    {
        const float MatchDistance = 0.6f;   // m entre el centro de la roca inspeccionada y la roca muestreada

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            RocasMarte.RoverRockScanner.Inspected -= OnInspected;
            RocasMarte.RoverRockScanner.Inspected += OnInspected;
        }

        static void OnInspected(Vector3 rockCenter, RocasMarte.RockInfoUI ui)
        {
            foreach (var t in RockSampleTarget.All)
            {
                if (t == null || !t.IsSampled) continue;
                var r = t.GetComponentInChildren<Renderer>();
                var c = r != null ? r.bounds.center : t.transform.position;
                if ((c - rockCenter).sqrMagnitude <= MatchDistance * MatchDistance)
                {
                    ui.SetTag(MissionTexts.SampleStatusSampled);
                    return;
                }
            }
        }
    }
}
