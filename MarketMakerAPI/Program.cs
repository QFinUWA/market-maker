using MarketMaker.Hubs;
using MarketMaker.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// LocalMarketGroup<LocalMarketService>
builder.Services.AddSingleton<MarketGroup>();
builder.Services.AddSingleton<IUserService, LocalUserService>();
builder.Services.AddSingleton<Dictionary<string, CancellationTokenSource>>();

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
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(corsPolicyBuilder =>
{
    corsPolicyBuilder.WithOrigins("http://127.0.0.1:5500/") //Source
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
});


app.MapHub<MarketHub>("/market");

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.Run();
