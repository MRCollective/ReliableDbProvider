using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ReliableDbProvider.SqlAzure;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.Entities;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
    // Run the tests against the standard SqlClientFactory as well as the SqlAzureProvider
    // That way, we know if the test is broken because of the SqlAzureProvider or the test is wrong
    // Also, test the retry logic actually fires by using the SqlExpressProvider that provides
    //  a reliable connection with a sql express transient error detection strategy
    class SqlExpressProviderShould : DbProviderShould<SqlExpressProvider>
    {
        [Test]
        public void Execute_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    Insert_and_select_entity();
                    Thread.Sleep(50);
                }
            }
        }
    }

    class SqlAzureProviderShould : DbProviderShould<SqlAzureProvider> {}
    class SqlClientFactoryProviderShould : DbProviderShould<SqlClientFactory> {}

    abstract class DbProviderShould<T> : PooledDbTestBase<T>
        where T : DbProviderFactory
    {
        [Test]
        public void Perform_empty_select()
        {
            using (var context = GetContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Id == -1);

                Assert.That(user, Is.Null);
            }
        }

        [Test]
        public void Insert_and_select_entity()
        {
            using (var context = GetContext())
            using (var context2 = GetContext())
            {
                var user = new User { Name = "Name" };
                context.Users.Add(user);
                context.SaveChanges();

                var dbUser = context2.Users.SingleOrDefault(u => u.Id == user.Id);

                Assert.That(dbUser.Name, Is.EqualTo(user.Name));
            }
        }
        /*
        [Test]
        public void Insert_and_select_multiple_entities()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var users = Builder<User>.CreateListOfSize(100)
                .All().With(u => u.Properties = new List<UserProperty>
                {
                    new UserProperty {Name = "Name", Value = "Value", User = u}
                })
                .Build().OrderBy(u => u.Name).ToList();
                using (var t = session.BeginTransaction())
                {
                    users.ForEach(u => session.Save(u));
                    t.Commit();
                }

                var dbUsers = session2.QueryOver<User>()
                    .WhereRestrictionOn(u => u.Id).IsIn(users.Select(u => u.Id).ToArray())
                    .OrderBy(u => u.Name).Asc
                    .List();

                Assert.That(dbUsers, Has.Count.EqualTo(users.Count));
                for (var i = 0; i < users.Count; i++)
                {
                    Assert.That(dbUsers[i], Has.Property("Name").EqualTo(users[i].Name), "User " + i);
                    Assert.That(dbUsers[i], Has.Property("Id").EqualTo(users[i].Id), "User " + i);
                    var userProperties = dbUsers[i].Properties.ToList();
                    Assert.That(userProperties, Is.Not.Null, "User " + i + " Properties");
                    Assert.That(userProperties, Has.Count.EqualTo(1), "User " + i + " Properties");
                    Assert.That(userProperties[0], Has.Property("Name").EqualTo("Name"), "User " + i + " property 0");
                    Assert.That(userProperties[0], Has.Property("Value").EqualTo("Value"), "User " + i + " property 0");
                }
            }
        }

        [Test]
        public void Select_a_scalar()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                using (var t = session.BeginTransaction())
                {
                    users.ForEach(u => session.Save(u));
                    t.Commit();
                }

                var count = session2.QueryOver<User>()
                    .WhereRestrictionOn(x => x.Id)
                        .IsIn(users.Select(x => x.Id).ToArray())
                    .RowCount();

                Assert.That(count, Is.EqualTo(100));
            }
        }

        [Test]
        public void Insert_and_update_an_entity()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var user = new User { Name = "Name1" };
                session.Save(user);
                session.Flush();
                user.Name = "Name2";
                session.Flush();

                var userFromDb = session2.Get<User>(user.Id);

                Assert.That(userFromDb.Name, Is.EqualTo("Name2"));
            }
        }

        [Test]
        public void Insert_and_update_multiple_entities()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                using (var t = session.BeginTransaction())
                {
                    users.ForEach(u => session.Save(u));
                    t.Commit();
                }
                foreach (var u in users)
                {
                    u.Name += "_2_";
                }
                session.Flush();

                var dbUsers = session2.QueryOver<User>()
                    .WhereRestrictionOn(u => u.Id).IsIn(users.Select(u => u.Id).ToArray())
                    .OrderBy(u => u.Name).Asc
                    .List();

                Assert.That(dbUsers, Has.Count.EqualTo(users.Count));
                foreach (var u in dbUsers)
                {
                    Assert.That(u.Name, Is.StringEnding("_2_"));
                }
            }
        }
        */
    }
}
