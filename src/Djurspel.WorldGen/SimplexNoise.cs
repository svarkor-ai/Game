using System;

namespace Djurspel.WorldGen;

/// <summary>
/// Simple 2D Perlin/Simplex noise generator for terrain generation.
/// Uses a permutation table and interpolation for smooth, repeatable noise values.
/// </summary>
public class SimplexNoise
{
    private readonly int[] _perm;
    private const int GradX = 1;
    private const int GradY = 2;

    public SimplexNoise(int seed)
    {
        // Initialize permutation table
        _perm = new int[512];
        int[] p = new int[256];
        
        for (int i = 0; i < 256; i++)
            p[i] = i;
        
        // Shuffle using seed
        Random rng = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int temp = p[i];
            p[i] = p[j];
            p[j] = temp;
        }
        
        // Duplicate for overflow
        for (int i = 0; i < 512; i++)
            _perm[i] = p[i & 255];
    }

    /// <summary>Generate 2D noise at (x, y) coordinates. Returns value in roughly [-1, 1].</summary>
    public double GetNoise2D(double x, double y)
    {
        // Find grid cell
        int xi = FastFloor(x);
        int yi = FastFloor(y);
        
        // Find relative position in cell
        double xf = x - xi;
        double yf = y - yi;
        
        // Hash the cell corner
        int hash = _perm[_perm[xi & 255] + yi & 255] & 7;
        
        // Get gradient contribution
        double value = 0;
        switch (hash)
        {
            case 0: value += (xf + yf); break;
            case 1: value += (xf - yf); break;
            case 2: value += (-xf + yf); break;
            case 3: value += (-xf - yf); break;
            case 4: value += (xf); break;
            case 5: value += (-xf); break;
            case 6: value += (yf); break;
            case 7: value += (-yf); break;
        }
        
        // Apply fade and return normalized value
        double fadeX = Fade(xf);
        double fadeY = Fade(yf);
        double result = value * (1.0 / Math.Sqrt(2.0));
        
        return result;
    }

    private static int FastFloor(double x)
    {
        int i = (int)x;
        return x < i ? i - 1 : i;
    }

    private static double Fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }
}
