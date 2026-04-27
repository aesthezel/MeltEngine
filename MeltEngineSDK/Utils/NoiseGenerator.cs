using System;

namespace MeltEngine.Utils;

public class NoiseGenerator
{
    private readonly int[] _permutation;
    private readonly int _seed;
    private const int Size = 256;

    public NoiseGenerator(int seed)
    {
        _seed = seed;
        _permutation = new int[Size * 2];
        var random = new Random(seed);
        var p = new int[Size];
        for (int i = 0; i < Size; i++) p[i] = i;
        for (int i = Size - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (int i = 0; i < Size * 2; i++) _permutation[i] = p[i % Size];
    }

    private static readonly (float X, float Y)[] Gradients =
    {
        (1, 1), (-1, 1), (1, -1), (-1, -1),
        (1, 0), (-1, 0), (0, 1), (0, -1)
    };

    private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private float Lerp(float a, float b, float t) => a + t * (b - a);
    private int Hash(int x, int y, int z) => _permutation[(_permutation[(x & 255) + _permutation[(y & 255)]] + z) & 255];

    private float DotProduct(int hash, float x, float y, float z)
    {
        var g = Gradients[hash & 7];
        return g.X * x + g.Y * y;
    }

    public float Noise2D(float x, float y)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        float xf = x - (float)Math.Floor(x);
        float yf = y - (float)Math.Floor(y);
        float u = Fade(xf);
        float v = Fade(yf);

        int aa = Hash(xi, yi, 0);
        int ab = Hash(xi, yi + 1, 0);
        int ba = Hash(xi + 1, yi, 0);
        int bb = Hash(xi + 1, yi + 1, 0);

        float x1 = Lerp(DotProduct(aa, xf, yf, 0), DotProduct(ba, xf - 1, yf, 0), u);
        float x2 = Lerp(DotProduct(ab, xf, yf - 1, 0), DotProduct(bb, xf - 1, yf - 1, 0), u);

        return (Lerp(x1, x2, v) + 1) / 2;
    }

    public float Noise3D(float x, float y, float z)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        int zi = (int)Math.Floor(z) & 255;
        float xf = x - (float)Math.Floor(x);
        float yf = y - (float)Math.Floor(y);
        float zf = z - (float)Math.Floor(z);
        float u = Fade(xf);
        float v = Fade(yf);
        float w = Fade(zf);

        int aaa = Hash(xi, yi, zi);
        int aba = Hash(xi, yi + 1, zi);
        int aab = Hash(xi, yi, zi + 1);
        int abb = Hash(xi, yi + 1, zi + 1);
        int baa = Hash(xi + 1, yi, zi);
        int bba = Hash(xi + 1, yi + 1, zi);
        int bab = Hash(xi + 1, yi, zi + 1);
        int bbb = Hash(xi + 1, yi + 1, zi + 1);

        float x1 = Lerp(DotProduct3D(aaa, xf, yf, zf), DotProduct3D(baa, xf - 1, yf, zf), u);
        float x2 = Lerp(DotProduct3D(aba, xf, yf - 1, zf), DotProduct3D(bba, xf - 1, yf - 1, zf), u);
        float y1 = Lerp(x1, x2, v);

        x1 = Lerp(DotProduct3D(aab, xf, yf, zf - 1), DotProduct3D(bab, xf - 1, yf, zf - 1), u);
        x2 = Lerp(DotProduct3D(abb, xf, yf - 1, zf - 1), DotProduct3D(bbb, xf - 1, yf - 1, zf - 1), u);
        float y2 = Lerp(x1, x2, v);

        return (Lerp(y1, y2, w) + 1) / 2;
    }

    private static readonly (float X, float Y, float Z)[] Gradients3D =
    {
        (1, 1, 0), (-1, 1, 0), (1, -1, 0), (-1, -1, 0),
        (1, 0, 1), (-1, 0, 1), (1, 0, -1), (-1, 0, -1),
        (0, 1, 1), (0, -1, 1), (0, 1, -1), (0, -1, -1)
    };

    private float DotProduct3D(int hash, float x, float y, float z)
    {
        var g = Gradients3D[hash % 12];
        return g.X * x + g.Y * y + g.Z * z;
    }

    public float OctaveNoise2D(float x, float y, int octaves, float persistence = 0.5f)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    public float FractalNoise2D(float x, float y, int octaves, float lacunarity = 2.0f, float persistence = 0.5f)
    {
        float value = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            value += Noise2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value / maxValue;
    }
}
