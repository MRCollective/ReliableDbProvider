using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;

namespace ReliableDbProvider
{
    /// <summary>
    /// Wrap <see cref="ReliableSqlConnection"/> in a class that extends <see cref="DbConnection"/>
    /// so internal type casts within ADO.NET work.
    /// </summary>
    public abstract class ReliableSqlDbConnection : DbConnection
    {
        /// <summary>
        /// The underlying <see cref="ReliableSqlConnection"/>.
        /// </summary>
        public ReliableSqlConnection ReliableConnection { get; set; }

        /// <summary>
        /// Constructs a <see cref="ReliableSqlDbConnection"/> to wrap around the given <see cref="ReliableSqlConnection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="ReliableSqlConnection"/> to wrap</param>
        protected ReliableSqlDbConnection(ReliableSqlConnection connection)
        {
            ReliableConnection = connection;
        }

        /// <summary>
        /// Explicit type-casting between <see cref="ReliableSqlDbConnection"/> and <see cref="SqlConnection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="ReliableSqlDbConnection"/> being casted</param>
        /// <returns>The underlying <see cref="SqlConnection"/></returns>
        public static explicit operator SqlConnection(ReliableSqlDbConnection connection)
        {
            return connection.ReliableConnection.Current;
        }

        /// <summary>
        /// Disposes the underling <see cref="ReliableSqlConnection"/> as well as the current class.
        /// </summary>
        public new void Dispose()
        {
            ReliableConnection.Dispose();
            base.Dispose();
        }

        protected override DbProviderFactory DbProviderFactory
        {
            get { return GetProviderFactory(); }
        }

        protected abstract DbProviderFactory GetProviderFactory();

        #region Wrapping code
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return ReliableConnection.CommandRetryPolicy.ExecuteAction(() =>
            {
                if (ReliableConnection.State != ConnectionState.Open)
                    ReliableConnection.Open();
                return (DbTransaction) ReliableConnection.BeginTransaction(isolationLevel);
            });
        }

        public override void Close()
        {
            ReliableConnection.Close();
        }

        public override DataTable GetSchema()
        {
            return ReliableConnection.ConnectionRetryPolicy.ExecuteAction(
                () => ReliableConnection.Current.GetSchema()
            );
        }

        public override DataTable GetSchema(string collectionName)
        {
            return ReliableConnection.ConnectionRetryPolicy.ExecuteAction(
                () => ReliableConnection.Current.GetSchema(collectionName)
            );
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return ReliableConnection.ConnectionRetryPolicy.ExecuteAction(
                () => ReliableConnection.Current.GetSchema(collectionName, restrictionValues)
            );
        }

        public override void ChangeDatabase(string databaseName)
        {
            ReliableConnection.ChangeDatabase(databaseName);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new ReliableSqlCommand(ReliableConnection.CreateCommand());
        }

        public override void Open()
        {
            ReliableConnection.Open();
        }

        public override string ConnectionString { get { return ReliableConnection.ConnectionString; } set { ReliableConnection.ConnectionString = value; } }
        public override int ConnectionTimeout { get { return ReliableConnection.ConnectionTimeout; } }
        public override string Database { get { return ReliableConnection.Database; } }
        public override string DataSource { get { return ReliableConnection.Current.DataSource; } }
        public override string ServerVersion { get { return ReliableConnection.Current.ServerVersion; } }
        public override ConnectionState State { get { return ReliableConnection.State; } }

        public override event StateChangeEventHandler StateChange
        {
            add { ReliableConnection.Current.StateChange += value; }
            remove { ReliableConnection.Current.StateChange -= value; }
        }

        #endregion
    }
}
