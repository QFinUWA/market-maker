using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MarketMaker.Contracts;
using MarketMaker.Hubs;
using MarketMaker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = config["KeyVault:keyVaultURL"]!;
    var keyVaultClientId    = config["KeyVault:ClientId"]!;
    var keyVaultClientSecret= config["KeyVault:ClientSecret"]!;

    config.AddAzureKeyVault(keyVaultUrl, keyVaultClientId, keyVaultClientSecret, new DefaultKeyVaultSecretManager());
}

// Add services to the container.
builder.Services.AddSingleton<ExchangeGroup>();
builder.Services.AddSingleton<Dictionary<string, CancellationTokenSource>>();
builder.Services.AddControllers();
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseSqlServer(config["UserDb"]);
});
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
    })
    .AddPolicy("authenticatedUser", pb =>
    {
        pb.RequireAuthenticatedUser()
            .AddAuthenticationSchemes()
            .RequireClaim("authenticatedUser", "true");
    });

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,

            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options => {
   options.AddDefaultPolicy(policy =>
   {
       var allowedOrigins = config.GetSection("CORS:AllowedOrigins").Get<string[]>();
       policy.WithOrigins(allowedOrigins!).AllowAnyHeader().AllowAnyMethod();
   }); 
});
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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<MarketHub>("/market");

app.Run();