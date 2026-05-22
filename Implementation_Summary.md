# Kinexus SSO — Implementation Summary

A single sign-on prototype for the Kinexus Knowledge Bank. Built on
ASP.NET Core 10 with OpenIddict as the OAuth 2.0 / OpenID Connect engine.
Two web apps share one identity store: log in once, every site recognises you.

---

## What this project is

Kinexus runs ten research sites — SigNET, PhosphoNET, KinATLAS, OncoNET, and
others — each historically requiring its own login. This project replaces
those ten silos with one trusted authority. A user signs in *once* and every
site they visit recognises the same session.

The prototype ships two applications that demonstrate the full round-trip:

- **Kinexus** (HTTPS) — the SSO authority. Hosts the entire Knowledgebank
  except PhosphoNET, plus the user database, registration/login pages, and
  all OIDC protocol endpoints.
- **Phosphonet** (HTTP locally / HTTPS in production) — a standalone "client"
  app that owns nothing about authentication itself. It trusts Kinexus.

Using different protocols locally (HTTPS authority, HTTP client) was a
deliberate choice. Production deploys both on HTTPS, but the mixed-protocol
local demo proves the system survives whatever the network conditions throw
at it.

---

## Architecture at a glance

```
┌─────────────────────────────────┐         ┌─────────────────────────────────────┐
│ Phosphonet (client)             │         │ Kinexus (authority + Knowledgebank) │
│                                 │         │                                     │
│ HomeController                  │         │ AuthorizationController             │
│   ↓ [Authorize]                 │ OIDC    │   /connect/authorize                │
│ AccountController.Login()       │←───────→│   /connect/token                    │
│   → Challenge(OIDC)             │         │   /connect/logout                   │
│                                 │         │   /connect/userinfo                 │
│ AccountController.Logout()      │         │                                     │
│   → single-logout chain         │         │ KnowledgebankController             │
└─────────────────────────────────┘         │   /SigNet, /Kinatlas, /OncoNet, …   │
                                            │                                     │
                                            │ ASP.NET Identity (Users, Roles)     │
                                            │ OpenIddict EF stores                │
                                            │ (OAuthClients, OAuthTokens, …)      │
                                            └─────────────────────────────────────┘
```

The protocol is **OpenID Connect with Authorization Code flow + PKCE**, the
same pattern used by Google, Microsoft, and most modern SSO providers.

---

## How a sign-in works

1. User hits any Knowledgebank page on Phosphonet without a session.
2. The page renders the **welcome / sign-in placeholder** in place of its
   body (the layout checks `User.Identity.IsAuthenticated`).
3. User clicks **Log in / REGISTER HERE** → Phosphonet's `AccountController.Login`
   issues an OIDC challenge → the browser is redirected to Kinexus's
   `/connect/authorize` carrying `client_id`, `redirect_uri`, scopes, and a
   PKCE challenge.
4. Kinexus's `AuthorizationController` validates the `client_id` against the
   `OAuthClients` table (seeded from config). If the user has no Kinexus
   session, the user is redirected to `/Identity/Account/Login`.
5. User signs in via the standard ASP.NET Identity scaffolded page. Identity
   drops a cookie on Kinexus's origin.
6. The browser bounces back through `/connect/authorize`; Kinexus issues a
   short-lived authorization code and redirects to Phosphonet's `/signin-oidc`.
7. Phosphonet's OIDC handler exchanges the code at `/connect/token` for an
   access + ID token, drops its own session cookie, and redirects the user
   to `returnUrl` — typically the page they originally asked for.

Subsequent visits to Kinexus pages reuse the Identity cookie silently. The
user perceives one login.

---

## How single-logout works

`AccountController.Logout` on either app starts a deterministic chain:

- **From Kinexus**: sign out the Identity cookie → redirect to Phosphonet's
  `/Account/Logout?returnUrl=<kinexus-home>` → Phosphonet clears its cookie
  and fires RP-initiated end-session at Kinexus (which is a near-no-op since
  Identity is already cleared) → browser lands back on Kinexus home.
- **From Phosphonet**: `SignOut(Cookie, OIDC)` → OIDC handler builds an
  end-session URL with `id_token_hint` and `post_logout_redirect_uri` →
  Kinexus's `/connect/logout` clears Identity, redirects back to Phosphonet's
  `/signout-callback-oidc` → browser lands back on Phosphonet's
  `/PhosphoNet`.

End result either way: both cookies cleared, user back on the originating
app's home page.

---

## File layout

