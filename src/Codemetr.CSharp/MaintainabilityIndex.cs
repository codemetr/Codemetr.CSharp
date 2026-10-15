namespace Codemetr.CSharp;

/// <summary>
/// Visual-Studio-style maintainability index:
/// max(0, (171 - 5.2 ln(V) - 0.23 CC - 16.2 ln(LOC)) * 100 / 171), V = Halstead volume, CC = total complexity.
/// </summary>
internal static class MaintainabilityIndex
{
    const double Base = 171, VolumeWeight = 5.2, ComplexityWeight = 0.23, LinesWeight = 16.2, Scale = 100;

    public static int Calculate(double halsteadVolume, int totalComplexity, int logicalLines)
    {
        var raw = Base - VolumeWeight * Math.Log(Math.Max(halsteadVolume, 1))
            - ComplexityWeight * Math.Max(totalComplexity, 1)
            - LinesWeight * Math.Log(Math.Max(logicalLines, 1));
        var scaled = Math.Max(0, raw) * Scale / Base;
        return (int)Math.Round(Math.Min(Scale, scaled), MidpointRounding.AwayFromZero);
    }
}
