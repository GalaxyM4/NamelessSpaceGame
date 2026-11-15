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
            s.useDensity = false;
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
            s.useDensity = true;
            s.decorationDensity = 60f / s.AreaHectares;
            s.obstacleDensity = 30f / s.AreaHectares;
            Check(s.DecorationCount == 60 && s.ObstacleCount == 30, "Density converts hectares to target counts");
            Check(RockFieldBaker.CreatePlan(s).hash == first.hash, "Equivalent density preserves layout");
            s.decorationDensity = 0;
            s.obstacleDensity = 0;
            Check(s.DecorationCount == 0 && s.ObstacleCount == 0 && s.samples.count == 6,
                "Zero density leaves passive sample count independent");
            s.decorationDensity = float.PositiveInfinity;
            ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Nonfinite density");
            s.decorationDensity = 100001f / s.AreaHectares;
            ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Excessive density");
            s.useDensity = false;
            s.decorationDensity = 60f / s.AreaHectares;
            passed++;
            float low = s.burialFraction, high = s.maximumBurialFraction;
            s.burialFraction = 0;
            s.maximumBurialFraction = 0;
            RockFieldBaker.Plan unburied = RockFieldBaker.CreatePlan(s);
            s.burialFraction = 0.08f;
            s.maximumBurialFraction = 0.35f;
            RockFieldBaker.Plan buried = RockFieldBaker.CreatePlan(s);
            Check(buried.rocks.Select(r => r.burial).Distinct().Count() > 1, "Varied burial");
            for (int i = 0; i < buried.rocks.Count; i++)
            {
                var a = unburied.rocks[i]; var b = buried.rocks[i];
                Check(a.position.x == b.position.x && a.position.z == b.position.z &&
                    a.rotation == b.rotation && a.diameter == b.diameter, "Burial preserves horizontal layout");
                float depth = s.definitions[b.definition].mesh.bounds.size.y * b.diameter * b.burial;
                Check(b.burial >= 0.08f && b.burial <= 0.35f &&
                    Mathf.Abs(a.position.y - b.position.y - depth) < 0.0001f, "Burial depth matches range");
            }
            s.burialFraction = 0.36f;
            ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Reversed burial range");
            s.burialFraction = 0.08f;
            s.maximumBurialFraction = 0.9f;
            Check(RockFieldBaker.CreatePlan(s).rocks.Count == 96, "Inspector maximum burial of 0.9 is accepted");
            s.maximumBurialFraction = 0.91f;
            ExpectFailure(() => RockFieldBaker.CreatePlan(s), "Burial above Inspector maximum");
            s.burialFraction = low;
            s.maximumBurialFraction = high;
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
