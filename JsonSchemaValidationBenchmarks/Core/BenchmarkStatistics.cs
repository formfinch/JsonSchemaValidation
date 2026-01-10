namespace JsonSchemaValidationBenchmarks.Core;

public static class BenchmarkStatistics
{
    public static double Mean(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return values.Average();
    }

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    public static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var mean = Mean(values);
        var sumSquares = values.Sum(x => (x - mean) * (x - mean));

        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    public static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(x => x).ToList();
        int index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;

        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    public static double Min(IReadOnlyList<double> values)
    {
        return values.Count == 0 ? 0 : values.Min();
    }

    public static double Max(IReadOnlyList<double> values)
    {
        return values.Count == 0 ? 0 : values.Max();
    }
}
