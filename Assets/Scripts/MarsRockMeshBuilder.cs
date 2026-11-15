using System.Collections.Generic;
using UnityEngine;

public static class MarsRockMeshBuilder
{
    private const float GoldenRatio = 1.61803398875f;

    /// <summary>
    /// Generates a flat-shaded, displaced icosahedron mesh resembling a Martian basalt rock.
    /// </summary>
    public static Mesh CreateRockMesh(int seed, int subdivisions = 2, float roughness = 0.32f, float squashY = 0.72f)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        BuildIcosahedron(vertices, triangles);

        for (int s = 0; s < subdivisions; s++)
        {
            Subdivide(vertices, triangles);
        }

        System.Random rng = new System.Random(seed);
        Vector3 noiseOffset = new Vector3(
            (float)rng.NextDouble() * 500f,
            (float)rng.NextDouble() * 500f,
            (float)rng.NextDouble() * 500f);

        Vector3 axisScale = new Vector3(
            Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble()),
            squashY * Mathf.Lerp(0.8f, 1.15f, (float)rng.NextDouble()),
            Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble()));

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 dir = vertices[i].normalized;
            float n1 = Mathf.PerlinNoise((dir.x + noiseOffset.x) * 1.6f, (dir.z + noiseOffset.z) * 1.6f);
            float n2 = Mathf.PerlinNoise((dir.y + noiseOffset.y) * 2.8f, (dir.x + dir.z + noiseOffset.x) * 2.8f);
            float displacement = 1f + ((n1 * 0.65f + n2 * 0.35f) - 0.5f) * 2f * roughness;
            vertices[i] = Vector3.Scale(dir * (0.5f * displacement), axisScale);
        }

        Mesh mesh = BuildFlatShadedMesh(vertices, triangles);
        mesh.name = $"MarsRock_{seed}";
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildFlatShadedMesh(List<Vector3> sharedVerts, List<int> sharedTris)
    {
        Vector3[] flatVerts = new Vector3[sharedTris.Count];
        int[] flatTris = new int[sharedTris.Count];

        for (int i = 0; i < sharedTris.Count; i++)
        {
            flatVerts[i] = sharedVerts[sharedTris[i]];
            flatTris[i] = i;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = flatVerts;
        mesh.triangles = flatTris;
        return mesh;
    }

    private static void BuildIcosahedron(List<Vector3> verts, List<int> tris)
    {
        float t = GoldenRatio;
        verts.AddRange(new[]
        {
            new Vector3(-1,  t,  0).normalized, new Vector3( 1,  t,  0).normalized,
            new Vector3(-1, -t,  0).normalized, new Vector3( 1, -t,  0).normalized,
            new Vector3( 0, -1,  t).normalized, new Vector3( 0,  1,  t).normalized,
            new Vector3( 0, -1, -t).normalized, new Vector3( 0,  1, -t).normalized,
            new Vector3( t,  0, -1).normalized, new Vector3( t,  0,  1).normalized,
            new Vector3(-t,  0, -1).normalized, new Vector3(-t,  0,  1).normalized
        });

        tris.AddRange(new[]
        {
            0,11,5,  0,5,1,   0,1,7,   0,7,10,  0,10,11,
            1,5,9,   5,11,4,  11,10,2, 10,7,6,  7,1,8,
            3,9,4,   3,4,2,   3,2,6,   3,6,8,   3,8,9,
            4,9,5,   2,4,11,  6,2,10,  8,6,7,   9,8,1
        });
    }

    private static void Subdivide(List<Vector3> verts, List<int> tris)
    {
        Dictionary<long, int> midpoints = new Dictionary<long, int>();
        List<int> nextTris = new List<int>(tris.Count * 4);

        for (int i = 0; i < tris.Count; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            int ab = GetMidpoint(a, b, verts, midpoints);
            int bc = GetMidpoint(b, c, verts, midpoints);
            int ca = GetMidpoint(c, a, verts, midpoints);

            nextTris.AddRange(new[]
            {
                a, ab, ca,
                b, bc, ab,
                c, ca, bc,
                ab, bc, ca
            });
        }

        tris.Clear();
        tris.AddRange(nextTris);
    }

    private static int GetMidpoint(int i1, int i2, List<Vector3> verts, Dictionary<long, int> cache)
    {
        long min = Mathf.Min(i1, i2);
        long max = Mathf.Max(i1, i2);
        long key = (min << 32) + max;

        if (cache.TryGetValue(key, out int idx))
        {
            return idx;
        }

        Vector3 mid = ((verts[i1] + verts[i2]) * 0.5f).normalized;
        idx = verts.Count;
        verts.Add(mid);
        cache[key] = idx;
        return idx;
    }
}
