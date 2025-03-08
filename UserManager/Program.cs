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

    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = AutoCreate.All;
    }
});

var app = builder.Build();

app.MapPost("users", async (IDocumentStore store, CreateUserRequest request) =>
{
    var order = new Events.UserCreated
    {
        Name = request.Name
    };

    await using var session = store.LightweightSession();
    session.Events.StartStream<User>(order.Id, order);
    await session.SaveChangesAsync();
    return Results.Ok(order);
});

app.MapPost("users/{userId:guid}/status", 
    async (IDocumentStore store, Guid userId, UpdateUserRequest request) =>
    {
        var updated = new Events.UserUpdated
        {
            Id = userId,
            Status = request.Status
        };
        
        await using var session = store.LightweightSession();
        session.Events.Append(userId, updated);
        await session.SaveChangesAsync();
        return Results.Ok(updated);
    });

app.MapGet("users/{userId:guid}", async (IQuerySession session, Guid userId) =>
{
    var order = await session.LoadAsync<User>(userId);
    return order is not null ? Results.Ok(order) : Results.NotFound(); 
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
    var orders = await session.Query<User>().ToListAsync();
    return Results.Ok(orders);
});

app.Run();
