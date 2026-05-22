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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Comment out respective SQL connection type
    // Local: Sqlite | Azure: SqlServer
    // options.UseSqlite(connectionString);
    options.UseSqlServer(connectionString);
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

builder.Services.AddKinexusSsoServer(builder.Configuration);

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
    //Uncomment this command to force HTTPS
    //app.UseHsts();
}

// Intentionally off: SSO clients may be deployed over HTTP.
// Re-enable once all clients are HTTPS-only.
// app.UseHttpsRedirection();

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
