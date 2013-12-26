using System.Data.Common;
using System.Data.Entity.Infrastructure;

namespace ReliableDbProvider.Tests.DatabaseFirst
{
    public class EntityFrameworkConnectionFactory : IDbConnectionFactory
    {
        public DbConnection CreateConnection(string connectionString)
        {
            var connection = DbProviderFactories
                .GetFactory("System.Data.SqlClient")
                .CreateConnection();
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}
