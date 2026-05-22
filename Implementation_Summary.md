# Kinexus SSO — Implementation Summary

A Single Sign-On server built with ASP.NET Core 10, ASP.NET Identity, and
OpenIddict. The goal: log a user in **once** on Kinexus, then have that login
silently carry across all 10 mockup sub-sites under `Views/Knowledgebank/`
(and, later, across separate mockup web apps that point at Kinexus the same
way Google's apps share a Google login).

This document walks each file: **what** it is, **why** it exists, **how** it
works, and an English pseudocode of what each chunk does.

---

## The whole flow, in one paragraph

A mockup site sends the user's browser to Kinexus at `/connect/authorize`.
If the user already has a Kinexus login cookie, Kinexus recognizes them and
sends them back to the mockup site with a short-lived code. If they don't,
Kinexus shows them the login page first, drops the cookie when they sign in,
then sends them back. The mockup site swaps that code at `/connect/token` for
an ID token (proving who the user is) and drops its own cookie. Visiting a
second mockup site repeats the same dance — but because the Kinexus cookie is
still alive, no login screen ever appears. That silent re-use of the Kinexus
cookie is the entire SSO trick.

---

## Folder layout

```
Kinexus/
├── Auth/                              # Self-contained SSO server module
│   ├── OpenIddictOptions.cs           #   Strongly-typed shape of the OpenIddict config section
│   ├── OpenIddictExtensions.cs        #   AddKinexusSsoServer(...) — one-call registration
│   └── OpenIddictClientSeeder.cs      #   On startup, syncs registered apps from config to DB
├── Controllers/
│   ├── AuthorizationController.cs     #   /connect/authorize, /token, /userinfo, /logout
│   ├── HomeController.cs              #   Public pages (Index, Privacy, 404, Error)
│   ├── KnowledgebankController.cs     #   [Authorize]-protected mockup pages
│   └── AdminController.cs             #   Admin dashboard
├── Data/
│   └── ApplicationDbContext.cs        #   EF Core context (Identity + OpenIddict tables)
├── Areas/Identity/Pages/Account/      # Scaffolded ASP.NET Identity UI
│   ├── Login.cshtml(.cs)              #   Email + password login page
│   └── Register.cshtml(.cs)           #   Account creation page
├── Views/                             # Razor views (incl. 10 Knowledgebank mockups)
├── Program.cs                         # App bootstrap — one screen of code, well-commented
├── appsettings.json                   # Connection string + OpenIddict client list (no secrets)
└── app.db                             # SQLite database (auto-created by migrations)
```

---

# File-by-file walkthrough

## 1. `Program.cs`

**What** — App bootstrap. Wires together: database, ASP.NET Identity, the
OpenIddict SSO server, MVC, the request pipeline.

**Why** — One place to see "everything this app turns on, in order." Anything
SSO-related is delegated to `AddKinexusSsoServer(...)` so the bootstrap stays
short.

**How** — Builder pattern: register services first, then build the pipeline.
The pipeline order matters (authentication must run before authorization).

### Pseudocode

```
build a new ASP.NET app builder

# DATABASE
read "DefaultConnection" from appsettings.json; crash if missing
register ApplicationDbContext that uses SQLite (file: app.db)
    and tell EF "you'll also be storing OpenIddict tables in here"
turn on the developer-friendly DB error page

# IDENTITY (real users in the database)
register the default Identity system with IdentityUser
    set "require email confirmation before sign-in" to false
        # demo only — there's no email sender wired up yet
plug Identity into our ApplicationDbContext

# SSO SERVER (delegated)
call AddKinexusSsoServer(configuration)
    # everything OpenIddict needs is set up there

# MVC
register MVC controllers and views

build the app

if running in Development
    enable the migrations diagnostics endpoint
else
    use the generic error page and HSTS

force HTTPS
turn on routing
route any unhandled status code to /Home/NotFoundPage

# IMPORTANT — order matters
turn on Authentication  (decrypt cookie → figure out who the user is)
turn on Authorization   (check [Authorize] attributes against that identity)

map static assets
map the default MVC route ({controller=Home}/{action=Index}/{id?})
map Razor Pages (this is what serves /Identity/Account/Login etc.)
catch-all: anything else falls back to HomeController.NotFoundPage

run the app
```

---

## 2. `Auth/OpenIddictOptions.cs`

**What** — A C# class that mirrors the shape of the `OpenIddict` section in
`appsettings.json`. Each property is one config value.

**Why** — Anywhere else in the codebase that needs OpenIddict config asks for
a strongly-typed `OpenIddictOptions` object instead of digging through raw
config strings. Compile-time safety, autocompletion in the IDE, and one
place to add new settings later.

**How** — Plain POCO (Plain Old C# Object). The ASP.NET options system reads
`appsettings.json → OpenIddict` and fills in an instance of this class
automatically; no manual parsing.

### Pseudocode

```
class OpenIddictOptions
    constant SectionName = "OpenIddict"
        # the config-file section name we map from

    list of ClientApplication called Clients
        # the apps allowed to use Kinexus for sign-in

    nested class ClientApplication
        ClientId       — the app's username (sent to Kinexus to identify itself)
        ClientSecret   — the app's password (only for server-side apps;
                          phone/SPAs leave this null and use PKCE instead)
        DisplayName    — friendly name for any consent screen
        RedirectUris   — URLs Kinexus is allowed to send the user back to
                          after login (anything else is treated as an attack)
        PostLogoutRedirectUris — same idea, but for after logout
```

---

## 3. `Auth/OpenIddictExtensions.cs`

**What** — One method: `AddKinexusSsoServer(IServiceCollection, IConfiguration)`.
All OpenIddict wiring lives here.

**Why** — Keeps `Program.cs` to a single readable screen. When SSO needs to
grow (refresh tokens, real signing certificates, MFA), it grows here without
touching `Program.cs`.

**How** — Standard OpenIddict three-part registration: **Core** (storage),
**Server** (issuing tokens), **Validation** (checking incoming tokens). A
hosted service is also registered so registered apps are seeded into the DB
on startup.

### Pseudocode

```
static method AddKinexusSsoServer(services, configuration)

    # Bind the "OpenIddict" config section to OpenIddictOptions so it's
    # injectable anywhere via IOptions<OpenIddictOptions>.
    bind configuration["OpenIddict"] into OpenIddictOptions

    # Identity uses non-standard claim names by default. OpenIddict expects
    # the standard ones. Tell Identity to use the standard names so tokens
    # are readable by any compliant OIDC client.
    configure IdentityOptions
        UserNameClaimType = OpenIddict's "name"
        UserIdClaimType   = OpenIddict's "sub"   (subject)
        RoleClaimType     = OpenIddict's "role"

    register OpenIddict
        # CORE — storage layer
        AddCore
            use Entity Framework Core with ApplicationDbContext
                # so OpenIddict reuses our existing SQLite DB

        # SERVER — the actual SSO endpoint set
        AddServer
            declare endpoint URLs:
                /connect/authorize       (start a login)
                /connect/token           (swap code for tokens)
                /connect/logout          (end the session)
                /connect/userinfo        (read user claims from a token)

            allow the authorization-code flow with PKCE required
                # modern + safe for web, mobile, SPA

            register the scopes a client may request:
                openid, profile, email, roles, offline_access

            add a development-time encryption cert
            add a development-time signing cert
                # ephemeral — replace with real X.509 certs in production,
                # otherwise tokens become invalid on every app restart

            use ASP.NET Core integration with pass-through enabled for:
                authorize, token, end-session, userinfo
                # "pass-through" = each endpoint hands the request to our
                #   AuthorizationController so we can shape the token
                # we deliberately do NOT call DisableTransportSecurityRequirement(),
                #   so OpenIddict refuses to serve these over plain http

        # VALIDATION — checks incoming tokens (for when we add APIs later)
        AddValidation
            use the local server (we trust our own tokens, no remote call needed)
            integrate with ASP.NET Core

    # Hosted service runs once on app startup, syncs client list to DB
    register OpenIddictClientSeeder as a hosted service

    return services
```

---

## 4. `Auth/OpenIddictClientSeeder.cs`

**What** — A background service that runs once at startup and copies the
list of registered apps from `appsettings.json` into the OpenIddict tables
in the database.

**Why** — OpenIddict will only let *known* apps use Kinexus for login. The
list of "known apps" lives in the database. Editing config and restarting is
much friendlier for the team than inserting DB rows by hand.

**How** — Idempotent: if a client already exists in the DB, it's *updated*
in place (so changes to redirect URIs / secrets in the config file take
effect after a restart); if not, it's *created*. Migrations are applied
first so the OpenIddict tables exist before we try to write.

### Pseudocode

```
class OpenIddictClientSeeder(serviceProvider, options, logger) : IHostedService

    method StartAsync(cancellationToken)
        # Background services live for the whole app, but the DB is per-scope.
        # Open a fresh scope so we can pull the DbContext.
        create a service scope

        get ApplicationDbContext from the scope
        apply any pending EF migrations
            # ensures Identity + OpenIddict tables exist before we write

        get the OpenIddict application manager from the scope

        for each client in options.Clients
            if ClientId is blank
                log a warning and skip this entry

            build an OpenIddict descriptor from the client config
                (see BuildDescriptor below)

            ask the manager: do we already have a client with this ClientId?
            if no
                create it; log "Seeded ..."
            if yes
                overwrite it with the new descriptor; log "Updated ..."
                # this is what makes config edits + restart take effect

    method StopAsync(cancellationToken)
        do nothing (return completed task)

    private static method BuildDescriptor(client)
        # Translate a config entry into the shape OpenIddict stores in the DB.
        create descriptor with:
            ClientId, ClientSecret, DisplayName from config

            ClientType =
                Public        if ClientSecret is empty   (SPA / mobile)
                Confidential  if ClientSecret is set     (server-side web app)

            Permissions (an allow-list — client can ONLY do these things):
                use the authorize endpoint
                use the token endpoint
                use the end-session endpoint
                use the authorization_code grant
                use the "code" response type
                request the email scope
                request the profile scope
                request the roles scope

            Requirements:
                force PKCE on every code exchange

        for each RedirectUri in config
            add it to descriptor.RedirectUris

        for each PostLogoutRedirectUri in config
            add it to descriptor.PostLogoutRedirectUris

        return descriptor
```

---

## 5. `Controllers/AuthorizationController.cs`

**What** — The four SSO endpoints that OpenIddict's pass-through hands off
to us. This is where the login dance actually happens.

**Why** — Pass-through means *we* decide what claims go into the token, what
the consent screen looks like, who's allowed to log in, etc. Keeping that
logic in a real controller (instead of leaving OpenIddict to do it
automatically) is what lets us shape the SSO experience to Kinexus's needs.

**How** — Four actions, one per SSO endpoint. Each follows the OpenIddict
sample-controller pattern (Authorize → SignIn, Exchange → SignIn, Logout →
SignOut, UserInfo → claim dump). Helper `BuildPrincipalAsync` decides which
claims go in which token.

### Pseudocode

```
class AuthorizationController(signInManager, userManager) : Controller

    # ─── /connect/authorize ──────────────────────────────────────────────
    [GET, POST] Authorize()
        read the incoming OpenIddict request
            (which client, where to send the user back, which scopes asked for)

        check the browser: does it carry a Kinexus login cookie?
        if NO cookie (user is not logged in)
            issue a Challenge against Identity's cookie scheme
                set RedirectUri = the same /connect/authorize URL with all
                    its querystring/form values preserved
                # this is what makes "log in → bounce back to authorize → finish SSO" work
            return  # the user is now seeing the login page

        if YES cookie
            look up the actual user in the DB from the cookie's principal
            if user is null
                throw an exception

            build a ClaimsPrincipal with the requested scopes (see BuildPrincipalAsync)

            call SignIn(principal, OpenIddict scheme)
                # OpenIddict turns the principal into a one-time auth code
                # and redirects the browser back to the client app with the code

    # ─── /connect/token ──────────────────────────────────────────────────
    [POST] Exchange()
        read the incoming OpenIddict request

        if the grant type is "authorization_code"
            authenticate against the OpenIddict scheme
                # this pulls back the user data we embedded in the code earlier

            get the actual user from the DB
            if user no longer exists
                return Forbid with error "InvalidGrant — token is no longer valid"

            check if the user is still allowed to sign in (not locked out)
            if not
                return Forbid with error "InvalidGrant — user is no longer allowed"

            build a fresh ClaimsPrincipal with the same scopes
            call SignIn(principal, OpenIddict scheme)
                # this time OpenIddict produces the real tokens (ID + access)

        else
            throw "unsupported grant type"

    # ─── /connect/userinfo ───────────────────────────────────────────────
    [GET, POST]  [Authorize via OpenIddict bearer scheme]  UserInfo()
        get the user from the bearer token's identity
        if user is null
            return Challenge with error "InvalidToken — account no longer exists"

        start a claims dictionary with:
            "sub" = the user's id

        if the token's scopes include "email"
            add "email" and "email_verified"

        if the token's scopes include "profile"
            add "preferred_username"

        if the token's scopes include "roles"
            add "role" = list of user's roles

        return Ok(claims)
            # OpenIddict serializes this as the userinfo response

    # ─── /connect/logout ─────────────────────────────────────────────────
    [GET, POST] Logout()
        sign out of Identity (clears the Kinexus cookie)
            # this is what breaks SSO for any client that hasn't logged out yet

        call SignOut(OpenIddict scheme) with RedirectUri = "/"
            # OpenIddict completes the logout and, if the client app passed
            # a post-logout redirect URI, sends the browser there

    # ─── helper: build the "ID card" that becomes a token ───────────────
    private async BuildPrincipalAsync(user, requestedScopes)
        create a ClaimsPrincipal from the user (Identity does the heavy lifting)

        set its Scopes  = requestedScopes
        # (no SetResources call — we don't have a backend API to validate tokens
        #  yet; add it back when one exists, with that API's real audience name)

        for each claim on the principal
            decide where it belongs (see GetDestinations)
            attach those destinations to the claim

        return the principal

    # ─── helper: which token does each claim live in? ────────────────────
    private static GetDestinations(claim, principal)
        switch on claim.Type
            case "name" or "preferred_username"
                emit AccessToken
                if scope "profile" requested  → also emit IdentityToken

            case "email"
                emit AccessToken
                if scope "email" requested    → also emit IdentityToken

            case "role"
                emit AccessToken
                if scope "roles" requested    → also emit IdentityToken

            case "AspNet.Identity.SecurityStamp"
                emit nothing  # internal Identity value, never expose

            default
                emit AccessToken
```

---

## 6. `Data/ApplicationDbContext.cs`

**What** — The Entity Framework gateway to the SQLite database.

**Why** — Identity needs a DB context for its user/role tables; OpenIddict
needs one for its app/code/token tables. We share one context so there's
one connection, one set of migrations, one `app.db` file.

**How** — Inherits `IdentityDbContext` (which adds all the Identity tables).
The OpenIddict tables are attached *not* here but back in `Program.cs` via
`options.UseOpenIddict()` — a one-liner that registers the OpenIddict model
against this context.

### Pseudocode

```
class ApplicationDbContext(options) : IdentityDbContext(options)
    # no DbSets declared here — both Identity and OpenIddict
    # add their own tables via the configuration in Program.cs
```

---

## 7. `Areas/Identity/Pages/Account/Login.cshtml.cs`

**What** — The page model for the login form. Scaffolded from ASP.NET
Identity, lightly customized.

**Why** — This is the page users land on when `/connect/authorize` detects
they don't have a cookie. After they submit, Identity drops the cookie and
the browser bounces back to the SSO flow.

**How** — Razor Pages PageModel pattern: `OnGetAsync` renders the form,
`OnPostAsync` validates and signs in via `SignInManager.PasswordSignInAsync`.

### Pseudocode

```
[AllowAnonymous]
class LoginModel : PageModel
    inject SignInManager<IdentityUser>, ILogger<LoginModel>

    [BindProperty] Input  — Email, Password, RememberMe
    ExternalLogins        — list of external providers (currently none)
    ReturnUrl             — where to send the user after login

    OnGetAsync(returnUrl)
        if there's a pending error message, surface it on the form
        default returnUrl to "~/"
        sign out of any external-provider scheme  (clean slate)
        load the list of external login schemes  (none today)

    OnPostAsync(returnUrl)
        default returnUrl to "~/"
        reload external login list

        if form is invalid → re-render the page

        try PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false)
        if it succeeded
            log "User logged in"
            LocalRedirect to returnUrl
                # if returnUrl was /connect/authorize?..., this is what
                # silently completes the SSO flow after login

        if 2FA required → redirect to /Account/LoginWith2fa
        if locked out   → redirect to /Account/Lockout
        otherwise add "Invalid login attempt" model error and re-render
```

---

## 8. `Areas/Identity/Pages/Account/Register.cshtml.cs`

**What** — The page model for the register form. Scaffolded from Identity.

**Why** — A demo wouldn't be a demo without a way to create a new account.
After registration, the user is auto-signed-in so they can immediately enter
the SSO flow.

**How** — `UserManager.CreateAsync` writes a new user row; an email
confirmation token is generated but ignored if
`RequireConfirmedAccount = false` (the demo setting in `Program.cs`).

### Pseudocode

```
[AllowAnonymous]
class RegisterModel : PageModel
    inject UserManager, IUserStore, SignInManager, ILogger, IEmailSender

    [BindProperty] Input  — FirstName, LastName, Email, Password, AgreeToTerms

    OnGetAsync(returnUrl)
        load external login schemes

    OnPostAsync(returnUrl)
        default returnUrl to "~/"
        reload external login list

        if form is invalid → re-render the page

        create a new IdentityUser
        set its UserName = Email
        set its Email    = Email

        call UserManager.CreateAsync(user, password)
        if succeeded
            log "User created a new account with password"
            generate an email-confirmation token (base64-url encoded)
            build a confirmation callback URL
            if URL was produced
                send the confirmation email via IEmailSender
                    # currently a no-op until SMTP/SendGrid is wired up

            if RequireConfirmedAccount is true
                redirect to /Account/RegisterConfirmation
            else
                sign the user in (drop the cookie)
                LocalRedirect to returnUrl
                    # at this point the user can immediately enter the SSO flow

        if CreateAsync failed
            for each error → add to the model state
            re-render
```

---

## 9. `Controllers/HomeController.cs`

**What** — Public, **unauthenticated** pages: landing page, privacy, 404,
error.

**Why** — Anything you can reach without logging in lives here. Anything
that *requires* login lives in `KnowledgebankController`. Keeping them in
separate controllers makes the auth boundary visible at a glance.

**How** — Plain MVC: each action returns its matching view.

### Pseudocode

```
class HomeController : Controller
    # No [Authorize] — anonymous access allowed.
    # Do NOT add Knowledgebank actions here; that would silently bypass auth.

    Index()           → return view  (the landing page)
    Privacy()         → return view  (privacy notice)

    NotFoundPage()
        set Response.StatusCode = 404
        return view

    [ResponseCache disabled]
    Error()
        return view bound to ErrorViewModel
            with RequestId = current Activity.Id ?? HttpContext.TraceIdentifier
```

---

## 10. `Controllers/KnowledgebankController.cs`

**What** — The 9 mockup pages the SSO flow is designed to protect.

**Why** — This is the demo's "protected surface." The whole point of the
demo is: hit one of these URLs while logged out → land on the login page →
log in → return automatically → click any other Knowledgebank page → it
loads with no second login. That second-without-login is SSO.

**How** — `[Authorize]` on the controller forces login. `[Route("[action]")]`
means each action is reachable at `/Kinatlas`, `/TranscriptoNet`, etc.,
rather than `/Knowledgebank/Kinatlas` — friendlier URLs for the demo.

> **Known gap:** There's a `SigNet.cshtml` view but no matching action here
> or in the nav. Either add a `SigNet()` action + nav entry, or delete the
> view; otherwise it's unreachable.

### Pseudocode

```
[Authorize]            # every action below requires a logged-in user
[Route("[action]")]    # URLs are /<ActionName>
class KnowledgebankController : Controller
    Kinatlas()       → return view
    TranscriptoNet() → return view
    PhosphoNet()     → return view
    OncoNet()        → return view
    KinaseNet()      → return view
    DrugKinet()      → return view
    DrugProNet()     → return view
    KinetAM()        → return view
    Kinector()       → return view
```

---

## 11. `appsettings.json`

**What** — Configuration: SQLite connection string and the list of apps
allowed to use Kinexus for SSO.

**Why** — Keeps environment-specific values out of code. The
`OpenIddictClientSeeder` reads this on startup and syncs the DB to match —
so adding a new mockup site is one config entry + restart.

**How** — Standard ASP.NET Core JSON config. The `OpenIddict` section maps
to `OpenIddictOptions.cs` automatically.

### Pseudocode

```
{
    ConnectionStrings:
        DefaultConnection = "DataSource=app.db;Cache=Shared"
            # SQLite file in the project; Cache=Shared lets EF reuse
            # one in-memory page cache across connections

    Logging: standard ASP.NET defaults

    AllowedHosts: "*"

    OpenIddict:
        Clients:
            - one entry per app that uses Kinexus to log users in
              for the demo, one mock client:
                ClientId       = "kinexus-demo-client"
                ClientSecret   = "demo-secret-change-me"
                                  # placeholder; replace before any real deploy
                DisplayName    = "Kinexus Demo Client"
                RedirectUris            = [ "https://localhost:7081/signin-oidc" ]
                PostLogoutRedirectUris  = [ "https://localhost:7081/signout-callback-oidc" ]
}
```

---

# Things deferred (called out so they don't surprise you)

- **No email sender.** `RequireConfirmedAccount = false` in `Program.cs`.
  Wire up SMTP/SendGrid + flip the flag back to `true` before any real use.
- **Dev-only signing/encryption certs.** Tokens reset on every app restart.
  Replace `AddDevelopmentSigningCertificate()` / `AddDevelopmentEncryptionCertificate()`
  with real X.509 certs for production.
- **No backend API yet.** `SetResources(...)` was removed from
  `AuthorizationController` because there's no API to validate tokens
  against. Add it back the day a real Kinexus API exists.
- **`SigNet.cshtml` is orphaned.** View exists but no controller action and
  no nav link reaches it.
- **Demo client secret is in `appsettings.json` in plaintext.** Fine for
  localhost demo; move to User Secrets or environment variables before any
  remote deployment.
- **9 mockup actions, 10 mockup views, "8 mockup websites" target.** The
  intent is that each becomes its own separate ASP.NET app and points at
  Kinexus as the SSO server — at which point each gets its own `Clients[]`
  entry in `appsettings.json` and the views move out of `Views/Knowledgebank/`
  into their own projects.
