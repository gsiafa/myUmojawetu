using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text.Json.Serialization;
using WebOptimus;
using WebOptimus.Configuration;
using WebOptimus.Data;
using WebOptimus.Data.Initialiser;
using WebOptimus.Helpers;
using WebOptimus.Models;
using WebOptimus.Models.Stripe;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;


var builder = WebApplication.CreateBuilder(args);

AppVersionInfo.InitialiseBuildInfoGivenPath(Directory.GetCurrentDirectory());

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.RequireHeaderSymmetry = false;
    options.ForwardLimit = null;
});

builder.Services.Configure<PasswordHasherOptions>(opts =>
{
    opts.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    opts.IterationCount = 210_000;
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new System.Globalization.CultureInfo("en-GB") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-GB");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var cookieSecurePolicy = CookieSecurePolicy.SameAsRequest;

builder.Services.AddAntiforgery(
    options =>
    {
        options.SuppressXFrameOptionsHeader = true;
        options.Cookie.Name = "f";
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.Cookie.HttpOnly = true;
        options.FormFieldName = "f";
        options.HeaderName = "X-XSRF-TOKEN";
    });

var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString,
        sqlServerOptionsAction: opts =>
        {
            opts.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(60),
                errorNumbersToAdd: null);

            opts.CommandTimeout(3600);
        });

    options.UseExceptionProcessor();
});

//  Load configuration from appsettings.json
var configuration = builder.Configuration;
//  Retrieve Postmark API Key
var postmarkApiKey = configuration["Postmark:ApiKey"];
if (string.IsNullOrWhiteSpace(postmarkApiKey))
{
    throw new Exception("Postmark API Key is missing in configuration. Please check appsettings.json.");
}

//  Register Configuration to DI (Dependency Injection)
builder.Services.AddSingleton<IConfiguration>(configuration);

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddSingleton<RequestIpHelper, RequestIpHelper>();
builder.Services.AddSingleton<DataProtectionPurposeStrings>();

builder.AddHealthChecks();
// Register Background Service
builder.Services.AddScoped<IAuditService, AuditService>(); // Keep scoped
builder.Services.AddSingleton<IHostedService, DonationClosureService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHostedService<CauseManagementService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

builder.Services.AddAutoMapper(typeof(MapperConfig));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IDBInitialiser, DBInitialiser>();


builder.Services.AddTransient<IPasswordValidator<User>, CustomPasswordPolicy>();

var mvcBuilder = builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization()
    .AddSessionStateTempDataProvider()
    .AddJsonOptions(opts =>
    {
        if (builder.Environment.IsDevelopment())
        {
            opts.JsonSerializerOptions.WriteIndented = true;
        }

        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

if (!WebAppContext.IsRunningInAzureWebApp && builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.Configure<PostmarkOptions>(builder.Configuration.GetSection(PostmarkOptions.Postmark));

builder.Services.AddPostmark();

builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = long.MaxValue);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(1800);
    options.Cookie.Name = "weboptimus.Session";
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.Configure<IdentityOptions>(opt =>
{
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(3652);
    opt.Lockout.MaxFailedAccessAttempts = 5;

    opt.Password.RequireLowercase = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireDigit = false;
});

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(2);
});

builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = new PathString("/Account/Login");
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.LoginPath = "/Home/Index";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseAppHealthChecks();
app.UseRequestLocalization();

app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404 && !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/Home/Error";
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var postmarkKey = config["Postmark:ApiKey"];
        if (string.IsNullOrWhiteSpace(postmarkKey))
        {
            throw new Exception("Postmark API Key is not set. Please configure it in appsettings.json.");
        }

        await next();
    }
});

var localizationOptions = ((IApplicationBuilder)app).ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();

app.UseRouting();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("StripeSettings")["SecretKey"];
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInit = scope.ServiceProvider.GetRequiredService<IDBInitialiser>();
        dbInit.Initialise();
    }
}
