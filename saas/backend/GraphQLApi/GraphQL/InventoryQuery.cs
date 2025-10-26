using GraphQLApi.Application.Inventory;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;

namespace GraphQLApi.GraphQL
{
    public class InventoryQuery
    {
        public async Task<InventoryReadDto?> inventoryByProduct(
        Guid organizationId,
        string productName,
        [Service] IMediator mediator)
        => await mediator.Send(new GetInventoryQuery(organizationId, productName));
    }
}
