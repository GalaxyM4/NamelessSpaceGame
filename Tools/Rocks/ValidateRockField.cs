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
        var expected = repeated.rocks.ToDictionary(r => PositionKey(r.position));
        foreach (MeshCollider collider in field.GetComponentsInChildren<MeshCollider>())
            Check(collider.convex && collider.sharedMesh != null && !collider.isTrigger, "Invalid rock collision hull.");
        foreach (MeshRenderer renderer in renderers)
        {
            Check(renderer.sharedMaterial != null && renderer.sharedMaterial.enableInstancing, "Invalid material.");
            Check(renderer.GetComponent<LODGroup>() != null, "Missing distance culling.");
            Check(expected.TryGetValue(PositionKey(renderer.transform.position), out var rock),
                "A saved rock moved away from its deterministic placement.");
            Check(Vector3.Distance(renderer.transform.position, rock.position) < 0.001f &&
                Mathf.Abs(Quaternion.Dot(renderer.transform.rotation, rock.rotation)) > 0.999999f &&
                Vector3.Distance(renderer.transform.localScale, Vector3.one * rock.diameter) < 0.00001f &&
                renderer.GetComponent<MeshFilter>().sharedMesh == s.definitions[rock.definition].mesh &&
                renderer.sharedMaterial == s.definitions[rock.definition].material,
                "Saved rock transform, mesh or material differs from the deterministic plan.");
            expected.Remove(PositionKey(renderer.transform.position));
        }
        Check(expected.Count == 0, "A deterministic placement is missing from the saved hierarchy.");
        foreach (RockSamplePlacement sample in samples)
        {
            Check(Vector3.Distance(sample.transform.position, sample.approachPosition) <=
                s.candidateApproachRange, "Candidate approach exceeds placement proximity setting.");
        }
        foreach (var sample in repeated.rocks.Where(r => r.role == RockRole.Sample))
        foreach (var solid in repeated.rocks.Where(r => r.role != RockRole.Decoration))
            Check(Vector2.Distance(new Vector2(sample.approach.x, sample.approach.z),
                new Vector2(solid.position.x, solid.position.z)) >=
                s.roverClearanceRadius + solid.radius + s.minimumGap,
                "A candidate approach overlaps a solid rock's clearance envelope.");
        string prefabPath = UnityEditor.AssetDatabase.GetAssetPath(s.layoutPrefab);
        string partFolder = Path.Combine(Path.GetDirectoryName(prefabPath),
            Path.GetFileNameWithoutExtension(prefabPath) + "Parts");
        string[] partPaths = Directory.Exists(partFolder)
            ? Directory.GetFiles(partFolder, "*.prefab") : Array.Empty<string>();
        long largestPrefab = partPaths.Append(prefabPath).Max(path => new FileInfo(path).Length);
        Check(largestPrefab < 100 * 1024 * 1024, "A generated prefab exceeds normal GitHub file limits.");
        var report = new Report
        {
            passed = true, reproducibleLayout = true, layoutSha256 = repeated.hash,
            rockCount = renderers.Length, colliderCount = s.ObstacleCount + s.samples.count,
            sampleCount = samples.Length, reachableCells = repeated.reachableCells,
            allSavedTransformsMatch = true, prefabPartCount = partPaths.Length,
            largestPrefabBytes = largestPrefab
        };
        File.WriteAllText("Tools/Rocks/validation.json", JsonUtility.ToJson(report, true) + "\n");
        return report;
    }

    [Serializable] private sealed class Report
    {
        public bool passed, reproducibleLayout, allSavedTransformsMatch;
        public string layoutSha256;
        public int rockCount, colliderCount, sampleCount, reachableCells;
        public int prefabPartCount;
        public long largestPrefabBytes;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static Vector3Int PositionKey(Vector3 position) => new Vector3Int(
        Mathf.RoundToInt(position.x * 1000), Mathf.RoundToInt(position.y * 1000),
        Mathf.RoundToInt(position.z * 1000));
}
