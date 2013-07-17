using System.Configuration;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using Massive;
using NUnit.Framework;
using ReliableDbProvider.SqlAzureWithTimeoutRetries;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
    class MassiveTests : PooledDbTestBase<SqlExpressProvider>
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            EnsureDatabaseExists();
        }

        [Test]
        public void Perform_select()
        {
            dynamic table = GetDatabaseModel();

            Assert.DoesNotThrow(() => table.Single(Id: 0));
        }

        [Test]
        public void Insert_and_select()
        {
            dynamic table = GetDatabaseModel();
            dynamic userProperty = new ExpandoObject();
            userProperty.Name = "Massive User";

            table.Insert(userProperty);

            var dbUser = table.Single(Id: userProperty.Id);
            Assert.That(dbUser.Name, Is.EqualTo(userProperty.Name));
        }

        private static DynamicModel GetDatabaseModel()
        {
            return new DynamicModel("ReliableDatabase", tableName: "UserProperty", primaryKeyField: "Id");
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
