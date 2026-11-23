using System.Numerics;
using BezierSurfacesApp.Models;

namespace BezierSurfacesApp.Rendering;

public static class LightingModel
{
    private static readonly Vector3 ViewVector = Vector3.UnitZ; // V = [0, 0, 1]

    public static int ComputePixelArgb(
        Vector3 fragmentPos,
        Vector3 surfaceNormal,
        Vector3 tangentPu,
        Vector3 tangentPv,
        float u,
        float v,
        BezierSurface surface,
        Scene scene,
        Vector3 lightPos,
        Vector3 lightColor)
    {
        // 1. Wektor normalny N (opcjonalnie zmodyfikowany przez Normal Map: N = M * N_tex)
        Vector3 n = surfaceNormal;
        if (scene.UseNormalMap)
        {
            Vector3 nTex = scene.NormalMap.SampleNormal(u, v);
            // M = [Pu, Pv, N_pow] -> N = Pu * nx + Pv * ny + N_pow * nz
            Vector3 perturbed = tangentPu * nTex.X + tangentPv * nTex.Y + surfaceNormal * nTex.Z;
            if (perturbed.LengthSquared() > 1e-8f)
                n = Vector3.Normalize(perturbed);
        }

        // 2. Obsługa dwóch stron powierzchni: jeśli powierzchnia skierowana tyłem do V=[0,0,1], odwracamy N
        if (surfaceNormal.Z < 0f)
            n = -n;

        // 3. Kolor obiektu I_O (jednolity albo z tekstury przeskalowanej na [0,1])
        Vector3 objectColor = surface.ColorMode == SurfaceColorMode.Texture
            ? surface.Texture.SampleColor(u, v)
            : surface.SolidColorNormalized;

        // 4. Wersor L wskazujący kierunek do światła
        Vector3 toLight = lightPos - fragmentPos;
        float distSq = toLight.LengthSquared();
        Vector3 l = distSq > 1e-8f ? toLight / MathF.Sqrt(distSq) : Vector3.UnitZ;

        // 5. Składowa rozproszona Lamberta: cos(N, L) = <N, L>, ujemne -> 0
        float dotNL = Vector3.Dot(n, l);

        // Światło rozproszone
        float cosNL = MathF.Max(0f, dotNL);

        // Połysk
        Vector3 r = Vector3.Normalize(2f * dotNL * n - l);
        float cosVR = MathF.Max(0f, Vector3.Dot(ViewVector, r));
        float spec = MathF.Pow(cosVR, scene.ShininessM);

        // 7. Końcowe równanie oświetlenia dla R, G, B w [0, 1]
        float factor = scene.Kd * cosNL + scene.Ks * spec;
        float red = factor * lightColor.X * objectColor.X;
        float green = factor * lightColor.Y * objectColor.Y;
        float blue = factor * lightColor.Z * objectColor.Z;

        // 8. Przeliczenie na [0, 255] i ograniczenie
        int rByte = Math.Clamp((int)(red * 255f), 0, 255);
        int gByte = Math.Clamp((int)(green * 255f), 0, 255);
        int bByte = Math.Clamp((int)(blue * 255f), 0, 255);

        return unchecked((int)0xFF000000) | (rByte << 16) | (gByte << 8) | bByte;
    }
}