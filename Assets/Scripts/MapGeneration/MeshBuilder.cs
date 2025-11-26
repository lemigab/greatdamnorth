using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using WorldUtil;
using G = WorldUtil.Geometry;

public class MeshBuilder : MonoBehaviour
{

    public MeshFilter landFilter;
    public MeshFilter waterFilter;
    public MeshCollider landCollider;
    public MeshCollider waterCollider;
    public MeshRenderer landRenderer;

    public Material flatLand, hillLand, mountainLand;

    public int resolution, scale, seed;

    public bool lowPoly, forceHills;

    public int hillHeight = 32;
    public float hillDensity = 1.2f;
    public int FbOctaveCount = 3;
    public float FbOctaveDamping = 0.5f;
    public float borderEasing = 0.1f;
    public float riverEasing = 10f;
    public float pathWidth = 0.5f;


    [ContextMenu("Generate")]
    public Tuple<Mesh, Mesh> Generate()
        => GenerateWithFeatures(false, HexSide.N, HexSide.SE, HexSide.S);


    public Tuple<Mesh, Mesh> GenerateWithFeatures(bool forceUpward,
        HexSide river1, HexSide river2, params HexSide[] roads)
    {
        resolution = 1 + resolution - (resolution % 2);
        float[,] l = NoiseMap.Export(resolution, seed,
            hillHeight, hillDensity, FbOctaveCount, 0.05f, FbOctaveDamping);
        float[,] w = NoiseMap.Export(resolution, 0, 0, 0f, 1, 0f, 0f);

        Mesh lMesh = CreateFromNoiseGrid(
            l, scale, lowPoly, forceHills,
            river1, river2, roads, forceUpward, true);
        Mesh wMesh = CreateFromNoiseGrid(
            w, scale, lowPoly, false);

        ApplyMeshes(lMesh, wMesh, LandMaterialFor(river1, river2, forceUpward));
        return new(lMesh, wMesh);
    }


    private void ApplyMeshes(Mesh land, Mesh water, Material landMat)
    {
        landFilter.mesh = land;
        waterFilter.mesh = water;
        landCollider.sharedMesh = land;
        waterCollider.sharedMesh = water;
        landRenderer.material = landMat;
    }


