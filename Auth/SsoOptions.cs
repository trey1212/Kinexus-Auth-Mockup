namespace KinexusMockup.Auth;

/// <summary>
/// Strongly-typed view of the <c>Sso</c> configuration section on the
/// authority side (Kinexus).
///
/// <para>
/// This is intentionally separate from <see cref="OpenIddictOptions"/>, which
/// describes the protocol-level client registrations (redirect URIs, secrets,
/// permissions) consumed by the seeder. <c>SsoOptions</c> describes the
/// <em>operational</em> peer URLs used by the UI: the "PhosphoNET" nav link,
/// the single-logout chain target, etc.
/// </para>
///
/// Environment-variable overrides follow the standard double-underscore
/// convention: <c>Sso__PublicUrl</c>, <c>Sso__PhosphonetPublicUrl</c>.
/// </summary>
public sealed class SsoOptions
{
    public const string SectionName = "Sso";

    /// <summary>Kinexus's own externally visible base URL (e.g. https://localhost:7081).</summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>Phosphonet client app's externally visible base URL (e.g. http://localhost:5200).</summary>
    public string PhosphonetPublicUrl { get; set; } = string.Empty;

    /// <summary>Phosphonet's logout endpoint path. Used to chain single-logout from Kinexus.</summary>
    public string PhosphonetLogoutPath { get; set; } = "/Account/Logout";

    /// <summary><see cref="PhosphonetPublicUrl"/> with any trailing slash trimmed.</summary>
    public string PhosphonetBase => (PhosphonetPublicUrl ?? string.Empty).TrimEnd('/');

    /// <summary><see cref="PublicUrl"/> with any trailing slash trimmed.</summary>
    public string KinexusBase => (PublicUrl ?? string.Empty).TrimEnd('/');
}
