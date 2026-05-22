using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinexusMockup.Data.Migrations
{
    /// <summary>
    /// Renames every framework-prefixed table to a name that reads naturally
    /// in a database browser.
    ///
    /// <para>
    /// <b>Identity</b> ships its tables with an <c>AspNet*</c> prefix; we drop
    /// it so the schema describes the application (Users, Roles, …), not the
    /// framework. <b>OpenIddict</b> ships with its library name as a prefix;
    /// we swap it for <c>OAuth*</c> so the protocol name shows through instead.
    /// </para>
    ///
    /// <para>
    /// Pure rename — no column changes, no data movement. EF Core handles the
    /// SQLite-specific dance internally (rebuild + copy) so existing rows
    /// (registered users, seeded OpenIddict clients, issued tokens) are
    /// preserved exactly.
    /// </para>
    /// </summary>
    public partial class RenameTablesForReadability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ASP.NET Identity → application-domain names.
            migrationBuilder.RenameTable(name: "AspNetUsers", newName: "Users");
            migrationBuilder.RenameTable(name: "AspNetRoles", newName: "Roles");
            migrationBuilder.RenameTable(name: "AspNetUserClaims", newName: "UserClaims");
            migrationBuilder.RenameTable(name: "AspNetUserLogins", newName: "UserLogins");
            migrationBuilder.RenameTable(name: "AspNetUserRoles", newName: "UserRoles");
            migrationBuilder.RenameTable(name: "AspNetUserTokens", newName: "UserTokens");
            migrationBuilder.RenameTable(name: "AspNetRoleClaims", newName: "RoleClaims");

            // OpenIddict → protocol-named tables.
            migrationBuilder.RenameTable(name: "OpenIddictApplications", newName: "OAuthClients");
            migrationBuilder.RenameTable(name: "OpenIddictAuthorizations", newName: "OAuthAuthorizations");
            migrationBuilder.RenameTable(name: "OpenIddictScopes", newName: "OAuthScopes");
            migrationBuilder.RenameTable(name: "OpenIddictTokens", newName: "OAuthTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "Users", newName: "AspNetUsers");
            migrationBuilder.RenameTable(name: "Roles", newName: "AspNetRoles");
            migrationBuilder.RenameTable(name: "UserClaims", newName: "AspNetUserClaims");
            migrationBuilder.RenameTable(name: "UserLogins", newName: "AspNetUserLogins");
            migrationBuilder.RenameTable(name: "UserRoles", newName: "AspNetUserRoles");
            migrationBuilder.RenameTable(name: "UserTokens", newName: "AspNetUserTokens");
            migrationBuilder.RenameTable(name: "RoleClaims", newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(name: "OAuthClients", newName: "OpenIddictApplications");
            migrationBuilder.RenameTable(name: "OAuthAuthorizations", newName: "OpenIddictAuthorizations");
            migrationBuilder.RenameTable(name: "OAuthScopes", newName: "OpenIddictScopes");
            migrationBuilder.RenameTable(name: "OAuthTokens", newName: "OpenIddictTokens");
        }
    }
}
