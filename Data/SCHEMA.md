# Kinexus Database Schema

A walk-through of every table in `app.db` — what it stores, why the columns
are shaped the way they are, and how the tables relate. Aimed at a reviewer
who wants to understand the schema without reading any C# source.

The database is **SQLite** in this prototype (one file, `app.db`); the schema
is portable to SQL Server unchanged — column types are all `TEXT` / `INTEGER`
which map cleanly to `nvarchar` / `int`.

---

## Why primary keys look like random strings

Most tables use a GUID-shaped string as their primary key (`Id`, `UserId`, etc.).
This is intentional:

- **Stability.** A user's display name or email can change without breaking
  every foreign key that points at them. The GUID never changes.
- **Security.** Sequential integer IDs leak business information (how many
  users exist, how fast they're being added) and make ID guessing trivial.
  Randomly-generated IDs do neither.
- **Library compatibility.** ASP.NET Identity and OpenIddict were both
  designed around string GUID keys. Changing the key type would mean rewriting
  the libraries; gain would be cosmetic only.

Wherever a row "represents a person/client/grant," the PK is a GUID. Wherever
a row is a child record with no independent identity (a single claim, a single
login), the PK is a small auto-increment integer.

---

## Identity tables (user accounts and authorization)

### `Users`

The list of registered Kinexus accounts. One row per person.

| Column | Type | Purpose |
|---|---|---|
| `Id` | TEXT (GUID) | Primary key. |
| `UserName` | TEXT | Login name. In our setup this equals the email. |
| `NormalizedUserName` | TEXT | Upper-cased copy of `UserName` so case-insensitive lookups can use an index. |
| `Email` | TEXT | User's email. |
| `NormalizedEmail` | TEXT | Upper-cased copy of `Email`, same reason. |
| `EmailConfirmed` | INTEGER (0/1) | Set to 1 once the user confirms their email. |
| `PasswordHash` | TEXT | The user's password, salted + hashed (PBKDF2). Never the plaintext. |
| `SecurityStamp` | TEXT | Rotated whenever security-sensitive data changes (password, email). Existing sessions are invalidated when this changes. |
| `ConcurrencyStamp` | TEXT | Optimistic-concurrency token; bumped on every update. |
| `PhoneNumber` | TEXT | Optional. |
| `PhoneNumberConfirmed` | INTEGER (0/1) | Optional. |
| `TwoFactorEnabled` | INTEGER (0/1) | Whether 2FA is on. Currently unused, off by default. |
| `LockoutEnd` | TEXT (timestamp) | If set, the account is locked until this time. |
| `LockoutEnabled` | INTEGER (0/1) | Whether lockout policies apply to this account. |
| `AccessFailedCount` | INTEGER | Counter that drives lockout. |
| **`FirstName`** | TEXT | (Kinexus-added) The user's first name. |
| **`LastName`** | TEXT | (Kinexus-added) The user's last name. |
| **`JoinedOn`** | TEXT (date) | (Kinexus-added) The date the account was created. |

Indexes:
- `EmailIndex` on `NormalizedEmail` — fast email lookups.
- `UserNameIndex` on `NormalizedUserName` (UNIQUE) — enforces no two users with the same login name.

### `Roles`

The list of role names a user can be assigned to (e.g. "Admin", "User").
Currently only seeded with whatever Identity defaults exist.

| Column | Type | Purpose |
|---|---|---|
| `Id` | TEXT (GUID) | Primary key. |
| `Name` | TEXT | Display name of the role. |
| `NormalizedName` | TEXT | Upper-cased copy for indexed lookups. |
| `ConcurrencyStamp` | TEXT | Optimistic-concurrency token. |

### `UserRoles`

Pure join table: which users belong to which roles. No data of its own.

| Column | Type | Purpose |
|---|---|---|
| `UserId` | TEXT (FK → Users.Id) | Composite PK part 1. |
| `RoleId` | TEXT (FK → Roles.Id) | Composite PK part 2. |

Cascade-delete on both sides — drop a user, their role memberships disappear.

### `UserClaims`

Arbitrary key/value pairs attached to a single user. Used for things that
don't deserve their own column on `Users` (departmental info, feature flags,
permissions beyond roles).

