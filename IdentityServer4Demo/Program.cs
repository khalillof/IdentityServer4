//using IdentityServer4;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using IdentityServer4Demo;
using IdentityServer4Demo.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
//using IdentityServer4;
//using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var connectionString = builder.Configuration.GetConnectionString("sqliteDemo");
var migrationsAssemblyConfigurationDbContext = "IdentityServer4Demo";
// Add services to the container.

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString, o => o.MigrationsAssembly(typeof(Program).Assembly.FullName)));
services.AddDatabaseDeveloperPageExceptionFilter();

//services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
//services.AddControllersWithViews();

services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
/// end of identity
services.AddControllersWithViews();
services.AddRazorPages().AddViewOptions(options =>
{
    options.HtmlHelperOptions.ClientValidationEnabled = false;
});
// cookie policy to deal with temporary browser incompatibilities
services.AddSameSiteCookiePolicy();

services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
    options.EmitStaticAudienceClaim = true;
})
        .AddInMemoryIdentityResources(ServerConfiguration.IdentityResources)
        .AddInMemoryApiResources(ServerConfiguration.ApiResources)
        .AddInMemoryApiScopes(ServerConfiguration.ApiScopes)
        .AddInMemoryClients(ServerConfiguration.Clients)
        .AddTestUsers(ServerConfiguration.TestUsers)
        // this adds the config data from DB (clients, resources, CORS)
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlite(connectionString, o => o.MigrationsAssembly(migrationsAssemblyConfigurationDbContext));
        })
        // this adds the operational data from DB (codes, tokens, consents)
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlite(connectionString, o => o.MigrationsAssembly(migrationsAssemblyConfigurationDbContext));

            // this enables automatic token cleanup. this is optional.
            options.EnableTokenCleanup = true;
        })
        //.AddAspNetIdentity<ApplicationUser>()
       // not recommended for production - you need to store your key material somewhere secure
       .AddDeveloperSigningCredential(persistKey: false);


//===============================================================================
services.AddAuthentication()
    .AddLocalApi(options =>
    {
        options.ExpectedScope = "api";
    });
/*
 .AddGoogle("Google", options =>
 {
     options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

     options.ClientId = builder.Configuration["Secret:GoogleClientId"];
     options.ClientSecret = builder.Configuration["Secret:GoogleClientSecret"];
 })
 .AddOpenIdConnect("aad", "Sign-in with Azure AD", options =>
 {
     options.Authority = "https://login.microsoftonline.com/common";
     options.ClientId = "https://leastprivilegelabs.onmicrosoft.com/38196330-e766-4051-ad10-14596c7e97d3";

     options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
     options.SignOutScheme = IdentityServerConstants.SignoutScheme;

     options.ResponseType = "id_token";
     options.CallbackPath = "/signin-aad";
     options.SignedOutCallbackPath = "/signout-callback-aad";
     options.RemoteSignOutPath = "/signout-aad";

     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = false,
         ValidAudience = "165b99fd-195f-4d93-a111-3e679246e6a9",

         NameClaimType = "name",
         RoleClaimType = "role"
     };
 })
 */

// preserve OIDC state in cache (solves problems with AAD and URL lenghts)
services.AddOidcStateDataFormatterCache("aad");

// add CORS policy for non-IdentityServer endpoints
services.AddCors(options =>
{
    options.AddPolicy("api", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// demo versions (never use in production)
services.AddTransient<IRedirectUriValidator, DemoRedirectValidator>();
services.AddTransient<ICorsPolicyService, DemoCorsPolicy>();

/*
 * services.AddAuthentication();
    .AddGoogle(options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

        // register your IdentityServer with Google at https://console.developers.google.com
        // enable the Google+ API
        // set the redirect URI to https://localhost:5001/signin-google
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    })
    .AddFacebook(facebookOptions =>
     {
         facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
         facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
         facebookOptions.AccessDeniedPath = "/AccessDeniedPathInfo";
     }); ;
*/
/*
#region Apis

services.AddDistributedMemoryCache();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => builder.Configuration.Bind("JwtSettings", options))
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => builder.Configuration.Bind("CookieSettings", options));
// adds an authorization policy to make sure the token is for scope 'api1'
services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api1");
    });
});
#endregion
*/
// cofig custom looger
builder.Host.UseSerilog((ctx, config) =>
{
    config.MinimumLevel.Debug()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
        .Enrich.FromLogContext();

    if (ctx.HostingEnvironment.IsDevelopment())
    {
        config.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}");
    }
    else if (ctx.HostingEnvironment.IsProduction())
    {
        config.WriteTo.File(Path.Combine(Directory.GetCurrentDirectory() + "identityserver.txt"),
            fileSizeLimitBytes: 1_000_000,
            rollOnFileSizeLimit: true,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1));
    }
});
//====================================================================================================================
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();

app.UseSerilogRequestLogging();

app.UseCors("api");

app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// seed ConfigrationDbContext database
SeedData.EnsureSeedData(connectionString, migrationsAssemblyConfigurationDbContext);
app.Run();
