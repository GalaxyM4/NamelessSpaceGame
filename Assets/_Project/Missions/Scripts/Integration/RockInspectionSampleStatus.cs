// RockInspectionSampleStatus.cs — connects missions with the rock analysis window (RocasMarte_UI):
// if the inspected rock was already sampled, the panel shows it ("SAMPLED") in the corner tag.
using UnityEngine;

namespace Game.Missions
{
    public static class RockInspectionSampleStatus
    {
        const float MatchDistance = 0.6f;   // m between the inspected rock center and the sampled rock

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
