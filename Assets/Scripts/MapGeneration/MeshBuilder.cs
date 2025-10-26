using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WorldUtil;

public class MeshBuilder : MonoBehaviour
{

    public MeshFilter landFilter;
    public MeshFilter waterFilter;
    public MeshCollider landCollider;
    public MeshCollider waterCollider;

    public int resolution, scale, seed;

    public bool lowPoly, forceHills;

    public int hillHeight = 32;
    public float hillDensity = 1.2f;
    public int FbOctaveCount = 3;
    public float FbOctaveDamping = 0.5f;
    public float borderEasing = 0.1f;
    public float riverEasing = 10f;


    [ContextMenu("Generate")]
    public void Generate()
        => GenerateWithRiverNodes(HexSide.NULL, HexSide.NULL);


    public void GenerateWithRiverNodes(HexSide side1, HexSide side2)
    {
        resolution = 1 + resolution - (resolution % 2);
        float[,] l = NoiseMap.Export(resolution, seed,
            hillHeight, hillDensity, FbOctaveCount, 0.05f, FbOctaveDamping);
        float[,] w = NoiseMap.Export(resolution, 0, 0, 0f, 1, 0f, 0f);

        ApplyMeshes(
            CreateFromNoiseGrid(l, scale, lowPoly, forceHills, side1, side2),
            CreateFromNoiseGrid(w, scale, lowPoly, false)
        );
    }


    private void ApplyMeshes(Mesh land, Mesh water)
    {
        landFilter.mesh = land;
        waterFilter.mesh = water;
        landCollider.sharedMesh = land;
        waterCollider.sharedMesh = water;
    }


    // Builds a hexagonal plane out of a given square array.
    // - The borders of the hexagon will be at a standardized (high) height.
    // - The rivers of the hexagon will be at a standardized (low) height.
    // - Areas near borders/rivers will be biased towards their height.
    // Math for the hexagon midpoints: https://www.desmos.com/calculator/mc0lxgyfno 
    // Hexagon will be triangulated like in: https://i.sstatic.net/CGBYv.jpg
    // - Rows of the array are rows of the vertices in the above image.
    // - The tail of each row (sans the middle one) will be ignored...
    // - i.e. a 5x5 array is a 5-row hexagon; ignore the last 2 of row 1.
    private Mesh CreateFromNoiseGrid(
        float[,] grid, float unitSize, bool lowPoly, bool forceEdge,
        HexSide riverIn, HexSide riverOut)
    {
        if (grid.GetLength(0) != grid.GetLength(1)
            || grid.GetLength(0) % 2 == 0)
            throw new Exception("Invalid input grid!");

        // River options
        int n = grid.GetLength(0);
        float trueWidth = (resolution - 1) * scale;
        bool hasRiverIn = riverIn != HexSide.NULL;
        bool hasRiverOut  = riverOut != HexSide.NULL;
        bool hasRiver = hasRiverIn || hasRiverOut;
        bool mount = forceEdge && !hasRiver;
        Vector2 rInPos = Geometry.EquivHexPos(riverIn, trueWidth);
        Vector2 rOutPos = Geometry.EquivHexPos(riverOut, trueWidth);
        Vector2 cntPos = Geometry.EquivHexPos(HexSide.NULL, trueWidth);
        System.Random rng = new();

        // Build verts
        int fn = n / 2; // floor division
        float hSq3 = (float)Math.Sqrt(3.0f) / 2.0f;
        int topLen = n - fn; // ceil division
        List<Vector3> tempVerts = new();
        int vCount = 0;
        int[,] tempRef = new int[n, n];
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
                // terrain height adjusted to border/river bias
                float biasedAlt = grid[row, pt];
                // tiles without rivers are hills
                if (mount) biasedAlt *= 4f;
                // distance to border
                int distToEdge = Math.Min(
                    Math.Min(pt, rowLen - 1 - pt),
                    Math.Min(row, n - 1 - row));
                // hill tiles follow limited edge lerp
                if (forceEdge) biasedAlt = HexEdgeLerp(biasedAlt, distToEdge);
                // distance to river 
                if (hasRiver)
                {
                    float distToRiv = Math.Min(
                        Geometry.DistanceToRiver(cntPos.x, cntPos.y,
                        rOutPos.x, rOutPos.y, vx, vy),
                        Geometry.DistanceToRiver(rInPos.x, rInPos.y,
                        cntPos.x, cntPos.y, vx, vy)
                        );
                    // border verts will be less biased to rivers
                    if ((distToEdge == 0 && distToRiv < n / 16f)
                        || (distToEdge != 0 && distToRiv < n / 8f))
                        biasedAlt = NearRiverLerp(
                            biasedAlt, distToRiv);
                }
                // add to vertex list
                float vh = unitSize * biasedAlt - hillHeight;
                tempVerts.Add(new Vector3(vx, vh, vy));
                // track which list element matches the grid location
                tempRef[row, pt] = vCount++;
            }
        }

        // Duplicate vertices
        // - 6 copies of each vert; one per connected triangle
        // - Edge verts will have excess but thats fine
        int[][,] gridRef = new int[6][,];
        List<Vector3> verts = new();
        for (int i = 0; i < 6; i++)
        {
            gridRef[i] = (int[,])tempRef.Clone();
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
        List<int> tris = new();
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
                    tris.Add(CollectVertex(row, pt));
                    tris.Add(CollectVertex(row + 1, pt + offU));
                    tris.Add(CollectVertex(row, pt + 1));
                }
                // downward-tri with this point as the left vert 
                if (row > 0)
                {
                    tris.Add(CollectVertex(row, pt));
                    tris.Add(CollectVertex(row, pt + 1));
                    tris.Add(CollectVertex(row - 1, pt + offD));
                }
            }
        }

        // Build mesh and export
        Mesh mesh = new()
        {
            vertices = verts.ToArray(),
            triangles = tris.ToArray()
        };
        mesh.RecalculateNormals();
        return mesh;
    }


    private Mesh CreateFromNoiseGrid(
        float[,] grid, float unitSize, bool lowPoly, bool forceEdge)
        => CreateFromNoiseGrid(grid, unitSize, lowPoly, forceEdge,
            HexSide.NULL, HexSide.NULL);


    private float HexEdgeLerp(float height, int distFromEdge)
    {
        float t = 1f / ((borderEasing * distFromEdge * distFromEdge) + 1);
        return height + (t * (hillHeight - height));
    }

    private float NearRiverLerp(float height, float distFromRiver)
    {
        float t = 1f / ((riverEasing * distFromRiver * distFromRiver) + 1);
        return height + (t * (0 - height));
    }

}
