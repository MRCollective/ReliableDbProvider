using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration.Conventions;
using ReliableDbProvider.Tests.Entities;

namespace ReliableDbProvider.Tests.Config
{
    public class Context : DbContext
    {
        public Context(string connectionStringName) : base(connectionStringName) {}
        public Context(DbConnection connection) : base(connection, true) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Configuration.LazyLoadingEnabled = false;
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<User>().HasMany(e => e.Properties);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<User> Users { get; set; }
    }

    internal sealed class ContextConfiguration : DbMigrationsConfiguration<Context>
    {
        public ContextConfiguration()
        {
            AutomaticMigrationsEnabled = true;
        }
    }
}
