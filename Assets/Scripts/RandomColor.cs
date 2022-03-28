using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class RandomColor {
    private class DefinedColor {
        public Range HueRange { get; set; }
        public Vector2[] LowerBounds { get; set; }
        public Range SaturationRange { get; set; }
        public Range BrightnessRange { get; set; }
    }

    private static readonly Dictionary<ColorScheme, DefinedColor> ColorDictionary = new Dictionary<ColorScheme, DefinedColor>();
    private static readonly object LockObj = new object();

    static RandomColor() {
        LoadColorBounds();
    }

    public static Color GetColor(ColorScheme colorScheme, Luminosity luminosity) {
        int H, S, B;

        H = PickHue(colorScheme);

        S = PickSaturation(H, luminosity, colorScheme);

        B = PickBrightness(H, S, luminosity);

        return HsvToColor(H, S, B);
    }

    public static Color[] GetColors(ColorScheme colorScheme, Luminosity luminosity, int count) {
        Color[] colors = new Color[count];

        for (int i = 0; i < count; i++) {
            colors[i] = GetColor(colorScheme, luminosity);
        }

        return colors;
    }

    public static Color[] GetColors(params Options[] options) {
        if (options == null) {
            throw new System.ArgumentNullException(nameof(options));
        }

        return options.Select(o => GetColor(o.ColorScheme, o.Luminosity)).ToArray();
    }

    public static void Seed(int seed) {
        lock (LockObj) {
            Random.InitState(seed);
        }
    }

    public static void Seed() {
        lock (LockObj) {
            Random.InitState((int)DateTime.Now.Ticks);
        }
    }

    private static int PickHue(ColorScheme colorScheme) {
        Range hueRange = GetHueRange(colorScheme);
        int hue = RandomWithin(hueRange);

        if (hue < 0) {
            hue += 360;
        }

        return hue;
    }

    private static int PickSaturation(int hue, Luminosity luminosity, ColorScheme colorScheme) {
        if (colorScheme == ColorScheme.Monochrome) {
            return 0;
        }

        if (luminosity == Luminosity.Random) {
            return RandomWithin(0, 100);
        }

        Range saturationRange = GetColorInfo(hue).SaturationRange;

        int sMin = saturationRange.Lower;
        int sMax = saturationRange.Upper;

        switch (luminosity) {
            case Luminosity.Bright:
                sMin = 55;
                break;
            case Luminosity.Dark:
                sMin = sMax - 10;
                break;
            case Luminosity.Light:
                sMax = 55;
                break;
        }

        return RandomWithin(sMin, sMax);
    }

    private static int PickBrightness(int H, int S, Luminosity luminosity) {
        int bMin = GetMinimumBrightness(H, S);
        int bMax = 100;

        switch (luminosity) {
            case Luminosity.Dark:
                bMax = bMin + 20;
                break;
            case Luminosity.Light:
                bMin = (bMax + bMin) / 2;
                break;
            case Luminosity.Random:
                bMin = 0;
                bMin = 100;
                break;
        }

        return RandomWithin(bMin, bMax);
    }

    private static int GetMinimumBrightness(int H, int S) {
        Vector2[] lowerBounds = GetColorInfo(H).LowerBounds;

        for (int i = 0; i < lowerBounds.Length - 1; i++) {
            float s1 = lowerBounds[i].x;
            float v1 = lowerBounds[i].y;

            float s2 = lowerBounds[i + 1].x;
            float v2 = lowerBounds[i + 1].y;

            if (S >= s1 && S <= s2) {
                float m = (v2 - v1) / (s2 - s1);
                float b = v1 - m * s1;

                return (int)(m * S + b);
            }
        }

        return 0;
    }

    private static Range GetHueRange(ColorScheme colorScheme) {
        DefinedColor color;

        if (ColorDictionary.TryGetValue(colorScheme, out color)) {
            if (color.HueRange != null) {
                return color.HueRange;
            }
        }

        return new Range(0, 360);
    }

    private static DefinedColor GetColorInfo(int hue) {
        if (hue >= 334 && hue <= 360) {
            hue -= 360;
        }

        return ColorDictionary.FirstOrDefault(c => c.Value.HueRange != null &&
                                                            hue >= c.Value.HueRange[0] &&
                                                            hue <= c.Value.HueRange[1]).Value;
    }

    private static int RandomWithin(Range range) {
        return RandomWithin(range.Lower, range.Upper);
    }

    private static int RandomWithin(int lower, int upper) {
        lock (LockObj) {
            return Random.Range(lower, upper + 1);
        }
    }

    private static void DefineColor(ColorScheme colorScheme, int[] hueRange, int[,] lowerBounds) {
        int[][] jagged = new int[lowerBounds.GetLength(0)][];

        for (int i = 0; i < lowerBounds.GetLength(0); i++) {
            jagged[i] = new int[lowerBounds.GetLength(1)];

            for (int j = 0; j < lowerBounds.GetLength(1); j++) {
                jagged[i][j] = lowerBounds[i, j];
            }
        }

        int sMin = jagged[0][0];
        int sMax = jagged[jagged.Length - 1][0];
        int bMin = jagged[jagged.Length - 1][1];
        int bMax = jagged[0][1];

        ColorDictionary[colorScheme] = new DefinedColor() {
            HueRange = Range.ToRange(hueRange),
            LowerBounds = jagged.Select(j => new Vector2(j[0], j[1])).ToArray(),
            SaturationRange = new Range(sMin, sMax),
            BrightnessRange = new Range(bMin, bMax)
        };
    }

    private static void LoadColorBounds() {
        DefineColor(
            ColorScheme.Monochrome,
            null,
            new[,] { { 0, 0 }, { 100, 0 } }
            );

        DefineColor(
            ColorScheme.Red,
            new[] { -26, 18 },
            new[,] { { 20, 100 }, { 30, 92 }, { 40, 89 }, { 50, 85 }, { 60, 78 }, { 70, 70 }, { 80, 60 }, { 90, 55 }, { 100, 50 } }
            );

        DefineColor(
            ColorScheme.Orange,
            new[] { 19, 46 },
            new[,] { { 20, 100 }, { 30, 93 }, { 40, 88 }, { 50, 86 }, { 60, 85 }, { 70, 70 }, { 100, 70 } }
            );

        DefineColor(
            ColorScheme.Yellow,
            new[] { 47, 62 },
            new[,] { { 25, 100 }, { 40, 94 }, { 50, 89 }, { 60, 86 }, { 70, 84 }, { 80, 82 }, { 90, 80 }, { 100, 75 } }
            );

        DefineColor(
            ColorScheme.Green,
            new[] { 63, 178 },
            new[,] { { 30, 100 }, { 40, 90 }, { 50, 85 }, { 60, 81 }, { 70, 74 }, { 80, 64 }, { 90, 50 }, { 100, 40 } }
            );

        DefineColor(
            ColorScheme.Blue,
            new[] { 179, 257 },
            new[,] { { 20, 100 }, { 30, 86 }, { 40, 80 }, { 50, 74 }, { 60, 60 }, { 70, 52 }, { 80, 44 }, { 90, 39 }, { 100, 35 } }
            );

        DefineColor(
            ColorScheme.Purple,
            new[] { 258, 282 },
            new[,] { { 20, 100 }, { 30, 87 }, { 40, 79 }, { 50, 70 }, { 60, 65 }, { 70, 59 }, { 80, 52 }, { 90, 45 }, { 100, 42 } }
            );

        DefineColor(
            ColorScheme.Pink,
            new[] { 283, 334 },
            new[,] { { 20, 100 }, { 30, 90 }, { 40, 86 }, { 60, 84 }, { 80, 80 }, { 90, 75 }, { 100, 73 } }
            );
    }

    public static Color HsvToColor(int hue, int saturation, float value) {
        // hack to push 0 or 360
        float h = hue;
        if (h == 0) {
            h = 1;
        }
        if (h == 360) {
            h = 359;
        }

        h /= 360.0f;
        float s = saturation / 100.0f;
        float v = value / 100.0f;

        float hInt = (int)Mathf.Floor(h * 6.0f);
        float f = h * 6 - hInt;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);
        float r = 256.0f;
        float g = 256.0f;
        float b = 256.0f;

        switch (hInt) {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        Color c = new Color((byte)Mathf.Floor(r * 255.0f),
                            (byte)Mathf.Floor(g * 255.0f),
                            (byte)Mathf.Floor(b * 255.0f),
                            255);

        return c;
    }
}
