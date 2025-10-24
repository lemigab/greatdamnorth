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
        Vector2 rInPos = EquivHexPos(riverIn);
        Vector2 rOutPos = EquivHexPos(riverOut);
        Vector2 cntPos = EquivHexPos(HexSide.NULL);
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
            float offZ = row * hSq3;
            for (int pt = 0; pt < rowLen; pt++)
            {
                // terrain height adjusted to border/river bias
                float biasedAlt = grid[row, pt];
                if (forceEdge)
                {
                    // distance to border
                    int distToEdge = Math.Min(
                        Math.Min(pt, rowLen - 1 - pt),
                        Math.Min(row, n - 1 - row));
                    biasedAlt = HexEdgeLerp(biasedAlt, distToEdge);
                    // distance to river 
                    if (hasRiver)
                    {
                        float distToRiv = Math.Min(
                            DistanceToRiver(cntPos.x, cntPos.y,
                            rOutPos.x, rOutPos.y, unitSize * ((-0.5f * offX) + pt), unitSize * offZ),

                            DistanceToRiver(rInPos.x, rInPos.y,
                            cntPos.x, cntPos.y, unitSize * ((-0.5f * offX) + pt), unitSize * offZ)
                            );
                        if (distToRiv < n / 16f)
                            biasedAlt = NearRiverLerp(biasedAlt, distToRiv);
                    }
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
        float ease = 1f / ((riverEasing * distFromRiver) + 1);
        return height + (ease * (0 - height));
    }

    // Distance of {x0,y0} from the line going through p,q
    // Shamelessly stolen from StackOverflow
    private float DistanceToRiver(
        float px, float py, float qx, float qy,
        float x0, float y0)
    {
        float A = x0 - px;
        float B = y0 - py;
        float C = qx - px;
        float D = qy - py;

        float dot = A * C + B * D;
        float len_sq = C * C + D * D;
        float param = -1;
        if (len_sq != 0) param = dot / len_sq;

        float xx, yy;

        if (param < 0) { xx = px; yy = py; }
        else if (param > 1) { xx = qx; yy = qy; }
        else { xx = px + param * C; yy = py + param * D; }

        float dx = x0 - xx;
        float dy = y0 - yy;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    // Equivalent hex mesh positions of a river node
    // w is the number of vertex rows in the hex
    private Vector2 EquivHexPos(HexSide side)
    {
        float w = (resolution - 1) * scale;
        float w8 = w / 8f;
        float sq3 = (float)Math.Sqrt(3.0f);
        float h = sq3 * w / 2f;
        float xOff = w / 4f;
        return side switch
        {
            HexSide.N => new(w / 2f - xOff, h),
            HexSide.NW => new(w8 - xOff, h / 4f * 3f),
            HexSide.NE => new(w - w8 - xOff, h / 4f * 3f),
            HexSide.S => new(w / 2f - xOff, 0f),
            HexSide.SE => new(w - w8 - xOff, h / 4f),
            HexSide.SW => new(w8 - xOff, h / 4f),
            _ => new(w / 2f - xOff, h / 2f)
        };
    }

}
