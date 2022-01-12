using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using IdentityServer4Demo;
using IdentityServerHost.Quickstart.UI;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
builder.Services.AddControllersWithViews();
// cookie policy to deal with temporary browser incompatibilities
services.AddSameSiteCookiePolicy();

services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
    .AddInMemoryApiScopes(Config.GetApiScopes())
    .AddInMemoryApiResources(Config.GetApis())
    .AddInMemoryIdentityResources(Config.GetIdentityResources())
    .AddInMemoryClients(Config.GetClients())
    .AddTestUsers(TestUsers.Users)
    .AddDeveloperSigningCredential(persistKey: false);

services.AddAuthentication()
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

        options.ClientId = Configuration["Secret:GoogleClientId"];
        options.ClientSecret = Configuration["Secret:GoogleClientSecret"];
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
    .AddLocalApi(options =>
    {
        options.ExpectedScope = "api";
    });

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
        }

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
