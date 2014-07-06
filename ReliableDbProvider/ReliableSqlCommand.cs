﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
        ReliableSqlDbConnection ReliableDbConnection;
        ReliableSqlDbTransaction ReliableDbTransaction;
        
        /// <summary>
        /// The underlying <see cref="SqlCommand"/> being proxied.
        /// </summary>
        SqlCommand Current { get; set; }

        /// <summary>
        /// The <see cref="ReliableSqlConnection"/> that has been assigned to the command via the Connection property.
        /// </summary>
        ReliableSqlConnection ReliableConnection { get; set; }


        /// <summary>
        /// Constructs a <see cref="ReliableSqlCommand"/>. with no associated connection
        /// </summary>
        internal ReliableSqlCommand(SqlCommand commandToWrap)
        {
            this.Current = commandToWrap;
            System.Diagnostics.Debug.Assert(
                Current.Connection == null, 
                "Expected Command connection to be uninitialised. This constructor creates a new command witn no associated connection!");

            this.ReliableDbConnection = null;
            this.ReliableConnection = null;
        }

        //Bug Fix: Failure when executing SQL string
        public ReliableSqlCommand(ReliableSqlDbConnection connection, SqlCommand commandToWrap) 
        {
            this.Current = commandToWrap;
            this.ReliableDbConnection = connection;
            this.ReliableConnection = (connection==null) ? null : connection.ReliableConnection;
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
                return ReliableDbConnection;
            }
            set
            {
                if (value == null)
                    return;

                ReliableDbConnection = ((ReliableSqlDbConnection)value);
                ReliableConnection = ReliableDbConnection.ReliableConnection;
                Current.Connection = ReliableConnection.Current;
            }
        }

        public object Clone()
        {
            return new ReliableSqlCommand(this.ReliableDbConnection, Current.Clone());
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
            return ReliableConnection.CommandRetryPolicy.ExecuteAction(() => {
                if (Connection == null)
                    Connection = ReliableConnection.Open();
                if (Connection.State != ConnectionState.Open)
                    Connection.Open();
                return Current.ExecuteReader(behavior);
            });
        }

        public override int ExecuteNonQuery()
        {
            return ReliableConnection.ExecuteCommand(Current);
        }

        public override object ExecuteScalar()
        {
            //Bug: In Entlib 5 this returns an IDataReader
            //return ReliableConnection.ExecuteCommand<object>(Current);

            return ReliableConnection.CommandRetryPolicy.ExecuteAction(() =>
            {
                if (Connection == null)
                    Connection = ReliableConnection.Open();
                if (Connection.State != ConnectionState.Open)
                    Connection.Open();
                return Current.ExecuteScalar();
            });            
        }

        protected override DbTransaction DbTransaction
        {
            get 
            { 
                return ReliableDbTransaction;
            }
            set 
            {
                if (value == null)
                {
                    ReliableDbTransaction = null;
                    Current.Transaction = null;
                }
                else
                {
                    ReliableDbTransaction = (ReliableSqlDbTransaction)value;
                    Current.Transaction = ReliableDbTransaction.InnerTransaction;
                }
            }
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
