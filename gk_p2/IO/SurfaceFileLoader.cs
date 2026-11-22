using System.Globalization;
using System.Numerics;
using BezierSurfacesApp.Models;

namespace BezierSurfacesApp.IO;

public static class SurfaceFileLoader
{
    private static readonly Color[] DefaultSurfaceColors =
    [
        Color.FromArgb(225, 115, 145),
        Color.FromArgb(95, 175, 225),
        Color.FromArgb(135, 205, 130),
        Color.FromArgb(230, 185, 95)
    ];

    private static readonly Color[] DefaultNetColors =
    [
        Color.FromArgb(255, 200, 75),
        Color.FromArgb(95, 235, 255),
        Color.FromArgb(170, 255, 130),
        Color.FromArgb(255, 140, 220)
    ];

    public static List<BezierSurface> LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Nie znaleziono pliku z powierzchniami: {filePath}");

        var points = new List<Vector3>();
        string[] lines = File.ReadAllLines(filePath);

        for (int lineNo = 0; lineNo < lines.Length; lineNo++)
        {
            string trimmed = lines[lineNo].Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                throw new FormatException($"Błąd w linii {lineNo + 1}: oczekiwano trzech liczb (x y z).");

            float x = ParseFloat(parts[0], lineNo + 1);
            float y = ParseFloat(parts[1], lineNo + 1);
            float z = ParseFloat(parts[2], lineNo + 1);
            points.Add(new Vector3(x, y, z));
        }

        if (points.Count < 32 || points.Count % 16 != 0)
            throw new FormatException(
                $"Plik musi zawierać co najmniej 2 powierzchnie po 16 punktów (wczytano {points.Count} punktów).");

        int surfaceCount = points.Count / 16;
        var result = new List<BezierSurface>(surfaceCount);

        for (int s = 0; s < surfaceCount; s++)
        {
            var grid = new Vector3[4, 4];
            int offset = s * 16;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    grid[i, j] = points[offset + i * 4 + j];
                }
            }

            Color solidColor = DefaultSurfaceColors[s % DefaultSurfaceColors.Length];
            Color netColor = DefaultNetColors[s % DefaultNetColors.Length];
            var texture = TextureBuffer.CreateDefaultColorTexture(s);

            result.Add(new BezierSurface($"Powierzchnia {s + 1}", grid, solidColor, netColor, texture));
        }

        return result;
    }

    private static float ParseFloat(string token, int lineNo)
    {
        string normalized = token.Replace(',', '.');
        if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            throw new FormatException($"Błąd parsowania liczby '{token}' w linii {lineNo}.");
        return value;
    }
}