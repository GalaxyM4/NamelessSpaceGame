using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ValidateRockField
{
    public static object Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Validate the saved field in Edit Mode.");
        RockScatterSettings s = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        BakedRockField field = s.bakedRoot.GetComponent<BakedRockField>();
        RockFieldBaker.Plan repeated = RockFieldBaker.CreatePlan(s);
        Check(repeated.hash == field.layoutSha256, "Regenerated plan differs from the baked layout.");
        RockSamplePlacement[] samples = field.GetComponentsInChildren<RockSamplePlacement>(true);
        Check(samples.Length == s.samples.count && samples.Select(r => r.placementId).Distinct().Count() == samples.Length,
            "Missing or duplicate samples.");
        MeshRenderer[] renderers = field.GetComponentsInChildren<MeshRenderer>();
        Check(renderers.Length == s.DecorationCount + s.ObstacleCount + s.samples.count, "Incorrect rock count.");
        Check(field.GetComponentsInChildren<Collider>().Length == s.ObstacleCount + s.samples.count,
            "Decorations should not have colliders; every obstacle/sample must have one.");
        foreach (MeshCollider collider in field.GetComponentsInChildren<MeshCollider>())
            Check(collider.convex && collider.sharedMesh != null && !collider.isTrigger, "Invalid rock collision hull.");
        foreach (MeshRenderer renderer in renderers)
        {
            Check(renderer.sharedMaterial != null && renderer.sharedMaterial.enableInstancing, "Invalid material.");
            Check(renderer.GetComponent<LODGroup>() != null, "Missing distance culling.");
        }
        foreach (RockSamplePlacement sample in samples)
        {
            Check(Vector3.Distance(sample.transform.position, sample.approachPosition) <=
                s.candidateApproachRange, "Candidate approach exceeds placement proximity setting.");
        }
        var report = new Report
        {
            passed = true, reproducibleLayout = true, layoutSha256 = repeated.hash,
            rockCount = renderers.Length, colliderCount = s.ObstacleCount + s.samples.count,
            sampleCount = samples.Length, reachableCells = repeated.reachableCells
        };
        File.WriteAllText("Tools/Rocks/validation.json", JsonUtility.ToJson(report, true) + "\n");
        return report;
    }

    [Serializable] private sealed class Report
    {
        public bool passed, reproducibleLayout;
        public string layoutSha256;
        public int rockCount, colliderCount, sampleCount, reachableCells;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
