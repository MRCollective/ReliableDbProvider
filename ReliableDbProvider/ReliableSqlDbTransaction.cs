using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace ReliableDbProvider
{
    /// <summary>
    /// This class wraps a DbTransaction so that any access of the DbConnection gives the reliable sql connection.
    /// </summary>
    public class ReliableSqlDbTransaction : DbTransaction
    {
        private readonly ReliableSqlDbConnection _innerConnection;
        private readonly SqlTransaction _innerTransaction;

        internal SqlTransaction InnerTransaction { get { return _innerTransaction; } }

        public ReliableSqlDbTransaction(ReliableSqlDbConnection connection, SqlTransaction transaction)
        {
            _innerConnection = connection;
            _innerTransaction = transaction;
        }

        public override void Commit()
        {
            _innerTransaction.Commit();
        }

        protected override DbConnection DbConnection
        {
            get { return _innerConnection; }
        }

        public override IsolationLevel IsolationLevel
        {
            get { return _innerTransaction.IsolationLevel; }
        }

        public override void Rollback()
        {
            _innerTransaction.Rollback();
        }

        protected override void Dispose(bool disposing)
        {
            _innerTransaction.Dispose();
            base.Dispose(disposing);
        }
       
    }
}
