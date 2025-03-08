using Marten;
using Marten.Events.Projections;
using UserManager;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(options =>
{
    options.Connection("Server=localhost;Port=5432;Database=mydb;User ID=test;Password=pass;");
    options.UseSystemTextJsonForSerialization();
    options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<ReportProjection>(ProjectionLifecycle.Inline);

    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = AutoCreate.All;
    }
});

var app = builder.Build();

app.MapPost("users", async (IDocumentStore store, CreateUserRequest request) =>
{
    var user = new Events.UserCreated
    {
        Name = request.Name
    };

    await using var session = store.LightweightSession();
    session.Events.StartStream<User>(user.Id, user);
    await session.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapPost("reports",
    async (IDocumentStore store, ReportUserRequest request) =>
    {
        var created = new Events.ReportCreated
        {
            UserId = request.UserId,
            ReportedBy = request.ReportedBy,
            ReportedOn = DateTimeOffset.UtcNow,
        };

        await using var session = store.LightweightSession();
        session.Events.StartStream<Report>(created.Id, created);

        var report = await session.LoadAsync<Report>(created.Id);
        var user = await session.LoadAsync<User>(created.UserId);
        user.Reports.Add(report);
        session.Events.Append(user.Id, user);

        await session.SaveChangesAsync();
        return Results.Ok(created);
    });

app.MapGet("users/{userId:guid}", async (IQuerySession session, Guid userId) =>
{
    var user = await session.LoadAsync<User>(userId);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapGet("users/{userId:guid}/history", async (IQuerySession session, Guid userId) =>
{
    var events = await session.Events.FetchStreamAsync(userId);

    var eventDtos = events.Select(e => new
    {
        Id = e.Id,
        Type = e.EventType.Name,
        Data = e.Data,
        Timestamp = e.Timestamp
    });

    return Results.Ok(eventDtos);
});

app.MapGet("users", async (IQuerySession session) =>
{
    var users = await session.Query<User>().ToListAsync();
    return Results.Ok(users);
});

app.MapGet("reports", async (IQuerySession session) =>
{
    var reports = await session.Query<Report>().ToListAsync();
    return Results.Ok(reports);
});

app.Run();