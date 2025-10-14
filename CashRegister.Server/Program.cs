using CashRegister.Server.Services;
using CashRegister.Server.Middleware;
using Microsoft.Extensions.Options;
using Serilog.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cashregister-.txt", rollingInterval: RollingInterval.Day));

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure CashRegister service with configuration
builder.Services.Configure<CashRegisterConfiguration>(
    builder.Configuration.GetSection("CashRegister"));
builder.Services.AddSingleton<CashRegisterService>(sp =>
{
    // TODO: Retrieve based on server locale?
    var config = sp.GetRequiredService<IOptions<CashRegisterConfiguration>>();
    var logger = sp.GetRequiredService<ILogger<CashRegisterService>>();
    return new CashRegisterService(config.Value, logger: logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add custom exception middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
