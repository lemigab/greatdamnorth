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

    public bool lowPoly, forceEdge;

    public int hillHeight = 32;
    public float hillDensity = 1.2f;
    public int FbOctaveCount = 3;
    public float FbOctaveDamping = 0.5f;
    public float borderEasing = 1f;




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
            CreateFromNoiseGrid(l, scale, lowPoly, forceEdge, side1, side2),
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
        if ((riverIn == HexSide.NULL && riverOut != HexSide.NULL)
            || (riverIn != HexSide.NULL && riverOut == HexSide.NULL))
            throw new Exception("Invalid river setup!");

        // River options
        int n = grid.GetLength(0);
        bool hasRiver = riverIn != HexSide.NULL;
        Vector2Int rInPos = EquivHexPos(riverIn, n);
        Vector2Int rOutPos = EquivHexPos(riverOut, n);

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
            float offZ = row * hSq3;
            for (int pt = 0; pt < rowLen; pt++)
            {
                // distance to border
                int distToEdge = Math.Min(
                    Math.Min(pt, rowLen - 1 - pt),
                    Math.Min(row, n - 1 - row));
                // distance to river 
                float distToRiv = !hasRiver ? 0f : DistanceToRiver(
                    rInPos.x, rInPos.y, rOutPos.x, rOutPos.y, row, pt);
                // terrain height adjusted to border/river bias
                float biasedAlt = grid[row, pt];
                if (forceEdge)
                {
                    biasedAlt = HexEdgeLerp(biasedAlt, distToEdge);
                    biasedAlt = NearRiverLerp(biasedAlt, distToRiv);
                }
                // add to vertex list
                tempVerts.Add(new Vector3(
                    unitSize * ((-0.5f * offX) + pt),
                    unitSize * biasedAlt - hillHeight, // keeps border at y=0
                    unitSize * offZ
                ));
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
        float ease = 1f / ((borderEasing * distFromEdge) + 1);
        return height + (ease * (hillHeight - height));
    }

    private float NearRiverLerp(float height, float distFromRiver)
    {
        float ease = 1f / ((borderEasing * distFromRiver) + 1);
        return ease * height; // lerp between 0 and target height
    }

    // Distance of {x0,y0} from the line going through p,q
    private float DistanceToRiver(
        float px, float py, float qx, float qy,
        float x0, float y0)
    {
        float prll = Math.Abs(
            (py - qy) * x0 - (px - qx) * y0 + (px * qy) - (py * qx));
        float len = (float)Math.Sqrt(
            (py - qy) * (py - qy) + (px - qx) * (px - qx));
        return prll / len;
    }

    // Equivalent hex mesh positions of a river node
    // w is the number of vertex rows in the hex
    private Vector2Int EquivHexPos(HexSide side, int w)
    {
        int x = side switch
        {
            HexSide.N or HexSide.S => w / 2,
            HexSide.NW or HexSide.SW => 0,
            HexSide.NE or HexSide.SE => w - 1,
            _ => 0,
        };
        int y = side switch
        {
            HexSide.N => 0,
            HexSide.NW or HexSide.NE => (int)((w / 2) * 1.5f),
            HexSide.S => w - 1,
            HexSide.SW or HexSide.SE => (int)((w / 2) * 0.5f),
            _ => 0,
        };
        return new(x, y);
    }

}