```
KinexusAuth/                            # The SSO authority + Knowledgebank
├── Auth/
│   ├── OpenIddictOptions.cs            # Strongly-typed OpenIddict client config
│   ├── OpenIddictExtensions.cs         # AddKinexusSsoServer(...) — one-call wiring
│   ├── OpenIddictClientSeeder.cs       # Startup task: config → OAuthClients table
│   └── SsoOptions.cs                   # Peer URLs (Phosphonet base, etc.)
├── Controllers/
│   ├── AuthorizationController.cs      # OIDC protocol endpoints
│   ├── AccountController.cs            # User-facing Login/Logout (chains through Phosphonet)
│   ├── HomeController.cs               # Marketing pages, /, /Privacy, errors
│   ├── KnowledgebankController.cs      # 10 Knowledgebank actions, no [Authorize]
│   └── AdminController.cs              # Admin dashboard
├── Data/
│   ├── ApplicationDbContext.cs         # EF Core context; OnModelCreating renames tables
│   ├── SCHEMA.md                       # Table-by-table reference for DB reviewers
│   └── Migrations/                     # EF migration history (latest: RenameTablesForReadability)
├── Areas/Identity/Pages/Account/       # Scaffolded Identity UI (Login, Register)
├── Views/
│   ├── Home/                           # Marketing home, error pages
│   ├── Knowledgebank/                  # 10 site views (filled from INFO FOR THE WEBSITES.txt)
│   └── Shared/
│       ├── _Layout.cshtml              # Marketing layout
│       ├── _KnowledgeBankLayout.cshtml # Knowledgebank layout; renders welcome partial when unauthed
│       └── _KnowledgebankWelcome.cshtml # Welcome partial shown to unauthed visitors
├── wwwroot/
│   ├── css/knowledgebase.css           # Shared .content styles for KB views
│   └── img/                            # Banners, logos
├── certs/                              # Dev certs (regenerated each run; ignored in prod)
├── Program.cs                          # One-screen bootstrap
├── appsettings.json                    # Schema with safe defaults
├── appsettings.Development.json        # Dev defaults — `dotnet run` works zero-config
├── appsettings.Production.json         # Template for Azure App Service overrides
├── AZURE_DEPLOYMENT.md                 # Step-by-step Azure deployment guide
├── INFO FOR THE WEBSITES.txt           # Source content for Knowledgebank views
└── KinexusMockup.csproj                # Net10.0, EF Sqlite + SqlServer, OpenIddict

Phosphonet/                             # The SSO client (deliberately minimal)
├── Auth/
│   ├── SsoOptions.cs                   # Authority URL, client id/secret
│   ├── SsoServiceCollectionExtensions.cs  # AddPhosphonetSso(...) — cookie + OIDC wiring
│   └── ReturnUrlPolicy.cs              # Local-or-allow-listed returnUrl validation
├── Controllers/
│   ├── AccountController.cs            # Login (challenge), Logout (RP-initiated)
│   ├── HomeController.cs               # / → /PhosphoNet
│   └── KnowledgebankController.cs      # ONE action: PhosphoNet
├── Views/
│   ├── Knowledgebank/PhosphoNet.cshtml
│   └── Shared/
│       ├── _KnowledgeBankLayout.cshtml
│       └── _KnowledgebankWelcome.cshtml
├── wwwroot/                            # PhosphoNET banner + shared CSS
├── Program.cs
├── appsettings.json
├── appsettings.Development.json        # Sso defaults pointing at localhost:7081
├── appsettings.Production.json         # Template for App Service overrides
├── README.md
└── Phosphonet.csproj
```

---

## Why we chose what we chose

