using System;

namespace Brick2Life.Vision
{
    /// <summary>Low-rate polling while settled; bounded fast sampling for a usable unconfirmed RGB candidate.</summary>
    public sealed class RecognitionSamplingCadence
    {
        public double StableInterval { get; }
        public double ConfirmationInterval { get; }
        public double MaximumFastSeconds { get; }
        private bool confirming;
        private double fastUntil;

        public RecognitionSamplingCadence(double stableInterval, double confirmationInterval, double maximumFastSeconds)
        {
            if (!PositiveFinite(stableInterval) || !PositiveFinite(confirmationInterval) ||
                !PositiveFinite(maximumFastSeconds) || confirmationInterval > stableInterval)
                throw new ArgumentOutOfRangeException(nameof(stableInterval), "Sampling intervals must be positive and confirmation no slower than stable polling.");
            StableInterval = stableInterval;
            ConfirmationInterval = confirmationInterval;
            MaximumFastSeconds = maximumFastSeconds;
        }

        public double IntervalAt(double now) => confirming && now < fastUntil ? ConfirmationInterval : StableInterval;

        public void RequestFast(double now)
        {
            confirming = true;
            fastUntil = now + MaximumFastSeconds;
        }

        public void Observe(double now, bool confirmed, bool usableCandidate)
        {
            if (confirmed || !usableCandidate)
            {
                confirming = false;
                return;
            }
            // Changing candidates do not extend the deadline indefinitely. Unknown frames use slow polling;
            // a later usable candidate can begin a new bounded confirmation period.
            if (!confirming) RequestFast(now);
        }

        private static bool PositiveFinite(double value) => value > 0 && !double.IsInfinity(value) && !double.IsNaN(value);
    }
}
