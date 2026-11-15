using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Deterministic terrain-guided scatter. Plans/validates first, then swaps its owned hierarchy.</summary>
public static class RockFieldBaker
{
    public sealed class Placement
    {
        public RockRole role;
        public int definition;
        public Vector3 position;
        public Quaternion rotation;
        public float diameter;
        public float radius;
        public float burial;
        public Vector3 approach;
    }

    public sealed class Plan
    {
        public readonly List<Placement> rocks = new List<Placement>();
        public string hash;
        public int reachableCells;
    }

    private sealed class Planner
    {
        private readonly RockScatterSettings s;
        private readonly System.Random random;
        private readonly Plan plan = new Plan();
        private readonly Dictionary<Vector2Int, List<Placement>> bins = new Dictionary<Vector2Int, List<Placement>>();
        private readonly Vector3 origin;
        private readonly Vector3 size;
        private readonly Vector2 noiseOffset;
        private readonly float largestRadius;
        private readonly Collider[] overlaps = new Collider[128];
        private bool[,] reachable;
        private int nx, nz;
        private const float BinSize = 4f;

        internal Planner(RockScatterSettings settings)
        {
            s = settings;
            random = new System.Random(s.seed);
            origin = s.terrain.transform.position;
            size = s.terrain.terrainData.size;
            noiseOffset = new Vector2(Next() * 10000f, Next() * 10000f);
            largestRadius = s.definitions.Max(d => d.mesh.bounds.extents.magnitude) *
                Mathf.Max(s.obstacles.maximumDiameter, s.decoration.maximumDiameter, s.samples.maximumDiameter);
        }

        private float Next() => (float)random.NextDouble();
        private float Ground(Vector2 p) => origin.y + s.terrain.SampleHeight(new Vector3(p.x, 0, p.y));
        private Vector2 UV(Vector2 p) => new Vector2((p.x - origin.x) / size.x, (p.y - origin.z) / size.z);
        private float Slope(Vector2 p) { Vector2 uv = UV(p); return s.terrain.terrainData.GetSteepness(uv.x, uv.y); }
        private Vector2Int Bin(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / BinSize), Mathf.FloorToInt(p.y / BinSize));

        private bool Inside(Vector2 p, float margin)
        {
            return p.x >= origin.x + margin && p.y >= origin.z + margin &&
                p.x <= origin.x + size.x - margin && p.y <= origin.z + size.z - margin;
        }

        private float Field(Vector2 p)
        {
            Vector2 v = p / s.fieldScale + noiseOffset;
            return Mathf.PerlinNoise(v.x, v.y);
        }

        private bool Occupied(Vector2 p, float radius, bool solidOnly)
        {
            Vector2Int cell = Bin(p);
            int search = Mathf.CeilToInt((radius + largestRadius + s.minimumGap) / BinSize);
            for (int z = -search; z <= search; z++)
            for (int x = -search; x <= search; x++)
            {
                if (!bins.TryGetValue(cell + new Vector2Int(x, z), out var entries)) continue;
                foreach (Placement rock in entries)
                {
                    if (solidOnly && rock.role == RockRole.Decoration) continue;
                    if (Vector2.Distance(p, new Vector2(rock.position.x, rock.position.z)) <
                        radius + rock.radius + s.minimumGap) return true;
                }
            }
            return false;
        }

