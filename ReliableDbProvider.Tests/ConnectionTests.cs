using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using NUnit.Framework;
using ReliableDbProvider.Tests.Config;
using ReliableDbProvider.Tests.SqlExpress;

namespace ReliableDbProvider.Tests
{
    [TestFixture]
    class WhenConnectingSqlExpressProviderShould : ConnectionTests<SqlExpressProvider>
    {
        [Test]
        public void Establish_connection_during_temporary_shutdown_of_sql_server()
        {
            TestConnectionEstablishment(SqlExpressProvider.Instance);
        }
    }

    [TestFixture]
    class WhenConnectingSqlClientFactoryShould : ConnectionTests<SqlClientFactory>
    {
        [Test]
        [ExpectedException(typeof(SqlException))]
        public void Fail_to_establish_connection_during_temporary_shutdown_of_sql_server()
        {
            TestConnectionEstablishment(SqlClientFactory.Instance);
        }
    }

    abstract class ConnectionTests<T> : NonPooledDbTestBase<T>
        where T : DbProviderFactory
    {
        protected void TestConnectionEstablishment(T instance)
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 1000; i++)
                {
                    using (var connection = instance.CreateConnection())
                    {
                        connection.ConnectionString = ConnectionString;
                        connection.Open();
                        Thread.Sleep(1);
                    }
                }
            }
        }
    }
}
