using KinexusMockup.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace KinexusMockup.Auth;

// This file is the "setup" for our login server.
//
// Every line of OpenIddict configuration lives here so Program.cs can stay tidy —
// it just calls AddKinexusSsoServer() and walks away. When we later want to add
// things like refresh tokens, multi-factor auth, or a real signing certificate,
// we change them here and nothing else needs to move.


public static class OpenIddictExtensions
{
    public static IServiceCollection AddKinexusSsoServer(this IServiceCollection services, IConfiguration configuration)
    {
        // Read the "OpenIddict" section from appsettings.json 
        services.Configure<OpenIddictOptions>(configuration.GetSection(OpenIddictOptions.SectionName));

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
        });

        services.AddOpenIddict()

            // CORE: the part that knows how to save and load registered apps,
            // login grants, and tokens. 
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })

            // SERVER: the part that hands out tokens. 
            .AddServer(options =>
            {
                // These are the four URLs other apps will talk to.
                // /connect/authorize  -> "please log this user in"
                // /connect/token      -> "trade this code for a real token"
                // /connect/logout     -> "log this user out"
                // /connect/userinfo   -> "who is the user holding this token?"
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetEndSessionEndpointUris("/connect/logout")
                       .SetUserInfoEndpointUris("/connect/userinfo");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();

                // The kinds of info an app is allowed to ask for:
                //   openid          -> "I want to log a user in"  (required)
                //   profile         -> their name / username
                //   email           -> their email
                //   roles           -> their roles (admin, user, etc.)
                //   offline_access  -> permission to use refresh tokens later
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess);

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();
            })

            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Register the seeder so it runs once on startup and writes our configured
        // client apps into the database. See OpenIddictClientSeeder.cs.
        services.AddHostedService<OpenIddictClientSeeder>();

        return services;
    }
}