| Column | Type | Purpose |
|---|---|---|
| `Id` | INTEGER | Auto-increment PK. |
| `UserId` | TEXT (FK → Users.Id) | Owner. |
| `ClaimType` | TEXT | The "name" half of the claim, e.g. `"department"`. |
| `ClaimValue` | TEXT | The "value" half, e.g. `"R&D"`. |

### `RoleClaims`

Same shape as `UserClaims` but attached to a role rather than an individual.
Lets you grant a claim to everyone in a role at once.

| Column | Type | Purpose |
|---|---|---|
| `Id` | INTEGER | Auto-increment PK. |
| `RoleId` | TEXT (FK → Roles.Id) | Owner role. |
| `ClaimType` | TEXT | Claim name. |
| `ClaimValue` | TEXT | Claim value. |

### `UserLogins`

External-provider logins (e.g. "this user is also signed in via Google").
Empty in our current prototype since we don't have any external providers
wired up.

| Column | Type | Purpose |
|---|---|---|
| `LoginProvider` | TEXT | Provider name. |
| `ProviderKey` | TEXT | The user's id at that provider. |
| `ProviderDisplayName` | TEXT | Human-readable. |
| `UserId` | TEXT (FK → Users.Id) | Which Kinexus user this maps to. |

Composite PK: `(LoginProvider, ProviderKey)`.

### `UserTokens`

Long-lived per-user tokens (password-reset tokens, email-confirmation tokens,
2FA recovery codes).

| Column | Type | Purpose |
|---|---|---|
| `UserId` | TEXT (FK → Users.Id) | Owner. |
| `LoginProvider` | TEXT | Issuing provider name. |
| `Name` | TEXT | Token name, e.g. `"PasswordReset"`. |
| `Value` | TEXT | The actual token. |

Composite PK: `(UserId, LoginProvider, Name)`.

---

## OAuth / OpenID Connect tables (the SSO engine)

These store everything the SSO server hands out at runtime: registered client
apps, issued grants, issued tokens, defined scopes.

### `OAuthClients`

The list of applications allowed to use Kinexus for sign-in. The seeder
populates this on startup from `appsettings.json`'s `OpenIddict:Clients`
section — one row per registered relying party.

| Column | Type | Purpose |
|---|---|---|
| `Id` | TEXT (GUID) | Primary key. |
| `ClientId` | TEXT (UNIQUE) | The public identifier the client sends in OIDC requests, e.g. `phosphonet-client`. |
| `ClientSecret` | TEXT | Hashed secret. Confidential clients only. |
| `ClientType` | TEXT | `Public` (no secret) or `Confidential` (server-side, has secret). |
| `ApplicationType` | TEXT | Future use — web vs native. |
| `ConsentType` | TEXT | Whether consent screens apply (we currently bypass). |
| `DisplayName` | TEXT | Friendly name shown on consent screens. |
| `DisplayNames` | TEXT (JSON) | Localised display names. |
| `RedirectUris` | TEXT (JSON array) | Allowed URLs the IDP can send the user back to. Anything else is rejected. |
| `PostLogoutRedirectUris` | TEXT (JSON array) | Allowed URLs after logout. |
| `Permissions` | TEXT (JSON array) | What the client is allowed to ask for — endpoints, grant types, scopes. |
| `Requirements` | TEXT (JSON array) | What the client is forced to do (e.g. PKCE). |
| `Properties` | TEXT (JSON) | Arbitrary metadata. |
| `JsonWebKeySet` | TEXT (JSON) | For clients that use JWT auth. |
| `Settings` | TEXT (JSON) | Per-client overrides. |
| `ConcurrencyToken` | TEXT | Optimistic-concurrency token. |

Indexes:
- `UNIQUE(ClientId)` — two clients cannot share an identifier.

> The JSON-encoded list/dictionary columns are an OpenIddict design choice:
> they trade relational tidiness for fewer joins on the SSO hot path. Reading
> them as JSON in your favourite SQLite browser shows the actual values.

### `OAuthScopes`

The catalog of scopes a client may request (`openid`, `profile`, `email`,
`roles`, `offline_access`). Currently driven entirely by code; this table
is empty in our prototype because OpenIddict supports inlined scope
declarations.

### `OAuthAuthorizations`

One row per *consent grant* issued. When a user signs in to PhosphoNet for
the first time, an Authorization row is created and reused on subsequent
logins.

