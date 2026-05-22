using System.Security.Cryptography.X509Certificates;
using KinexusMockup.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace KinexusMockup.Auth;

/// <summary>
/// Wires OpenIddict — the OAuth 2.0 / OpenID Connect engine — into the
/// application. Lives in its own file so <c>Program.cs</c> stays a one-screen
/// bootstrap and so adding things like refresh tokens, MFA, or production
/// certificates is a single-file edit.
/// </summary>
public static class OpenIddictExtensions
{
    public static IServiceCollection AddKinexusSsoServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.Configure<OpenIddictOptions>(configuration.GetSection(OpenIddictOptions.SectionName));

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
        });

        services.AddOpenIddict()

            // CORE — storage layer. Reads/writes registered clients, grants,
            // and tokens through Entity Framework against ApplicationDbContext.
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })

            // SERVER — the four endpoints relying parties talk to:
            //   /connect/authorize  — "please log this user in"
            //   /connect/token      — "trade this code for a real token"
            //   /connect/logout     — "log this user out"
            //   /connect/userinfo   — "who is the user holding this token?"
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetEndSessionEndpointUris("/connect/logout")
                       .SetUserInfoEndpointUris("/connect/userinfo");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess);

                // Signing + encryption certificates.
                //   Dev  → ephemeral certs generated on each startup (tokens are
                //          invalidated when the process restarts; fine locally).
                //   Prod → real X.509 certs loaded from disk so tokens survive
                //          restarts and can be rotated independently. Paths and
                //          password come from the "Auth:Certificates" config
                //          section, which Azure App Service can override via
                //          environment variables / Key Vault references.
                ConfigureCertificates(options, configuration, environment);

                var aspNetOptions = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();

                // Only relax the HTTPS requirement locally — production must
                // enforce TLS so tokens never travel over plain HTTP.
                if (environment.IsDevelopment())
                {
                    aspNetOptions.DisableTransportSecurityRequirement();
                }
            })

            // VALIDATION — used when this server later validates its own
            // tokens (e.g. on /connect/userinfo).
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddHostedService<OpenIddictClientSeeder>();

        return services;
    }

    /// <summary>
    /// Either loads real X.509 certificates from disk (Production) or asks
    /// OpenIddict to generate ephemeral dev certs in-process (Development).
    /// </summary>
    private static void ConfigureCertificates(
        OpenIddictServerBuilder options,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var signingPath = configuration["Auth:Certificates:SigningPath"];
        var encryptionPath = configuration["Auth:Certificates:EncryptionPath"];
        var password = configuration["Auth:Certificates:Password"];

        var canUseRealCerts =
            !string.IsNullOrWhiteSpace(signingPath) &&
            !string.IsNullOrWhiteSpace(encryptionPath) &&
            File.Exists(signingPath) &&
            File.Exists(encryptionPath);

        if (canUseRealCerts)
        {
            options.AddSigningCertificate(new X509Certificate2(signingPath!, password));
            options.AddEncryptionCertificate(new X509Certificate2(encryptionPath!, password));
            return;
        }

        if (!environment.IsDevelopment())
        {
            // Production with no certs configured is a deployment mistake we
            // want to surface loudly, not silently fall back to dev certs.
            throw new InvalidOperationException(
                "Production requires real X.509 signing + encryption certificates. " +
                "Set Auth:Certificates:SigningPath, Auth:Certificates:EncryptionPath, " +
                "and Auth:Certificates:Password (or the equivalent env vars / Key Vault refs).");
        }

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();
    }
}
