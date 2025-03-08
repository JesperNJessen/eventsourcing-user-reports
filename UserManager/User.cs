namespace UserManager;
public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string? Name { get; set; }
    public string? Status { get; set; }
    public HashSet<Report> Reports { get; set; } = new();
}