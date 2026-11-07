using Brick2Life.Vision;

internal static class RecognitionSamplingCadenceChecks
{
    public static void Run()
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        var cadence = new RecognitionSamplingCadence(0.8, 0.12, 1);
        cadence.RequestFast(0);
        Check(cadence.IntervalAt(0) == 0.12, "First baseline must use a short confirmation interval.");
        cadence.Observe(0.12, false, true);
        Check(cadence.IntervalAt(0.2) == 0.12, "Candidate lost fast confirmation.");
        cadence.Observe(0.24, true, true);
        Check(cadence.IntervalAt(0.24) == 0.8, "Confirmed layout kept flashing at the fast rate.");
        for (int i = 1; i <= 10; i++) cadence.Observe(i, true, true);
        Check(cadence.IntervalAt(10) == 0.8, "Repeated unchanged RGB should keep low-rate polling.");
        cadence.Observe(11, false, true);
        Check(cadence.IntervalAt(11.1) == 0.12, "A changed RGB candidate did not speed up confirmation.");
        for (double t = 11.1; t < 13; t += 0.1) cadence.Observe(t, false, true);
        Check(cadence.IntervalAt(13) == 0.8, "Unstable candidates extended fast flashing indefinitely.");
        cadence.Observe(13.8, false, false);
        Check(cadence.IntervalAt(13.8) == 0.8, "Unknown or rejected RGB should use low-rate recovery.");
        cadence.Observe(14.6, false, true);
        Check(cadence.IntervalAt(14.7) == 0.12, "Usable RGB after occlusion did not resume fast confirmation.");
        cadence.Observe(14.84, true, true);
        Check(cadence.IntervalAt(14.84) == 0.8, "Recovery confirmation failed to settle.");
        cadence.RequestFast(20);
        Check(cadence.IntervalAt(20) == 0.12, "Fresh confirmation request waited for slow polling.");
        cadence.Observe(20.12, false, true);
        Check(cadence.IntervalAt(20.2) == 0.12, "Incomplete fresh request returned to slow polling early.");
        cadence.Observe(20.24, true, true);
        Check(cadence.IntervalAt(20.24) == 0.8, "Fresh confirmation did not restore slow polling.");
        cadence.RequestFast(30);
        cadence.Observe(30.01, false, false);
        Check(cadence.IntervalAt(30.01) == 0.8, "Camera failure did not stop rapid retries.");
        foreach (var values in new[] { (0.0, 0.12, 1.0), (0.8, 0.9, 1.0), (0.8, double.NaN, 1.0), (0.8, 0.12, double.PositiveInfinity) })
        {
            bool rejected = false;
            try { _ = new RecognitionSamplingCadence(values.Item1, values.Item2, values.Item3); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid sampling configuration was accepted.");
        }
        Console.WriteLine($"RECOGNITION_SAMPLING_CADENCE_CHECKS_PASSED: {checks} assertions.");
    }
}
