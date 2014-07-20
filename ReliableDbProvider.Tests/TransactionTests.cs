using NUnit.Framework;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
    public class TransactionShould : PooledDbTestBase<SqlExpressProvider>
    {
        [Test]
        public void Return_reliable_connection_when_initiated_from_reliable_connection()
        {
            using (var context = GetContext())
            {
                using (var transaction = context.Database.Connection.BeginTransaction())
                {
                    Assert.That(transaction.Connection, Is.InstanceOf<ReliableSqlDbConnection>());
                }
            }
        }
    }
}