        private bool ExistingBlocked(Vector2 p, float radius)
        {
            // Conservative rover/rock envelope; ignore only this field's previous bake and the rover.
            int count = Physics.OverlapSphereNonAlloc(new Vector3(p.x, Ground(p) + radius, p.y),
                radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return true;
            for (int i = 0; i < count; i++)
            {
                Collider c = overlaps[i];
                if (c == s.terrain.GetComponent<TerrainCollider>() || c.transform.IsChildOf(s.rover)) continue;
                var field = c.GetComponentInParent<BakedRockField>();
                if (field != null && field.owner == s) continue;
                return true;
            }
            return false;
        }

        private bool Excluded(Vector2 p, float radius)
        {
            Vector2 spawn = new Vector2(s.rover.position.x, s.rover.position.z);
            if (Vector2.Distance(p, spawn) < s.spawnClearance + radius) return true;
            Vector3 world = new Vector3(p.x, 0, p.y);
            return s.exclusions.Any(zone => zone != null && zone.Contains(world, radius));
        }

        private bool Driveable(Vector2 p)
        {
            if (!Inside(p, s.roverClearanceRadius) || Slope(p) > s.drivingSlopeLimit ||
                Occupied(p, s.roverClearanceRadius, true) || ExistingBlocked(p, s.roverClearanceRadius)) return false;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector2 edge = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s.roverClearanceRadius;
                if (Slope(edge) > s.drivingSlopeLimit) return false;
            }
            return true;
        }

        private Vector2 Cell(int x, int z) => new Vector2(origin.x + x * s.routeGridMeters, origin.z + z * s.routeGridMeters);