| Column | Type | Purpose |
|---|---|---|
| `Id` | TEXT (GUID) | Primary key. |
| `ApplicationId` | TEXT (FK → OAuthClients.Id) | Which client this grant belongs to. |
| `Subject` | TEXT | The user this grant is for (matches `Users.Id`). |
| `Type` | TEXT | `Permanent` or `Ad hoc`. |
| `Status` | TEXT | `Valid`, `Revoked`. |
| `Scopes` | TEXT (JSON array) | What the user agreed to grant. |
| `CreationDate` | TEXT (timestamp) | When issued. |
| `Properties` | TEXT (JSON) | Arbitrary metadata. |
| `ConcurrencyToken` | TEXT | Optimistic-concurrency token. |

### `OAuthTokens`

One row per *issued token*: authorization codes, access tokens, refresh
tokens, identity tokens. Short-lived rows — they get pruned as they expire.

| Column | Type | Purpose |
|---|---|---|
| `Id` | TEXT (GUID) | Primary key. |
| `ApplicationId` | TEXT (FK → OAuthClients.Id) | Issuing client. |
| `AuthorizationId` | TEXT (FK → OAuthAuthorizations.Id) | Parent grant. |
| `Subject` | TEXT | Owner user (`Users.Id`). |
| `Type` | TEXT | `authorization_code`, `access_token`, `refresh_token`, etc. |
| `ReferenceId` | TEXT (UNIQUE) | The public token string the client sees. |
| `Payload` | TEXT | Encrypted/signed token contents. |
| `Status` | TEXT | `Valid`, `Redeemed`, `Revoked`. |
| `CreationDate` | TEXT (timestamp) | Issue time. |
| `ExpirationDate` | TEXT (timestamp) | When the token stops being honoured. |
| `RedemptionDate` | TEXT (timestamp) | When (if ever) a code was exchanged. |
| `Properties` | TEXT (JSON) | Arbitrary metadata. |
| `ConcurrencyToken` | TEXT | Optimistic-concurrency token. |

Indexes:
- `UNIQUE(ReferenceId)` — token strings are unique.
- `(ApplicationId, Status, Subject, Type)` — fast lookup for "is this user's
  access token still valid?"

---

## Relationships at a glance

```
Users ──< UserClaims
Users ──< UserLogins
Users ──< UserTokens
Users ──< UserRoles >── Roles
Roles ──< RoleClaims

OAuthClients ──< OAuthAuthorizations ──< OAuthTokens
OAuthClients ──────────────────────────< OAuthTokens   (some tokens skip the auth row)

OAuthAuthorizations.Subject  → string ref to Users.Id  (not a real FK — OIDC subjects can be users in other identity stores)
OAuthTokens.Subject          → string ref to Users.Id  (same)
```

All foreign-key relationships within Identity and within OpenIddict are
`ON DELETE CASCADE`. There is no FK *between* the two clusters — that
boundary is by design so the SSO machinery can outlive any specific user
identity store swap.

---

## How tables get populated

- **`Users` / `Roles` / `User*`** — written by `UserManager` and `SignInManager`
  when someone registers or signs in.
- **`OAuthClients`** — written by `OpenIddictClientSeeder` on startup, from
  the `OpenIddict:Clients` block in `appsettings.json`.
- **`OAuthAuthorizations` / `OAuthTokens`** — written by the OIDC handler
  during a sign-in flow. The `AuthorizationController.Authorize()` and
  `Exchange()` actions are the entry points.
- **`OAuthScopes`** — empty in this prototype; scopes are declared in code.

---

## Migration history

| Timestamp | Migration | Purpose |
|---|---|---|
| `00000000000000` | CreateIdentitySchema | Initial Identity tables. |
| `20260515023040` | AddOpenIddict | Add the four OpenIddict tables. |
| `20260520000514` | AddAdminMockUserFields | Add `FirstName`, `LastName`. |
| `20260520001513` | ChangeApplicationDbContext | Re-type context. |
| `20260520001834` | ChangeDefaultUserType | Switch base user. |
| `20260520002217` | AddRazorPages | (No-op; placeholder.) |
| `20260523000000` | **RenameTablesForReadability** | Drop the `AspNet*` and `OpenIddict*` prefixes — this is the current state. |
