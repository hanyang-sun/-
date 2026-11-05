using Brick2Life.Application;

internal static class ProjectionUnblendChecks
{
    public static void Run()
    {
        int assertions = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }

        ProjectionUnblend.Blend(180, 180, 180, 40f / 255f, 220f / 255f, 1f, 0.55f, out byte mixedR, out byte mixedG, out byte mixedB);
        Check(ProjectionUnblend.TryRecover(mixedR, mixedG, mixedB, 40f / 255f, 220f / 255f, 1f, 0.55f, out byte backR, out byte backG, out byte backB),
            "A real blend of table gray and cyan light should be recoverable.");
        Check(Math.Abs(backR - 180) <= 1 && Math.Abs(backG - 180) <= 1 && Math.Abs(backB - 180) <= 1,
            "Unblend should return the table color. Got " + backR + "," + backG + "," + backB);

        Check(!ProjectionUnblend.TryRecover(210, 40, 40, 40f / 255f, 220f / 255f, 1f, 0.7f, out _, out _, out _),
            "A red brick hidden under the light map must not be repainted.");
        Check(!ProjectionUnblend.TryRecover(20, 140, 40, 0.2f, 0.8f, 0.3f, 0f, out byte sameR, out byte sameG, out byte sameB),
            "Zero light must leave the pixel alone.");
        Check(sameR == 20 && sameG == 140 && sameB == 40, "A rejected pixel keeps its camera color.");

        ProjectionUnblend.Blend(190, 48, 42, 1f, 0.55f, 0.15f, 0.4f, out byte enemyR, out byte enemyG, out byte enemyB);
        Check(ProjectionUnblend.TryRecover(enemyR, enemyG, enemyB, 1f, 0.55f, 0.15f, 0.4f, out byte tableR, out byte tableG, out byte tableB),
            "An orange enemy glow on the table should come back off.");
        Check(Math.Abs(tableR - 190) <= 1 && Math.Abs(tableG - 48) <= 1 && Math.Abs(tableB - 42) <= 1,
            "Enemy unblend missed the surface. Got " + tableR + "," + tableG + "," + tableB);
        Console.WriteLine("Projection unblend checks passed (" + assertions + ").");
    }
}
