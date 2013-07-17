using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Security;
using System.Security.Permissions;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;

namespace ReliableDbProvider
{
    /// <summary>
    /// Base class that allows you to easily create a Db Provider that provides a reliable sql connection based
    /// on the specified transient error detecton strategy.
    /// </summary>
    /// <typeparam name="TTransientErrorDetectionStrategy">The transient error detection strategy to use</typeparam>
    public abstract class ReliableSqlClientProvider<TTransientErrorDetectionStrategy> : DbProviderFactory, IServiceProvider
        where TTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy, new()
    {
        protected abstract RetryStrategy GetCommandRetryStrategy();
        protected abstract RetryStrategy GetConnectionRetryStrategy();
        protected abstract DbConnection GetConnection(ReliableSqlConnection connection);

        public override DbConnection CreateConnection()
        {
            var connection = new ReliableSqlConnection("",
                new RetryPolicy<TTransientErrorDetectionStrategy>(GetConnectionRetryStrategy()),
                new RetryPolicy<TTransientErrorDetectionStrategy>(GetCommandRetryStrategy())
            );

            return GetConnection(connection);
        }

        public object GetService(Type serviceType)
        {
            return serviceType == typeof(DbProviderServices)
                ? ReliableDbProviderServices.Instance
                : null;
        }

        public override DbCommand CreateCommand()
        {
            return new ReliableSqlCommand(new SqlCommand());
        }

        #region NotSupported

        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override DbCommandBuilder CreateCommandBuilder()
        {
            throw new NotSupportedException();
        }

        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            throw new NotSupportedException();
        }

        public override DbDataAdapter CreateDataAdapter()
        {
            throw new NotSupportedException();
        }

        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            throw new NotSupportedException();
        }

        public override DbParameter CreateParameter()
        {
            throw new NotSupportedException();
        }

        public override CodeAccessPermission CreatePermission(PermissionState state)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