        private void BuildReachability()
        {
            plan.reachableCells = 0;
            nx = Mathf.FloorToInt(size.x / s.routeGridMeters) + 1;
            nz = Mathf.FloorToInt(size.z / s.routeGridMeters) + 1;
            reachable = new bool[nz, nx];
            var checkedCells = new bool[nz, nx];
            var queue = new Queue<Vector2Int>();
            int sx = Mathf.Clamp(Mathf.RoundToInt((s.rover.position.x - origin.x) / s.routeGridMeters), 0, nx - 1);
            int sz = Mathf.Clamp(Mathf.RoundToInt((s.rover.position.z - origin.z) / s.routeGridMeters), 0, nz - 1);
            queue.Enqueue(new Vector2Int(sx, sz));
            checkedCells[sz, sx] = true;
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                Vector2 p = Cell(cell.x, cell.y);
                if (!Driveable(p)) continue;
                reachable[cell.y, cell.x] = true;
                plan.reachableCells++;
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int n = cell + direction;
                    if (n.x < 0 || n.y < 0 || n.x >= nx || n.y >= nz || checkedCells[n.y, n.x]) continue;
                    // Swept corridor approximation: sample the link, not just grid endpoints.
                    bool clear = true;
                    for (int step = 1; step < 4; step++)
                        if (!Driveable(Vector2.Lerp(p, Cell(n.x, n.y), step / 4f))) { clear = false; break; }
                    if (!clear) continue;
                    checkedCells[n.y, n.x] = true;
                    queue.Enqueue(n);
                }
            }
            if (plan.reachableCells == 0)
                throw new InvalidOperationException("No driving area from the rover. Check its position and route settings; terrain is never flattened.");
        }

        private bool Approach(Vector2 p, out Vector3 approach)
        {
            approach = default;
            int cx = Mathf.RoundToInt((p.x - origin.x) / s.routeGridMeters);
            int cz = Mathf.RoundToInt((p.y - origin.z) / s.routeGridMeters);
            float roverHeight = s.settledRoverHeight;
            for (int z = -2; z <= 2; z++)
            for (int x = -2; x <= 2; x++)
            {
                int ix = cx + x, iz = cz + z;
                if (ix < 0 || iz < 0 || ix >= nx || iz >= nz || !reachable[iz, ix]) continue;
                Vector2 candidate = Cell(ix, iz);
                if (Vector2.Distance(p, candidate) < s.roverClearanceRadius + s.samples.maximumDiameter ||
                    !Driveable(candidate)) continue;
                Vector3 roverCenter = new Vector3(candidate.x, Ground(candidate) + roverHeight, candidate.y);
                Vector3 target = new Vector3(p.x, Ground(p) + s.samples.minimumDiameter * 0.3f, p.y);
                if (Vector3.Distance(roverCenter, target) > s.candidateApproachRange - 0.2f) continue;
                // No obstacles between the planned tool origin and sample.
                bool clear = true;
                for (int step = 1; step < 12; step++)
                {
                    Vector3 v = Vector3.Lerp(roverCenter, target, step / 12f);
                    Vector2 q = new Vector2(v.x, v.z);
                    if (Occupied(q, 0.1f, true) || v.y < Ground(q) + 0.02f) { clear = false; break; }
                }
                if (!clear) continue;
                foreach (RaycastHit hit in Physics.RaycastAll(roverCenter, (target - roverCenter).normalized,
                             Vector3.Distance(roverCenter, target), ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(s.rover)) continue;
                    var previous = hit.collider.GetComponentInParent<BakedRockField>();
                    if (previous != null && previous.owner == s) continue;
                    clear = false;
                    break;
                }
                if (!clear) continue;
                approach = roverCenter;
                return true;
            }
            return false;
        }

        private Vector2 Candidate(Vector3? center, float radius)
        {
            if (!center.HasValue && Next() > 0.22f)
                return new Vector2(origin.x + Next() * size.x, origin.z + Next() * size.z);
            Vector3 c = center ?? s.rover.position;
            float angle = Next() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Next()) * (center.HasValue ? radius : 160f);
            return new Vector2(c.x + Mathf.Cos(angle) * r, c.z + Mathf.Sin(angle) * r);
        }

        private void Populate(RockRole role, RockPopulation population, int count, Vector3? center = null,
            float nearRadius = 0, int forcedDefinition = -1)
        {
            int placed = 0;
            int attempts = Math.Max(2000, count * 120);
            for (int attempt = 0; attempt < attempts && placed < count; attempt++)
            {
                Vector2 p = Candidate(center, nearRadius);
                float diameter = Mathf.Lerp(population.minimumDiameter, population.maximumDiameter, Mathf.Pow(Next(), 2.4f));
                float field = Field(p);
                float slope = Inside(p, 2) ? Slope(p) : 90;
                float breaks = slope < 89 ? Mathf.Abs(slope - Slope(p + Vector2.right * 3f)) : 0;
                float suitability = Mathf.Lerp(1f, Mathf.SmoothStep(0, 1, (field - 0.25f) / 0.5f), s.clustering);
                suitability *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(slope / 12f + breaks / 4f));
                if (Next() > suitability || slope > (role == RockRole.Sample ? s.drivingSlopeLimit : s.maximumSlope)) continue;
                int definition = forcedDefinition;
                if (definition < 0)
                {
                    // Shared patch preference produces coherent rock families, with some mixing.
                    definition = random.Next(s.definitions.Length);
                    for (int i = 0; i < 3; i++)
                    {
                        int other = random.Next(s.definitions.Length);
                        if (Mathf.Abs(s.definitions[other].preferredField - field) <
                            Mathf.Abs(s.definitions[definition].preferredField - field)) definition = other;
                    }
                }
                Mesh mesh = s.definitions[definition].mesh;
                float rockRadius = mesh.bounds.extents.magnitude * diameter;
                if (!Inside(p, rockRadius + 0.5f) || Excluded(p, rockRadius) ||
                    Occupied(p, rockRadius, false) || ExistingBlocked(p, Mathf.Max(0.25f, rockRadius))) continue;
                // Samples may not occupy any previously validated sample approach envelope.
                if (plan.rocks.Any(r => r.role == RockRole.Sample &&
                    Vector2.Distance(p, new Vector2(r.approach.x, r.approach.z)) < s.roverClearanceRadius + rockRadius)) continue;
                Vector3 approach = default;
                if (role == RockRole.Sample && !Approach(p, out approach)) continue;
                Vector2 uv = UV(p);
                Quaternion rotation = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(Vector3.up, s.terrain.terrainData.GetInterpolatedNormal(uv.x, uv.y)),
                    s.groundAlignment) * Quaternion.Euler(Next() * 10f - 5f, Next() * 360f, Next() * 10f - 5f);
                float supportY = float.NegativeInfinity;
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 v = rotation * (vertex * diameter);
                    supportY = Mathf.Max(supportY, Ground(p + new Vector2(v.x, v.z)) - v.y);
                }
                // Separate stable depth noise: changing burial does not consume placement RNG.
                float burialNoise = Mathf.PerlinNoise(p.x * 0.731f + noiseOffset.x, p.y * 0.917f + noiseOffset.y);
                float burial = Mathf.Lerp(s.burialFraction, s.maximumBurialFraction, burialNoise);
                var rock = new Placement
                {
                    role = role, definition = definition, diameter = diameter, radius = rockRadius,
                    position = new Vector3(p.x, supportY - mesh.bounds.size.y * diameter * burial, p.y),
                    burial = burial,
                    rotation = rotation, approach = approach
                };
                plan.rocks.Add(rock);
                Vector2Int bin = Bin(p);
                if (!bins.TryGetValue(bin, out var list)) bins[bin] = list = new List<Placement>();
                list.Add(rock);
                placed++;
            }
            if (placed != count)
                throw new InvalidOperationException($"Only {placed}/{count} {role} rocks fit. Reduce counts/clearance or expand the area; previous bake retained.");
        }

        internal Plan Run()
        {
            Populate(RockRole.Obstacle, s.obstacles, s.ObstacleCount);
            BuildReachability();
            // Required starter samples cover all models in a small, reachable exploration area.
            int starter = Mathf.Min(s.starterSamples, s.samples.count);
            for (int i = 0; i < starter; i++)
                Populate(RockRole.Sample, s.samples, 1, s.rover.position, s.starterSampleRadius, i % s.definitions.Length);
            foreach (Transform site in s.studySites)
            {
                Populate(RockRole.Sample, s.samples, 1, site.position, s.starterSampleRadius);
            }
            int remaining = s.samples.count - starter - s.studySites.Length;
            Populate(RockRole.Sample, s.samples, remaining);
            // Samples are solid too. Confirm their final layout has not severed an approach route.
            BuildReachability();
            foreach (Placement sample in plan.rocks.Where(r => r.role == RockRole.Sample))
            {
                int x = Mathf.RoundToInt((sample.approach.x - origin.x) / s.routeGridMeters);
                int z = Mathf.RoundToInt((sample.approach.z - origin.z) / s.routeGridMeters);
                if (!reachable[z, x])
                    throw new InvalidOperationException("A sample blocks another approach route. Change the seed or clearance; previous bake retained.");
            }
            Populate(RockRole.Decoration, s.decoration, s.DecorationCount);
            var text = new StringBuilder();
            foreach (Placement rock in plan.rocks)
                text.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1}:{2:R},{3:R},{4:R}:{5:R},{6:R},{7:R},{8:R}:{9:R};",
                    (int)rock.role, rock.definition, rock.position.x, rock.position.y, rock.position.z,
                    rock.rotation.x, rock.rotation.y, rock.rotation.z, rock.rotation.w, rock.diameter);
            using (var sha = SHA256.Create())
                plan.hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "").ToLowerInvariant();
            return plan;
        }
    }

    public static Plan CreatePlan(RockScatterSettings settings)
    {
        Validate(settings);
        Physics.SyncTransforms();
        return new Planner(settings).Run();
    }

    private static void Validate(RockScatterSettings s)
    {
        if (Application.isPlaying || s == null || s.terrain == null || s.terrain.terrainData == null || s.rover == null)
            throw new InvalidOperationException("A terrain and rover are required in Edit Mode.");
        if (s.terrain.transform.lossyScale != Vector3.one || s.terrain.transform.rotation != Quaternion.identity)
            throw new InvalidOperationException("Terrain must use real metre scale with identity rotation.");
        if (s.definitions == null || s.definitions.Length == 0 ||
            s.definitions.Any(d => d == null || d.mesh == null || d.material == null || d.collisionMesh == null))
            throw new InvalidOperationException("Every rock definition requires a render mesh, collision hull and material.");
        if (s.definitions.Any(d => d.collisionMesh.triangles.Length / 3 > 255 ||
            Mathf.Abs(Mathf.Max(d.mesh.bounds.size.x, d.mesh.bounds.size.y, d.mesh.bounds.size.z) - 1f) > 0.001f))
            throw new InvalidOperationException("Use metre-normalized render meshes and convex hulls with at most 255 triangles.");
        foreach (RockPopulation p in new[] { s.decoration, s.obstacles, s.samples })
            if (p == null || p.count < 0 || p.count > 100000 || !float.IsFinite(p.minimumDiameter) ||
                !float.IsFinite(p.maximumDiameter) || p.minimumDiameter <= 0 ||
                p.maximumDiameter < p.minimumDiameter || !float.IsFinite(p.cullDistance) || p.cullDistance <= 0)
                throw new InvalidOperationException("Invalid population counts, size thresholds or cull distances.");
        if (s.DecorationCount < 0 || s.ObstacleCount < 0 ||
            !float.IsFinite(s.decorationDensity) || !float.IsFinite(s.obstacleDensity) ||
            s.decorationDensity < 0 || s.obstacleDensity < 0)
            throw new InvalidOperationException("Densities must be finite and nonnegative.");
        if (s.starterSamples < 0 || s.studySites == null || s.exclusions == null ||
            s.studySites.Any(t => t == null) || s.starterSamples + s.studySites.Length > s.samples.count ||
            s.fieldScale < 2 || s.roverClearanceRadius < 0.5f || s.routeGridMeters < 2 ||
            s.minimumGap < 0 || s.spawnClearance < s.roverClearanceRadius ||
            s.terrain.gameObject.scene != s.gameObject.scene || s.rover.gameObject.scene != s.gameObject.scene)
            throw new InvalidOperationException("Invalid field/clearance/sample settings or references from another scene.");
        float[] finite = { s.fieldScale, s.roverClearanceRadius, s.settledRoverHeight, s.routeGridMeters,
            s.minimumGap, s.spawnClearance, s.maximumSlope, s.drivingSlopeLimit, s.clustering,
            s.burialFraction, s.maximumBurialFraction, s.groundAlignment, s.starterSampleRadius, s.candidateApproachRange };
        if (finite.Any(v => !float.IsFinite(v)) || s.settledRoverHeight <= 0 ||
            s.clustering < 0 || s.clustering > 1 || s.burialFraction < 0 || s.burialFraction > 0.9f ||
            s.maximumBurialFraction < s.burialFraction || s.maximumBurialFraction > 0.9f ||
            s.groundAlignment < 0 || s.groundAlignment > 1 || s.drivingSlopeLimit <= 0 ||
            s.drivingSlopeLimit > s.maximumSlope || s.maximumSlope > 50 || s.starterSampleRadius <= s.spawnClearance ||
            s.candidateApproachRange < 0.5f)
            throw new InvalidOperationException("Placement controls must be finite and within their Inspector ranges.");
        if (s.bakedRoot != null && (s.bakedRoot.GetComponent<BakedRockField>() == null ||
            s.bakedRoot.GetComponent<BakedRockField>().owner != s))
            throw new InvalidOperationException("Refusing to replace a hierarchy not owned by this scatterer.");
        if (!AssetDatabase.IsValidFolder("Assets/Models/Rocks/Prepared") ||
            (s.layoutPrefab != null && (!PrefabUtility.IsPartOfPrefabAsset(s.layoutPrefab) ||
                s.layoutPrefab.GetComponent<BakedRockField>() == null)))
            throw new InvalidOperationException("Prepare rock assets first; layoutPrefab must be a generated rock field prefab.");
    }

    [MenuItem("Tools/Mars Rocks/Bake Selected Field")]
    private static void BakeSelected()
    {
        RockScatterSettings settings = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<RockScatterSettings>() : null;
        Bake(settings);
    }

    public static BakedRockField Bake(RockScatterSettings settings)
    {
        Plan plan = CreatePlan(settings);
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Bake Mars rock field");
        var root = new GameObject("Baked Mars Rocks");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, settings.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(root, "Create rock field");
        try
        {
            var marker = root.AddComponent<BakedRockField>();
            marker.owner = settings;
            marker.seed = settings.seed;
            marker.layoutSha256 = plan.hash;
            var chunks = new Dictionary<Vector2Int, Transform>();
            int sampleIndex = 0;
            foreach (Placement rock in plan.rocks)
            {
                Vector2Int key = new Vector2Int(Mathf.FloorToInt(rock.position.x / 64f), Mathf.FloorToInt(rock.position.z / 64f));
                if (!chunks.TryGetValue(key, out Transform chunk))
                {
                    chunk = new GameObject($"Chunk {key.x},{key.y}").transform;
                    chunk.SetParent(root.transform, false);
                    chunks.Add(key, chunk);
                }
                RockDefinition definition = settings.definitions[rock.definition];
                var obj = new GameObject($"{rock.role} {definition.displayName}");
                obj.transform.SetParent(chunk, false);
                obj.transform.SetPositionAndRotation(rock.position, rock.rotation);
                obj.transform.localScale = Vector3.one * rock.diameter;
                obj.AddComponent<MeshFilter>().sharedMesh = definition.mesh;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = definition.material;
                renderer.shadowCastingMode = rock.role == RockRole.Decoration ? ShadowCastingMode.Off : ShadowCastingMode.On;
                var lod = obj.AddComponent<LODGroup>();
                float distance = rock.role == RockRole.Decoration ? settings.decoration.cullDistance :
                    rock.role == RockRole.Obstacle ? settings.obstacles.cullDistance : settings.samples.cullDistance;
                float relativeHeight = Mathf.Clamp(definition.mesh.bounds.size.y * rock.diameter * 0.866f / distance, 0.00001f, 0.5f);
                lod.SetLODs(new[] { new LOD(relativeHeight, new Renderer[] { renderer }) });
                lod.RecalculateBounds();
                if (rock.role != RockRole.Decoration)
                {
                    var collider = obj.AddComponent<MeshCollider>();
                    collider.sharedMesh = definition.collisionMesh;
                    collider.convex = true;
                }
                if (rock.role == RockRole.Sample)
                {
                    var sample = obj.AddComponent<RockSamplePlacement>();
                    sample.definition = definition;
                    sample.diameterMeters = rock.diameter;
                    sample.approachPosition = rock.approach;
                    sample.placementId = $"mars-{settings.seed}-{sampleIndex++:D4}";
                    marker.sampleCount++;
                }
                else if (rock.role == RockRole.Obstacle) marker.obstacleCount++;
                else marker.decorativeCount++;
            }
            // Keep thousands of generated objects out of the hand-authored scene diff.
            // A prefab cannot store scene-object references: owner is an instance override.
            const string folder = "Assets/Models/Rocks/Prepared";
            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Prepare the rock assets before baking.");
            string prefabPath = settings.layoutPrefab != null ? AssetDatabase.GetAssetPath(settings.layoutPrefab) :
                AssetDatabase.GenerateUniqueAssetPath(folder + "/BakedMarsRocks.prefab");
            marker.owner = null;
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath,
                InteractionMode.AutomatedAction, out bool saved);
            marker.owner = settings;
            if (!saved) throw new InvalidOperationException("Could not save the generated rock layout prefab.");
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
            Undo.RecordObject(settings, "Link baked field");
            if (settings.bakedRoot != null) Undo.DestroyObjectImmediate(settings.bakedRoot.gameObject);
            settings.bakedRoot = root.transform;
            settings.layoutPrefab = prefab;
            EditorUtility.SetDirty(settings);
            EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
            Undo.CollapseUndoOperations(undo);
            Debug.Log($"Baked {plan.rocks.Count} rocks; {marker.sampleCount} reachable samples; layout {plan.hash}", settings);
            return marker;
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(root);
            throw;
        }
    }
}
