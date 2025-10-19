using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MeshBuilder : MonoBehaviour
{

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public int resolution, seed;

    public bool lowPoly, forceEdge;

    public int hillHeight = 32;
    public float hillDensity = 1.2f;
    public int FbOctaveCount = 3;
    public float FbOctaveDamping = 0.5f;
    public float borderEasing = 1f;

    [ContextMenu("Generate")]
    public void Generate()
    {
        resolution = 1 + resolution - (resolution % 2);
        float[,] flts = NoiseMapBuilder.Export(resolution, seed,
            hillHeight, hillDensity, FbOctaveCount, 0.05f, FbOctaveDamping);

        ApplyMesh(CreateFromNoiseGrid(
            flts, 256f / resolution, lowPoly, forceEdge));
    }


    private void ApplyMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }


    // Builds a hexagonal plane out of a given square array.
    // - The borders of the hexagon will be at a standardized height.
    // - Areas near said border will be biased towards that standard height.
    // Math for the hexagon midpoints: https://www.desmos.com/calculator/mc0lxgyfno 
    // Hexagon will be triangulated like in: https://i.sstatic.net/CGBYv.jpg
    // - Rows of the array are rows of the vertices in the above image.
    // - The tail of each row (sans the middle one) will be ignored...
    // - i.e. a 5x5 array is a 5-row hexagon; ignore the last 2 of row 1.
    private Mesh CreateFromNoiseGrid(
        float[,] grid, float unitSize, bool lowPoly, bool forceEdge)
    {
        if (grid.GetLength(0) != grid.GetLength(1)
            || grid.GetLength(0) % 2 == 0)
            throw new Exception("Invalid input grid!");

        // Build verts
        int n = grid.GetLength(0);
        float hSq3 = (float)Math.Sqrt(3.0f) / 2.0f;
        int topLen = n - (n / 2); // ceil division
        List<Vector3> tempVerts = new();
        int vCount = 0;
        int[,] tempRef = new int[n, n];
        for (int row = 0; row < n; row++)
        {
            int rowLen = n - Math.Abs(row - (n / 2));
            float offX = rowLen - topLen;
            float offZ = row * hSq3;
            for (int pt = 0; pt < rowLen; pt++)
            {
                // distance to border
                int distToEdge = Math.Min(
                    Math.Min(pt, rowLen - 1 - pt),
                    Math.Min(row, n - 1 - row));
                // ease towards max height near border
                tempVerts.Add(new Vector3(
                    unitSize * ((-0.5f * offX) + pt),
                    (forceEdge ? HexEdgeLerp(grid[row, pt], distToEdge) 
                    : grid[row, pt]) - hillHeight,
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
            int rowLen = n - Math.Abs(row - (n / 2));
            int offU = (row >= (n / 2)) ? 0 : 1;
            int offD = (row > (n / 2)) ? 1 : 0;
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


    private float HexEdgeLerp(float height, int distFromEdge)
    {
        float ease = 1f / ((borderEasing * distFromEdge) + 1);
        return height + (ease * (hillHeight - height));
    }

}
