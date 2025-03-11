using System.Threading.Channels;
using Marten;
using UserManager;

public class UpdateUserMessageHandler : BackgroundService
{
    private readonly IDocumentStore _store;
    private readonly Channel<Messages.UserUpdated> _channel;

    public UpdateUserMessageHandler(IDocumentStore store, Channel<Messages.UserUpdated> channel)
    {
        _channel = channel;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            var updated = await _channel.Reader.ReadAsync(stoppingToken);
            //Simulate delay
            await Task.Delay(3000);
            Console.WriteLine($"Update user event recevied. Processing...");

            await using var session = _store.LightweightSession();

            var user = await session.LoadAsync<User>(updated.Id, stoppingToken);
            if (user == default) throw new Exception($"User {updated.Id} not found");

            var reports = await session.Query<Report>()
                                        .Where(w => w.UserId == updated.Id)
                                        .ToListAsync(stoppingToken);

            if (reports.Count >= 5)
            {
                //User must be blocked
                Console.WriteLine($"User {user.Id} has 5 or more reports.");
                user.Status = "blocked";

                session.Events.Append(user.Id, new Events.UserUpdated
                {
                    Id = user.Id,
                    Status = user.Status
                });
                await session.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"No change required for user {user.Id}");
            }
        }

    }
}