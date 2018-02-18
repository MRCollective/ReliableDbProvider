using System.Data;
using System.Data.SqlClient;
using System.Threading;
using NUnit.Framework;

namespace ReliableDbProvider.Tests
{
    class SqlClientFactoryProviderShould : DbProviderTestBase<SqlClientFactory>
    {
        [Test]
        public void Fail_to_execute_commands_during_temporary_shutdown_of_sql_server()
        {
            Assert.Throws(Is.InstanceOf<EntityException>(), () =>
                {
                    using (TemporarilyShutdownSqlServerExpress())
                    {
                        for (var i = 0; i < 1000; i++)
                        {
                            Insert_and_select_entity();
                            Thread.Sleep(50);
                        }
                    }
                }
            );
        }

        [Test]
        public void Fail_to_execute_batched_commands_during_temporary_shutdown_of_sql_server()
        {
            Assert.Throws(Is.InstanceOf<EntityException>(), () =>
                {
                    using (TemporarilyShutdownSqlServerExpress())
                    {
                        for (var i = 0; i < 1000; i++)
                        {
                            Insert_and_select_multiple_entities();
                            Thread.Sleep(50);
                        }
                    }
                }
            );
        }
    }
}