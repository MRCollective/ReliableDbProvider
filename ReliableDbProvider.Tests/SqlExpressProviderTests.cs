using System.Threading;
using NUnit.Framework;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
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
}