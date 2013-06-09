using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using FizzWare.NBuilder;
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

        [Test]
        public void Execute_batched_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 20; i++)
                {
                    Insert_and_select_multiple_entities();
                    Thread.Sleep(50);
                }
            }
        }
    }

    class SqlAzureProviderShould : DbProviderShould<SqlAzureProvider> {}
    class SqlClientFactoryProviderShould : DbProviderShould<SqlClientFactory>
    {
        [Test]
        [ExpectedException(typeof(EntityException))]
        public void Fail_to_execute_commands_during_temporary_shutdown_of_sql_server()
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

        [Test]
        [ExpectedException(typeof(EntityException))]
        public void Fail_to_execute_batched_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    Insert_and_select_multiple_entities();
                    Thread.Sleep(50);
                }
            }
        }
    }

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
        
        [Test]
        public void Insert_and_select_multiple_entities()
        {
            using (var context = GetContext())
            using (var context2 = GetContext())
            {
                var users = Builder<User>.CreateListOfSize(100)
                .All().With(u => u.Properties = new List<UserProperty>
                {
                    new UserProperty {Name = "Name", Value = "Value", User = u}
                })
                .Build().OrderBy(u => u.Name).ToList();
                users.ForEach(u => context.Users.Add(u));
                context.SaveChanges();
                var ids = users.Select(uu => uu.Id).ToArray();

                var dbUsers = context2.Users
                    .Where(u => ids.Contains(u.Id))
                    .Include(u => u.Properties)
                    .OrderBy(u => u.Name)
                    .ToList();

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
            using (var context = GetContext())
            using (var context2 = GetContext())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                users.ForEach(u => context.Users.Add(u));
                context.SaveChanges();
                var ids = users.Select(uu => uu.Id).ToArray();

                var count = context2.Users.Count(u => ids.Contains(u.Id));

                Assert.That(count, Is.EqualTo(100));
            }
        }

        [Test]
        public void Insert_and_update_an_entity()
        {
            using (var context = GetContext())
            using (var context2 = GetContext())
            {
                var user = new User { Name = "Name1" };
                context.Users.Add(user);
                context.SaveChanges();
                user.Name = "Name2";
                context.SaveChanges();

                var userFromDb = context2.Users.Single(u => u.Id == user.Id);

                Assert.That(userFromDb.Name, Is.EqualTo("Name2"));
            }
        }
        
        [Test]
        public void Insert_and_update_multiple_entities()
        {
            using (var context = GetContext())
            using (var context2 = GetContext())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                users.ForEach(u => context.Users.Add(u));
                context.SaveChanges();
                foreach (var u in users)
                {
                    u.Name += "_2_";
                }
                context.SaveChanges();
                var ids = users.Select(uu => uu.Id).ToArray();

                var dbUsers = context2.Users
                    .Where(u => ids.Contains(u.Id))
                    .OrderBy(u => u.Name)
                    .ToList();

                Assert.That(dbUsers, Has.Count.EqualTo(users.Count));
                foreach (var u in dbUsers)
                {
                    Assert.That(u.Name, Is.StringEnding("_2_"));
                }
            }
        }
    }
}
