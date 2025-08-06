using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure API base URL from configuration (with fallback)
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5050";

// Add logging with console and debug outputs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug); // Keep Debug level for detailed logging
});

// Configure distributed memory cache for session
builder.Services.AddDistributedMemoryCache(); // Required for session state

// Configure session with secure settings
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Prevent client-side access to session cookie
    options.Cookie.IsEssential = true; // Required for GDPR compliance
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always; // Allow HTTP in development
    options.Cookie.SameSite = SameSiteMode.Strict; // Prevent CSRF
});

// Add HttpContextAccessor for accessing session and other context data
builder.Services.AddHttpContextAccessor();

// Configure HttpClient for API calls
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    UseCookies = true // Ensure cookies (including session cookies) are sent with API requests
});

// Add Razor Pages
builder.Services.AddRazorPages();

// Configure Antiforgery for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always; // Allow HTTP in development
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("KhachHangOnly", policy => policy.RequireRole("KhachHang"));
});

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/DangNhap";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Match session timeout
        options.SlidingExpiration = true; // Enable sliding expiration for better UX
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always; // Allow HTTP in development
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnSigningOut = async (context) =>
        {
            context.HttpContext.Session.Clear();
            await context.HttpContext.Session.CommitAsync();
        };
    });

// Configure CORS to allow API communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins(apiBaseUrl)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Remove HTTPS redirection to allow HTTP
// app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
app.UseStaticFiles();
app.UseRouting();

// Ensure session is initialized before authentication
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
