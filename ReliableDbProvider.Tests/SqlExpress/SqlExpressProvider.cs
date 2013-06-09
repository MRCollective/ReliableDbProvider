using System;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;
using ReliableDbProvider.SqlAzure;

namespace ReliableDbProvider.Tests.SqlExpress
{
    public class SqlExpressProvider : ReliableSqlClientProvider<SqlExpressTransientErrorDetectionStrategy>
    {
        public static readonly SqlExpressProvider Instance = new SqlExpressProvider();

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
            EventHandler<RetryingEventArgs> retry = (sender, args) => Console.WriteLine("Retry - Count:{0}, Delay:{1}, Exception:{2}\r\n\r\n", args.CurrentRetryCount, args.Delay, args.LastException);
            connection.CommandRetryPolicy.Retrying += retry;
            connection.ConnectionRetryPolicy.Retrying += retry;

            return new SqlExpressConnection(connection);
        }
    }

    public class SqlExpressConnection : ReliableSqlDbConnection
    {
        public SqlExpressConnection(ReliableSqlConnection connection) : base(connection) { }

        protected override DbProviderFactory GetProviderFactory()
        {
            return SqlExpressProvider.Instance;
        }
    }
}
