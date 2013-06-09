namespace ReliableDbProvider.Tests.Entities
{
    public class UserProperty
    {
        public int Id { get; set; }
        public User User { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
