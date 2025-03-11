using System.Threading.Channels;
using Marten;
using UserManager;

public class ReportMessageHandler : BackgroundService
{
    private readonly IDocumentStore _store;
    private readonly Channel<Messages.ReportCreated> _reportCreatedChannel;
    private readonly Channel<Messages.UserUpdated> _userUpdatedChannel;

    public ReportMessageHandler(IDocumentStore store, Channel<Messages.ReportCreated> reportCreatedChannel, Channel<Messages.UserUpdated> userUpdatedChannel)
    {
        _store = store;
        _reportCreatedChannel = reportCreatedChannel;
        _userUpdatedChannel = userUpdatedChannel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _reportCreatedChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            var created = await _reportCreatedChannel.Reader.ReadAsync(stoppingToken);
            //Simulate delay
            await Task.Delay(3000);
            Console.WriteLine($"Create report event recevied. Processing...");

            await using var session = _store.LightweightSession();
            var reports = await session.Query<Report>()
                                        .Where(w => w.UserId == created.UserId)
                                        .Where(w => w.ReportedBy == created.ReportedBy)
                                        .ToListAsync(stoppingToken);
            if (reports.Any())
            {
                //Report of this user by the reporting user already exists.
                Console.WriteLine($"Report not created. User {created.ReportedBy} has previously reported user {created.UserId}");
                continue;
            }

            session.Events.StartStream<Report>(created.Id, new Events.ReportCreated
            {
                Id = created.Id,
                ReportedBy = created.ReportedBy,
                ReportedOn = created.ReportedOn,
                UserId = created.UserId
            });
            await session.SaveChangesAsync(stoppingToken);

            Console.WriteLine($"Report created for user {created.UserId}");

            var report = await session.LoadAsync<Report>(created.Id, stoppingToken);
            await _userUpdatedChannel.Writer.WriteAsync(new Messages.UserUpdated
            {
                Id = created.UserId,
            });
        }

    }
}