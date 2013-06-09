using System;
using System.Data.Common;
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
using System.Data.SqlClient;
#if NET45
using System.Data.Spatial;
using System.Reflection;
#endif

namespace ReliableDbProvider
{
    /// <summary>
    /// Db Provider Services for a <see cref="ReliableDbProvider"/>.
    /// </summary>
    public class ReliableDbProviderServices : DbProviderServices
    {
        /// <summary>
        /// Singleton instance of the <see cref="ReliableDbProviderServices"/> class.
        /// </summary>
        public static readonly ReliableDbProviderServices Instance = new ReliableDbProviderServices();

        #region Helpers and Setup
        private static readonly DbProviderServices UnderlyingInstance = GetProviderServices(new SqlConnection());

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

        #region Wrapping Code

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

        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            return WrappedCall(p => p.CreateCommandDefinition(prototype));
        }

        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            ReliableWrappedCall(connection, (p, c) => p.CreateDatabase(c, commandTimeout, storeItemCollection));
        }

        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            return WrappedCall(p => p.CreateDatabaseScript(providerManifestToken, storeItemCollection));
        }

        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            return ReliableWrappedCall(connection, (p, c) => p.DatabaseExists(c, commandTimeout, storeItemCollection));
        }

        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            ReliableWrappedCall(connection, (p, c) => p.DeleteDatabase(c, commandTimeout, storeItemCollection));
        }
        #endregion

        // todo: Build .NET 4.5 version
#if NET45
        private static readonly MethodInfo UnderlyingSetDbParameterValueMethod =
            UnderlyingInstance.GetType().GetMethod("SetDbParameterValue", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly Action<DbParameter, TypeUsage, object> UnderlyingSetDbParameterValue = (p, t, v) =>
            UnderlyingSetDbParameterValueMethod.Invoke(UnderlyingInstance, new[] { p, t, v });
        
        protected override DbSpatialServices DbGetSpatialServices(string manifestToken)
        {
            return WrappedCall(p => DbGetSpatialServices(manifestToken));
        }

        protected override DbSpatialDataReader GetDbSpatialDataReader(DbDataReader fromReader, string manifestToken)
        {
            return WrappedCall(p => GetDbSpatialDataReader(fromReader, manifestToken));
        }

        protected override void SetDbParameterValue(DbParameter parameter, TypeUsage parameterType, object value)
        {
            UnderlyingSetDbParameterValue(parameter, parameterType, value);
        }
#endif
    }
}
