using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TruthGate_Web.Middleware;

namespace Test.TruthGate;

public sealed class DomainGatewayMiddlewareTests
{
    [Fact]
    public async Task AuthenticatedRequest_BypassesGatewayBeforeMappedDomainLookup()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        var nextCalled = false;

        app.UseDomainGateway();
        app.Run(context =>
        {
            nextCalled = true;
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "admin") },
                    authenticationType: "Test"))
        };
        context.Request.Host = new HostString("truthgate.io");

        await app.Build()(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }
}
