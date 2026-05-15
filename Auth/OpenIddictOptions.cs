namespace KinexusMockup.Auth;

// This class is a "shape" for the OpenIddict section in appsettings.json.
// When the app starts, .NET reads that JSON and fills in one of these objects
// for us — so everywhere else in the code we just ask for OpenIddictOptions
// instead of digging through config strings.
//
// To add a new setting later (like a token lifetime, or where signing keys
// live on disk): add a property here, add the matching key in appsettings.json,
// done.
public sealed class OpenIddictOptions
{
    public const string SectionName = "OpenIddict";

    // The list of apps that are allowed to log users in through Kinexus.
    public List<ClientApplication> Clients { get; set; } = new();

    // One entry per app that uses Kinexus for sign-in.
    public sealed class ClientApplication
    {
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public List<string> RedirectUris { get; set; } = new();
        public List<string> PostLogoutRedirectUris { get; set; } = new();
    }
}
