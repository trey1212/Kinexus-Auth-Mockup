using KinexusMockup.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace KinexusMockup.Data;

/// <summary>
/// Single Entity Framework context that owns every table in <c>app.db</c>.
///
/// <para>
/// Two libraries bring their own tables to the party here:
/// <list type="bullet">
///   <item><b>ASP.NET Identity</b> — the user accounts, roles, claims, logins.
///         By default these arrive with an <c>AspNet*</c> prefix
///         (<c>AspNetUsers</c>, <c>AspNetRoles</c>, …).</item>
///   <item><b>OpenIddict</b> — the registered SSO clients, scopes, issued
///         authorizations and tokens. These arrive with an <c>OpenIddict*</c>
///         prefix.</item>
/// </list>
/// Both prefixes leak the library names into a schema that should describe
/// our domain (users, clients, tokens). We rename them below so the table
/// list reads like an application schema, not a framework changelog.
/// </para>
///
/// <para>
/// Column names and primary-key types are left as the libraries provide them:
/// renaming columns or swapping GUID PKs to integers would break the libraries'
/// internal queries. The full schema is documented in <c>Data/SCHEMA.md</c>.
/// </para>
/// </summary>
public class ApplicationDbContext : IdentityDbContext<AdminMockUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Let Identity + OpenIddict register their entities with their defaults
        // first. We override only the table names afterwards so EF emits clean
        // RenameTable migration steps instead of dropping and recreating.
        base.OnModelCreating(builder);

        // ───── ASP.NET Identity ──────────────────────────────────────────
        // Domain tables: users, roles, and the join/claim tables that wire them
        // together. Same shape as defaults, just without the framework prefix.
        builder.Entity<AdminMockUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

        // ───── OpenIddict ────────────────────────────────────────────────
        // Renamed to use "OAuth" — the protocol name — instead of the library
        // name. A DB-focused reader recognises OAuthClients/OAuthTokens
        // immediately; OpenIddictApplications/OpenIddictTokens require a
        // glossary lookup.
        builder.Entity<OpenIddictEntityFrameworkCoreApplication>().ToTable("OAuthClients");
        builder.Entity<OpenIddictEntityFrameworkCoreAuthorization>().ToTable("OAuthAuthorizations");
        builder.Entity<OpenIddictEntityFrameworkCoreScope>().ToTable("OAuthScopes");
        builder.Entity<OpenIddictEntityFrameworkCoreToken>().ToTable("OAuthTokens");
    }
}
