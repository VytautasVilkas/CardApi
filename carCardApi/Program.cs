using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using carCard;
using carCard.Services;





var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SecretManager>();
var secretManager = new SecretManager();
var jwtSecretKey = secretManager.GetJwtSecretCode();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers()
.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
builder.Services.AddSingleton<ConnectionProvider>();
builder.Services.AddSingleton<DataTableService>();
builder.Services.AddSingleton<SecretManager>();
builder.Services.AddSingleton<IAdminService, AdminService>();

builder.Services.AddTransient<IFuelTypeService, FuelTypeService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "CarCard",
            ValidAudience = "CarCard",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["ACCESS_TOKEN"];
                Console.WriteLine("Extracted Token: " + context.Token);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

// Forwarded headers configuration
// builder.Services.Configure<ForwardedHeadersOptions>(options =>
// {
//     options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
//                                 Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
//     options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
//     options.KnownProxies.Add(IPAddress.Parse("::1"));
// });

// CORS policies
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
    
});

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7279;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7279, listenOptions =>
    {
        listenOptions.UseHttps(); 
    });
    options.ListenAnyIP(5130); 
});






var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
    context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
    await next();
});
app.UseForwardedHeaders();
app.UseCors("DefaultPolicy");
app.UseHttpsRedirection(); 
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

