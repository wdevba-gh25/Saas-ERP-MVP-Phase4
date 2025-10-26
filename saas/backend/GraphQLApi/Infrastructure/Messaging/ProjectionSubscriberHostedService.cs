using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Arenas;
using Shared;
using Shared.Entities;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text.Json;

namespace GraphQLApi.Infrastructure.Messaging
{
    public class ProjectionSubscriberHostedService : BackgroundService
    {
   


        private readonly ILogger<ProjectionSubscriberHostedService> _log;
        private readonly IConnectionMultiplexer _redis;

        private readonly IServiceScopeFactory _scopeFactory;

        public ProjectionSubscriberHostedService(IServiceScopeFactory scopeFactory, ILogger<ProjectionSubscriberHostedService> log, IConnectionMultiplexer redis)
        { 
            _scopeFactory = scopeFactory;
            _log = log;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sub = _redis.GetSubscriber();
            await sub.SubscribeAsync("inventory.events", async (_, message) =>
            {
                        try
                        {
                            var evt = JsonSerializer.Deserialize<JsonElement>(message!)!;
                                            if (evt.GetProperty("eventType").GetString() != "InventoryAdjusted") return;
            
                            var orgId = evt.GetProperty("organizationId").GetGuid();
                            var projectId = evt.GetProperty("projectId").GetGuid();
                            var productName = evt.GetProperty("productName").GetString()!;
                            var newLevel = evt.GetProperty("newLevel").GetInt32();

                
                                            // Each message handled in its own scoped DbContext
                                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<SaaSMvpContext>();
                
                            var row = await db.InventoryReads
                                                .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.ProjectId == projectId && x.ProductName == productName, stoppingToken);
                
                            if (row is null)
                            {
                                db.InventoryReads.Add(new InventoryRead
                                {
                                    OrganizationId = orgId,
                                    ProjectId = projectId,
                                    ProductName = productName,
                                    StockLevel = newLevel,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                row.StockLevel = newLevel;
                                row.UpdatedAt = DateTime.UtcNow;
                            }

                            await db.SaveChangesAsync(stoppingToken);
}
                        catch (Exception ex)
                        {
                            _log.LogWarning(ex, "Projection failed for message {Message}", message);
                        }
            });
        }
    }
}
