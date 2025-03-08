public record CreateUserRequest(string? Name);

public record ReportUserRequest(Guid UserId, Guid ReportedBy);