using System.Data.Common;
using NUnit.Framework;
using ReliableDbProvider.SqlAzure;

namespace ReliableDbProvider.Tests
{
    class SqlAzureProviderShould : DbProviderTestBase<SqlAzureProvider>
    {
        [Test]
        public void Create_valid_command()
        {
            var provider = GetProvider();

            var command = provider.CreateCommand();

            Assert.That(command, Is.Not.Null);
        }
    }
}