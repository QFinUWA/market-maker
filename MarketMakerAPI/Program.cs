using System.Text;
using MarketMaker.Hubs;
using MarketMaker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.
builder.Services.AddSingleton<ExchangeGroup>();
builder.Services.AddSingleton<LocalUserDatabase>();
builder.Services.AddSingleton<IUserService, LocalUserService>();
builder.Services.AddSingleton<Dictionary<string, CancellationTokenSource>>();
builder.Services.AddControllers();
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)),
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });
builder.Services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });

var app = builder.Build();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();

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