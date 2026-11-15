using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Dependency-free Editor regression tests; restores all settings even on failure.</summary>
public static class TestRockPlacement
{
    public static object Run()
    {
        RockScatterSettings s = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        string before = EditorJsonUtility.ToJson(s);
        Terrain terrain = s.terrain;
        Transform rover = s.rover, bakedRoot = s.bakedRoot;
        Transform[] studySites = s.studySites;
        RockExclusionZone[] exclusions = s.exclusions;
        RockDefinition[] definitions = s.definitions;
        GameObject layoutPrefab = s.layoutPrefab;
        int passed = 0;
        try
        {
            s.decoration.count = 60;
            s.obstacles.count = 30;
            s.samples.count = 6;
            s.starterSamples = 5;
            s.routeGridMeters = 8f;
            foreach (RockDefinition definition in s.definitions)
            {
                Vector3 span = definition.mesh.bounds.size;
                Check(Mathf.Abs(Mathf.Max(span.x, span.y, span.z) - 1f) < 0.001f, "Metre-normalized mesh");
                Check(definition.material.enableInstancing && definition.material.GetTexture("_BumpMap") != null,
                    "Shared instanced material and normal texture");
                Check(definition.collisionMesh.triangles.Length / 3 <= 255, "Convex hull triangle budget");
            }
            passed++;
            RockFieldBaker.Plan first = RockFieldBaker.CreatePlan(s);
            RockFieldBaker.Plan second = RockFieldBaker.CreatePlan(s);
            Check(first.hash == second.hash, "Same seed produces identical layout");
            passed++;
            s.seed++;
            RockFieldBaker.Plan different = RockFieldBaker.CreatePlan(s);
            Check(first.hash != different.hash, "Different seed changes layout");
            passed++;
            Check(first.rocks.Count == 96 && first.rocks.Count(r => r.role == RockRole.Sample) == 6, "Exact counts");
            foreach (var rock in first.rocks)
            {
                RockPopulation population = rock.role == RockRole.Decoration ? s.decoration :
                    rock.role == RockRole.Obstacle ? s.obstacles : s.samples;
                Check(rock.diameter >= population.minimumDiameter && rock.diameter <= population.maximumDiameter, "Size thresholds");
                Check(Vector2.Distance(new Vector2(rock.position.x, rock.position.z),
                    new Vector2(s.rover.position.x, s.rover.position.z)) >= s.spawnClearance + rock.radius, "Spawn exclusion");
                if (rock.role == RockRole.Sample)
                    Check(Vector3.Distance(rock.position, rock.approach) <= s.candidateApproachRange,
                        "Candidate approach within placement proximity");
            }
            passed++;
            for (int i = 0; i < first.rocks.Count; i++)
            for (int j = i + 1; j < first.rocks.Count; j++)
            {
                var a = first.rocks[i]; var b = first.rocks[j];
                Check(Vector2.Distance(new Vector2(a.position.x, a.position.z), new Vector2(b.position.x, b.position.z)) >=
                    a.radius + b.radius + s.minimumGap, "Non-overlap");
            }
            passed++;
            float minimum = s.samples.minimumDiameter;
            s.samples.minimumDiameter = s.samples.maximumDiameter + 1;
            ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Invalid size range");
            s.samples.minimumDiameter = minimum;
            passed++;
            float proximity = s.candidateApproachRange;
            s.candidateApproachRange = float.NaN;
            ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Invalid approach proximity");
            s.candidateApproachRange = proximity;
            passed++;
            // Required placements must fail clearly, rather than quietly producing fewer samples.
            s.exclusions = new[] { new GameObject("Temporary test exclusion").AddComponent<RockExclusionZone>() };
            try
            {
                s.exclusions[0].radius = 10000;
                ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Impossible field");
                passed++;
            }
            finally { UnityEngine.Object.DestroyImmediate(s.exclusions[0].gameObject); }
            return new { passed, deterministicHash = first.hash, reachableTestCells = first.reachableCells };
        }
        finally
        {
            EditorJsonUtility.FromJsonOverwrite(before, s);
            // EditorJsonUtility's scene references are not portable through JSON in Unity 6.6.
            s.terrain = terrain;
            s.rover = rover;
            s.bakedRoot = bakedRoot;
            s.studySites = studySites;
            s.exclusions = exclusions;
            s.definitions = definitions;
            s.layoutPrefab = layoutPrefab;
        }
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("Rock regression failed: " + label);
    }

    private static void ExpectFailure(Action action, string label)
    {
        bool failed = false;
        try { action(); }
        catch (InvalidOperationException) { failed = true; }
        Check(failed, label + " should reject without changing the scene");
    }
}
