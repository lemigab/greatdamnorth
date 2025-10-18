using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class MeshBuilder : MonoBehaviour
{

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public int size, seed;

    public int layers;
    public int octaves;
    public float density;
    public float frequency;
    public float damping;

    [ContextMenu("Generate")]
    public void Generate()
    {
        float[,] flts = TerrainBuilder.Export(size, seed, 
            c => c.BuildTopography(layers, density, octaves, frequency, damping));

        ApplyMesh(CreateFromHexGrid(flts, 0.3f));
    }


    private void ApplyMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }


    // Builds a hexagonal plane out of a given square array.
    // Math for the hexagon midpoints: https://www.desmos.com/calculator/mc0lxgyfno 
    // Hexagon will be triangulated like in: https://i.sstatic.net/CGBYv.jpg
    // - Rows of the array are rows of the vertices in the above image.
    // - The tail of each row (sans the middle one) will be ignored...
    // - i.e. a 5x5 array is a 5-row hexagon; ignore the last 2 of row 1.
    public static Mesh CreateFromHexGrid(float[,] grid, float unitSize)
    {
        if (grid.GetLength(0) != grid.GetLength(1)
            || grid.GetLength(0) % 2 == 0)
            throw new Exception("Invalid input grid!");

        // Build verts
        int n = grid.GetLength(0);
        float hSq3 = (float)Math.Sqrt(3.0f) / 2.0f;
        int topLen = n - (n / 2); // ceil division
        List<Vector3> verts = new();
        int vCount = 0;
        int[,] gridRef = new int[n, n];
        for (int row = 0; row < n; row++)
        {
            int rowLen = n - Math.Abs(row - (n / 2));
            float offX = rowLen - topLen;
            float offZ = row * hSq3;
            for (int pt = 0; pt < rowLen; pt++)
            {
                verts.Add(new Vector3(
                    unitSize * ((-0.5f * offX) + pt),
                    grid[row, pt],
                    unitSize * offZ
                ));
                // track which list element matches the grid location
                gridRef[row, pt] = vCount++;
            }
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
                    tris.Add(gridRef[row, pt]);
                    tris.Add(gridRef[row + 1, pt + offU]);
                    tris.Add(gridRef[row, pt + 1]);
                }
                // downward-tri with this point as the left vert 
                if (row > 0)
                {
                    tris.Add(gridRef[row, pt]);
                    tris.Add(gridRef[row, pt + 1]);
                    tris.Add(gridRef[row - 1, pt + offD]);
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

}
