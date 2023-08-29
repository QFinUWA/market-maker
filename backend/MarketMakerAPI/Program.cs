using MarketMaker.Hubs;
using MarketMaker.Services;
using MarketMaker.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<IMarketService, LocalMarketService>();
builder.Services.AddSingleton<IUserService, LocalUserService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddAuthentication();
//builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddSignalR(options =>
{

      options.EnableDetailedErrors = true;
    
});
builder.Services.AddSwaggerGen();
 
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(builder =>
{
    builder.WithOrigins("http://127.0.0.1:5500/") //Source
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
});


app.MapHub<MarketHub>("/market");

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.Run();
