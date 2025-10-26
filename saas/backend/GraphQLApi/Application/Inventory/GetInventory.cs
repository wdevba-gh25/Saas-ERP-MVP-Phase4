using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared;


namespace GraphQLApi.Application.Inventory
{
    public record GetInventoryQuery(Guid OrganizationId, string ProductName) : IRequest<InventoryReadDto?>;
    public record InventoryReadDto(string ProductName, int StockLevel, DateTime UpdatedAt);

    public class GetInventoryHandler : IRequestHandler<GetInventoryQuery, InventoryReadDto?>
    {
        private readonly SaaSMvpContext _db;
        public GetInventoryHandler(SaaSMvpContext db) => _db = db;

        public async Task<InventoryReadDto?> Handle(GetInventoryQuery q, CancellationToken ct)
        {
            var r = await _db.InventoryReads
                .Where(x => x.OrganizationId == q.OrganizationId && x.ProductName == q.ProductName)
                .FirstOrDefaultAsync(ct);

            return r is null ? null : new InventoryReadDto(r.ProductName, r.StockLevel, r.UpdatedAt);
        }
    }
}
