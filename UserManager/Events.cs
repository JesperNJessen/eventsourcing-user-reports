namespace UserManager;
public class Events
{
    public class UserCreated
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string? Name { get; set; }
    }
    public class ReportCreated
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public Guid UserId { get; set; }
        public Guid ReportedBy { get; set; }
        public DateTimeOffset ReportedOn { get; set; }
    }
}