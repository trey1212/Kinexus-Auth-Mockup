using KinexusMockup.Auth;
using KinexusMockup.Data;
using KinexusMockup.Models;
using KinexusMockup.Services;
using KinexusMockup.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Per-developer overrides for secrets and machine-specific values.
// appsettings.Local.json is gitignored.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Pick the EF provider from the connection string shape. Local dev hits a
// SQLite file ("DataSource=app.db"); Azure SQL connection strings include
// "Server=" and a database name. Detect once at startup so deployment doesn't
// need a code change — just a different connection string.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var isSqlite = connectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) && connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase);

    if (isSqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }

    options.UseOpenIddict();
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRazorPages();

builder.Services.AddIdentity<AdminMockUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Operational peer URLs (used by the UI for the PhosphoNet nav link and the
// single-logout chain). Distinct from the OIDC client list, which is consumed
// by the seeder and lives under "OpenIddict" in config. Defaults live in
// appsettings.Development.json so bare `dotnet run` works locally; env vars
// (Sso__PublicUrl, Sso__PhosphonetPublicUrl) override for deployments.
builder.Services
    .AddOptions<SsoOptions>()
    .Bind(builder.Configuration.GetSection(SsoOptions.SectionName));

builder.Services.AddKinexusSsoServer(builder.Configuration, builder.Environment);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<HomeContentService>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailService, SmtpEmailService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Force HTTPS everywhere except local development. Dev needs to be reachable
// over plain HTTP for the HTTP-client demo (Phosphonet) so we leave that
// alone; production traffic must always be encrypted.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.MapFallbackToController("NotFoundPage", "Home");

app.Run();