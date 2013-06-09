using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using ReliableDbProvider.Tests.Entities;

namespace ReliableDbProvider.Tests.Config
{
    public class Context : DbContext
    {
        public Context(string connectionString) : base(connectionString) {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<User>().HasMany(e => e.Properties);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<User> Users { get; set; }
    }
}
