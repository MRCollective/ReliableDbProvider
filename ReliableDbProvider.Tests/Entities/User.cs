using System.Collections.Generic;

namespace ReliableDbProvider.Tests.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<UserProperty> Properties { get; set; }
    }
}
