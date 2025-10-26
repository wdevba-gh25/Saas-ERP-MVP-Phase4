using GraphQLApi.Application.Inventory;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using System.ComponentModel.Design;
using static HotChocolate.ErrorCodes;

namespace GraphQLApi.GraphQL
{
    public class InventoryMutation
    {
       public async Task<int> adjustInventory(
        Guid organizationId,
        Guid inventoryId,
        int delta,
        string commandId,
        [Service] IMediator mediator)
        => await mediator.Send(new AdjustInventoryCommand(organizationId, inventoryId, delta, commandId));
    }
}
