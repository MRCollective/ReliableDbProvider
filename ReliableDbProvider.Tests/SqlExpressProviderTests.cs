using System.Linq;
using System.Threading;
using FizzWare.NBuilder;
using NUnit.Framework;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.Entities;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
    class SqlExpressProviderViaConnectionStringNameShould : SqlExpressProviderShould
    {
        protected override Context GetContext()
        {
            return new Context("ReliableDatabase");
        }
    }

    class SqlExpressProviderShould : DbProviderTestBase<SqlExpressProvider>
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
        public void Execute_scalar_commands_during_temporary_shutdown_of_sql_server()
        {
            int[] ids;
            using (var context = GetContext())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                users.ForEach(u => context.Users.Add(u));
                context.SaveChanges();
                ids = users.Select(uu => uu.Id).ToArray();
            }

            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    using (var context = GetContext())
                    {
                        var count = context.Users.Count(u => ids.Contains(u.Id));
                        Assert.That(count, Is.EqualTo(100));
                    }
                    Thread.Sleep(50);
                }
            }
        }

        [Test]
        public void Execute_batched_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 50; i++)
                {
                    Insert_and_select_multiple_entities();
                    Thread.Sleep(50);
                }
            }
        }
    }
}