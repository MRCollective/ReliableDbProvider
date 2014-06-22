using System.Linq;
using NUnit.Framework;

namespace ReliableDbProvider.Tests.DatabaseFirst
{
    class DatabaseFirstTests
    {
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
    }
}
