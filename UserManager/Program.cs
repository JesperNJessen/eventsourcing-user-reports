using System.Threading.Channels;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using UserManager;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<ReportMessageHandler>();
builder.Services.AddHostedService<UpdateUserMessageHandler>();

builder.Services.AddSingleton(_ => Channel.CreateUnbounded<Messages.ReportCreated>(new UnboundedChannelOptions
{
    SingleReader = true,
    AllowSynchronousContinuations = false
}));

builder.Services.AddSingleton(_ => Channel.CreateUnbounded<Messages.UserUpdated>(new UnboundedChannelOptions
{
    SingleReader = true,
    AllowSynchronousContinuations = false
}));


builder.Services.AddMarten(options =>
{
    options.Connection("Server=localhost;Port=5432;Database=mydb;User ID=test;Password=pass;");

    options.DisableNpgsqlLogging = true;
    options.UseSystemTextJsonForSerialization();
    options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<ReportProjection>(ProjectionLifecycle.Inline);

    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = AutoCreate.All;
    }
});

var app = builder.Build();

app.MapGet("/", () =>
{
    return Results.Ok("User management system online");
});


app.MapPost("seed", async (IDocumentStore store) =>
{
    await using var session = store.LightweightSession();
    var users = await session.Query<User>().ToListAsync();
    if (users.Any()) return Results.BadRequest("Users already seeded");

    var userNames = new string[] { "James", "Mary", "Darryl", "Hannah", "Dudley", "Victor" };

    foreach (var name in userNames)
    {
        var user = new Events.UserCreated
        {
            Name = name
        };
        session.Events.StartStream<User>(user.Id, user);
    }
    await session.SaveChangesAsync();
    return Results.Ok("Users seeded");
});

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
    async (ReportUserRequest request, Channel<Messages.ReportCreated> channel) =>
    {
        var created = new Messages.ReportCreated
        {
            UserId = request.UserId,
            ReportedBy = request.ReportedBy,
            ReportedOn = DateTimeOffset.UtcNow,
        };

        //Add message to queue
        await channel.Writer.WriteAsync(created);

        return Results.Accepted();
    });

app.MapGet("users/{userId:guid}", async (IQuerySession session, Guid userId) =>
{
    var user = await session.LoadAsync<User>(userId);
    if (user is null) return Results.NotFound();
    var reports = await session.Query<Report>()
                            .Where(w => w.UserId == userId)
                            .ToListAsync();

    user.Reports = reports.ToHashSet();

    return Results.Ok(user);
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
    var reports = await session.Query<Report>().ToListAsync();

    foreach (var user in users)
    {
        user.Reports = reports
        .Where(w => w.UserId == user.Id)
        .ToHashSet();
    }

    return Results.Ok(users);
});

app.MapGet("reports", async (IQuerySession session) =>
{
    var reports = await session.Query<Report>().ToListAsync();
    return Results.Ok(reports);
});

app.Run();