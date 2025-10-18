using System;
using UnityEngine;

public class TerrainBuilder
{

    /*
     * This is an implementation of perlin noise used in old Unity
     * project I found on my GitHub. Some changes have been made
     * so it makes floating point values (as wanted by the MeshBuilder).
     * 
     * Regardless most of this is copy-pasted and I forget how it 
     * works exactly. Seems to work though.
     * 
     * IIRC its based off this paper:
     * https://rtouti.github.io/graphics/perlin-noise-algorithm
     */


    private const int PERM_LEN = 256;

    private static readonly Vector2[] CON_VECS =
    { new(1f, 1f), new(-1f, 1f), new(-1f, -1f), new(1f, -1f) };

    private readonly float[,] _map;
    private readonly int _size;
    private readonly int[] _p;

    public delegate void Construct(TerrainBuilder constructor);

    public static float[,] Export(int size, int seed, Construct construct)
    {
        TerrainBuilder c = new(size, seed);
        construct(c);
        return c._map;
    }

    private TerrainBuilder(int size, int seed)
    {
        _p = new int[PERM_LEN * 2];
        for (int i = 0; i < PERM_LEN; i++) _p[i] = i;
        Randomize(_p, seed);
        for (int i = 0; i < PERM_LEN; i++) _p[i + PERM_LEN] = _p[i];

        _size = size;
        _map = new float[size, size];
        _map = new float[size, size];
    }

    private static void Randomize(int[] array, int seed)
    {
        System.Random rng = new(seed);
        for (int i = array.Length - 1; i > 0; i--)
        {
            int sel = (int)Math.Round(rng.NextDouble() * (i - 1));
            (array[sel], array[i]) = (array[i], array[sel]);
        }
    }

    private static float Ease(float t)
        => ((6 * t - 15) * t + 10) * t * t * t;

    private static float Lerp(float a, float b, float t)
        => a + t * (b - a);

    private float Noise(float x, float y)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        float xf = x - (float)Math.Floor(x);
        float yf = y - (float)Math.Floor(y);

        float dNE = Vector2.Dot(
            new(xf - 1f, yf - 1f),
            CON_VECS[_p[_p[xi + 1] + yi + 1] & 3]);
        float dNW = Vector2.Dot(
            new(xf, yf - 1f),
            CON_VECS[_p[_p[xi] + yi + 1] & 3]);
        float dSE = Vector2.Dot(
            new(xf - 1f, yf),
            CON_VECS[_p[_p[xi + 1] + yi] & 3]);
        float dSW = Vector2.Dot(
            new(xf, yf),
            CON_VECS[_p[_p[xi] + yi] & 3]);

        return Lerp(
            Lerp(dSW, dNW, Ease(yf)),
            Lerp(dSE, dNE, Ease(yf)),
            Ease(xf));
    }

    private float FractalBrownian(float x, float y,
        int octaves, float frequency, float damping)
    {
        float result = 0f;
        float amp = 1f;

        for (int oct = 0; oct < octaves; oct++)
        {
            result += amp * Noise(x * frequency, y * frequency);
            amp *= damping;
            frequency /= damping;
        }

        return result;
    }

    public TerrainBuilder BuildTopography(
            int elevationLayers, float genDensity,
            int fbOctaves, float fbFrequency, float fbDamping)
    {
        if (fbOctaves < 1 || fbFrequency < 0f
            || fbDamping < 0f || fbDamping > 1f)
            throw new ArgumentException("Invalid Topography!");

        for (int x = 0; x < _size; x++)
            for (int y = 0; y < _size; y++)
            {
                float val = FractalBrownian(
                    x * genDensity, y * genDensity,
                    fbOctaves, fbFrequency, fbDamping);

                val = (val + 1f) / 2f;
                _map[x, y] = val * elevationLayers;
            }

        return this;
    }

}
