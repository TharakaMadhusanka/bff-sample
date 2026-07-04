using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);


// For Cross Domain scenarios, you can configure CORS to allow requests from your frontend application.
// This is optional and depends on your specific use case.
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(builder =>
//    {
//        builder.WithOrigins("http://localhost:4200") // Replace with your frontend application's URL
//               .AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials(); // Allow credentials (cookies) to be sent in cross-origin requests
//    });
//});

// Register the YARP Reverse Proxy core engine from appsettings
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


builder.Services.AddAuthentication(options =>
{
    // To say that we're using cookies to maintain the user's session and OpenID Connect for authentication.
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
// Enable Cookie Authentication to maintain the user's session after they have logged in.
.AddCookie(options =>
{
    options.LoginPath = "/login"; // Redirect to /login when the user is not authenticated
    options.LogoutPath = "/logout"; // Redirect to /logout when the user logs out

    // Required BFF Configurations
    // Here are the configurations that you can play around for non cross-domain or cross-domain scenarios, as I explained in the above section.
    // Refer Section 5.2
    options.Cookie.SameSite = SameSiteMode.Strict; // Prevents the cookie from being sent in cross-site requests
    options.Cookie.HttpOnly = true; // Prevents JavaScript from accessing the cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensures the cookie is only sent over HTTPS
})
// Enable OpenID Connect Authentication to authenticate users with an external identity provider.
.AddOpenIdConnect(options =>
{
    options.Authority = "http://127.0.0.1:8080/realms/finpilot/"; // The URL of the identity provider
    options.ClientId = "finpilot-bff"; // The client ID registered with the identity provider
    options.ClientSecret = "HgoRlsRvORUGlYDLYDis89m64mzrVpW9"; // The client secret registered with the identity provider
    options.ResponseType = "code"; // Use the authorization code flow
    options.SaveTokens = true; // Save the tokens in the authentication properties for later use
    options.GetClaimsFromUserInfoEndpoint = true; // Get additional claims from the user info endpoint
    // This is to allow the application to work in development environments where HTTPS might not be configured.
    // In production, you should always use HTTPS.
    options.RequireHttpsMetadata = false;
    // Configure the scopes to request from the identity provider
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("roles"); // Request roles if your identity provider supports it
    // Map claims from the identity provider to standard claim types
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    options.ClaimActions.MapJsonKey(ClaimTypes.Role, "role");
});

builder.Services.AddAuthorization();

// Add services to the container.

var app = builder.Build();

//app.UseCors();


app.UseAuthentication();
app.UseAuthorization();

// Enable Reverse Proxy to forward requests to the backend API.
// This is useful in a BFF architecture where the frontend communicates with the backend through the BFF.
app.MapReverseProxy();

/**
 * Apis required for Backend for Frontend (BFF) pattern. These endpoints are used to handle authentication and authorization flows.
 * this will be used to trigger the OIDC challenge and redirect the user to the identity provider for authentication.
 * **/
app.MapGet("/login", (HttpContext context) =>
{
    var properties = new AuthenticationProperties
    {
        // Can configure the redirect URI after successful login
        // But for now I will redirect to the Angular app root path after successful login
        RedirectUri = "/"
    };

    // Triggers the OIDC challenge to redirect to the identity provider securely
    return Results.Challenge(
        properties: properties,
        authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]
    );
});

app.MapGet("/logout", async (HttpContext context) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/"
    };
    return Results.SignOut(properties: properties,
        authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
});

// This endpoint is used to get the authenticated user's information. It checks if the user is authenticated and returns their name and claims.
// If the user is not authenticated, it returns a 401 Unauthorized response.
app.MapGet("/user", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated ?? false)
    {
        var claims = context.User.Claims.Select(c => new { c.Type, c.Value });
        return Results.Ok(new User($"{context.User.FindFirstValue(ClaimTypes.GivenName)} {context.User.FindFirstValue(ClaimTypes.Surname)}" ?? string.Empty,
            context.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            [.. context.User.FindAll(ClaimTypes.Role).Select(c => c.Value)]));
    }
    else
    {
        return Results.Unauthorized();
    }
});

app.UseHttpsRedirection();

app.Run();

sealed record User(string Name, string Email, string[] Roles);
