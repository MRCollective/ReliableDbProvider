using System;
using Microsoft.Practices.TransientFaultHandling;

namespace ReliableDbProvider.SqlAzure
{
    /// <summary>
    /// Supplies some predefined retry strategies.
    /// </summary>
    public static class RetryStrategies
    {
        /// <summary>
        /// Default command retry strategy - Incremental every 1s for 10 retries.
        /// </summary>
        public static RetryStrategy DefaultCommandStrategy = new Incremental("Incremental Retry Strategy", 10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        /// <summary>
        /// Default connection retry strategy - Exponential Backoff 10 retries from 1 to 30s backoffs with an increase of 10s.
        /// </summary>
        public static RetryStrategy DefaultConnectionStrategy = new ExponentialBackoff("Backoff Retry Strategy", 10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), false);
    }
}
