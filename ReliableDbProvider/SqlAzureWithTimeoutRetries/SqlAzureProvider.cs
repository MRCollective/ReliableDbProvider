using System;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;
using ReliableDbProvider.SqlAzure;

namespace ReliableDbProvider.SqlAzureWithTimeoutRetries
{
    /// <summary>
    /// Db provider that will result in reliable connections to a SQL Azure database that retries for timeouts.
    /// </summary>
    public class SqlAzureProvider : ReliableSqlClientProvider<SqlAzureTransientErrorDetectionStrategyWithTimeouts>
    {
        /// <summary>
        /// Singleton instance of the provider.
        /// </summary>
        public static readonly SqlAzureProvider Instance = new SqlAzureProvider();

        /// <summary>
        /// Event that is fired when a command is retried using this provider.
        /// </summary>
        public static event EventHandler<RetryingEventArgs> CommandRetry;

        /// <summary>
        /// Event that is fired when a connection is retried using this provider.
        /// </summary>
        public static event EventHandler<RetryingEventArgs> ConnectionRetry;

        protected override RetryStrategy GetCommandRetryStrategy()
        {
            return RetryStrategies.DefaultCommandStrategy;
        }

        protected override RetryStrategy GetConnectionRetryStrategy()
        {
            return RetryStrategies.DefaultConnectionStrategy;
        }

        protected override DbConnection GetConnection(ReliableSqlConnection connection)
        {
            connection.CommandRetryPolicy.Retrying += CommandRetry;
            connection.ConnectionRetryPolicy.Retrying += ConnectionRetry;
            return new SqlAzureConnection(connection);
        }
    }

    /// <summary>
    /// Wraps a <see cref="ReliableSqlConnection"/> in a <see cref="DbConnection"/> for the <see cref="SqlAzureWithTimeoutRetries.SqlAzureProvider"/> Db Provider.
    /// </summary>
    public class SqlAzureConnection : ReliableSqlDbConnection
    {
        /// <summary>
        /// Create a <see cref="SqlAzure.SqlAzureConnection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="ReliableSqlConnection"/> to wrap</param>
        public SqlAzureConnection(ReliableSqlConnection connection) : base(connection) { }
        protected override DbProviderFactory GetProviderFactory()
        {
            return SqlAzureProvider.Instance;
        }
    }
}
