using System;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace ReliableDbProvider.Tests.DatabaseFirst
{
    public class EntityFrameworkReliableDbProviderServices : DbProviderServices
    {
        /// <summary>
        /// Singleton instance of the <see cref="ReliableDbProviderServices"/> class.
        /// </summary>
        public static readonly EntityFrameworkReliableDbProviderServices Instance = new EntityFrameworkReliableDbProviderServices();

        #region Helpers and Setup
        private static readonly DbProviderServices UnderlyingInstance = SqlProviderServices.Instance;

        private static TResult ReliableWrappedCall<TResult>(DbConnection connection, Func<DbProviderServices, DbConnection, TResult> callToReliablyWrap)
        {
            var reliableConnection = (ReliableSqlDbConnection)connection;
            var underlyingConnection = reliableConnection.ReliableConnection.Current;
            return reliableConnection.ReliableConnection.CommandRetryPolicy.ExecuteAction(
                () => callToReliablyWrap(GetProviderServices(underlyingConnection), underlyingConnection)
            );
        }

        private static void ReliableWrappedCall(DbConnection connection, Action<DbProviderServices, DbConnection> callToReliablyWrap)
        {
            ReliableWrappedCall(connection, (p, c) => { callToReliablyWrap(p, c); return 1; });
        }

        private static TResult WrappedCall<TResult>(Func<DbProviderServices, TResult> callToWrap)
        {
            return callToWrap(UnderlyingInstance);
        }
        #endregion

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            var commandDefinition = WrappedCall(p => p.CreateCommandDefinition(providerManifest, commandTree));
            var prototypeCommand = (SqlCommand)commandDefinition.CreateCommand();
            return CreateCommandDefinition(new ReliableSqlCommand(prototypeCommand));
        }

        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            return ReliableWrappedCall(connection, (p, c) => p.GetProviderManifestToken(c));
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return WrappedCall(p => p.GetProviderManifest(manifestToken));
        }
    }
}
