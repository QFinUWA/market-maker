using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MarketMaker.Hubs;
using MarketMaker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = config["KeyVault:keyVaultURL"]!;
    var keyVaultClientId    = config["KeyVault:ClientId"]!;
    var keyVaultClientSecret= config["KeyVault:ClientSecret"]!;
    var keyVaultDirectoryId = config["KeyVault:DirectoryId"]!;

    // ClientSecretCredential credential = new(keyVaultDirectoryId, keyVaultClientId, keyVaultClientSecret);

    config.AddAzureKeyVault(keyVaultUrl, keyVaultClientId, keyVaultClientSecret, new DefaultKeyVaultSecretManager());
    
    // SecretClient client = new(new Uri(keyVaultUrl), credential);
    // anonymousKey = client.GetSecret("AnonymousAccess").Value.Value;
    // var c = config["AnonymousAccess"];
    // var a = 1 + 1;
}
// Add services to the container.
builder.Services.AddSingleton<ExchangeGroup>();
builder.Services.AddSingleton<LocalUserDatabase>();
builder.Services.AddSingleton<IUserService, LocalUserService>();
builder.Services.AddSingleton<Dictionary<string, CancellationTokenSource>>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IConfiguration>(config);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidAudience = config["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["AnonymousAccess"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };

    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/market"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("admin", pb =>
    {
        pb.RequireAuthenticatedUser()
            .AddAuthenticationSchemes()
            .RequireClaim("admin", "true");
    });

builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });
builder.Services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });

var app = builder.Build();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseCors(corsPolicyBuilder =>
{
    corsPolicyBuilder.WithOrigins("http://127.0.0.1:5500") //Source
        .AllowAnyMethod()
        .AllowCredentials()
        .AllowAnyHeader();
});

app.MapHub<MarketHub>("/market");

app.Run();