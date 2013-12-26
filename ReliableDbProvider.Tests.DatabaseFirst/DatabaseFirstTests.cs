using System.Linq;
using NUnit.Framework;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.DatabaseFirst;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
    class DatabaseFirstTests : PooledDbTestBase<SqlExpressProvider>
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            EnsureDatabaseExists();
        }

        [Test]
        public void Perform_select()
        {
            using (var context = GetDatabaseFirstContext())
            {
                var data = context.Users.ToList();
                Assert.That(data, Is.Empty);
            }
        }

        [Test]
        public void Insert_and_select()
        {
            using (var context = GetDatabaseFirstContext())
            {
                context.Users.Add(new User {Name = "User 1"});
                context.SaveChanges();
                var data = context.Users.ToList();
                
                Assert.That(data.Single().Name, Is.EqualTo("User 1"));
            }
        }

        private static ReliableDatabaseFirst GetDatabaseFirstContext()
        {
            return new ReliableDatabaseFirst();
        }

        private void EnsureDatabaseExists()
        {
            using (var context = GetContext())
            {
                context.Database.Initialize(false);
            }
        }
    }
}
