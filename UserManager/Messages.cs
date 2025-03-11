public class Messages
{
    public class ReportCreated
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public Guid UserId { get; set; }
        public Guid ReportedBy { get; set; }
        public DateTimeOffset ReportedOn { get; set; }
    }

    public class UserUpdated
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
    }
}