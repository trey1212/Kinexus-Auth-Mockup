# Azure Deployment Guide

End-to-end checklist for taking this project from a local clone to a working
production deployment on Azure. Written for a developer with portal access
and the Azure CLI installed — no prior Azure-specific knowledge assumed.

The deployment uses three Azure resources:

1. **Azure App Service** for **Kinexus** (HTTPS, hosts the SSO authority)
2. **Azure App Service** for **Phosphonet** (HTTPS, hosts the client app)
3. **Azure SQL Database** (single instance, shared by both — Kinexus owns the schema)

Optional but recommended:

4. **Azure Key Vault** for the signing certificate, encryption certificate, client secrets, and SMTP password.

---

## Phase 1 — Provision the resources

The portal works fine; CLI is faster. Either route gives you the same result.

### 1.1 Resource group

```bash
az group create --name kinexus-prod --location canadacentral
```

### 1.2 Azure SQL Database

```bash
az sql server create \
  --name kinexus-sql-prod \
  --resource-group kinexus-prod \
  --location canadacentral \
  --enable-ad-only-auth false \
  --admin-user kxadmin \
  --admin-password '<STRONG-PASSWORD>'

az sql db create \
  --resource-group kinexus-prod \
  --server kinexus-sql-prod \
  --name kinexus \
  --service-objective S0
```

Open the firewall so App Services (and you, for migrations) can reach it:

```bash
az sql server firewall-rule create \
  --resource-group kinexus-prod \
  --server kinexus-sql-prod \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

(That rule allows traffic from inside Azure. Add your own IP separately if you want to run migrations from your laptop.)

### 1.3 App Service plan and two web apps

```bash
az appservice plan create \
  --name kinexus-plan \
  --resource-group kinexus-prod \
  --sku B1 \
  --is-linux

az webapp create \
  --resource-group kinexus-prod \
  --plan kinexus-plan \
  --name kinexus-auth \
  --runtime "DOTNETCORE:10.0"

az webapp create \
  --resource-group kinexus-prod \
  --plan kinexus-plan \
  --name kinexus-phosphonet \
  --runtime "DOTNETCORE:10.0"
```

You now have:
- `https://kinexus-auth.azurewebsites.net` — will host Kinexus
- `https://kinexus-phosphonet.azurewebsites.net` — will host Phosphonet

(Or pick your own globally-unique names.)

---

## Phase 2 — Generate signing + encryption certificates

OpenIddict needs two real X.509 certs in production. Generate them once,
upload, reference from config.

```powershell
# From an elevated PowerShell prompt:

$signingCert = New-SelfSignedCertificate `
  -Subject "CN=Kinexus SSO Signing" `
  -KeyAlgorithm RSA -KeyLength 2048 `
  -KeyUsage DigitalSignature `
  -CertStoreLocation "cert:\CurrentUser\My" `
  -NotAfter (Get-Date).AddYears(2)

$encryptionCert = New-SelfSignedCertificate `
  -Subject "CN=Kinexus SSO Encryption" `
  -KeyAlgorithm RSA -KeyLength 2048 `
  -KeyUsage KeyEncipherment, DataEncipherment `
  -CertStoreLocation "cert:\CurrentUser\My" `
  -NotAfter (Get-Date).AddYears(2)

$pwd = ConvertTo-SecureString -String '<CERT-PASSWORD>' -Force -AsPlainText

Export-PfxCertificate -Cert $signingCert    -FilePath .\signing.pfx    -Password $pwd
Export-PfxCertificate -Cert $encryptionCert -FilePath .\encryption.pfx -Password $pwd
```

You now have `signing.pfx` and `encryption.pfx`. Upload both to the **Kinexus**
App Service:

> App Service → **TLS/SSL settings** → **Private Key Certificates** → **Upload Certificate**.

Note the **thumbprint** of each upload, then under **Configuration** add:

| Name | Value |
|---|---|
| `WEBSITE_LOAD_CERTIFICATES` | `<signing-thumbprint>,<encryption-thumbprint>` |

This makes App Service mount them at `D:\home\site\wwwroot\<thumbprint>.pfx`
on Windows hosting or `/var/ssl/private/<thumbprint>.pfx` on Linux.

---

## Phase 3 — Configure Kinexus (the SSO authority)

Open the **kinexus-auth** App Service → **Configuration** → add these settings:

### Application settings

