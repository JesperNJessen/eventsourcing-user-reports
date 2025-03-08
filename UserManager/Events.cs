namespace UserManager;
public class Events
{
    public class UserCreated
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string? Name { get; set; }
    }
    public class UserUpdated
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Status { get; set; } = String.Empty;
    }
}