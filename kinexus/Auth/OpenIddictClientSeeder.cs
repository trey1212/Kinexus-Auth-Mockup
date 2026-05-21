using KinexusMockup.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinexusMockup.Auth;

// copies the list of allowed apps from appsettings.json
// into the database every time Kinexus starts up.
//
// OpenIddict only lets known apps use Kinexus for login. The list of
// "known apps" lives in the database. Instead of asking someone to insert rows by
// hand, we let them edit appsettings.json and this file syncs the database to match.
//
// "Idempotent" just means running it twice is the same as running it once — if an
// app is already in the database, we update it instead of creating a duplicate.
public sealed class OpenIddictClientSeeder(
    IServiceProvider serviceProvider,
    IOptions<OpenIddictOptions> options,
    ILogger<OpenIddictClientSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Background services like this one are "shared" across the whole app,
        // but the database connection is "per-request". So we manually open a
        // little request-like scope here just so we can use the database.
        using var scope = serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Make sure all database changes are applied before we try to write anything.
        await db.Database.MigrateAsync(cancellationToken);

        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Go through every client listed in appsettings.json and either add it
        // to the database or update the existing entry.
        foreach (var client in options.Value.Clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                logger.LogWarning("Skipping OpenIddict client with empty ClientId.");
                continue;
            }

            var descriptor = BuildDescriptor(client);

            var existing = await appManager.FindByClientIdAsync(client.ClientId, cancellationToken);
            if (existing is null)
            {
                // First time we've seen this app — create it.
                await appManager.CreateAsync(descriptor, cancellationToken);
                logger.LogInformation("Seeded OpenIddict client {ClientId}.", client.ClientId);
            }
            else
            {
                await appManager.UpdateAsync(existing, descriptor, cancellationToken);
                logger.LogInformation("Updated OpenIddict client {ClientId}.", client.ClientId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static OpenIddictApplicationDescriptor BuildDescriptor(OpenIddictOptions.ClientApplication client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            DisplayName = client.DisplayName,

            ClientType = string.IsNullOrEmpty(client.ClientSecret)
                ? ClientTypes.Public
                : ClientTypes.Confidential,

            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
            },

            // Force every client to use PKCE. This is an extra security check that
            // prevents someone from stealing the login code mid-flight.
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange,
            },
        };

        foreach (var uri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        foreach (var uri in client.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        return descriptor;
    }
}
