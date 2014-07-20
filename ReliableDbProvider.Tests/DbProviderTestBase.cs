using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using FizzWare.NBuilder;
using NUnit.Framework;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.Entities;

namespace ReliableDbProvider.Tests
{
    abstract class DbProviderTestBase<T> : PooledDbTestBase<T>
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
        public void Insert_and_select_entity_with_custom_sql()
        {
            using (var context = GetContext())
            using (var context2 = GetContext())
            {
                var user = new User { Name = "Name" };
                context.Users.Add(user);
                context.SaveChanges();

                var dbUser = context2.Database.SqlQuery<User>("select * from [user] where Id = '" + user.Id + "'").FirstOrDefault();

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
                        }
                    )
                    .Build().OrderBy(u => u.Name).ToList();
                users.ForEach(u => context.Users.Add(u));
                context.SaveChanges();
                var ids = users.Select(uu => uu.Id).ToArray();

                var dbUsers = context2.Users
                    .Where(u => ids.Contains(u.Id))
                    .Include(u => u.Properties)
                    .OrderBy(u => u.Name)
                    .Skip(1)
                    .Take(users.Count - 1)
                    .ToList();

                Assert.That(dbUsers, Has.Count.EqualTo(users.Count-1));
                for (var i = 1; i < users.Count; i++)
                {
                    Assert.That(dbUsers[i-1], Has.Property("Name").EqualTo(users[i].Name), "User " + i);
                    Assert.That(dbUsers[i-1], Has.Property("Id").EqualTo(users[i].Id), "User " + i);
                    var userProperties = dbUsers[i-1].Properties.ToList();
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

                using (var cmd = context2.Database.Connection.CreateCommand())
                {
                    cmd.CommandText = string.Format("SELECT COUNT(*) FROM [User] WHERE ID IN ({0})", string.Join(",", ids));
                    context2.Database.Connection.Open();
                    var count = cmd.ExecuteScalar();
                    Assert.That(count, Is.EqualTo(100));
                }
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