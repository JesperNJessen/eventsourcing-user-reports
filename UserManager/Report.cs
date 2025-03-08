namespace UserManager;

public class Report
{
    public Guid  Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ReportedBy { get; set; }
    public DateTimeOffset ReportedOn { get; set; }
}