    // Builds a hexagonal plane out of a given square array.
    // - The borders of the hexagon will be at a standardized (high) height.
    // - The rivers of the hexagon will be at a standardized (low) height.
    // - Areas near borders/rivers will be biased towards their height.
    // - Roads are areas with a unique material index; lines from centre->edge
    // Math for the hexagon midpoints: https://www.desmos.com/calculator/mc0lxgyfno 
    // Hexagon will be triangulated like in: https://i.sstatic.net/CGBYv.jpg
    // - Rows of the array are rows of the vertices in the above image.
    // - The tail of each row (sans the middle one) will be ignored...
    // - i.e. a 5x5 array is a 5-row hexagon; ignore the last 2 of row 1.
    private Mesh CreateFromNoiseGrid(
        float[,] grid, float unitSize, bool lowPoly, bool forceEdge,
        HexSide riverIn, HexSide riverOut, HexSide[] roads,
        bool upward, bool settle)
    {
        if (grid.GetLength(0) != grid.GetLength(1)
            || grid.GetLength(0) % 2 == 0)
            throw new Exception("Invalid input grid!");

        // River options
        int n = grid.GetLength(0);
        float trueWidth = (resolution - 1) * scale;
        bool hasRiverIn = riverIn != HexSide.NULL;
        bool hasRiverOut = riverOut != HexSide.NULL;
        bool hasRiver = hasRiverIn || hasRiverOut;
        bool mount = forceEdge && !hasRiver;
        Vector2 rInPos = G.EquivHexPos(riverIn, trueWidth);
        Vector2 rOutPos = G.EquivHexPos(riverOut, trueWidth);
        Vector2 cntPos = G.EquivHexPos(HexSide.NULL, trueWidth);

        // Road vectors
        System.Random rng = new(seed);
        bool hasRoads = roads.Length > 0;
        Vector2[] rPs = new Vector2[roads.Length];
        for (int i = 0; i < roads.Length; i++)
            rPs[i] = G.EquivHexPos(roads[i], trueWidth);

        // Build verts
        int fn = n / 2; // floor division
        float hSq3 = (float)Math.Sqrt(3.0f) / 2.0f;
        int topLen = n - fn; // ceil division
        List<Vector3> tempVerts = new();
        int vCount = 0;
        int[,] tempGridRef = new int[n, n];
        bool[,] roadRef = new bool[n, n]; // true for (x,y) verts on a road
        for (int row = 0; row < n; row++)
        {
            int rowLen = n - Math.Abs(row - fn);
            float offX = rowLen - topLen;
            float offY = row * hSq3;
            for (int pt = 0; pt < rowLen; pt++)
            {
                // target true x,y coords
                float vx = unitSize * ((-0.5f * offX) + pt);
                float vy = unitSize * offY;
                Vector2 v = new(vx, vy);
                // terrain height adjusted to border/river bias
                float biasedAlt = grid[row, pt];
                // tiles without rivers are hills
                if (mount) biasedAlt *= 4f;
                // tiles with upward push have increased elevation
                else if (upward) biasedAlt *= 2f;
                // distance to border
                int distToEdge = Math.Min(
                    Math.Min(pt, rowLen - 1 - pt),
                    Math.Min(row, n - 1 - row));
                // hill tiles follow limited edge lerp
                if (forceEdge) biasedAlt = HexEdgeLerp(biasedAlt, distToEdge);
                // distance to river 
                if (hasRiver)
                {
                    float distToRiv = G.DistFromAny(v, cntPos, rOutPos, rInPos);
                    // border verts will be less biased to rivers
                    // if upward push turned on, all bias is decreased
                    bool decBias = distToEdge == 0 || upward;
                    if ((decBias && distToRiv < n / 16f)
                        || (!decBias && distToRiv < n / 8f))
                        biasedAlt = NearRiverLerp(biasedAlt, distToRiv);
                }
                // save which vert positions are on a road
                float pW = G.Lerp(pathWidth, 0f, distToEdge / (float)fn);
                bool roadHere = hasRoads
                    && G.DistFromAny(v, cntPos, rPs) <= pW
                    && (float)rng.NextDouble() > (distToEdge / (float)fn);
                roadRef[row, pt] = roadHere;
                float roadOff = roadHere ? hillHeight * 0.01f : 0f;
                // add to vertex list
                float set = settle ? hillHeight : 0f;
                float vh = unitSize * biasedAlt - set + roadOff;
                tempVerts.Add(new Vector3(vx, vh, vy));
                // track which list element matches the grid location
                tempGridRef[row, pt] = vCount++;
            }
        }

        // Duplicate vertices
        // - 6 copies of each vert; one per connected triangle
        // - Edge verts will have excess but thats fine
        int[][,] gridRef = new int[6][,];
        List<Vector3> verts = new();
        for (int i = 0; i < 6; i++)
        {
            gridRef[i] = (int[,])tempGridRef.Clone();
            verts.AddRange(tempVerts);
        }

        // Force reads of ref to use a new copy of that vert
        int CollectVertex(int x, int y)
        {
            for (int i = 0; i < 6; i++)
            {
                if (gridRef[i][x, y] != -1)
                {
                    int found = gridRef[i][x, y];
                    if (lowPoly) gridRef[i][x, y] = -1;
                    return found + (vCount * i);
                }
            }
            throw new Exception("No suitable distinct vertex!");
        }

        // Build triangles
        List<int> baseTris = new();
        List<int> roadTris = new();
        for (int row = 0; row < n; row++)
        {
            int rowLen = n - Math.Abs(row - fn);
            int offU = (row >= fn) ? 0 : 1;
            int offD = (row > fn) ? 1 : 0;
            for (int pt = 0; pt < rowLen - 1; pt++)
            {
                // upward-tri with this point as the left vert 
                if (row < n - 1)
                {
                    // get correct submesh
                    bool rOnAny = roadRef[row, pt]
                        || roadRef[row + 1, pt + offU]
                        || roadRef[row, pt + 1];
                    List<int> t = rOnAny ? roadTris : baseTris;
                    // find vertices of this face
                    t.Add(CollectVertex(row, pt));
                    t.Add(CollectVertex(row + 1, pt + offU));
                    t.Add(CollectVertex(row, pt + 1));
                }
                // downward-tri with this point as the left vert 
                if (row > 0)
                {
                    // get correct submesh
                    bool rOnAny = roadRef[row, pt]
                        || roadRef[row, pt + 1]
                        || roadRef[row - 1, pt + offD];
                    List<int> t = rOnAny ? roadTris : baseTris;
                    // find vertices of this face
                    t.Add(CollectVertex(row, pt));
                    t.Add(CollectVertex(row, pt + 1));
                    t.Add(CollectVertex(row - 1, pt + offD));
                }
            }
        }

        // Build mesh and export
        Mesh mesh = new();
        mesh.SetVertices(verts);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(baseTris, 0);
        mesh.SetTriangles(roadTris, 1);
        mesh.RecalculateNormals();
        return mesh;
    }


    private Material LandMaterialFor(
        HexSide riverIn, HexSide riverOut, bool hill)
    {
        if (riverIn == HexSide.NULL && riverOut == HexSide.NULL)
            return mountainLand;
        else if (hill) return hillLand;
        return flatLand;
    }


    private Mesh CreateFromNoiseGrid(
        float[,] grid, float unitSize, bool lowPoly, bool forceEdge)
        => CreateFromNoiseGrid(grid, unitSize, lowPoly, forceEdge,
            HexSide.NULL, HexSide.NULL, new HexSide[] { }, false, false);


    private float HexEdgeLerp(float height, int distFromEdge)
    {
        float d = distFromEdge;
        float t = 1f / ((borderEasing * d * d) + 1);
        return height + (t * (hillHeight - height));
    }

    private float NearRiverLerp(float height, float distFromRiver)
    {
        float d = distFromRiver;
        float t = 1f / ((riverEasing * d * d) + 1);
        return height + (t * (0 - height));
    }

}
