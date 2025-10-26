using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared;
using StackExchange.Redis;

namespace GraphQLApi.Infrastructure.Messaging
{
    public class OutboxDispatcherHostedService : BackgroundService
    {
   


        private readonly ILogger<OutboxDispatcherHostedService> _log;
        private readonly IConnectionMultiplexer _redis;
        private readonly IServiceScopeFactory _scopeFactory;

        public OutboxDispatcherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherHostedService> log, IConnectionMultiplexer redis)
        {
            _scopeFactory = scopeFactory;
            _log = log;
            _redis = redis;
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bus = _redis.GetSubscriber();
        while (!stoppingToken.IsCancellationRequested)
        {

           using var scope = _scopeFactory.CreateScope();
           var db = scope.ServiceProvider.GetRequiredService<SaaSMvpContext>();
           var batch = await db.OutboxMessages
                .Where(o => !o.Dispatched)
                .OrderBy(o => o.OccurredAt)
                .Take(100)
                .ToListAsync(stoppingToken);


                foreach (var o in batch)
                {
                    try
                    {
                        await bus.PublishAsync("inventory.events", o.Payload);
                        o.Dispatched = true;
                        o.DispatchAttempts++;
                    }
                   catch (Exception ex)
                    {
                        _log.LogWarning(ex, "Failed to publish Outbox {OutboxId}", o.OutboxId);
                        o.DispatchAttempts++;
                    }
                 }

                if (batch.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
                
                await Task.Delay(150, stoppingToken);
            }
    }
    }
}