| Name | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Sso__PublicUrl` | `https://kinexus-auth.azurewebsites.net` |
| `Sso__PhosphonetPublicUrl` | `https://kinexus-phosphonet.azurewebsites.net` |
| `Sso__PhosphonetLogoutPath` | `/Account/Logout` |
| `Auth__Certificates__SigningPath` | path where you uploaded the cert — for Linux App Service: `/var/ssl/private/<signing-thumbprint>.pfx` |
| `Auth__Certificates__EncryptionPath` | same idea — `/var/ssl/private/<encryption-thumbprint>.pfx` |
| `Auth__Certificates__Password` | the password you used in step 2 |
| `OpenIddict__Clients__0__ClientId` | `phosphonet-client` |
| `OpenIddict__Clients__0__ClientSecret` | a strong randomly-generated secret (same one you'll set on Phosphonet) |
| `OpenIddict__Clients__0__DisplayName` | `PhosphoNET` |
| `OpenIddict__Clients__0__RedirectUris__0` | `https://kinexus-phosphonet.azurewebsites.net/signin-oidc` |
| `OpenIddict__Clients__0__PostLogoutRedirectUris__0` | `https://kinexus-phosphonet.azurewebsites.net/signout-callback-oidc` |

### Connection string

Under **Configuration** → **Connection strings** add:

| Name | Type | Value |
|---|---|---|
| `DefaultConnection` | SQLAzure | `Server=tcp:kinexus-sql-prod.database.windows.net,1433;Initial Catalog=kinexus;Persist Security Info=False;User ID=kxadmin;Password=<STRONG-PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |

> **Better long-term**: enable Managed Identity on the App Service, grant it `db_owner` on the SQL DB, and switch the connection string to `Authentication=Active Directory Default;` — then no password lives anywhere.

---

## Phase 4 — Configure Phosphonet (the client)

Open the **kinexus-phosphonet** App Service → **Configuration**:

| Name | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Sso__Authority` | `https://kinexus-auth.azurewebsites.net/` (trailing slash matters) |
| `Sso__ClientId` | `phosphonet-client` |
| `Sso__ClientSecret` | same secret you set on Kinexus in Phase 3 |
| `Sso__DefaultReturnPath` | `/PhosphoNet` |

Phosphonet has no database; that's the entire client-side configuration.

---

## Phase 5 — Deploy the code

From your local clone:

### Kinexus

```bash
cd C:\Users\rodri\source\repos\KinexusAuth
dotnet publish -c Release -o ./publish

cd publish
zip -r ../kinexus.zip .
cd ..

az webapp deploy \
  --resource-group kinexus-prod \
  --name kinexus-auth \
  --src-path ./kinexus.zip \
  --type zip
```

### Phosphonet

```bash
cd C:\Users\rodri\source\repos\Phosphonet
dotnet publish -c Release -o ./publish

cd publish
zip -r ../phosphonet.zip .
cd ..

az webapp deploy \
  --resource-group kinexus-prod \
  --name kinexus-phosphonet \
  --src-path ./phosphonet.zip \
  --type zip
```

GitHub Actions and Azure DevOps Pipelines work too — the `azure/webapps-deploy@v3`
action takes the same publish folder.

---

## Phase 6 — First-run schema bootstrap

The `OpenIddictClientSeeder` runs on Kinexus startup and calls
`Database.MigrateAsync()`. The first time the deployed Kinexus boots, it will
apply every migration in `Data/Migrations/` against the empty Azure SQL DB,
producing the same table layout you have locally (`Users`, `OAuthClients`,
…). No manual SQL is needed.

Hit `https://kinexus-auth.azurewebsites.net/` in a browser. If you see the
welcome page, the deploy worked. If not, check **Log stream** in the portal —
the most common first-deploy failure is a wrong connection string.

---

## Phase 7 — Verify the SSO loop

1. Open `https://kinexus-phosphonet.azurewebsites.net/` → expect the SigNET welcome placeholder (unauthed).
2. Click **REGISTER HERE** → you land on the Kinexus login page (HTTPS) → click *Register as a new user* → fill the form.
3. After registration, you bounce back to PhosphoNET, signed in, on the real PhosphoNET content page.
4. Click any other Knowledgebank link in the nav → jumps to Kinexus, already signed in, no second login.
5. Click **Log out** → both cookies clear, you land back on the unauthed PhosphoNET welcome.

---

## Operational notes

### Certificate rotation

The signing/encryption certs are good for two years (the PowerShell script
above sets `NotAfter (Get-Date).AddYears(2)`). To rotate:

1. Generate new certs, upload to App Service.
2. Update `Auth__Certificates__SigningPath` / `EncryptionPath` to point at
   the new thumbprints.
3. Restart Kinexus. Existing tokens issued with the old cert remain valid
   until they expire (default ~1 hour for access tokens).

### Scaling

The B1 App Service plan can scale out to multiple instances. Both Kinexus
and Phosphonet are stateless at the app level — sessions live in cookies
(client side) and persistent data lives in Azure SQL. Scaling out is a
slider in the portal; no code change.

### Logs and monitoring

Enable **Application Insights** on both App Services. Useful queries:

```kusto
// Failed sign-ins
traces
| where message contains "Failed to sign in"
| project timestamp, message, customDimensions

// Token issuances per hour
requests
| where url endswith "/connect/token"
| summarize count() by bin(timestamp, 1h)
```

### Cost

Approximate monthly cost in CAD (2026 pricing, Canada Central):

| Resource | Tier | Cost |
|---|---|---|
| App Service Plan B1 (2 apps share) | Basic | ~$20 |
| Azure SQL DB | S0 | ~$20 |
| Key Vault (if used) | Standard | ~$0.50 |
| **Total** | | **~$40/month** |

Drop to F1 (free) App Service tier for non-production. Drop the SQL DB to
Basic ($6/month) or the serverless tier for true bursty workloads.

---

## Local-vs-production summary

| Concern | Development | Production |
|---|---|---|
| Database | SQLite (`app.db` file) | Azure SQL Database |
| Signing/encryption certs | Ephemeral dev certs | Real X.509 from disk |
| HTTPS | Self-signed dev cert, HTTP also allowed | Always-on HTTPS, HSTS enabled |
| Cookie SecurePolicy | `SameAsRequest` (HTTP demo) | `Always` |
| Config source | `appsettings.Development.json` | `appsettings.Production.json` + App Service Configuration + Key Vault |
| Logs | Console | Application Insights |
| URLs | `localhost:7081` / `localhost:5200` | `kinexus-auth.azurewebsites.net` / `kinexus-phosphonet.azurewebsites.net` |

Same codebase. Only `ASPNETCORE_ENVIRONMENT` and the App Service Configuration
values change.
