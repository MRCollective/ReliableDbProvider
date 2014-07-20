using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;

namespace ReliableDbProvider
{
    /// <summary>
    /// A <see cref="DbCommand"/> implementation that wraps a <see cref="SqlCommand"/> object such that any
    /// queries that are executed are executed via a <see cref="ReliableSqlConnection"/>.
    /// </summary>
    /// <remarks>
    /// Note: For this to work it requires that the Connection property be set with a <see cref="ReliableSqlConnection"/> object.
    /// </remarks>
    public class ReliableSqlCommand : DbCommand, ICloneable
    {
        /// <summary>
        /// The underlying <see cref="SqlCommand"/> being proxied.
        /// </summary>
        private SqlCommand Current { get; set; }

        /// <summary>
        /// The <see cref="ReliableSqlDbConnection"/> wrapper that has been assigned to the command via the Connection property or the ctor.
        /// </summary>
        private ReliableSqlDbConnection ReliableConnection { get; set; }

        /// <summary>
        /// Constructs a <see cref="ReliableSqlCommand"/>. with no associated connection
        /// </summary>
        internal ReliableSqlCommand(SqlCommand commandToWrap)
        {
            Debug.Assert(commandToWrap.Connection == null, "Expected Command connection to be uninitialised. This constructor creates a new command with no associated connection.");
            Current = commandToWrap;
        }

        public ReliableSqlCommand(ReliableSqlDbConnection connection, SqlCommand commandToWrap) 
        {
            Current = commandToWrap;
            ReliableConnection = connection;
            if (connection != null)
                Current.Connection = ReliableConnection.ReliableConnection.Current;
        }

        /// <summary>
        /// Explicit type-casting between a <see cref="ReliableSqlCommand"/> and a <see cref="SqlCommand"/>.
        /// </summary>
        /// <param name="command">The <see cref="ReliableSqlCommand"/> being casted</param>
        /// <returns>The underlying <see cref="SqlCommand"/> being proxied.</returns>
        public static explicit operator SqlCommand(ReliableSqlCommand command)
        {
            return command.Current;
        }

        /// <summary>
        /// Returns the underlying <see cref="SqlConnection"/> and expects a <see cref="ReliableSqlConnection"/> when being set.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get 
            {
                return ReliableConnection;
            }
            set
            {
                if (value == null)
                    return;

                ReliableConnection = ((ReliableSqlDbConnection) value);
                Current.Connection = ReliableConnection.ReliableConnection.Current;
            }
        }

        public object Clone()
        {
            return new ReliableSqlCommand(ReliableConnection, Current.Clone());
        }

        #region Wrapping code

        public override bool DesignTimeVisible
        {
            get { return Current.DesignTimeVisible; }
            set { Current.DesignTimeVisible = value; }
        }

        protected override void Dispose(bool disposing)
        {
            Current.Dispose();
        }

        public override void Prepare()
        {
            Current.Prepare();
        }

        public override void Cancel()
        {
            Current.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return Current.CreateParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ReliableConnection.ReliableConnection.CommandRetryPolicy.ExecuteAction(() => {
                if (Connection == null)
                    Connection = ReliableConnection.ReliableConnection.Open();
                if (Connection.State != ConnectionState.Open)
                    Connection.Open();
                return Current.ExecuteReader(behavior);
            });
        }

        public override int ExecuteNonQuery()
        {
            return ReliableConnection.ReliableConnection.ExecuteCommand(Current);
        }

        public override object ExecuteScalar()
        {
            return ReliableConnection.ReliableConnection.ExecuteCommand<int>(Current);
        }

        protected override DbTransaction DbTransaction
        {
            get { return Current.Transaction; }
            set { Current.Transaction = (SqlTransaction)value; }
        }

        public override string CommandText
        {
            get { return Current.CommandText; }
            set { Current.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return Current.CommandTimeout; }
            set { Current.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return Current.CommandType; }
            set { Current.CommandType = value; }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return Current.Parameters; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return Current.UpdatedRowSource; }
            set { Current.UpdatedRowSource = value; }
        }
        #endregion
    }
}
