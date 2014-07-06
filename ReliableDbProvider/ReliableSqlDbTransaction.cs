using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDbProvider
{
    /// <summary>
    /// This class just wraps a Dbtransaction and prevents our SqlConnection from being leaked to the outside world
    /// </summary>
    public class ReliableSqlDbTransaction : DbTransaction
    {
        readonly ReliableSqlDbConnection innerConnection;
        readonly SqlTransaction innerTransaction;

        internal SqlTransaction InnerTransaction { get { return innerTransaction; } }

        public ReliableSqlDbTransaction(ReliableSqlDbConnection connection,  SqlTransaction transaction)
        {
            this.innerConnection = connection;
            this.innerTransaction = transaction;
        }
        public override void Commit()
        {
            innerTransaction.Commit();
        }

        protected override DbConnection DbConnection
        {
            get { return innerConnection; }
        }

        public override System.Data.IsolationLevel IsolationLevel
        {
            get { return innerTransaction.IsolationLevel; }
        }

        public override void Rollback()
        {
            innerTransaction.Rollback();
        }

        protected override void Dispose(bool disposing)
        {
            innerTransaction.Dispose();
            base.Dispose(disposing);
        }
       
    }
}
