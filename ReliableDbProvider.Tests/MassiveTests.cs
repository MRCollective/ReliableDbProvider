using System.Dynamic;
using System.Linq;
using Massive;
using NUnit.Framework;

namespace ReliableDbProvider.Tests
{
    class MassiveTests
    {
        [Test]
        public void Perform_select()
        {
            AssureDatabaseExists();

            dynamic table = GetMassive();

            Assert.DoesNotThrow(() =>
                {
                    var user = table.Single(Id: 0);
                });
        }

        [Test]
        public void Insert_and_select()
        {
            AssureDatabaseExists();

            dynamic table = GetMassive();

            dynamic user = new ExpandoObject();
            user.Name = "Massive User";
            table.Insert(user);

            var userId = user.ID;

            var dbUser = table.Single(Id: userId);

            Assert.That(dbUser.Name, Is.EqualTo("Massive User"));
        }

        private static DynamicModel GetMassive()
        {
            // Massive doesn't work with table "User" so let's user UserProperty instead. (Massive doesn't transform User to [User]).
            return new DynamicModel("ReliableDatabase", tableName: "UserProperty", primaryKeyField: "Id");
        }

        private void AssureDatabaseExists()
        {
            var provider = new SqlExpressProviderShould();
            using (var context = provider.GetContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Id == -1);
            }
        }
    }
}
