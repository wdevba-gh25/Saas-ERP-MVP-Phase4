using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Entities;
using System.Text.Json;


namespace GraphQLApi.Application.Inventory
{
    public record AdjustInventoryCommand(Guid OrganizationId, Guid InventoryId, int Delta, string CommandId) : IRequest<int>;
    public class AdjustInventoryHandler : IRequestHandler<AdjustInventoryCommand, int>
    {
            private readonly SaaSMvpContext _db;
    public AdjustInventoryHandler(SaaSMvpContext db) => _db = db;

    public async Task<int> Handle(AdjustInventoryCommand cmd, CancellationToken ct)
    {
        // Load inventory, enforce tenant boundary via Project -> OrganizationId
        var inv = await _db.Inventory
            .Join(_db.Projects, i => i.ProjectId, p => p.ProjectId, (i, p) => new { i, p })
            .Where(x => x.p.OrganizationId == cmd.OrganizationId && x.i.InventoryId == cmd.InventoryId)
            .Select(x => x.i)
            .FirstOrDefaultAsync(ct);

             if (inv is null)
                throw new System.Collections.Generic.KeyNotFoundException("Inventory not found for tenant");


            var newLevel = inv.StockLevel + cmd.Delta;
        if (newLevel< 0) throw new InvalidOperationException("Stock cannot be negative");

        inv.StockLevel = newLevel;
        inv.LastUpdated = DateTime.UtcNow;

        // Durable outbox event (serialized JSON)
        var evt = new
        {
            eventId = Guid.NewGuid(),
            eventType = "InventoryAdjusted",
            occurredAt = DateTime.UtcNow,
            organizationId = cmd.OrganizationId,
            inventoryId = inv.InventoryId,
            projectId = inv.ProjectId,
            productName = inv.ProductName,
            newLevel,
            delta = cmd.Delta,
            commandId = cmd.CommandId
        };

        _db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "InventoryAdjusted",
            OrganizationId = cmd.OrganizationId,
            Payload = JsonSerializer.Serialize(evt)
        });

    await _db.SaveChangesAsync(ct);
        return newLevel;
    }
    }
}