| Decision | Why |
|---|---|
| **C# / ASP.NET Core 10** | Matches Kinexus's existing stack. Picking anything else would mean Kinexus couldn't realistically maintain what we built. |
| **OpenIddict** (not IdentityServer, not Azure AD) | OpenIddict is open-source and free; IdentityServer went commercial; Azure AD locks you into one cloud. OpenIddict implements the OIDC spec strictly, so future integrations work without surprises. |
| **ASP.NET Identity** | Hashing, lockout, claims, role management — all built-in. Writing user management from scratch is a security minefield best avoided. |
| **Authorization Code flow + PKCE** | The current OIDC best practice, recommended for every client type (not just SPAs/mobile). PKCE blocks code-interception attacks. |
| **SQLite locally, Azure SQL in prod** | SQLite is a single file — easy to demo, easy to reset between runs. Production swap is one connection-string change; EF Core picks the provider automatically from the string shape. |
| **Cookie auth + bearer tokens** | Cookies for browser sessions (HttpOnly, can't be read by JavaScript). Bearer tokens flow only between Phosphonet's OIDC handler and Kinexus's `/connect/token`, never to the browser. |
| **ResponseMode=Query** (not form_post) | Browsers' SameSite=Lax rules silently drop cookies on cross-site POSTs. Switching the auth-code return to a top-level GET keeps the correlation cookie working without weakening cookie security. |
| **Razor server-rendered views** (not React/Vue) | Kinexus's existing sites are Razor. Introducing a frontend framework would either fork their codebase or run two rendering pipelines side by side. |
| **`dotnet watch run`** as dev workflow | Source edits auto-rebuild and reload without a manual build step. Same `dotnet publish` pipeline works for Azure deployment. |
| **Strongly-typed `SsoOptions`** | One config section per concern, `ValidateOnStart` could catch misconfiguration. No raw `Configuration["..."]` lookups scattered through the codebase. |
| **Phosphonet as one-page client** | The whole point of SSO is that a relying party owns *no* identity logic. Phosphonet has exactly one route (`/PhosphoNet`) — everything else lives on Kinexus. |
| **Friendly table names** (`Users`, `OAuthClients` instead of `AspNetUsers`, `OpenIddictApplications`) | A database reviewer should be able to read the schema without knowing which .NET libraries we used. Library names belong in the codebase, not the data model. |

---

## The database

11 tables in two clusters:

**Identity cluster** — `Users`, `Roles`, `UserClaims`, `RoleClaims`,
`UserRoles`, `UserLogins`, `UserTokens`. The user identity model: who exists,
what they're allowed to do, what tokens belong to them.

**OAuth cluster** — `OAuthClients`, `OAuthScopes`, `OAuthAuthorizations`,
`OAuthTokens`. The SSO engine's state: which apps may use us, what grants
they've been issued, what tokens are currently outstanding.

Full schema documentation: [`Data/SCHEMA.md`](Data/SCHEMA.md).

Primary keys are GUID strings (not integers) by deliberate library design:
GUIDs survive email changes and other rename events that would break a
sequential-integer foreign-key model, and they don't leak the number of
accounts to anyone who can read a URL.

---

## Development workflow

Local dev with two terminals, zero env vars:

```
# Terminal 1 — Kinexus (HTTPS authority)
cd C:\Users\rodri\source\repos\KinexusAuth
dotnet watch run --project KinexusMockup.csproj --launch-profile https

# Terminal 2 — Phosphonet (HTTP client)
cd C:\Users\rodri\source\repos\Phosphonet
dotnet watch run
```

Open `http://localhost:5200/` in an incognito window. The dev defaults in
each `appsettings.Development.json` wire Phosphonet to Kinexus automatically.

`dotnet watch` rebuilds and reloads on every file save — no manual rebuild
needed.

---

## Production workflow

See [`AZURE_DEPLOYMENT.md`](AZURE_DEPLOYMENT.md) for the full step-by-step.
In summary:

1. Provision Azure App Service (×2) + Azure SQL DB + (optional) Key Vault.
2. Generate signing + encryption X.509 certificates, upload to App Service.
3. Configure App Settings (`Sso__*`, connection string, cert paths,
   `OpenIddict__Clients__*`).
4. `dotnet publish -c Release` + `az webapp deploy`.
5. First-startup migration auto-creates the schema on Azure SQL.

Everything that's `localhost` in dev becomes a configurable App Setting in
production. No code changes between environments.

---

## What's deferred (called out explicitly)

- **Email confirmation flow** — Identity has it, the project has an
  `IEmailService` interface, but the production SMTP credentials are blank
  in `appsettings.Production.json` and need real wiring. SendGrid or Azure
  Communication Services are the standard choices.
- **External login providers** — `UserLogins` table exists for future
  "Sign in with Google" support; no providers wired up today.
- **Refresh tokens** — `offline_access` scope is registered but the client
  doesn't request it. Adding refresh-token rotation is a one-config-change
  enable.
- **Role-based authorization beyond the basics** — `Roles` and `RoleClaims`
  exist; no roles are currently seeded. Pick a small set (`Admin`, `Researcher`)
  and seed them at startup the same way OpenIddict clients are seeded.
- **Front-channel single-logout via iframes** — current implementation uses
  an explicit redirect chain between Kinexus and Phosphonet. For 10+ relying
  parties the standard front-channel iframe approach scales better.
- **Application Insights / metrics** — not wired up. Recommended in Azure
  deployment guide.

None of these are research problems — they're follow-up engineering tasks
with well-trodden answers.
