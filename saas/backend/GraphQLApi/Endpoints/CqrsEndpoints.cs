using GraphQLApi.Application.Inventory;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using System.Security.Cryptography;

namespace GraphQLApi.Endpoints
{
    public static class CqrsEndpoints
    {
        public static IEndpointRouteBuilder MapCqrsEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/commands/inventory/adjust",
            async(AdjustInventoryCommand cmd, IMediator mediator) =>
            {
                var newLevel = await mediator.Send(cmd);
                return Results.Ok(new 
                { 
                    ok = true, newLevel
                });
            });

            app.MapGet("/api/queries/inventory/{orgId:guid}/{productName}",
            async(Guid orgId, string productName, IMediator mediator) =>
            {
                                var dto = await mediator.Send(new GetInventoryQuery(orgId, productName));
                                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            return app;
        }
    }
}
