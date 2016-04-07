using System.Transactions;
using NUnit.Framework;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.Entities;
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

        [Test]
        public void Enlist_in_transaction_when_using_a_transaction_scope()
        {
            Assert.DoesNotThrow(() =>
            {
                using (var context = GetContext())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var user = new User() {Name = "Name"};
                        context.Users.Add(user);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
            });
        }
    }
}
